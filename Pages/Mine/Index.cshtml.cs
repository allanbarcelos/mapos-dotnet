using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class IndexModel(ApplicationDbContext db) : MinePageModelBase(db)
{
    public List<Models.Os> OsRecentes { get; set; } = [];
    public List<Venda> Compras    { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        OsRecentes = await Db.Os.AsNoTracking()
            .Where(o => o.ClienteId == ClienteId)
            .OrderByDescending(o => o.Id)
            .Take(5)
            .ToListAsync();

        Compras = await Db.Vendas.AsNoTracking()
            .Where(v => v.ClienteId == ClienteId)
            .OrderByDescending(v => v.Id)
            .Take(5)
            .ToListAsync();

        return Page();
    }
}
