using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages
{
    [Authorize] // Requires login
    public class TeeTimeBookTeeTimeModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public TeeTimeBookTeeTimeModel(TeeTimeDbContext context)
        {
            _context = context;
            ConfirmationNumber = string.Empty; // Initialize to empty string
        }

        [BindProperty]
        [Required(ErrorMessage = "Please select a date")]
        [DataType(DataType.Date)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        [BindProperty]
        public int SelectedTimeId { get; set; }

        [BindProperty]
        [Range(1, 4, ErrorMessage = "Number of players must be between 1 and 4")]
        public int NumberOfPlayers { get; set; } = 1;

        [BindProperty]
        [Range(0, 2, ErrorMessage = "Number of carts must be between 0 and 2")]
        public int NumberOfCarts { get; set; } = 0;

        public bool DateSelected { get; set; } = false;
        public bool BookingConfirmed { get; set; } = false;
        public TimeSpan ConfirmedTime { get; set; }
        public string ConfirmationNumber { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate => StartDate.AddDays(6);
        
        // Dictionary of date -> list of tee times for that date
        public Dictionary<DateTime, List<ScheduledGolfTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<ScheduledGolfTime>>();
        
        // Dictionary of tee time ID -> event name
        public Dictionary<int, string> Events { get; set; } = new Dictionary<int, string>();

        public List<Reservation> UserReservations { get; set; } = new List<Reservation>();

        [BindProperty]
        public int ReservationToCancel { get; set; }

        private async Task<Member?> GetCurrentMemberAsync()
        {
            // Get the current user ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return null;
            }

            // Find the member associated with this user
            return await _context.Members
                .Include(m => m.MembershipCategory)
                .FirstOrDefaultAsync(m => m.UserID == userId);
        }

        public async Task<IActionResult> OnGetAsync(DateTime? startDate = null)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Set the start date to the provided date or today
            StartDate = startDate?.Date ?? DateTime.Today;
            
            // Load tee sheet data for the week
            await LoadTeeSheetDataAsync(StartDate);
            
            // Load user's existing reservations
            await LoadUserReservationsAsync(member.MemberID);
            
            // Retrieve confirmation data from TempData if it exists
            if (TempData["BookingConfirmed"] != null && TempData["BookingConfirmed"] is bool bookingConfirmed && bookingConfirmed)
            {
                BookingConfirmed = true;
                ConfirmedTime = TempData["ConfirmedTime"] is TimeSpan confirmedTime ? confirmedTime : TimeSpan.Zero;
                ConfirmationNumber = TempData["ConfirmationNumber"]?.ToString() ?? string.Empty;
                NumberOfPlayers = TempData["BookedPlayers"] is int bookedPlayers ? bookedPlayers : 1;
                NumberOfCarts = TempData["BookedCarts"] is int bookedCarts ? bookedCarts : 0;
                SelectedDate = TempData["BookedDate"] is DateTime bookedDate ? bookedDate : DateTime.Today;
            }
            
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
        }

        private async Task LoadUserReservationsAsync(int memberId)
        {
            UserReservations = await _context.Reservations
                .Where(r => r.MemberID == memberId && r.ReservationStatus != "Cancelled")
                .Include(r => r.ScheduledGolfTime)
                .OrderBy(r => r.ScheduledGolfTime.ScheduledDate)
                .ThenBy(r => r.ScheduledGolfTime.ScheduledTime)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostBookTimeAsync()
        {
            if (!ModelState.IsValid)
            {
                StartDate = SelectedDate.Date;
                await LoadTeeSheetDataAsync(StartDate);
                await LoadUserReservationsAsync((await GetCurrentMemberAsync()).MemberID);
                return Page();
            }

            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Verify the selected time is still available
            var selectedTime = await _context.ScheduledGolfTimes.FindAsync(SelectedTimeId);
            if (selectedTime == null || !selectedTime.IsAvailable)
            {
                ModelState.AddModelError("SelectedTimeId", "This tee time is no longer available");
                StartDate = SelectedDate.Date;
                await LoadTeeSheetDataAsync(StartDate);
                await LoadUserReservationsAsync(member.MemberID);
                return Page();
            }

            // Create the reservation
            var reservation = new Reservation
            {
                MemberID = member.MemberID,
                ScheduledGolfTimeID = SelectedTimeId,
                ReservationMadeDate = DateTime.Now,
                ReservationStatus = "Confirmed",
                NumberOfPlayers = NumberOfPlayers,
                NumberOfCarts = NumberOfCarts
            };

            try
            {
                _context.Reservations.Add(reservation);
                
                // Update the scheduled time to be unavailable if fully booked
                if (await IsTimeFullyBookedAsync(selectedTime.ScheduledGolfTimeID))
                {
                    selectedTime.IsAvailable = false;
                    _context.ScheduledGolfTimes.Update(selectedTime);
                }

                await _context.SaveChangesAsync();
                
                // Store confirmation data in TempData so it persists after redirect
                TempData["BookingConfirmed"] = true;
                TempData["ConfirmedTime"] = selectedTime.ScheduledTime;
                TempData["ConfirmationNumber"] = $"TT{reservation.ReservationID:D6}";
                TempData["BookedPlayers"] = NumberOfPlayers;
                TempData["BookedCarts"] = NumberOfCarts;
                TempData["BookedDate"] = selectedTime.ScheduledDate;
                TempData["SuccessMessage"] = $"Your tee time has been successfully booked! Confirmation #: TT{reservation.ReservationID:D6}";
                
                // Redirect to refresh the page completely, ensuring everything is reloaded
                return RedirectToPage(new { startDate = StartDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Database error: {ex.Message}");
                
                // Add error to TempData
                TempData["ErrorMessage"] = $"Failed to book tee time: {ex.Message}";
                
                // Reload available times on failure
                StartDate = SelectedDate.Date;
                await LoadTeeSheetDataAsync(StartDate);
                await LoadUserReservationsAsync(member.MemberID);
                
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCancelReservationAsync(string startDate)
        {
            DateTime startDateTime;
            if (!DateTime.TryParse(startDate, out startDateTime))
            {
                startDateTime = DateTime.Today;
            }
            
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Find the reservation
            var reservation = await _context.Reservations
                .Include(r => r.ScheduledGolfTime)
                .FirstOrDefaultAsync(r => r.ReservationID == ReservationToCancel && r.MemberID == member.MemberID);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found or you don't have permission to cancel it.";
                return RedirectToPage(new { startDate = startDateTime.ToString("yyyy-MM-dd") });
            }

            try
            {
                // Update reservation status
                reservation.ReservationStatus = "Cancelled";
                _context.Reservations.Update(reservation);

                // Update the tee time to be available again
                var teeTime = reservation.ScheduledGolfTime;
                if (teeTime != null && !teeTime.IsAvailable)
                {
                    teeTime.IsAvailable = true;
                    _context.ScheduledGolfTimes.Update(teeTime);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your reservation has been successfully cancelled.";
                
                // Reload user's reservations
                await LoadUserReservationsAsync(member.MemberID);
                
                return RedirectToPage(new { startDate = startDateTime.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Database error during cancellation: {ex.Message}");
                
                // Add error to TempData
                TempData["ErrorMessage"] = $"Failed to cancel reservation: {ex.Message}";
                
                // Reload user's reservations
                await LoadUserReservationsAsync(member.MemberID);
                
                return RedirectToPage(new { startDate = startDateTime.ToString("yyyy-MM-dd") });
            }
        }

        private async Task<List<ScheduledGolfTime>> GetAvailableTeeTimesAsync(DateTime date, Member? member)
        {
            if (member == null)
            {
                return new List<ScheduledGolfTime>();
            }

            // Get all available tee times for the selected date
            var allTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == date.Date && t.IsAvailable)
                .OrderBy(t => t.ScheduledTime)
                .ToListAsync();

            // Filter times based on membership level
            var filteredTimes = FilterTeeTimesByMembershipLevel(allTimes, member.MembershipCategory);

            return filteredTimes;
        }

        private List<ScheduledGolfTime> FilterTeeTimesByMembershipLevel(List<ScheduledGolfTime> allTimes, MembershipCategory? membershipCategory)
        {
            // Apply time restrictions based on membership level
            // This is a simplified implementation - modify as needed based on actual business rules
            
            if (membershipCategory == null)
            {
                return new List<ScheduledGolfTime>();
            }

            // For example, if membership name is "Gold", show all times
            if (membershipCategory.MembershipName == "Gold")
            {
                return allTimes;
            }
            
            // For "Silver", filter out prime time slots on weekends
            else if (membershipCategory.MembershipName == "Silver")
            {
                bool isWeekend = SelectedDate.DayOfWeek == DayOfWeek.Saturday || SelectedDate.DayOfWeek == DayOfWeek.Sunday;
                
                if (isWeekend)
                {
                    // Restrict weekend morning tee times (before noon)
                    return allTimes.Where(t => t.ScheduledTime.Hours >= 12).ToList();
                }
                
                return allTimes;
            }
            
            // For "Bronze", more restrictions
            else if (membershipCategory.MembershipName == "Bronze")
            {
                bool isWeekend = SelectedDate.DayOfWeek == DayOfWeek.Saturday || SelectedDate.DayOfWeek == DayOfWeek.Sunday;
                
                if (isWeekend)
                {
                    // Only allow afternoon tee times on weekends (after 2pm)
                    return allTimes.Where(t => t.ScheduledTime.Hours >= 14).ToList();
                }
                
                // Restrict weekday morning tee times (before 10am)
                return allTimes.Where(t => t.ScheduledTime.Hours >= 10).ToList();
            }
            
            // Default case - if membership doesn't match known categories
            return allTimes;
        }

        private async Task<bool> IsTimeFullyBookedAsync(int scheduledGolfTimeId)
        {
            // This is a simplified check - you may need to consider GolfSessionInterval and other factors
            int bookedPlayers = await _context.Reservations
                .Where(r => r.ScheduledGolfTimeID == scheduledGolfTimeId)
                .SumAsync(r => r.NumberOfPlayers);

            // Max players per tee time (e.g., 4 golfers per tee time)
            return bookedPlayers >= 4;
        }

        private async Task<bool> IsDateFullyBookedAsync(DateTime date)
        {
            // Check if all tee times for the given date are booked
            var teeTimesForDate = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == date.Date)
                .ToListAsync();
            
            // If there are no tee times for this date yet, it's not fully booked
            if (!teeTimesForDate.Any())
                return false;
            
            // Count available tee times
            int availableTeeTimesCount = teeTimesForDate.Count(t => t.IsAvailable);
            
            // If no available tee times, the date is fully booked
            return availableTeeTimesCount == 0;
        }
    }
}
