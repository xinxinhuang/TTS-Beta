using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages.TeeSheet
{
    [Authorize(Roles = "Clerk")]
    public class ViewModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public ViewModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        public DateTime StartDate { get; set; }
        
        // Dictionary of date -> list of tee times for that date
        public Dictionary<DateTime, List<ScheduledGolfTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<ScheduledGolfTime>>();
        
        // Dictionary of tee time ID -> event name
        public Dictionary<int, string> Events { get; set; } = new Dictionary<int, string>();

        public async Task<IActionResult> OnGetAsync(DateTime startDate)
        {
            StartDate = startDate.Date;
            await LoadTeeSheetDataAsync(StartDate);
            
            return Page();
        }

        private async Task LoadTeeSheetDataAsync(DateTime startDate)
        {
            // Clear existing data
            TeeSheets.Clear();
            Events.Clear();

            // Load all tee times for the week with reservations
            var endDate = startDate.AddDays(7);
            var teeTimes = await _context.ScheduledGolfTimes
                .Include(t => t.Reservations)
                    .ThenInclude(r => r.Member)
                        .ThenInclude(m => m.User)
                .Where(t => t.ScheduledDate >= startDate && t.ScheduledDate < endDate)
                .OrderBy(t => t.ScheduledDate)
                .ThenBy(t => t.ScheduledTime)
                .ToListAsync();

            // Group by date
            foreach (var teeTime in teeTimes)
            {
                var date = teeTime.ScheduledDate.Date;
                if (!TeeSheets.ContainsKey(date))
                {
                    TeeSheets[date] = new List<ScheduledGolfTime>();
                }
                TeeSheets[date].Add(teeTime);

                // In a real app, we'd load events from a separate table
                // For now, we'll just use a placeholder
                if (!teeTime.IsAvailable && !teeTime.Reservations.Any())
                {
                    Events[teeTime.ScheduledGolfTimeID] = "Special Event";
                }
            }
        }
    }
}
