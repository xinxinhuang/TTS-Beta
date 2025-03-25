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
using TeeTime.Services;

namespace TeeTime.Pages
{
    [Authorize] // Requires login
    public class TeeTimeBookTeeTimeModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ITeeTimeService _teeTimeService;

        public TeeTimeBookTeeTimeModel(TeeTimeDbContext context, ITeeTimeService teeTimeService)
        {
            _context = context;
            _teeTimeService = teeTimeService;
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
            return await _teeTimeService.GetMemberByUserIdAsync(userId);
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
            TeeSheets = await _teeTimeService.GetTeeSheetDataAsync(startDate, endDate);
            
            // Get events for all tee times
            var allTeeTimes = TeeSheets.Values.SelectMany(list => list).ToList();
            Events = await _teeTimeService.GetEventsForTeeTimesAsync(allTeeTimes);
        }

        private async Task LoadUserReservationsAsync(int memberId)
        {
            UserReservations = await _teeTimeService.GetUserReservationsAsync(memberId);
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

            try
            {
                // Use the service to create the reservation
                var reservation = await _teeTimeService.CreateReservationAsync(
                    member.MemberID, 
                    SelectedTimeId, 
                    NumberOfPlayers, 
                    NumberOfCarts);
                
                // Get the tee time details for confirmation
                var selectedTime = await _teeTimeService.GetTeeTimeByIdAsync(SelectedTimeId);
                
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

            try
            {
                // Use the service to cancel the reservation
                await _teeTimeService.CancelReservationAsync(ReservationToCancel, member.MemberID);
                
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

            return await _teeTimeService.GetAvailableTeeTimesAsync(date, member);
        }

        private async Task<bool> IsTimeFullyBookedAsync(int scheduledGolfTimeId)
        {
            return await _teeTimeService.IsTimeFullyBookedAsync(scheduledGolfTimeId);
        }

        private async Task<bool> IsDateFullyBookedAsync(DateTime date)
        {
            return await _teeTimeService.IsDateFullyBookedAsync(date);
        }
    }
}
