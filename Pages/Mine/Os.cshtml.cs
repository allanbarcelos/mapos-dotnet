using mapos_dotnet.Data;
using mapos_dotnet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mapos_dotnet.Pages.Mine;

public class OsModel(ApplicationDbContext db) : MinePageModelBase(db)
{
    public List<Models.Os> Items { get; set; } = [];
    public new int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    private const int PageSize = 10;

    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        Page = Math.Max(1, page);

        var query = Db.Os.AsNoTracking()
            .Where(o => o.ClienteId == ClienteId)
            .OrderByDescending(o => o.Id);

        var total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        Items = await query.Skip((Page - 1) * PageSize).Take(PageSize).ToListAsync();

        return Page();
    }
}
