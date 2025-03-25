using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
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
        
        // List of all events
        public List<Event> AllEvents { get; set; } = new List<Event>();
        
        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime? EventDate { get; set; }

        [BindProperty(SupportsGet = false)]
        [Required(ErrorMessage = "Event name is required when adding an event")]
        public string EventName { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedStartTime { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedEndTime { get; set; } = string.Empty;

        [BindProperty]
        public string EventColor { get; set; } = "blue";

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
                        .ThenInclude(m => m.User!)
                .Include(t => t.Event)
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

                // Store event names in the dictionary for easy access
                if (!teeTime.IsAvailable && teeTime.EventID.HasValue)
                {
                    Events[teeTime.ScheduledGolfTimeID] = teeTime.Event?.EventName ?? "Special Event";
                }
                else if (!teeTime.IsAvailable && teeTime.Reservations != null && !teeTime.Reservations.Any())
                {
                    Events[teeTime.ScheduledGolfTimeID] = "Blocked";
                }
            }
            
            // Load all events
            AllEvents = await _context.Events
                .Where(e => e.EventDate >= startDate && e.EventDate < endDate)
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();
        }
        
        public async Task<IActionResult> OnPostAddEventAsync(string startDate)
        {
            // Parse the start date parameter
            DateTime startDateTime;
            if (!DateTime.TryParse(startDate, out startDateTime))
            {
                // Default to today if parsing fails
                startDateTime = DateTime.Today;
            }
            
            // Set the StartDate for page rendering
            StartDate = startDateTime.Date;
            
            if (!ModelState.IsValid || !EventDate.HasValue)
            {
                TempData["ErrorMessage"] = "Please select a valid date and provide an event name.";
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }
            
            // Parse selected times
            if (!TimeSpan.TryParse(SelectedStartTime, out var startTime) || 
                !TimeSpan.TryParse(SelectedEndTime, out var endTime))
            {
                ModelState.AddModelError(string.Empty, "Invalid time format");
                TempData["ErrorMessage"] = "Please select valid start and end times.";
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }
            
            // Ensure end time is after start time
            if (endTime <= startTime)
            {
                ModelState.AddModelError(string.Empty, "End time must be after start time");
                TempData["ErrorMessage"] = "End time must be after start time.";
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }

            // Use the date part only to avoid any time zone issues
            DateTime eventDateValue = EventDate.Value.Date;

            var teeTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == eventDateValue &&
                           t.ScheduledTime >= startTime &&
                           t.ScheduledTime <= endTime)
                .ToListAsync();

            if (!teeTimes.Any())
            {
                ModelState.AddModelError(string.Empty, "No tee times found in the specified time range");
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }
            
            // Create the event
            var newEvent = new Event
            {
                EventName = EventName,
                EventDate = eventDateValue,
                StartTime = startTime,
                EndTime = endTime,
                EventColor = EventColor
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
            
            TempData["SuccessMessage"] = $"Event '{EventName}' has been added successfully.";
            
            // Redirect back to the same view with the original startDate
            return RedirectToPage("./View", new { startDate = startDate });
        }
        
        public async Task<IActionResult> OnPostCreateEventAsync(string startDate)
        {
            // Parse the start date parameter
            DateTime startDateTime;
            if (!DateTime.TryParse(startDate, out startDateTime))
            {
                // Default to today if parsing fails
                startDateTime = DateTime.Today;
            }
            
            // Set the StartDate for page rendering
            StartDate = startDateTime.Date;
            
            if (!ModelState.IsValid || !EventDate.HasValue)
            {
                TempData["ErrorMessage"] = "Please select a valid date and provide an event name.";
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }
            
            // Parse selected times
            if (!TimeSpan.TryParse(SelectedStartTime, out var startTime) || 
                !TimeSpan.TryParse(SelectedEndTime, out var endTime))
            {
                ModelState.AddModelError(string.Empty, "Invalid time format");
                TempData["ErrorMessage"] = "Please select valid start and end times.";
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }
            
            // Ensure end time is after start time
            if (endTime <= startTime)
            {
                ModelState.AddModelError(string.Empty, "End time must be after start time");
                TempData["ErrorMessage"] = "End time must be after start time.";
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }

            // Use the date part only to avoid any time zone issues
            DateTime eventDateValue = EventDate.Value.Date;

            var teeTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == eventDateValue &&
                           t.ScheduledTime >= startTime &&
                           t.ScheduledTime <= endTime)
                .ToListAsync();

            if (!teeTimes.Any())
            {
                ModelState.AddModelError(string.Empty, "No tee times found in the specified time range");
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }
            
            // Create the event
            var newEvent = new Event
            {
                EventName = EventName,
                EventDate = eventDateValue,
                StartTime = startTime,
                EndTime = endTime,
                EventColor = EventColor
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
            
            TempData["SuccessMessage"] = $"Event '{EventName}' has been added successfully.";
            
            // Redirect back to the same view with the original startDate
            return RedirectToPage("./View", new { startDate = startDate });
        }
        
        public async Task<IActionResult> OnPostDeleteEventAsync(int eventId, string startDate)
        {
            // Parse the start date from the string parameter
            DateTime startDateTime;
            if (!DateTime.TryParse(startDate, out startDateTime))
            {
                // Default to today if parsing fails
                startDateTime = DateTime.Today;
            }
            
            var eventToDelete = await _context.Events
                .Include(e => e.ScheduledGolfTimes)
                .FirstOrDefaultAsync(e => e.EventID == eventId);
                
            if (eventToDelete == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToPage("./View", new { startDate = startDate });
            }
            
            try
            {
                // Make the associated tee times available again
                foreach (var teeTime in eventToDelete.ScheduledGolfTimes)
                {
                    teeTime.IsAvailable = true;
                    teeTime.EventID = null;
                }
                
                // Remove the event
                _context.Events.Remove(eventToDelete);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Event '{eventToDelete.EventName}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting event: {ex.Message}";
            }
            
            // Redirect back to the same page with the original startDate string
            return RedirectToPage("./View", new { startDate = startDate });
        }
        
        // Method to get available tee times for a specific date
        public async Task<IActionResult> OnGetAvailableTimesAsync(DateTime date)
        {
            // Ensure user has proper permissions (already handled by Authorize attribute)
            var teeTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == date.Date && t.IsAvailable)
                .OrderBy(t => t.ScheduledTime)
                .Select(t => new { 
                    Time = t.ScheduledTime.Hours.ToString("00") + ":" + t.ScheduledTime.Minutes.ToString("00") 
                })
                .ToListAsync();
                
            return new JsonResult(teeTimes);
        }
    }
}
