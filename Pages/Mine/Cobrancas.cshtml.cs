using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class CobrancasModel(ApplicationDbContext db) : MinePageModelBase(db)
{
    public List<Cobranca> Items { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Items = await Db.Cobrancas.AsNoTracking()
            .Where(c => c.ClienteId == ClienteId)
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        return Page();
    }
}
