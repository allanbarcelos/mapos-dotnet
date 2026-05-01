using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class ImprimirOsModel(ApplicationDbContext db) : MinePageModelBase(db)
{
    public Models.Os Os { get; set; } = null!;
    public Models.Emitente? Emitente { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var os = await Db.Os.AsNoTracking()
            .Include(o => o.Cliente)
            .Include(o => o.Usuario)
            .Include(o => o.ProdutosOs)
            .Include(o => o.ServicosOs)
            .Include(o => o.EquipamentosOs).ThenInclude(e => e.Equipamento)
            .Include(o => o.Anotacoes)
            .FirstOrDefaultAsync(o => o.Id == id && o.ClienteId == ClienteId);

        if (os is null) return NotFound();
        Os = os;
        Emitente = await Db.Emitente.AsNoTracking().FirstOrDefaultAsync();
        return Page();
    }
}
