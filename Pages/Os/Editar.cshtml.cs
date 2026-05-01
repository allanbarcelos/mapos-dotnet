using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Os;

public class EditarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty]
    public int OsId { get; set; }

    [BindProperty, Required(ErrorMessage = "Selecione um cliente.")]
    public int ClienteId { get; set; }

    [BindProperty, Required(ErrorMessage = "Selecione um técnico.")]
    public int TecnicoId { get; set; }

    [BindProperty]
    public DateOnly? DataInicial { get; set; }

    [BindProperty]
    public DateOnly? DataFinal { get; set; }

    [BindProperty, Required(ErrorMessage = "Selecione um status.")]
    public string Status { get; set; } = "Aberto";

    [BindProperty]
    public string? DescricaoProduto { get; set; }

    [BindProperty]
    public string? Defeito { get; set; }

    [BindProperty]
    public string? Observacoes { get; set; }

    [BindProperty]
    public string? LaudoTecnico { get; set; }

    [BindProperty]
    public string? Garantia { get; set; }

    [BindProperty]
    public decimal ValorTotal { get; set; }

    public bool Faturado { get; private set; }
    public bool BloqueadoPorFaturamento { get; private set; }
    public List<Cliente> Clientes { get; private set; } = [];
    public List<Usuario> Usuarios { get; private set; } = [];
    public List<string> StatusList { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await TemPermissaoAsync("eOs"))
            return SemPermissao();

        await SetLayoutDataAsync();

        var os = await Db.Os.FindAsync(id);
        if (os is null)
        {
            TempData["Error"] = "OS não encontrada.";
            return RedirectToPage("/Os/Index");
        }

        var controlEditos = await ConfigSvc.GetAsync("control_editos");
        if (os.Faturado && controlEditos == "0")
        {
            TempData["Error"] = "Esta OS está faturada e não pode ser editada.";
            return RedirectToPage("/Os/Visualizar", new { id });
        }

        Faturado = os.Faturado;
        BloqueadoPorFaturamento = os.Faturado && controlEditos == "0";

        OsId             = os.Id;
        ClienteId        = os.ClienteId;
        TecnicoId        = os.UsuarioId;
        DataInicial      = os.DataInicial;
        DataFinal        = os.DataFinal;
        Status           = os.Status;
        DescricaoProduto = os.DescricaoProduto;
        Defeito          = os.Defeito;
        Observacoes      = os.Observacoes;
        LaudoTecnico     = os.LaudoTecnico;
        Garantia         = os.Garantia;
        ValorTotal       = os.ValorTotal;

        await CarregarDadosAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("eOs"))
            return SemPermissao();

        var os = await Db.Os.FindAsync(OsId);
        if (os is null)
        {
            TempData["Error"] = "OS não encontrada.";
            return RedirectToPage("/Os/Index");
        }

        var controlEditos = await ConfigSvc.GetAsync("control_editos");
        if (os.Faturado && controlEditos == "0")
        {
            TempData["Error"] = "Esta OS está faturada e não pode ser editada.";
            return RedirectToPage("/Os/Visualizar", new { id = OsId });
        }

        await CarregarDadosAsync();
        Faturado = os.Faturado;

        if (!ModelState.IsValid)
            return Page();

        os.ClienteId        = ClienteId;
        os.UsuarioId        = TecnicoId;
        os.DataInicial      = DataInicial;
        os.DataFinal        = DataFinal;
        os.Status           = Status;
        os.DescricaoProduto = DescricaoProduto;
        os.Defeito          = Defeito;
        os.Observacoes      = Observacoes;
        os.LaudoTecnico     = LaudoTecnico;
        os.Garantia         = Garantia;
        os.ValorTotal       = ValorTotal;

        await Db.SaveChangesAsync();
        await AuditarAsync($"Editou Ordem de Serviço #{os.Id}");

        TempData["Success"] = $"OS #{os.Id} atualizada com sucesso.";
        return RedirectToPage("/Os/Visualizar", new { id = os.Id });
    }

    private async Task CarregarDadosAsync()
    {
        Clientes = await Db.Clientes
            .OrderBy(c => c.NomeCliente)
            .ToListAsync();

        Usuarios = await Db.Usuarios
            .Where(u => u.Situacao)
            .OrderBy(u => u.Nome)
            .ToListAsync();

        var statusJson = await ConfigSvc.GetAsync("os_status_list");
        if (!string.IsNullOrWhiteSpace(statusJson))
        {
            try { StatusList = JsonSerializer.Deserialize<List<string>>(statusJson) ?? []; }
            catch { StatusList = []; }
        }

        if (StatusList.Count == 0)
            StatusList = ["Aberto", "Em Andamento", "Finalizado", "Faturado", "Cancelado"];
    }
}
