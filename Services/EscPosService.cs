using System.Text;

namespace mapos_dotnet.Services;

/// <summary>
/// Gera bytes ESC/POS para impressoras térmicas.
/// Suporta papel de 58 mm (32 colunas) e 80 mm (48 colunas).
/// </summary>
public class EscPosService(IConfiguracaoService configSvc)
{
    // ESC/POS byte constants
    private static readonly byte[] Init       = [0x1B, 0x40];          // ESC @
    private static readonly byte[] AlignLeft  = [0x1B, 0x61, 0x00];
    private static readonly byte[] AlignCenter= [0x1B, 0x61, 0x01];
    private static readonly byte[] AlignRight = [0x1B, 0x61, 0x02];
    private static readonly byte[] BoldOn     = [0x1B, 0x45, 0x01];
    private static readonly byte[] BoldOff    = [0x1B, 0x45, 0x00];
    private static readonly byte[] DoubleOn   = [0x1B, 0x21, 0x30];    // double height+width
    private static readonly byte[] DoubleOff  = [0x1B, 0x21, 0x00];
    private static readonly byte[] LF         = [0x0A];
    private static readonly byte[] CutPartial = [0x1D, 0x56, 0x01];
    private static readonly byte[] FeedLines  = [0x1B, 0x64, 0x04];    // ESC d 4 — feed 4 lines

    private static readonly Encoding Cp850 = Encoding.GetEncoding(850);

    public async Task<byte[]> GerarCupomAsync(CupomData data)
    {
        var configs = await configSvc.GetAllAsync();
        int cols = int.TryParse(configs.GetValueOrDefault("pdv_paper_cols", "48"), out var c) ? c : 48;
        var header = configs.GetValueOrDefault("pdv_receipt_header", "");
        var footer = configs.GetValueOrDefault("pdv_receipt_footer", "Obrigado pela preferência!");

        var buf = new List<byte>();

        void Add(byte[] bytes) => buf.AddRange(bytes);
        void Txt(string text)  => buf.AddRange(Cp850.GetBytes(text));
        void Nl()              => Add(LF);
        void Line()            { Txt(new string('-', cols)); Nl(); }

        Add(Init);

        // Header (configurable multi-line)
        if (!string.IsNullOrWhiteSpace(header))
        {
            Add(AlignCenter);
            Add(BoldOn);
            foreach (var line in header.Split('\n'))
            {
                Txt(line.Trim().Truncate(cols)); Nl();
            }
            Add(BoldOff);
        }

        Line();

        // Title
        Add(AlignCenter);
        Add(BoldOn);
        Txt("CUPOM NAO FISCAL"); Nl();
        Add(BoldOff);
        Txt($"#{data.VendaId:D6}  {data.DataHora:dd/MM/yyyy HH:mm}"); Nl();

        if (!string.IsNullOrWhiteSpace(data.ClienteNome))
        {
            Txt($"Cliente: {data.ClienteNome.Truncate(cols - 9)}"); Nl();
        }

        Line();

        // Items
        Add(AlignLeft);
        foreach (var item in data.Itens)
        {
            var qtdPreco = $"{item.Quantidade:0.###}x{item.PrecoUnitario,8:F2}";
            var subtotal = $"{item.SubTotal,9:F2}";
            var descMaxLen = cols - qtdPreco.Length - subtotal.Length - 1;
            var desc = item.Descricao.Truncate(descMaxLen).PadRight(descMaxLen);
            Txt($"{desc} {qtdPreco}{subtotal}"); Nl();
        }

        Line();

        // Totals
        Add(AlignRight);
        if (data.Desconto > 0)
        {
            Txt(FormatTotal("DESCONTO:", $"-{data.Desconto:F2}", cols)); Nl();
        }
        Add(BoldOn);
        Add(DoubleOn);
        Txt(FormatTotal("TOTAL:", $"R${data.Total:F2}", cols)); Nl();
        Add(DoubleOff);
        Add(BoldOff);

        Line();

        // Payment
        Add(AlignLeft);
        Txt($"PAGAMENTO: {data.FormaPagamento.ToUpper()}"); Nl();
        if (data.ValorRecebido > 0 && data.FormaPagamento.Equals("dinheiro", StringComparison.OrdinalIgnoreCase))
        {
            Txt(FormatTotal("RECEBIDO:", $"R${data.ValorRecebido:F2}", cols)); Nl();
            Txt(FormatTotal("TROCO:", $"R${Math.Max(0, data.ValorRecebido - data.Total):F2}", cols)); Nl();
        }

        Line();

        // Footer
        if (!string.IsNullOrWhiteSpace(footer))
        {
            Add(AlignCenter);
            foreach (var line in footer.Split('\n'))
            {
                Txt(line.Trim().Truncate(cols)); Nl();
            }
        }

        Add(FeedLines);
        Add(CutPartial);

        return [.. buf];
    }

