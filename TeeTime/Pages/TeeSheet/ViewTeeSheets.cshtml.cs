using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeeTime.Data;
using TeeTime.Models;
using TeeTime.Models.TeeSheet;

namespace TeeTime.Pages.TeeSheet
{
    [Authorize(Roles = "Clerk")]
    public class ViewTeeSheetsModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public ViewTeeSheetsModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        public DateTime? WeekStartDate { get; set; }
        
        // Dictionary of date -> list of tee times for that date
        public Dictionary<DateTime, List<Models.TeeSheet.TeeTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<Models.TeeSheet.TeeTime>>();
        
        // Dictionary of tee time ID -> event name
        public Dictionary<int, string> Events { get; set; } = new Dictionary<int, string>();
        
        // List of published weeks
        public List<PublishedWeekInfo> PublishedWeeks { get; set; } = new List<PublishedWeekInfo>();

        public class PublishedWeekInfo
        {
            public DateTime StartDate { get; set; }
            public int TeeTimeCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(DateTime? startDate = null)
        {
            await LoadPublishedWeeksAsync();
            
            if (startDate.HasValue)
            {
                WeekStartDate = startDate.Value.Date;
                await LoadTeeSheetDataAsync(WeekStartDate.Value);
            }
            else if (PublishedWeeks.Any())
            {
                // Default to the most recent published week
                WeekStartDate = PublishedWeeks.First().StartDate;
                await LoadTeeSheetDataAsync(WeekStartDate.Value);
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostBlockAsync(int teeTimeId, DateTime startDate)
        {
            var teeTime = await _context.TeeTimes.FindAsync(teeTimeId);
            if (teeTime != null)
            {
                teeTime.IsAvailable = false;
                teeTime.Notes = "Blocked manually";
                _context.TeeTimes.Update(teeTime);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tee time at {teeTime.StartTime.ToString("hh:mm tt")} has been blocked successfully.";
            }

            return RedirectToPage(new { startDate = startDate.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostUnblockAsync(int teeTimeId, DateTime startDate)
        {
            var teeTime = await _context.TeeTimes
                .Include(t => t.Reservations)
                .FirstOrDefaultAsync(t => t.Id == teeTimeId);
                
            if (teeTime != null)
            {
                // Check if this tee time has a reservation
                if (teeTime.Reservations.Any())
                {
                    TempData["ErrorMessage"] = "Cannot unblock a tee time that has a reservation.";
                    return RedirectToPage(new { startDate = startDate.ToString("yyyy-MM-dd") });
                }
                
                teeTime.IsAvailable = true;
                teeTime.Notes = string.Empty;
                _context.TeeTimes.Update(teeTime);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tee time at {teeTime.StartTime.ToString("hh:mm tt")} has been unblocked successfully.";
            }

            return RedirectToPage(new { startDate = startDate.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostDeleteTeeSheetAsync(DateTime startDate)
        {
            // Ensure user is authorized (already checked by Authorize attribute)
            
            // Calculate the week's date range
            DateTime weekStartDate = startDate.Date;
            DateTime weekEndDate = weekStartDate.AddDays(7);

            try
            {
                // Delete all tee sheets for this week (cascade delete will handle tee times)
                var teeSheetsToDelete = await _context.TeeSheets
                    .Where(ts => ts.Date >= weekStartDate && ts.Date < weekEndDate)
                    .ToListAsync();

                if (teeSheetsToDelete.Any())
                {
                    _context.TeeSheets.RemoveRange(teeSheetsToDelete);
                    await _context.SaveChangesAsync();
                    
                    int teeSheetCount = teeSheetsToDelete.Count;
                    TempData["SuccessMessage"] = $"Successfully deleted {teeSheetCount} tee sheets for the week of {weekStartDate:MMMM d, yyyy}.";
                }
                else
                {
                    TempData["WarningMessage"] = $"No tee sheets found for the week of {weekStartDate:MMMM d, yyyy}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the tee sheet: {ex.Message}";
            }

            await LoadPublishedWeeksAsync();
            
            if (PublishedWeeks.Any())
            {
                // Redirect to the most recent published week
                return RedirectToPage(new { startDate = PublishedWeeks.First().StartDate.ToString("yyyy-MM-dd") });
            }
            else
            {
                // No published weeks left, just show the page
                return RedirectToPage();
            }
        }

        private async Task LoadTeeSheetDataAsync(DateTime startDate)
        {
            // Clear existing data
            TeeSheets.Clear();
            Events.Clear();

            // Load all tee sheets for the week
            var endDate = startDate.AddDays(7);
            var teeSheets = await _context.TeeSheets
                .Where(ts => ts.Date >= startDate && ts.Date < endDate)
                .Include(ts => ts.TeeTimes)
                    .ThenInclude(tt => tt.Event)
                .OrderBy(ts => ts.Date)
                .ToListAsync();

            // Group tee times by date
            foreach (var teeSheet in teeSheets)
            {
                var date = teeSheet.Date.Date;
                if (!TeeSheets.ContainsKey(date))
                {
                    TeeSheets[date] = new List<Models.TeeSheet.TeeTime>();
                }
                
                // Add all tee times for this sheet
                var orderedTeeTimes = teeSheet.TeeTimes.OrderBy(tt => tt.StartTime).ToList();
                TeeSheets[date].AddRange(orderedTeeTimes);
                
                // Store notes in the dictionary for easy access
                foreach(var teeTime in orderedTeeTimes)
                {
                    if (teeTime.Event != null)
                    {
                        // If teeTime has an associated Event, use its information
                        Events[teeTime.Id] = $"{teeTime.Event.EventName} ({teeTime.Event.EventColor})";
                    }
                    else if (!teeTime.IsAvailable && !string.IsNullOrEmpty(teeTime.Notes))
                    {
                        Events[teeTime.Id] = teeTime.Notes;
                    }
                    else if (!teeTime.IsAvailable)
                    {
                        Events[teeTime.Id] = "Blocked";
                    }
                }
            }
        }

        private async Task LoadPublishedWeeksAsync()
        {
            // Clear the current list
            PublishedWeeks.Clear();
            
            // Get all distinct dates that have tee sheets
            var distinctDates = await _context.TeeSheets
                .Select(t => t.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
                
            // Group them by week
            var weekGroups = new Dictionary<DateTime, int>();
            
            foreach (var date in distinctDates)
            {
                // Get the Sunday of the week for this date
                DateTime weekStart = date.AddDays(-(int)date.DayOfWeek);
                
                // If we don't have this week start in our dictionary yet, add it
                if (!weekGroups.ContainsKey(weekStart))
                {
                    // Count all tee times for this week
                    var teeTimeCount = await _context.TeeTimes
                        .Include(tt => tt.TeeSheet)
                        .Where(tt => tt.TeeSheet.Date >= weekStart && 
                                     tt.TeeSheet.Date < weekStart.AddDays(7))
                        .CountAsync();
                    
                    if (teeTimeCount > 0)
                    {
                        weekGroups[weekStart] = teeTimeCount;
                    }
                }
            }
            
            // Convert the dictionary to our PublishedWeekInfo list
            PublishedWeeks = weekGroups
                .Select(g => new PublishedWeekInfo
                {
                    StartDate = g.Key,
                    TeeTimeCount = g.Value
                })
                .OrderByDescending(w => w.StartDate)
                .ToList();
        }
    }
}