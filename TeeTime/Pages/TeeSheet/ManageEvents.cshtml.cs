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
    public class ManageEventsModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public ManageEventsModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        public DateTime StartDate { get; set; }
        
        // Dictionary of date -> list of tee times for that date
        public Dictionary<DateTime, List<Models.TeeSheet.TeeTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<Models.TeeSheet.TeeTime>>();
        
        // List of all events for the week
        public List<Event> AllEvents { get; set; } = new List<Event>();
        
        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime? EventDate { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Event name is required")]
        public string EventName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Start time is required")]
        public string SelectedStartTime { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "End time is required")]
        public string SelectedEndTime { get; set; } = string.Empty;

        [BindProperty]
        public string EventColor { get; set; } = "blue";

        public async Task<IActionResult> OnGetAsync(DateTime? startDate = null)
        {
            // Default to current week's Sunday if no date provided
            StartDate = startDate?.Date ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            
            await LoadTeeSheetDataAsync(StartDate);
            await LoadEventsAsync(StartDate);
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostCreateEventAsync(string startDate)
        {
            // Parse the start date parameter
            DateTime startDateTime;
            if (!DateTime.TryParse(startDate, out startDateTime))
            {
                // Default to current week's Sunday if parsing fails
                startDateTime = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            }
            
            // Set the StartDate for page rendering
            StartDate = startDateTime.Date;
            
            // Validate required fields
            if (!ModelState.IsValid || !EventDate.HasValue)
            {
                TempData["ErrorMessage"] = "Please select a valid date and provide an event name.";
                await LoadTeeSheetDataAsync(StartDate);
                await LoadEventsAsync(StartDate);
                return Page();
            }
            
            // Parse selected times
            if (!TimeSpan.TryParse(SelectedStartTime, out var startTimeSpan) || 
                !TimeSpan.TryParse(SelectedEndTime, out var endTimeSpan))
            {
                TempData["ErrorMessage"] = "Please select valid start and end times.";
                await LoadTeeSheetDataAsync(StartDate);
                await LoadEventsAsync(StartDate);
                return Page();
            }
            
            // Ensure end time is after start time
            if (endTimeSpan <= startTimeSpan)
            {
                TempData["ErrorMessage"] = "End time must be after start time.";
                await LoadTeeSheetDataAsync(StartDate);
                await LoadEventsAsync(StartDate);
                return Page();
            }

            // Use the date part only to avoid any time zone issues
            DateTime eventDateValue = EventDate.Value.Date;
            
            // Convert TimeSpans to DateTimes for comparison
            DateTime startTime = eventDateValue.Add(startTimeSpan);
            DateTime endTime = eventDateValue.Add(endTimeSpan);

            // Find the tee sheet for the selected date
            var teeSheet = await _context.TeeSheets
                .Include(ts => ts.TeeTimes)
                .FirstOrDefaultAsync(ts => ts.Date.Date == eventDateValue.Date);
                
            if (teeSheet == null)
            {
                TempData["ErrorMessage"] = "No tee sheet found for the selected date.";
                await LoadTeeSheetDataAsync(StartDate);
                await LoadEventsAsync(StartDate);
                return Page();
            }

            // Find all tee times in the selected time range
            var teeTimes = teeSheet.TeeTimes
                .Where(tt => tt.StartTime >= startTime && tt.StartTime <= endTime)
                .ToList();

            if (!teeTimes.Any())
            {
                TempData["ErrorMessage"] = "No tee times found in the specified time range.";
                await LoadTeeSheetDataAsync(StartDate);
                await LoadEventsAsync(StartDate);
                return Page();
            }
            
            // Check if any of the tee times are already blocked or reserved
            var unavailableTeeTime = teeTimes.FirstOrDefault(t => !t.IsAvailable || t.ReservationId.HasValue);
            if (unavailableTeeTime != null)
            {
                TempData["ErrorMessage"] = $"Some tee times in this range are already blocked or reserved.";
                await LoadTeeSheetDataAsync(StartDate);
                await LoadEventsAsync(StartDate);
                return Page();
            }
            
            // Block all the tee times for the event
            foreach (var teeTime in teeTimes)
            {
                teeTime.IsAvailable = false;
                teeTime.Notes = $"{EventName} ({EventColor})";
                _context.TeeTimes.Update(teeTime);
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Event '{EventName}' has been added successfully.";
            
            // Redirect back to the same view with the original startDate
            return RedirectToPage("./ManageEvents", new { startDate = startDate });
        }
        
        public async Task<IActionResult> OnPostDeleteEventAsync(int eventId, string startDate)
        {
            // Parse the start date parameter
            DateTime startDateTime;
            if (!DateTime.TryParse(startDate, out startDateTime))
            {
                // Default to current week's Sunday if parsing fails
                startDateTime = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            }
            
            // Set the StartDate for page rendering
            StartDate = startDateTime.Date;
            
            try
            {
                // Find all tee times with this event in their notes
var teeTimesToUpdate = await _context.TeeTimes
                .Where(tt => tt.Notes.Contains($"({EventColor})") && !tt.IsAvailable)
                .ToListAsync();
                    
                if (teeTimesToUpdate.Any())
                {
                    // Make the associated tee times available again
                    foreach (var teeTime in teeTimesToUpdate)
                    {
                        teeTime.IsAvailable = true;
                        teeTime.Notes = string.Empty;
                        _context.TeeTimes.Update(teeTime);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Event deleted successfully and {teeTimesToUpdate.Count} tee times unblocked.";
                }
                else
                {
                    TempData["WarningMessage"] = "No tee times found for this event.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting event: {ex.Message}";
            }
            
            // Redirect back to the same page with the original startDate string
            return RedirectToPage("./ManageEvents", new { startDate = startDate });
        }
        
        // Method to get available tee times for a specific date
        public async Task<IActionResult> OnGetAvailableTimesAsync(DateTime date)
        {
            // Ensure user has proper permissions (already handled by Authorize attribute)
            var teeSheet = await _context.TeeSheets
                .Include(ts => ts.TeeTimes)
                .FirstOrDefaultAsync(ts => ts.Date.Date == date.Date);
                
            if (teeSheet == null)
            {
                return new JsonResult(new List<object>());
            }
                
            var availableTimes = teeSheet.TeeTimes
                .Where(tt => tt.IsAvailable)
                .OrderBy(tt => tt.StartTime)
                .Select(tt => new { 
                    Time = tt.StartTime.ToString("HH:mm") 
                })
                .ToList();
                
            return new JsonResult(availableTimes);
        }
        
        private async Task LoadTeeSheetDataAsync(DateTime startDate)
        {
            // Clear existing data
            TeeSheets.Clear();

            // Calculate week range (Sunday to Saturday)
            var endDate = startDate.AddDays(7);
            
            // Load all tee sheets for the week
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
                
                // Add all tee times for this sheet
                var orderedTeeTimes = teeSheet.TeeTimes.OrderBy(tt => tt.StartTime).ToList();
                TeeSheets[date].AddRange(orderedTeeTimes);
            }
        }
        
        private async Task LoadEventsAsync(DateTime startDate)
        {
            // Clear existing data
            AllEvents.Clear();
            
            // Calculate week range (Sunday to Saturday)
            var endDate = startDate.AddDays(7);
            
            // Get distinct event names from tee time notes
            var eventTeeTimes = await _context.TeeTimes
                .Where(tt => tt.TeeSheet.Date >= startDate && 
                            tt.TeeSheet.Date < endDate && 
                            !tt.IsAvailable && 
                            !string.IsNullOrEmpty(tt.Notes) &&
                            tt.Notes.Contains("(")
                )
                .OrderBy(tt => tt.StartTime)
                .ToListAsync();
                
            // Group by event name
            var eventGroups = eventTeeTimes.GroupBy(tt => {
                // Extract event name from notes (format: "Event Name (color)") 
                var note = tt.Notes;
                var parenIndex = note.LastIndexOf('(');
                return parenIndex > 0 ? note.Substring(0, parenIndex).Trim() : note;
            }).ToList();
            
            // Create Event objects for UI display
            foreach (var group in eventGroups)
            {   
                var firstTeeTime = group.First();
                var lastTeeTime = group.Last();
                var colorStart = firstTeeTime.Notes.LastIndexOf('(') + 1;
                var colorEnd = firstTeeTime.Notes.LastIndexOf(')');
                var color = "blue";
                
                if (colorStart > 0 && colorEnd > colorStart)
                {
                    color = firstTeeTime.Notes.Substring(colorStart, colorEnd - colorStart);
                }
                
                AllEvents.Add(new Event {
                    EventID = group.Key.GetHashCode(), // Generate a pseudoId from the name
                    EventName = group.Key,
                    EventDate = firstTeeTime.TeeSheet.Date,
                    StartTime = firstTeeTime.StartTime.TimeOfDay,
                    EndTime = lastTeeTime.StartTime.TimeOfDay,
                    EventColor = color
                });
            }
        }
    }
}