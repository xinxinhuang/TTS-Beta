using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TeeTime.Data;
using TeeTime.Models;
using TeeTime.Models.TeeSheet;
using TeeTime.Services;
using Microsoft.Extensions.Logging;

namespace TeeTime.Pages.TeeSheet
{
    [Authorize(Roles = "Clerk")]
    public class ScheduleTeeSheetModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly TeeSheetService _teeSheetService;
        private readonly ILogger<ScheduleTeeSheetModel> _logger;

        public ScheduleTeeSheetModel(TeeTimeDbContext context, TeeSheetService teeSheetService, ILogger<ScheduleTeeSheetModel> logger)
        {
            _context = context;
            _teeSheetService = teeSheetService;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek); // Default to next week's Sunday

        // Fixed tee time values - not editable by user
        private TimeSpan FirstTeeTime { get; } = new TimeSpan(7, 0, 0); // 7:00 AM
        private TimeSpan LastTeeTime { get; } = new TimeSpan(18, 0, 0); // 6:00 PM

        // Fixed intervals at: 00, 07, 15, 22, 30, 37, 45, 52 minutes
        private readonly int[] _fixedIntervals = { 0, 7, 15, 22, 30, 37, 45, 52 };

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

            // Determine week start/end dates
            DateTime inputDate = StartDate.Date;
            DateTime weekStartDate = inputDate.DayOfWeek == DayOfWeek.Sunday
                ? inputDate
                : inputDate.AddDays(-(int)inputDate.DayOfWeek);
            DateTime weekEndDate = weekStartDate.AddDays(6);

            _logger.LogInformation("Request received to generate tee sheet for week starting: {WeekStartDate}", weekStartDate);

            // Check if ANY tee sheet exists for this week (can be refined if needed)
            bool weekExists = await _context.TeeSheets
                .AnyAsync(t => t.Date.Date >= weekStartDate && t.Date.Date <= weekEndDate);

            if (weekExists)
            {
                ModelState.AddModelError(string.Empty, "Tee times already exist for this week. Please select a different week.");
                _logger.LogWarning("Generation cancelled: Tee sheet already exists for week starting {WeekStartDate}", weekStartDate);
                await LoadPublishedWeeksAsync();
                return Page();
            }

            int daysGenerated = 0;
            List<string> errors = new List<string>();

            // Generate tee sheets for each day using the service
            for (int day = 0; day < 7; day++)
            {
                DateTime currentDate = weekStartDate.Date.AddDays(day);
                try
                {
                    // Call the service to generate the sheet for the current day
                    // This now includes standing requests and standard slots
                    await _teeSheetService.GenerateTeeSheetWithStandingRequestsAsync(currentDate);
                    daysGenerated++;
                }
                catch (InvalidOperationException ex) // Service throws this if sheet for the day exists
                {
                    // This specific day might already exist if generation was interrupted
                    _logger.LogWarning("Skipping generation for {CurrentDate}: {ErrorMessage}", currentDate, ex.Message);
                    // Continue to the next day
                }
                catch (Exception ex)
                {
                    // Log critical error for this specific day and stop or collect errors
                    _logger.LogError(ex, "Failed to generate tee sheet for {CurrentDate}", currentDate);
                    errors.Add($"Error generating sheet for {currentDate:MM/dd}: {ex.Message}");
                    // Depending on requirements, you might want to break the loop here
                    // or attempt to continue with other days.
                    // For now, let's collect errors and report after.
                }
            }

            // --- Post-Generation Handling --- 
            WeekStartDate = weekStartDate;
            await LoadTeeSheetDataAsync(weekStartDate); // Reload data to show the new sheets
            await LoadPublishedWeeksAsync();

            if (errors.Any())
            {
                TempData["ErrorMessage"] = $"Tee sheet generation completed with errors: {string.Join("; ", errors)}";
            }
            else if (daysGenerated > 0)
            {
                TempData["SuccessMessage"] = $"Tee sheet for week of {weekStartDate:MMMM d, yyyy} ({daysGenerated} days) has been successfully generated.";
            }
            else
            {
                TempData["InfoMessage"] = "No new tee sheets were generated (they may have already existed).";
            }

            return Page();
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