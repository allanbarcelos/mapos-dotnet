using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using mapos_dotnet.Services;

namespace mapos_dotnet.Services;

public interface IPdvPointService
{
    Task<(bool ok, string paymentIntentId, string erro)> CriarIntencaoAsync(
        decimal valor, string descricao, string tipo, string referencia);

    Task<PdvPaymentIntentStatus> ConsultarAsync(string paymentIntentId);
    Task CancelarAsync(string deviceId);
}

public record PdvPaymentIntentStatus(
    string Status,          // OPEN | ON_TERMINAL | PROCESSING | CANCELED | ERROR | FINISHED
    long?  MpPaymentId,
    string? Estado);        // approved | rejected | pending (quando FINISHED)

public class PdvPointService(
    IConfiguracaoService configSvc,
    IHttpClientFactory httpFactory) : IPdvPointService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private async Task<(string token, string deviceId)> GetConfigAsync()
    {
        var cfg = await configSvc.GetAllAsync();
        var token    = cfg.GetValueOrDefault("pdv_mp_access_token", "");
        var deviceId = cfg.GetValueOrDefault("pdv_mp_device_id", "");
        return (token, deviceId);
    }

    private HttpClient Client(string token)
    {
        var client = httpFactory.CreateClient("MpPoint");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<(bool ok, string paymentIntentId, string erro)> CriarIntencaoAsync(
        decimal valor, string descricao, string tipo, string referencia)
    {
        var (token, deviceId) = await GetConfigAsync();
        if (string.IsNullOrWhiteSpace(token))   return (false, "", "Access Token do MercadoPago não configurado.");
        if (string.IsNullOrWhiteSpace(deviceId)) return (false, "", "ID do dispositivo Point não configurado.");

        // Map PDV payment type → MP Point payment type
        var mpType = tipo switch
        {
            "pix"     => "pix",
            "debito"  => "debit_card",
            "credito" => "credit_card",
            _         => "credit_card",
        };

        var body = new
        {
            amount      = (int)Math.Round(valor * 100),   // centavos
            description = descricao,
            payment = new
            {
                installments = 1,
                type         = mpType,
            },
            additional_info = new
            {
                external_reference = referencia,
            },
        };

        var json = JsonSerializer.Serialize(body, _json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var http = Client(token);
            var resp = await http.PostAsync(
                $"https://api.mercadopago.com/point/integration-api/devices/{deviceId}/payment-intents",
                content);

            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return (false, "", $"MP Point API erro {(int)resp.StatusCode}: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var intentId = doc.RootElement.TryGetProperty("id", out var idEl)
                ? idEl.GetString() ?? ""
                : "";

            return string.IsNullOrEmpty(intentId)
                ? (false, "", "MP Point não retornou ID da intenção.")
                : (true, intentId, "");
        }
        catch (Exception ex)
        {
            return (false, "", $"Erro ao conectar com MP Point: {ex.Message}");
        }
    }

    public async Task<PdvPaymentIntentStatus> ConsultarAsync(string paymentIntentId)
    {
        var (token, _) = await GetConfigAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new("ERROR", null, null);

        try
        {
            var http = Client(token);
            var resp = await http.GetAsync(
                $"https://api.mercadopago.com/point/integration-api/payment-intents/{paymentIntentId}");

            if (!resp.IsSuccessStatusCode)
                return new("ERROR", null, null);

            var raw = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var status = root.TryGetProperty("state", out var stEl)
                ? stEl.GetString() ?? "OPEN"
                : "OPEN";

            long? mpId = null;
            string? estado = null;

            if (root.TryGetProperty("payment", out var payEl))
            {
                if (payEl.TryGetProperty("id", out var pidEl) && pidEl.ValueKind == JsonValueKind.Number)
                    mpId = pidEl.GetInt64();
                if (payEl.TryGetProperty("state", out var pstEl))
                    estado = pstEl.GetString();
            }

            return new(status.ToUpperInvariant(), mpId, estado);
        }
        catch
        {
            return new("ERROR", null, null);
        }
    }

    public async Task CancelarAsync(string deviceId)
    {
        var (token, dev) = await GetConfigAsync();
        var id = string.IsNullOrWhiteSpace(deviceId) ? dev : deviceId;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(id)) return;

        try
        {
            var http = Client(token);
            await http.DeleteAsync(
                $"https://api.mercadopago.com/point/integration-api/devices/{id}/payment-intents");
        }
        catch { /* best effort */ }
    }
}
