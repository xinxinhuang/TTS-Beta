using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages.TeeSheet
{
    [Authorize]
    public class ViewModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public ViewModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        public DateTime StartDate { get; set; }
        public Dictionary<DateTime, List<ScheduledGolfTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<ScheduledGolfTime>>();
        public Dictionary<int, string> Events { get; set; } = new Dictionary<int, string>();

        public async Task<IActionResult> OnGetAsync(DateTime? date)
        {
            // Set the start date to the beginning of the current week if not specified
            StartDate = date.HasValue ? date.Value.Date : DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            
            // Get the end date (7 days after start date)
            var endDate = StartDate.AddDays(7);

            // Get all scheduled golf times for the week
            var scheduledTimes = await _context.ScheduledGolfTimes
                .Include(s => s.Reservations)
                    .ThenInclude(r => r.Member)
                        .ThenInclude(m => m.User)
                .Where(s => s.ScheduledDate >= StartDate && s.ScheduledDate < endDate)
                .ToListAsync();

            // Get all events for the week
            var events = await _context.Events
                .Where(e => e.EventDate >= StartDate && e.EventDate < endDate)
                .ToListAsync();

            // Group scheduled times by date
            foreach (var time in scheduledTimes)
            {
                if (!TeeSheets.ContainsKey(time.ScheduledDate.Date))
                {
                    TeeSheets[time.ScheduledDate.Date] = new List<ScheduledGolfTime>();
                }
                
                TeeSheets[time.ScheduledDate.Date].Add(time);
            }

            // Create a dictionary of event names by scheduled golf time ID
            foreach (var scheduledTime in scheduledTimes.Where(s => !s.IsAvailable && !s.Reservations.Any()))
            {
                // Find any events that overlap with this scheduled time
                var overlappingEvent = events.FirstOrDefault(e => 
                    e.EventDate.Date == scheduledTime.ScheduledDate.Date &&
                    e.StartTime <= scheduledTime.ScheduledTime &&
                    e.EndTime > scheduledTime.ScheduledTime);

                if (overlappingEvent != null)
                {
                    Events[scheduledTime.ScheduledGolfTimeID] = overlappingEvent.EventName;
                }
            }

            return Page();
        }
    }
}
