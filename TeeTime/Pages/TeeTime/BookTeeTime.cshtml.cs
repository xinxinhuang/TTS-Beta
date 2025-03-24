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

        public DateTime MinDate => DateTime.Today;
        public DateTime MaxDate => DateTime.Today.AddDays(14); // Allow booking up to 14 days in advance

        public List<ScheduledGolfTime> AvailableTimes { get; set; } = new List<ScheduledGolfTime>();

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

        public async Task<IActionResult> OnGetAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToPage("/Account/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDateSelectedAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Ensure selected date is within allowed range
            if (SelectedDate < MinDate || SelectedDate > MaxDate)
            {
                ModelState.AddModelError("SelectedDate", "Date must be between today and 14 days from now");
                return Page();
            }

            // Check if the date is fully booked
            var isFullyBooked = await IsDateFullyBookedAsync(SelectedDate);
            if (isFullyBooked)
            {
                ModelState.AddModelError("SelectedDate", "No tee times are available for the selected date. Please select another date.");
                return Page();
            }

            // Get available tee times for the selected date
            AvailableTimes = await GetAvailableTeeTimesAsync(SelectedDate, member);
            DateSelected = true;

            return Page();
        }

        public async Task<IActionResult> OnPostBookTimeAsync()
        {
            if (!ModelState.IsValid)
            {
                DateSelected = true;
                AvailableTimes = await GetAvailableTeeTimesAsync(SelectedDate, await GetCurrentMemberAsync());
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
                DateSelected = true;
                AvailableTimes = await GetAvailableTeeTimesAsync(SelectedDate, member);
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

            _context.Reservations.Add(reservation);
            
            // Update the scheduled time to be unavailable if fully booked
            if (await IsTimeFullyBookedAsync(selectedTime.ScheduledGolfTimeID))
            {
                selectedTime.IsAvailable = false;
                _context.ScheduledGolfTimes.Update(selectedTime);
            }

            await _context.SaveChangesAsync();

            // Set confirmation data
            BookingConfirmed = true;
            ConfirmedTime = selectedTime.ScheduledTime;
            ConfirmationNumber = $"TT{reservation.ReservationID:D6}";
            DateSelected = true;
            AvailableTimes = await GetAvailableTeeTimesAsync(SelectedDate, member);

            return Page();
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
