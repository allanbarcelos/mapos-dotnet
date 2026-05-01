using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class VisualizarCompraModel(ApplicationDbContext db) : MinePageModelBase(db)
{
    public Venda Venda { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var venda = await Db.Vendas.AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id && v.ClienteId == ClienteId);

        if (venda is null) return NotFound();
        Venda = venda;
        return Page();
    }
}
