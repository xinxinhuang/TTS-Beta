using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages.TeeSheet
{
    [Authorize(Roles = "Clerk")]
    public class ManageModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public ManageModel(TeeTimeDbContext context)
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

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime? EventDate { get; set; }

        [BindProperty(SupportsGet = false)]
        [Required(ErrorMessage = "Event name is required when adding an event")]
        public string EventName { get; set; } = string.Empty;

        [BindProperty]
        [DataType(DataType.Time)]
        public TimeSpan? EventStartTime { get; set; }

        [BindProperty]
        [DataType(DataType.Time)]
        public TimeSpan? EventEndTime { get; set; }

        [BindProperty]
        public List<string> AvailableStartTimes { get; set; } = new List<string>();

        [BindProperty]
        public List<string> AvailableEndTimes { get; set; } = new List<string>();

        [BindProperty]
        public string SelectedStartTime { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedEndTime { get; set; } = string.Empty;

        public DateTime? WeekStartDate { get; set; }
        
        // Dictionary of date -> list of tee times for that date
        public Dictionary<DateTime, List<ScheduledGolfTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<ScheduledGolfTime>>();
        
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
            // Add this to track if the handler is being called
            Console.WriteLine("Generate handler called.");
            
            // Clear validation errors for event form fields
            ModelState.Remove("EventName");
            ModelState.Remove("EventDate");
            ModelState.Remove("EventStartTime");
            ModelState.Remove("EventEndTime");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
                
                await LoadPublishedWeeksAsync();
                return Page();
            }

            // Round start date to Sunday (beginning of week)
            try
            {
                DateTime weekStartDate = StartDate.Date;
                Console.WriteLine($"StartDate: {weekStartDate}");

                if (weekStartDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    weekStartDate = weekStartDate.AddDays(-(int)weekStartDate.DayOfWeek);
                    Console.WriteLine($"Adjusted to Sunday: {weekStartDate}");
                }

                // Check if tee times already exist for this week
                bool teesExist = await _context.ScheduledGolfTimes
                    .AnyAsync(t => t.ScheduledDate.Date >= weekStartDate && 
                                   t.ScheduledDate.Date < weekStartDate.AddDays(7));

                Console.WriteLine($"Tee times exist for this week: {teesExist}");

                if (teesExist)
                {
                    ModelState.AddModelError(string.Empty, "Tee times already exist for this week. Please select a different week.");
                    await LoadPublishedWeeksAsync();
                    return Page();
                }

                // Generate tee times for each day of the week
                for (int day = 0; day < 7; day++)
                {
                    DateTime currentDate = weekStartDate.AddDays(day);
                    TimeSpan currentTime = FirstTeeTime;

                    Console.WriteLine($"Generating tee times for {currentDate.ToShortDateString()}");

                    while (currentTime <= LastTeeTime)
                    {
                        var teeTime = new ScheduledGolfTime
                        {
                            ScheduledDate = currentDate,
                            ScheduledTime = currentTime,
                            GolfSessionInterval = Interval,
                            IsAvailable = true
                        };

                        _context.ScheduledGolfTimes.Add(teeTime);
                        currentTime = currentTime.Add(TimeSpan.FromMinutes(Interval));
                    }
                }

                Console.WriteLine("Saving changes to database");
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Event '{EventName}' has been added successfully.";

                WeekStartDate = weekStartDate;
                await LoadTeeSheetDataAsync(weekStartDate);
                await LoadPublishedWeeksAsync();

                Console.WriteLine("Tee sheet generation completed successfully");
                TempData["SuccessMessage"] = $"Tee sheet for week of {weekStartDate:MMMM d, yyyy} has been successfully generated.";

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating tee sheet: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, $"Error generating tee sheet: {ex.Message}");
                await LoadPublishedWeeksAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAddEventAsync(DateTime selectedWeekStart)
        {
            WeekStartDate = selectedWeekStart;

            if (!EventDate.HasValue || string.IsNullOrWhiteSpace(EventName) || 
                string.IsNullOrWhiteSpace(SelectedStartTime) || string.IsNullOrWhiteSpace(SelectedEndTime))
            {
                ModelState.AddModelError(string.Empty, "All event fields are required");
                TempData["ErrorMessage"] = "Please fill in all event fields.";
                await LoadPublishedWeeksAsync();
                return Page();
            }
            
            // Parse selected times
            if (!TimeSpan.TryParse(SelectedStartTime, out var startTime) || 
                !TimeSpan.TryParse(SelectedEndTime, out var endTime))
            {
                ModelState.AddModelError(string.Empty, "Invalid time format");
                TempData["ErrorMessage"] = "Please select valid start and end times.";
                await LoadPublishedWeeksAsync();
                return Page();
            }
            
            // Ensure end time is after start time
            if (endTime <= startTime)
            {
                ModelState.AddModelError(string.Empty, "End time must be after start time");
                TempData["ErrorMessage"] = "End time must be after start time.";
                await LoadPublishedWeeksAsync();
                return Page();
            }

            var teeTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == EventDate.Value.Date &&
                           t.ScheduledTime >= startTime &&
                           t.ScheduledTime <= endTime)
                .ToListAsync();

            if (!teeTimes.Any())
            {
                ModelState.AddModelError(string.Empty, "No tee times found in the specified time range");
                await LoadPublishedWeeksAsync();
                return Page();
            }
            
            // Create the event
            var newEvent = new Event
            {
                EventName = EventName,
                EventDate = EventDate.Value.Date,
                StartTime = startTime,
                EndTime = endTime
            };
            
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            // Block all the tee times and assign them to the event
            foreach (var teeTime in teeTimes)
            {
                teeTime.IsAvailable = false;
                teeTime.EventID = newEvent.EventID;
                _context.ScheduledGolfTimes.Update(teeTime);
            }

            await _context.SaveChangesAsync();
            
            // Get the week that contains this event
            DateTime weekStartDate = EventDate.Value.Date;
            if (weekStartDate.DayOfWeek != DayOfWeek.Sunday)
            {
                weekStartDate = weekStartDate.AddDays(-(int)weekStartDate.DayOfWeek);
            }
            
            TempData["SuccessMessage"] = $"Event '{EventName}' has been added successfully.";

            WeekStartDate = weekStartDate;
            await LoadTeeSheetDataAsync(weekStartDate);
            await LoadPublishedWeeksAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostBlockAsync(int teeTimeId)
        {
            var teeTime = await _context.ScheduledGolfTimes.FindAsync(teeTimeId);
            if (teeTime != null)
            {
                teeTime.IsAvailable = false;
                _context.ScheduledGolfTimes.Update(teeTime);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tee time at {teeTime.ScheduledTime.Hours.ToString("00")}:{teeTime.ScheduledTime.Minutes.ToString("00")} has been blocked successfully.";
            }

            DateTime weekStartDate = teeTime?.ScheduledDate.Date ?? DateTime.Today;
            if (weekStartDate.DayOfWeek != DayOfWeek.Sunday)
            {
                weekStartDate = weekStartDate.AddDays(-(int)weekStartDate.DayOfWeek);
            }

            WeekStartDate = weekStartDate;
            await LoadTeeSheetDataAsync(weekStartDate);
            await LoadPublishedWeeksAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUnblockAsync(int teeTimeId)
        {
            var teeTime = await _context.ScheduledGolfTimes.FindAsync(teeTimeId);
            if (teeTime != null)
            {
                teeTime.IsAvailable = true;
                _context.ScheduledGolfTimes.Update(teeTime);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tee time at {teeTime.ScheduledTime.Hours.ToString("00")}:{teeTime.ScheduledTime.Minutes.ToString("00")} has been unblocked successfully.";
            }

            DateTime weekStartDate = teeTime?.ScheduledDate.Date ?? DateTime.Today;
            if (weekStartDate.DayOfWeek != DayOfWeek.Sunday)
            {
                weekStartDate = weekStartDate.AddDays(-(int)weekStartDate.DayOfWeek);
            }

            WeekStartDate = weekStartDate;
            await LoadTeeSheetDataAsync(weekStartDate);
            await LoadPublishedWeeksAsync();

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

        public async Task<IActionResult> OnGetAvailableTimesAsync(DateTime date)
        {
            var teeTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == date.Date && t.IsAvailable)
                .OrderBy(t => t.ScheduledTime)
                .Select(t => new { Time = t.ScheduledTime.ToString(@"hh\:mm") })
                .ToListAsync();
                
            return new JsonResult(teeTimes);
        }

        private async Task LoadTeeSheetDataAsync(DateTime startDate)
        {
            // Clear existing data
            TeeSheets.Clear();
            Events.Clear();

            // Load all tee times for the week
            var endDate = startDate.AddDays(7);
            var teeTimes = await _context.ScheduledGolfTimes
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
                if (!teeTime.IsAvailable)
                {
                    Events[teeTime.ScheduledGolfTimeID] = "Special Event";
                }
            }
        }

        private async Task LoadPublishedWeeksAsync()
        {
            // In a real application, we would load published weeks from the database
            // For now, we'll just get weeks that have tee times
            var teeTimeGroups = await _context.ScheduledGolfTimes
                .GroupBy(t => EF.Functions.DateDiffDay(new DateTime(1, 1, 1), t.ScheduledDate) / 7)
                .Select(g => new
                {
                    WeekGroup = g.Key,
                    MinDate = g.Min(t => t.ScheduledDate),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.MinDate)
                .ToListAsync();

            PublishedWeeks = teeTimeGroups.Select(g => new PublishedWeekInfo
            {
                StartDate = g.MinDate.Date,
                TeeTimeCount = g.Count
            }).ToList();
        }
    }
}
