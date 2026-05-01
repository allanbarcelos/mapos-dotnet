using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Services;

public interface IMercadoPagoService
{
    Task<(bool ok, string erro)> GerarBoletoAsync(int entityId, string tipo);
    Task<(bool ok, string erro)> AtualizarDadosAsync(int cobrancaId);
    Task<(bool ok, string erro)> CancelarAsync(int cobrancaId);
    Task<(bool ok, string erro)> ConfirmarPagamentoAsync(int cobrancaId);
}

public class MercadoPagoService(
    ApplicationDbContext db,
    IConfiguration configuration) : IMercadoPagoService
{
    private string AccessToken =>
        configuration["MercadoPago:AccessToken"]
        ?? throw new InvalidOperationException("MercadoPago AccessToken não configurado.");

    private RequestOptions ReqOptions() => new() { AccessToken = AccessToken };

    // ── Gerar boleto ──────────────────────────────────────────────────────────
    public async Task<(bool ok, string erro)> GerarBoletoAsync(int entityId, string tipo)
    {
        Cliente? cliente;
        decimal valorTotal;
        decimal desconto;
        string tipoDesconto;
        int?   osId    = null;
        int?   vendaId = null;

        if (tipo == "os")
        {
            var os = await db.Os
                .AsNoTracking()
                .Include(o => o.Cliente)
                .FirstOrDefaultAsync(o => o.Id == entityId);
            if (os is null) return (false, "OS não encontrada.");

            cliente      = os.Cliente;
            valorTotal   = os.ValorTotal;
            desconto     = os.Desconto;
            tipoDesconto = os.TipoDesconto;
            osId         = os.Id;
        }
        else if (tipo == "venda")
        {
            var venda = await db.Vendas
                .AsNoTracking()
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == entityId);
            if (venda is null) return (false, "Venda não encontrada.");

            cliente      = venda.Cliente;
            valorTotal   = venda.ValorTotal;
            desconto     = venda.Desconto;
            tipoDesconto = venda.TipoDesconto;
            vendaId      = venda.Id;
        }
        else
        {
            return (false, "Tipo inválido. Use 'os' ou 'venda'.");
        }

        // Validações de dados do cliente
        if (string.IsNullOrWhiteSpace(cliente?.Rua))      return (false, "Cliente sem endereço (rua).");
        if (string.IsNullOrWhiteSpace(cliente.Numero))    return (false, "Cliente sem número de endereço.");
        if (string.IsNullOrWhiteSpace(cliente.Bairro))    return (false, "Cliente sem bairro.");
        if (string.IsNullOrWhiteSpace(cliente.Cep))       return (false, "Cliente sem CEP.");
        if (string.IsNullOrWhiteSpace(cliente.Cidade))    return (false, "Cliente sem cidade.");
        if (string.IsNullOrWhiteSpace(cliente.Estado))    return (false, "Cliente sem estado.");
        if (string.IsNullOrWhiteSpace(cliente.Documento)) return (false, "Cliente sem CPF/CNPJ.");
        if (string.IsNullOrWhiteSpace(cliente.Email))     return (false, "Cliente sem e-mail.");

        // Calcular valor final com desconto
        decimal valorFinal = tipoDesconto == "porcento"
            ? valorTotal - (valorTotal * desconto / 100m)
            : valorTotal - desconto;

        if (valorFinal <= 0)
            return (false, "Valor da cobrança inválido (≤ 0).");

        // Tipo de documento
        var docLimpo = new string(cliente.Documento.Where(char.IsDigit).ToArray());
        var tipoDoc  = docLimpo.Length == 11 ? "CPF" : "CNPJ";

        // Expiração do boleto
        var expiracaoStr = configuration["MercadoPago:BoletoExpiration"] ?? "P3D";
        var expiracao    = DateTime.UtcNow.Add(ParseIsoDuration(expiracaoStr));

        // Telefone (área + número)
        var (areaCode, phoneNumber) = SplitPhone(cliente.Telefone ?? cliente.Celular ?? "");

        var nomes     = SplitName(cliente.NomeCliente);
        var firstName = nomes.firstName;
        var lastName  = nomes.lastName;

        var request = new PaymentCreateRequest
        {
            TransactionAmount = valorFinal,
            Description       = tipo == "os" ? $"OS #{entityId}" : $"Venda #{entityId}",
            PaymentMethodId   = "bolbradesco",
            DateOfExpiration  = expiracao,
            Payer = new PaymentPayerRequest
            {
                Email     = cliente.Email,
                FirstName = firstName,
                LastName  = lastName,
                Identification = new IdentificationRequest
                {
                    Type   = tipoDoc,
                    Number = docLimpo,
                },
                Address = new PaymentPayerAddressRequest
                {
                    ZipCode      = cliente.Cep,
                    StreetName   = cliente.Rua,
                    StreetNumber = int.TryParse(cliente.Numero, out var snInt) ? snInt : 0,
                    Neighborhood = cliente.Bairro,
                    City         = cliente.Cidade,
                    FederalUnit  = cliente.Estado,
                },
            },
        };

        if (!string.IsNullOrWhiteSpace(areaCode) && !string.IsNullOrWhiteSpace(phoneNumber))
        {
            request.Payer.Phone = new PaymentPayerPhoneRequest
            {
                AreaCode = areaCode,
                Number   = phoneNumber,
            };
        }

        var client   = new PaymentClient();
        var payment  = await client.CreateAsync(request, ReqOptions());

        if (payment?.Id is null)
            return (false, "MercadoPago não retornou ID do pagamento.");

        var barcode    = payment.TransactionDetails?.Barcode?.Content
                      ?? payment.TransactionDetails?.DigitableLine;
        var externalUrl = payment.TransactionDetails?.ExternalResourceUrl;

        db.Cobrancas.Add(new Cobranca
        {
            ChargeId       = payment.Id.ToString(),
            Status         = MapStatus(payment.Status),
            Total          = valorFinal,
            ExpireAt       = DateOnly.FromDateTime(expiracao),
            PaymentGateway = "mercadopago",
            PaymentMethod  = "boleto",
            Barcode        = barcode,
            Pdf            = externalUrl,
            ClienteId      = cliente.Id,
            OsId           = osId,
            VendaId        = vendaId,
            CreatedAt      = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return (true, string.Empty);
    }

    // ── Atualizar dados ───────────────────────────────────────────────────────
    public async Task<(bool ok, string erro)> AtualizarDadosAsync(int cobrancaId)
    {
        var cobranca = await db.Cobrancas.FindAsync(cobrancaId);
        if (cobranca is null) return (false, "Cobrança não encontrada.");
        if (string.IsNullOrWhiteSpace(cobranca.ChargeId))
            return (false, "Cobrança sem ID externo.");

        if (!long.TryParse(cobranca.ChargeId, out var paymentId))
            return (false, "ChargeId inválido.");

        var client  = new PaymentClient();
        var payment = await client.GetAsync(paymentId, ReqOptions());
        if (payment is null) return (false, "Pagamento não encontrado no MercadoPago.");

        cobranca.Status  = MapStatus(payment.Status);
        cobranca.Barcode = payment.TransactionDetails?.DigitableLine ?? cobranca.Barcode;
        cobranca.Pdf     = payment.TransactionDetails?.ExternalResourceUrl ?? cobranca.Pdf;
        await db.SaveChangesAsync();

        return (true, string.Empty);
    }

    // ── Cancelar ──────────────────────────────────────────────────────────────
    public async Task<(bool ok, string erro)> CancelarAsync(int cobrancaId)
    {
        var cobranca = await db.Cobrancas.FindAsync(cobrancaId);
        if (cobranca is null) return (false, "Cobrança não encontrada.");
        if (!long.TryParse(cobranca.ChargeId, out var paymentId))
            return (false, "ChargeId inválido.");

        var client = new PaymentClient();
        await client.CancelAsync(paymentId, ReqOptions());

        return await AtualizarDadosAsync(cobrancaId);
    }

    // ── Confirmar pagamento (capturar) ────────────────────────────────────────
    public async Task<(bool ok, string erro)> ConfirmarPagamentoAsync(int cobrancaId)
    {
        var cobranca = await db.Cobrancas.FindAsync(cobrancaId);
        if (cobranca is null) return (false, "Cobrança não encontrada.");
        if (!long.TryParse(cobranca.ChargeId, out var paymentId))
            return (false, "ChargeId inválido.");

        var client  = new PaymentClient();
        var payment = await client.GetAsync(paymentId, ReqOptions());
        if (payment is null) return (false, "Pagamento não encontrado no MercadoPago.");

        if (payment.Status == "authorized")
        {
            await client.CaptureAsync(paymentId, ReqOptions());
        }

        return await AtualizarDadosAsync(cobrancaId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string MapStatus(string? mpStatus) => mpStatus?.ToUpperInvariant() switch
    {
        "APPROVED"  => "RECEIVED",
        "CANCELLED" => "CANCELLED",
        "REFUNDED"  => "REFUNDED",
        "CHARGED_BACK" => "REFUND_REQUESTED",
        "IN_PROCESS" or "IN_MEDIATION" => "PENDING",
        "REJECTED"  => "failed",
        _ => mpStatus?.ToUpper() ?? "PENDING",
    };

    private static TimeSpan ParseIsoDuration(string iso)
    {
        // Very minimal ISO 8601 duration parser: P3D, PT2H, P1DT12H
        if (iso.StartsWith("P", StringComparison.OrdinalIgnoreCase))
        {
            int days = 0, hours = 0;
            var s = iso[1..]; // remove P
            if (s.Contains('T')) { var parts = s.Split('T'); ParseDate(parts[0], ref days); ParseTime(parts[1], ref hours); }
            else ParseDate(s, ref days);
            return TimeSpan.FromDays(days) + TimeSpan.FromHours(hours);
        }
        return TimeSpan.FromDays(3); // default

        static void ParseDate(string s, ref int days)
        {
            var idx = s.IndexOf('D');
            if (idx > 0 && int.TryParse(s[..idx], out var d)) days = d;
        }
        static void ParseTime(string s, ref int hours)
        {
            var idx = s.IndexOf('H');
            if (idx > 0 && int.TryParse(s[..idx], out var h)) hours = h;
        }
    }

    private static (string firstName, string lastName) SplitName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? (parts[0], parts[1]) : (fullName, "");
    }

    private static (string areaCode, string number) SplitPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length >= 10)
            return (digits[..2], digits[2..]);
        return (string.Empty, digits);
    }
}
