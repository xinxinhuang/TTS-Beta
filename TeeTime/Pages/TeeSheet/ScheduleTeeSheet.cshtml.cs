using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TeeTime.Data;
using TeeTime.Models;
using TeeTime.Models.TeeSheet;

namespace TeeTime.Pages.TeeSheet
{
    [Authorize(Roles = "Clerk")]
    public class ScheduleTeeSheetModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public ScheduleTeeSheetModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek); // Default to next week's Sunday

        // Fixed tee time values - not editable by user
        private TimeSpan FirstTeeTime { get; } = new TimeSpan(7, 0, 0); // 7:00 AM
        private TimeSpan LastTeeTime { get; } = new TimeSpan(18, 0, 0); // 6:00 PM

        [BindProperty]
        [Required(ErrorMessage = "Interval is required")]
        [Range(8, 15, ErrorMessage = "Interval must be between 8 and 15 minutes")]
        public int Interval { get; set; } = 8;

        public DateTime? WeekStartDate { get; set; }
        
        // Dictionary of date -> list of tee times for that date
        public Dictionary<DateTime, List<Models.TeeSheet.TeeTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<Models.TeeSheet.TeeTime>>();
        
        // List of published weeks
        public List<PublishedWeekInfo> PublishedWeeks { get; set; } = new List<PublishedWeekInfo>();

        public class PublishedWeekInfo
        {
            public DateTime StartDate { get; set; }
            public int TeeTimeCount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(DateTime? startDate = null)
        {
            if (startDate.HasValue)
            {
                WeekStartDate = startDate.Value.Date;
                await LoadTeeSheetDataAsync(WeekStartDate.Value);
            }

            await LoadPublishedWeeksAsync();
            
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
        if (!ModelState.IsValid)
        {
        await LoadPublishedWeeksAsync();
        return Page();
        }

        try
        {
        // Ensure we're working with just the date part, no time component
        DateTime inputDate = StartDate.Date;

        // Round start date to Sunday (beginning of week)
        DateTime weekStartDate = inputDate.DayOfWeek == DayOfWeek.Sunday
        ? inputDate
        : inputDate.AddDays(-(int)inputDate.DayOfWeek);

        // The end date is Saturday (inclusive)
        DateTime weekEndDate = weekStartDate.AddDays(6);

        // Check if tee times already exist for this week
        bool teesExist = await _context.TeeSheets
        .AnyAsync(t => t.Date.Date >= weekStartDate && 
        t.Date.Date <= weekEndDate);

        if (teesExist)
        {
        ModelState.AddModelError(string.Empty, "Tee times already exist for this week. Please select a different week.");
        await LoadPublishedWeeksAsync();
        return Page();
        }

        // Generate tee sheets and tee times for each day of the week (Sunday to Saturday, 7 days)
        for (int day = 0; day < 7; day++)
        {
        // Create a new date object for each day to avoid time component issues
        DateTime currentDate = weekStartDate.Date.AddDays(day);
        DateTime morningStart = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, FirstTeeTime.Hours, FirstTeeTime.Minutes, 0);
                DateTime eveningEnd = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, LastTeeTime.Hours, LastTeeTime.Minutes, 0);

        // Create tee sheet for this day
        var teeSheet = new Models.TeeSheet.TeeSheet
        {
        Date = currentDate,
        StartTime = morningStart,
        EndTime = eveningEnd,
        IntervalMinutes = Interval,
        IsActive = true
                };

        _context.TeeSheets.Add(teeSheet);
        await _context.SaveChangesAsync(); // Save to get the TeeSheet ID

                // Now create tee times
            DateTime currentTime = morningStart;
            while (currentTime <= eveningEnd)
            {
                var teeTime = new Models.TeeSheet.TeeTime
                {
                        StartTime = currentTime,
                    TeeSheetId = teeSheet.Id,
                        IsAvailable = true
                };

                    _context.TeeTimes.Add(teeTime);
                    currentTime = currentTime.AddMinutes(Interval);
            }
        }

            await _context.SaveChangesAsync();
                
            WeekStartDate = weekStartDate;
            await LoadTeeSheetDataAsync(weekStartDate);
            await LoadPublishedWeeksAsync();

            TempData["SuccessMessage"] = $"Tee sheet for week of {weekStartDate:MMMM d, yyyy} has been successfully generated.";

            return Page();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error generating tee sheet: {ex.Message}";
            await LoadPublishedWeeksAsync();
            return Page();
        }
    }

        public async Task<IActionResult> OnPostPublishAsync(DateTime startDate)
        {
            // In a real application, we would mark the tee sheet as published
            // For now, we'll just reload the data
            WeekStartDate = startDate;
            await LoadTeeSheetDataAsync(startDate);
            await LoadPublishedWeeksAsync();

            TempData["SuccessMessage"] = $"Tee sheet for week of {startDate:MMMM d, yyyy} has been published successfully!";
            return Page();
        }

        private async Task LoadTeeSheetDataAsync(DateTime startDate)
        {
        // Clear existing data
        TeeSheets.Clear();

        // Load all tee sheets for the week
        var endDate = startDate.AddDays(7);
        var teeSheets = await _context.TeeSheets
        .Where(ts => ts.Date >= startDate && ts.Date < endDate)
        .Include(ts => ts.TeeTimes)
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
        
            var orderedTeeTimes = teeSheet.TeeTimes.OrderBy(tt => tt.StartTime).ToList();
TeeSheets[date].AddRange(orderedTeeTimes);
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
        
        // Group them by week (Sunday to Saturday)
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
        StartDate = g.Key, // Sunday of the week
            TeeTimeCount = g.Value
        })
        .OrderByDescending(w => w.StartDate)
                .ToList();
    }
    }
}