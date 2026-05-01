using mapos_dotnet.Data;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;

namespace mapos_dotnet.Pages.Configuracoes;

public class IndexModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc)
    : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty] public string AppName         { get; set; } = string.Empty;
    [BindProperty] public string AppTheme        { get; set; } = "white";
    [BindProperty] public string PerPage         { get; set; } = "10";
    [BindProperty] public string OsNotification  { get; set; } = "cliente";
    [BindProperty] public bool ControlEstoque    { get; set; }
    [BindProperty] public bool ControlBaixa      { get; set; }
    [BindProperty] public bool ControlEditos     { get; set; }
    [BindProperty] public bool ControlEditVendas { get; set; }
    [BindProperty] public bool EmailAutomatico        { get; set; }
    [BindProperty] public bool Control2Vias           { get; set; }
    [BindProperty] public string PixKey               { get; set; } = string.Empty;
    [BindProperty] public string OsStatusList         { get; set; } = string.Empty;
    [BindProperty] public int EmailProcessInterval    { get; set; } = 2;
    [BindProperty] public int EmailRetryInterval      { get; set; } = 5;

    // PDV / Impressora Térmica
    [BindProperty] public string PdvPrinterHost   { get; set; } = string.Empty;
    [BindProperty] public string PdvPrinterPort   { get; set; } = "9100";
    [BindProperty] public string PdvPaperCols     { get; set; } = "48";
    [BindProperty] public string PdvReceiptHeader { get; set; } = string.Empty;
    [BindProperty] public string PdvReceiptFooter { get; set; } = string.Empty;
    [BindProperty] public bool PdvGerarLancamento { get; set; } = true;

    // PDV / MercadoPago Point (maquininha)
    [BindProperty] public string PdvMpAccessToken { get; set; } = string.Empty;
    [BindProperty] public string PdvMpDeviceId    { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        await SetLayoutDataAsync();
        var c = await ConfigSvc.GetAllAsync();
        AppName          = c.GetValueOrDefault("app_name", "Map-OS");
        AppTheme         = c.GetValueOrDefault("app_theme", "white");
        PerPage          = c.GetValueOrDefault("per_page", "10");
        OsNotification   = c.GetValueOrDefault("os_notification", "cliente");
        ControlEstoque   = c.GetValueOrDefault("control_estoque", "1") == "1";
        ControlBaixa     = c.GetValueOrDefault("control_baixa", "0") == "1";
        ControlEditos    = c.GetValueOrDefault("control_editos", "1") == "1";
        ControlEditVendas= c.GetValueOrDefault("control_edit_vendas", "1") == "1";
        EmailAutomatico  = c.GetValueOrDefault("email_automatico", "1") == "1";
        Control2Vias     = c.GetValueOrDefault("control_2vias", "0") == "1";
        PixKey               = c.GetValueOrDefault("pix_key", "");
        OsStatusList         = c.GetValueOrDefault("os_status_list", "");
        EmailProcessInterval = int.TryParse(c.GetValueOrDefault("email_process_interval", "2"), out var epi) ? epi : 2;
        EmailRetryInterval   = int.TryParse(c.GetValueOrDefault("email_retry_interval",   "5"), out var eri) ? eri : 5;
        PdvPrinterHost   = c.GetValueOrDefault("pdv_printer_host", "");
        PdvPrinterPort   = c.GetValueOrDefault("pdv_printer_port", "9100");
        PdvPaperCols     = c.GetValueOrDefault("pdv_paper_cols", "48");
        PdvReceiptHeader = c.GetValueOrDefault("pdv_receipt_header", "");
        PdvReceiptFooter = c.GetValueOrDefault("pdv_receipt_footer", "");
        PdvGerarLancamento = c.GetValueOrDefault("pdv_gerar_lancamento", "1") == "1";
        PdvMpAccessToken   = c.GetValueOrDefault("pdv_mp_access_token", "");
        PdvMpDeviceId      = c.GetValueOrDefault("pdv_mp_device_id", "");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("cSistema")) return SemPermissao();
        await ConfigSvc.SetAsync("app_name",          AppName);
        await ConfigSvc.SetAsync("app_theme",          AppTheme);
        await ConfigSvc.SetAsync("per_page",           PerPage);
        await ConfigSvc.SetAsync("os_notification",    OsNotification);
        await ConfigSvc.SetAsync("control_estoque",    ControlEstoque ? "1" : "0");
        await ConfigSvc.SetAsync("control_baixa",      ControlBaixa ? "1" : "0");
        await ConfigSvc.SetAsync("control_editos",     ControlEditos ? "1" : "0");
        await ConfigSvc.SetAsync("control_edit_vendas",ControlEditVendas ? "1" : "0");
        await ConfigSvc.SetAsync("email_automatico",        EmailAutomatico ? "1" : "0");
        await ConfigSvc.SetAsync("control_2vias",           Control2Vias ? "1" : "0");
        await ConfigSvc.SetAsync("pix_key",                 PixKey);
        await ConfigSvc.SetAsync("os_status_list",          OsStatusList);
        await ConfigSvc.SetAsync("email_process_interval",  EmailProcessInterval.ToString());
        await ConfigSvc.SetAsync("email_retry_interval",    EmailRetryInterval.ToString());
        await ConfigSvc.SetAsync("pdv_printer_host",   PdvPrinterHost);
        await ConfigSvc.SetAsync("pdv_printer_port",   PdvPrinterPort);
        await ConfigSvc.SetAsync("pdv_paper_cols",     PdvPaperCols);
        await ConfigSvc.SetAsync("pdv_receipt_header", PdvReceiptHeader);
        await ConfigSvc.SetAsync("pdv_receipt_footer", PdvReceiptFooter);
        await ConfigSvc.SetAsync("pdv_gerar_lancamento", PdvGerarLancamento ? "1" : "0");
        await ConfigSvc.SetAsync("pdv_mp_access_token",  PdvMpAccessToken);
        await ConfigSvc.SetAsync("pdv_mp_device_id",     PdvMpDeviceId);
        await AuditarAsync("Atualizou configurações do sistema");
        TempData["Success"] = "Configurações salvas.";
        return RedirectToPage();
    }
}
