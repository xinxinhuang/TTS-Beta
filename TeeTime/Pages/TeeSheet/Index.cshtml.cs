using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeeTime.Data;
using TeeTime.Models.TeeSheet;

namespace TeeTime.Pages.TeeSheet
{
    public class IndexModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public IndexModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public Models.TeeSheet.TeeSheet TeeSheet { get; set; } = new();
        public IList<Models.TeeSheet.TeeTime> TeeTimes { get; set; } = new List<Models.TeeSheet.TeeTime>();

        public async Task OnGetAsync()
        {
            TeeSheet = await _context.TeeSheets
                .FirstOrDefaultAsync(m => m.Date.Date == SelectedDate.Date) ?? new();

            if (TeeSheet != null)
            {
                TeeTimes = await _context.TeeTimes
                    .Where(tt => tt.TeeSheetId == TeeSheet.Id)
                    .OrderBy(tt => tt.StartTime)
                    .ToListAsync();
            }
        }
    }
}