    public async Task<byte[]> GerarExtratoAsync(ExtratoCaixaData data)
    {
        var configs = await configSvc.GetAllAsync();
        int cols = int.TryParse(configs.GetValueOrDefault("pdv_paper_cols", "48"), out var c) ? c : 48;
        var header = configs.GetValueOrDefault("pdv_receipt_header", "");
        var footer = configs.GetValueOrDefault("pdv_receipt_footer", "Obrigado pela preferencia!");

        var buf = new List<byte>();

        void Add(byte[] bytes) => buf.AddRange(bytes);
        void Txt(string text)  => buf.AddRange(Cp850.GetBytes(text));
        void Nl()              => Add(LF);
        void Line()            { Txt(new string('=', cols)); Nl(); }
        void Dash()            { Txt(new string('-', cols)); Nl(); }

        string Lr(string left, string right)
        {
            var pad = cols - left.Length - right.Length;
            return left + new string(' ', Math.Max(1, pad)) + right;
        }

        Add(Init);

        // Header
        if (!string.IsNullOrWhiteSpace(header))
        {
            Add(AlignCenter); Add(BoldOn);
            foreach (var line in header.Split('\n'))
            { Txt(line.Trim().Truncate(cols)); Nl(); }
            Add(BoldOff);
        }

        Line();
        Add(AlignCenter); Add(BoldOn);
        Txt("EXTRATO DE CAIXA"); Nl();
        Add(BoldOff);
        Line();

        // Session info
        Add(AlignLeft);
        Txt($"Sessao:   #{data.SessaoId}"); Nl();
        Txt($"Operador: {data.OperadorNome.Truncate(cols - 10)}"); Nl();
        Txt($"Aberto:   {data.AbertoEm:dd/MM/yyyy HH:mm}"); Nl();
        if (data.FechadoEm.HasValue)
        { Txt($"Fechado:  {data.FechadoEm.Value:dd/MM/yyyy HH:mm}"); Nl(); }

        Dash();

        // Sales summary
        Add(BoldOn); Txt("VENDAS"); Nl(); Add(BoldOff);
        Txt(Lr("Quantidade:", data.QtdVendas.ToString())); Nl();
        Txt(Lr("Total:", $"R${data.TotalVendas:F2}")); Nl();

        Dash();

        // By payment method
        Add(BoldOn); Txt("POR FORMA DE PAGAMENTO"); Nl(); Add(BoldOff);
        Txt(Lr("Dinheiro:", $"R${data.TotalDinheiro:F2}")); Nl();
        Txt(Lr("PIX:",      $"R${data.TotalPix:F2}"));      Nl();
        Txt(Lr("Debito:",   $"R${data.TotalDebito:F2}"));   Nl();
        Txt(Lr("Credito:",  $"R${data.TotalCredito:F2}"));  Nl();
        Txt(Lr("Fiado:",    $"R${data.TotalFiado:F2}"));    Nl();

        Dash();

        // Cash reconciliation
        Add(BoldOn); Txt("CONFERENCIA DE CAIXA"); Nl(); Add(BoldOff);
        Txt(Lr("Saldo inicial:",     $"R${data.SaldoInicial:F2}")); Nl();
        Txt(Lr("Entradas dinheiro:", $"R${data.TotalDinheiro:F2}")); Nl();
        Txt(Lr("Saldo esperado:",    $"R${data.SaldoEsperado:F2}")); Nl();

        if (data.SaldoInformado.HasValue)
        {
            Txt(Lr("Saldo informado:", $"R${data.SaldoInformado.Value:F2}")); Nl();

            var dif = data.Diferenca ?? 0;
            var difLabel = dif < 0 ? "Falta" : dif > 0 ? "Sobra" : "OK";
            Txt(Lr($"Diferenca ({difLabel}):", $"R${Math.Abs(dif):F2}")); Nl();
        }

        Line();

        // Footer
        if (!string.IsNullOrWhiteSpace(footer))
        {
            Add(AlignCenter);
            foreach (var line in footer.Split('\n'))
            { Txt(line.Trim().Truncate(cols)); Nl(); }
        }

        Add(FeedLines);
        Add(CutPartial);

        return [.. buf];
    }

