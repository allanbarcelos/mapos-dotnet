using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class ContaModel(ApplicationDbContext db) : MinePageModelBase(db)
{
    public Cliente Cliente { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var cliente = await Db.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ClienteId);

        if (cliente is null) return NotFound();
        Cliente = cliente;
        return Page();
    }
}
