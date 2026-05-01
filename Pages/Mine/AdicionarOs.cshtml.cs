using System.ComponentModel.DataAnnotations;
using mapos_dotnet.Data;
using mapos_dotnet.Models;
using mapos_dotnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class AdicionarOsModel(ApplicationDbContext db, IConfiguracaoService configSvc) : MinePageModelBase(db)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<string> StatusList { get; set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Descreva o defeito ou solicitação.")]
        public string Defeito { get; set; } = string.Empty;

        public string? DescricaoProduto { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadStatusAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadStatusAsync();
            return Page();
        }

        // Pega o primeiro usuário ativo como responsável padrão
        var usuario = await Db.Usuarios.AsNoTracking()
            .Where(u => u.Situacao)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();

        var os = new Models.Os
        {
            ClienteId        = ClienteId,
            UsuarioId        = usuario?.Id ?? 1,
            DataInicial      = (DateOnly?)DateOnly.FromDateTime(DateTime.Today),
            Status           = StatusList.FirstOrDefault() ?? "Aberto",
            Defeito          = Input.Defeito,
            DescricaoProduto = Input.DescricaoProduto,
            ValorTotal       = 0
        };

        Db.Os.Add(os);
        await Db.SaveChangesAsync();

        TempData["Success"] = $"OS #{os.Id} aberta com sucesso. Aguarde o retorno da equipe técnica.";
        return RedirectToPage("Os");
    }

    private async Task LoadStatusAsync()
    {
        var configs = await configSvc.GetAllAsync();
        var statusJson = configs.GetValueOrDefault("os_status_list", """["Aberto"]""");
        StatusList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(statusJson) ?? ["Aberto"];
    }
}