    public async Task<byte[]> GerarRelatorioAsync(RelatorioSessaoData data)
    {
        var configs = await configSvc.GetAllAsync();
        int cols = int.TryParse(configs.GetValueOrDefault("pdv_paper_cols", "48"), out var c) ? c : 48;
        var header = configs.GetValueOrDefault("pdv_receipt_header", "");

        var buf = new List<byte>();

        void Add(byte[] bytes) => buf.AddRange(bytes);
        void Txt(string text)  => buf.AddRange(Cp850.GetBytes(text));
        void Nl()              => Add(LF);
        void Line()            { Txt(new string('=', cols)); Nl(); }
        void Dash()            { Txt(new string('-', cols)); Nl(); }

        string Lr(string left, string right)
        {
            var pad = cols - left.Length - right.Length;
            return left + new string(' ', Math.Max(1, pad)) + right;
        }

        Add(Init);

        if (!string.IsNullOrWhiteSpace(header))
        {
            Add(AlignCenter); Add(BoldOn);
            foreach (var line in header.Split('\n'))
            { Txt(line.Trim().Truncate(cols)); Nl(); }
            Add(BoldOff);
        }

        Line();
        Add(AlignCenter); Add(BoldOn);
        Txt("RELATORIO PDV"); Nl();
        Add(BoldOff);
        Line();

        Add(AlignLeft);
        Txt($"Sessao:   #{data.SessaoId}"); Nl();
        Txt($"Operador: {data.OperadorNome.Truncate(cols - 10)}"); Nl();
        if (!string.IsNullOrWhiteSpace(data.TerminalNome))
        { Txt($"Terminal: {data.TerminalNome.Truncate(cols - 10)}"); Nl(); }
        Txt($"Aberto:   {data.AbertoEm:dd/MM/yyyy HH:mm}"); Nl();
        if (data.FechadoEm.HasValue)
        { Txt($"Fechado:  {data.FechadoEm.Value:dd/MM/yyyy HH:mm}"); Nl(); }

        if (data.Pausas.Count > 0)
        {
            Dash();
            Add(BoldOn); Txt($"PAUSAS ({data.Pausas.Count})"); Nl(); Add(BoldOff);

            var totalPausa = TimeSpan.Zero;
            foreach (var p in data.Pausas)
            {
                var dur = p.RetomadaEm.HasValue
                    ? (p.RetomadaEm.Value - p.IniciadaEm)
                    : (DateTime.Now - p.IniciadaEm);
                totalPausa += dur;
                var retomada = p.RetomadaEm.HasValue ? p.RetomadaEm.Value.ToString("HH:mm:ss") : "Em andamento";
                Txt($"  {p.IniciadaEm:HH:mm:ss} -> {retomada} ({(int)dur.TotalMinutes:D2}:{dur.Seconds:D2})"); Nl();
            }

            Dash();
            Txt(Lr("Total pausado:", $"{(int)totalPausa.TotalMinutes:D2}:{totalPausa.Seconds:D2}")); Nl();
        }

        Line();

        Add(FeedLines);
        Add(CutPartial);

        return [.. buf];
    }

    private static string FormatTotal(string label, string value, int cols)
    {
        var pad = cols - label.Length - value.Length;
        return label + new string(' ', Math.Max(1, pad)) + value;
    }
}

public static class StringExtensions2
{
    public static string Truncate(this string s, int max) =>
        s.Length <= max ? s : s[..max];
}

public class CupomData
{
    public int     VendaId       { get; set; }
    public DateTime DataHora     { get; set; }
    public string? ClienteNome   { get; set; }
    public List<CupomItem> Itens { get; set; } = [];
    public decimal Desconto      { get; set; }
    public decimal Total         { get; set; }
    public string  FormaPagamento{ get; set; } = "dinheiro";
    public decimal ValorRecebido { get; set; }
}

public class CupomItem
{
    public string  Descricao      { get; set; } = string.Empty;
    public decimal Quantidade     { get; set; }
    public decimal PrecoUnitario  { get; set; }
    public decimal SubTotal       { get; set; }
}

public class ExtratoCaixaData
{
    public int      SessaoId       { get; set; }
    public string   OperadorNome   { get; set; } = string.Empty;
    public DateTime AbertoEm       { get; set; }
    public DateTime? FechadoEm     { get; set; }
    public int      QtdVendas      { get; set; }
    public decimal  TotalVendas    { get; set; }
    public decimal  TotalDinheiro  { get; set; }
    public decimal  TotalPix       { get; set; }
    public decimal  TotalDebito    { get; set; }
    public decimal  TotalCredito   { get; set; }
    public decimal  TotalFiado     { get; set; }
    public decimal  SaldoInicial   { get; set; }
    public decimal  SaldoEsperado  { get; set; }
    public decimal? SaldoInformado { get; set; }
    public decimal? Diferenca      { get; set; }
}

public class RelatorioSessaoData
{
    public int      SessaoId     { get; set; }
    public string   OperadorNome { get; set; } = string.Empty;
    public string?  TerminalNome { get; set; }
    public DateTime AbertoEm    { get; set; }
    public DateTime? FechadoEm  { get; set; }
    public List<PdvPausaData> Pausas { get; set; } = [];
}

public class PdvPausaData
{
    public DateTime  IniciadaEm { get; set; }
    public DateTime? RetomadaEm { get; set; }
}
