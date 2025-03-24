using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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

        [BindProperty]
        [Required(ErrorMessage = "First tee time is required")]
        [DataType(DataType.Time)]
        public TimeSpan FirstTeeTime { get; set; } = new TimeSpan(7, 0, 0); // 7:00 AM

        [BindProperty]
        [Required(ErrorMessage = "Last tee time is required")]
        [DataType(DataType.Time)]
        public TimeSpan LastTeeTime { get; set; } = new TimeSpan(18, 0, 0); // 6:00 PM

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
                            IsAvailable = true,
                            IsPublished = false
                        };

                        _context.ScheduledGolfTimes.Add(teeTime);
                        currentTime = currentTime.Add(TimeSpan.FromMinutes(Interval));
                    }
                }

                Console.WriteLine("Saving changes to database");
                await _context.SaveChangesAsync();

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

        [BindProperty]
        public string EventJson { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAddEventAsync()
        {
            try
            {
                // Parse the JSON data from the request
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Get the raw request body
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                
                // Deserialize the JSON
                var eventData = JsonSerializer.Deserialize<EventData>(requestBody, options);

                if (eventData == null || string.IsNullOrWhiteSpace(eventData.EventName) ||
                    string.IsNullOrWhiteSpace(eventData.EventDate) ||
                    string.IsNullOrWhiteSpace(eventData.EventStartTime) ||
                    string.IsNullOrWhiteSpace(eventData.EventEndTime))
                {
                    return new JsonResult(new { success = false, message = "Invalid event data" });
                }

                // Create new event
                var newEvent = new Event
                {
                    EventName = eventData.EventName,
                    EventDate = DateTime.Parse(eventData.EventDate),
                    StartTime = TimeSpan.Parse(eventData.EventStartTime),
                    EndTime = TimeSpan.Parse(eventData.EventEndTime)
                };

                // Add event to database
                _context.Events.Add(newEvent);
                
                // Block all tee times that overlap with this event
                var overlappingTeeTimes = await _context.ScheduledGolfTimes
                    .Where(t => t.ScheduledDate.Date == newEvent.EventDate.Date &&
                               t.ScheduledTime >= newEvent.StartTime &&
                               t.ScheduledTime <= newEvent.EndTime)
                    .ToListAsync();

                foreach (var teeTime in overlappingTeeTimes)
                {
                    teeTime.IsAvailable = false;
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Event added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public class EventData
        {
            public string EventDate { get; set; } = string.Empty;
            public string EventName { get; set; } = string.Empty;
            public string EventStartTime { get; set; } = string.Empty;
            public string EventEndTime { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostDeleteEventAsync(int id)
        {
            try
            {
                // Extract the actual event ID from the format "event_{id}"
                var eventId = id;
                
                // Find the event
                var eventToDelete = await _context.Events.FindAsync(eventId);
                if (eventToDelete == null)
                {
                    return new JsonResult(new { success = false, message = "Event not found" }) { StatusCode = 404 };
                }

                // Find all blocked tee times associated with this event
                var blockedTeeTimes = await _context.ScheduledGolfTimes
                    .Where(t => t.ScheduledDate.Date == eventToDelete.EventDate.Date &&
                               t.ScheduledTime >= eventToDelete.StartTime &&
                               t.ScheduledTime <= eventToDelete.EndTime &&
                               !t.IsAvailable)
                    .ToListAsync();

                // Unblock the tee times
                foreach (var teeTime in blockedTeeTimes)
                {
                    teeTime.IsAvailable = true;
                }

                // Remove the event
                _context.Events.Remove(eventToDelete);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Event deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnPostPublishAsync(DateTime startDate)
        {
            try
            {
                // Find all tee times for the week
                var teeTimes = await _context.ScheduledGolfTimes
                    .Where(t => t.ScheduledDate.Date >= startDate && 
                               t.ScheduledDate.Date < startDate.AddDays(7))
                    .ToListAsync();
                
                if (!teeTimes.Any())
                {
                    TempData["ErrorMessage"] = "No tee times found for the selected week.";
                    await LoadPublishedWeeksAsync();
                    return RedirectToPage();
                }
                
                // Mark all tee times as published
                foreach (var teeTime in teeTimes)
                {
                    teeTime.IsPublished = true;
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Tee sheet for week of {startDate:MMMM d, yyyy} has been published successfully.";
                await LoadPublishedWeeksAsync();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error publishing tee sheet: {ex.Message}";
                await LoadPublishedWeeksAsync();
                return RedirectToPage();
            }
        }

        private async Task LoadTeeSheetDataAsync(DateTime weekStartDate)
        {
            // Load tee times for the week
            var teeTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date >= weekStartDate && 
                           t.ScheduledDate.Date < weekStartDate.AddDays(7))
                .ToListAsync();
            
            // Group tee times by date
            TeeSheets = teeTimes
                .GroupBy(t => t.ScheduledDate.Date)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            // Load events for the week
            var events = await _context.Events
                .Where(e => e.EventDate.Date >= weekStartDate && 
                           e.EventDate.Date < weekStartDate.AddDays(7))
                .ToListAsync();
            
            // Create a mapping of tee time ID to event name
            Events.Clear();
            foreach (var evt in events)
            {
                // Find all tee times that overlap with this event
                var overlappingTeeTimes = teeTimes
                    .Where(t => t.ScheduledDate.Date == evt.EventDate.Date &&
                               t.ScheduledTime >= evt.StartTime &&
                               t.ScheduledTime <= evt.EndTime)
                    .ToList();
                
                foreach (var teeTime in overlappingTeeTimes)
                {
                    Events[teeTime.ScheduledGolfTimeID] = evt.EventName;
                }
            }
        }

        private async Task LoadPublishedWeeksAsync()
        {
            // Get all published tee times
            var publishedTeeTimes = await _context.ScheduledGolfTimes
                .Where(t => t.IsPublished)
                .ToListAsync();
            
            // Group by week start date (Sunday)
            PublishedWeeks = publishedTeeTimes
                .GroupBy(t => t.ScheduledDate.Date.AddDays(-(int)t.ScheduledDate.DayOfWeek)) // Group by week start (Sunday)
                .Select(g => new PublishedWeekInfo
                {
                    StartDate = g.Key,
                    TeeTimeCount = g.Count()
                })
                .ToList();
        }
    }
}
