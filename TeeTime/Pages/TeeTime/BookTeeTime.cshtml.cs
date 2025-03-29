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
        public DateTime ConfirmedTime { get; set; }
        public string ConfirmationNumber { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate => StartDate.AddDays(6);
        
        // Dictionary of date -> list of tee times for that date
        public Dictionary<DateTime, List<Models.TeeSheet.TeeTime>> TeeSheets { get; set; } = new Dictionary<DateTime, List<Models.TeeSheet.TeeTime>>();
        
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
                ConfirmedTime = TempData["ConfirmedTime"] is DateTime confirmedTime ? confirmedTime : DateTime.MinValue;
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
            Events = _teeTimeService.GetEventsForTeeTimesAsync(allTeeTimes);
        }

        private async Task LoadUserReservationsAsync(int memberId)
        {
            UserReservations = await _teeTimeService.GetUserReservationsAsync(memberId);
        }

        public async Task<IActionResult> OnPostBookTimeAsync()
        {
            Console.WriteLine($"OnPostBookTimeAsync called. SelectedTimeId={SelectedTimeId}, NumberOfPlayers={NumberOfPlayers}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid. Errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"- {error.ErrorMessage}");
                }
                
                StartDate = SelectedDate.Date;
                await LoadTeeSheetDataAsync(StartDate);
                var currentMember = await GetCurrentMemberAsync();
                if (currentMember != null)
                {
                    await LoadUserReservationsAsync(currentMember.MemberID);
                }
                return Page();
            }

            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                Console.WriteLine("Member not found. Redirecting to login.");
                return RedirectToPage("/Account/Login");
            }

            Console.WriteLine($"Member found: MemberID={member.MemberID}");

            try
            {
                // Use the service to create the reservation
                Console.WriteLine($"Creating reservation: MemberID={member.MemberID}, TeeTimeId={SelectedTimeId}, Players={NumberOfPlayers}, Carts={NumberOfCarts}");
                var reservation = await _teeTimeService.CreateReservationAsync(
                    member.MemberID, 
                    SelectedTimeId, 
                    NumberOfPlayers, 
                    NumberOfCarts);
                
                Console.WriteLine($"Reservation created successfully: ReservationID={reservation.ReservationID}");
                
                // Get the tee time details for confirmation
                var selectedTime = await _teeTimeService.GetTeeTimeByIdAsync(SelectedTimeId);
                if (selectedTime == null)
                {
                    TempData["ErrorMessage"] = "Could not retrieve tee time details.";
                    return RedirectToPage(new { startDate = StartDate.ToString("yyyy-MM-dd") });
                }
                
                // Store success message in TempData
                TempData["SuccessMessage"] = $"Your tee time has been successfully booked! Confirmation #: TT{reservation.ReservationID:D6}";
                
                Console.WriteLine($"Redirecting to ViewReservation page with id={reservation.ReservationID}");
                
                // Redirect to the ViewReservation page with the reservation ID
                return RedirectToPage("./ViewReservation", new { id = reservation.ReservationID });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error booking tee time: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Add error to TempData
                TempData["ErrorMessage"] = $"Failed to book tee time: {ex.Message}";
                
                // Reload available times on failure
                StartDate = SelectedDate.Date;
                await LoadTeeSheetDataAsync(StartDate);
                var currentMember = await GetCurrentMemberAsync();
                if (currentMember != null)
                {
                    await LoadUserReservationsAsync(currentMember.MemberID);
                }
                
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCancelReservationAsync(string startDate)
        {
            if (ReservationToCancel <= 0)
            {
                TempData["ErrorMessage"] = "Invalid reservation ID.";
                return RedirectToPage(new { startDate = startDate });
            }

            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToPage("/Account/Login");
            }

            try
            {
                bool success = await _teeTimeService.CancelReservationAsync(ReservationToCancel, member.MemberID);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Your tee time has been successfully cancelled.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to cancel the tee time. It may have already been cancelled.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error cancelling reservation: {ex.Message}";
            }

            // Redirect back to the page with the selected date
            return RedirectToPage(new { startDate = startDate });
        }
    }
}
