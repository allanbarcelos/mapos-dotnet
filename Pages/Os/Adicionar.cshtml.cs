using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Os;

public class AdicionarModel(
    ApplicationDbContext db,
    IPermissaoService permissaoSvc,
    IAuditService auditSvc,
    IConfiguracaoService configSvc) : PageModelBase(db, permissaoSvc, auditSvc, configSvc)
{
    [BindProperty, Required(ErrorMessage = "Selecione um cliente.")]
    public int ClienteId { get; set; }

    [BindProperty, Required(ErrorMessage = "Selecione um técnico.")]
    public int TecnicoId { get; set; }

    [BindProperty]
    public DateOnly? DataInicial { get; set; } = DateOnly.FromDateTime(DateTime.Today);

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
    public string? Garantia { get; set; }

    [BindProperty]
    public decimal ValorTotal { get; set; }

    public List<Cliente> Clientes { get; private set; } = [];
    public List<Usuario> Usuarios { get; private set; } = [];
    public List<string> StatusList { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await TemPermissaoAsync("aOs"))
            return SemPermissao();

        await SetLayoutDataAsync();
        await CarregarDadosAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await TemPermissaoAsync("aOs"))
            return SemPermissao();

        await CarregarDadosAsync();

        if (!ModelState.IsValid)
            return Page();

        var os = new Models.Os
        {
            ClienteId        = ClienteId,
            UsuarioId        = TecnicoId,
            DataInicial      = DataInicial,
            DataFinal        = DataFinal,
            Status           = Status,
            DescricaoProduto = DescricaoProduto,
            Defeito          = Defeito,
            Observacoes      = Observacoes,
            Garantia         = Garantia,
            ValorTotal       = ValorTotal,
            TipoDesconto     = "real",
        };

        Db.Os.Add(os);
        await Db.SaveChangesAsync();
        await AuditarAsync($"Abriu Ordem de Serviço #{os.Id}");

        TempData["Success"] = $"OS #{os.Id} criada com sucesso.";
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
