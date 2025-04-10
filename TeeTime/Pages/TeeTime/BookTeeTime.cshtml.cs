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

        [BindProperty]
        public int ReservationToUpdate { get; set; }

        [BindProperty]
        [Range(1, 4, ErrorMessage = "Number of players must be between 1 and 4")]
        public int UpdatedNumberOfPlayers { get; set; }

        [BindProperty]
        [Range(0, 2, ErrorMessage = "Number of carts must be between 0 and 2")]
        public int UpdatedNumberOfCarts { get; set; }

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

        public async Task<IActionResult> OnGetAsync(DateTime? startDate = null, int? editReservation = null)
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

            // If editReservation parameter is provided, set a flag to show the edit modal
            if (editReservation.HasValue)
            {
                // Find the reservation to get details for populating the edit modal
                var reservationToEdit = UserReservations.FirstOrDefault(r => r.ReservationID == editReservation.Value);
                if (reservationToEdit != null && reservationToEdit.TeeTime != null)
                {
                    // Set TempData to tell the page to show the edit modal
                    TempData["ShowEditModal"] = true;
                    TempData["EditReservationId"] = reservationToEdit.ReservationID;
                    TempData["EditPlayers"] = reservationToEdit.NumberOfPlayers;
                    TempData["EditCarts"] = reservationToEdit.NumberOfCarts;
                    
                    // Format date and time for display
                    var teeTime = reservationToEdit.TeeTime;
                    var displayHour = teeTime.StartTime.Hour > 12 ? teeTime.StartTime.Hour - 12 : (teeTime.StartTime.Hour == 0 ? 12 : teeTime.StartTime.Hour);
                    var timeDisplay = $"{displayHour:D2}:{teeTime.StartTime.Minute:D2} {(teeTime.StartTime.Hour >= 12 ? "PM" : "AM")}";
                    
                    TempData["EditDate"] = teeTime.StartTime.ToString("MMM d, yyyy");
                    TempData["EditTime"] = timeDisplay;
                }
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
            Console.WriteLine("--- OnPostBookTimeAsync started ---");

            // Validate the model state first
            if (!ModelState.IsValid)
            {
                Console.WriteLine("--- OnPostBookTimeAsync: Model state invalid ---");
                // Reload available times if validation fails
                StartDate = SelectedDate.Date;
                await LoadTeeSheetDataAsync(StartDate);
                var currentMemberForValidation = await GetCurrentMemberAsync();
                if (currentMemberForValidation != null)
                {
                    await LoadUserReservationsAsync(currentMemberForValidation.MemberID);
                }
                return Page();
            }

            Console.WriteLine($"--- OnPostBookTimeAsync: Attempting to get current member ---");
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                Console.WriteLine("--- OnPostBookTimeAsync: Member not found, redirecting to login ---");
                return RedirectToPage("/Account/Login");
            }
            Console.WriteLine($"--- OnPostBookTimeAsync: Found member: {member.MemberID} ---");

            Console.WriteLine($"--- OnPostBookTimeAsync: Entering try block for reservation creation ---");
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
                Console.WriteLine($"Error booking tee time in OnPostBookTimeAsync: {ex.Message}");
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

        public async Task<IActionResult> OnPostUpdateReservationAsync(string startDate)
        {
            // Parse start date
            DateTime parsedStartDate;
            if (!DateTime.TryParse(startDate, out parsedStartDate))
            {
                parsedStartDate = DateTime.Today;
            }
            StartDate = parsedStartDate.Date;

            // Get the current member
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                TempData["ErrorMessage"] = "Member not found. Please log in again.";
                return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
            }

            try
            {
                // Validate that the reservation belongs to this member
                var reservation = await _context.Reservations
                    .Include(r => r.TeeTime)
                    .FirstOrDefaultAsync(r => r.ReservationID == ReservationToUpdate);

                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found.";
                    return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
                }

                // Check if this reservation belongs to the current member
                if (reservation.MemberID != member.MemberID)
                {
                    TempData["ErrorMessage"] = "You can only update your own reservations.";
                    return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
                }

                // Check if the reservation is still in the future
                if (reservation.TeeTime?.StartTime <= DateTime.Now)
                {
                    TempData["ErrorMessage"] = "You cannot update a reservation for a past tee time.";
                    return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
                }

                // Check if the status is confirmed
                if (reservation.ReservationStatus != "Confirmed")
                {
                    TempData["ErrorMessage"] = "You can only update confirmed reservations.";
                    return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
                }

                // Calculate the player count difference
                int playerDiff = UpdatedNumberOfPlayers - reservation.NumberOfPlayers;
                
                // Calculate the new total players for the tee time
                int newTotalPlayers = reservation.TeeTime!.TotalPlayersBooked + playerDiff;

                // Check if there's enough space for additional players
                if (playerDiff > 0 && newTotalPlayers > reservation.TeeTime.MaxPlayers)
                {
                    TempData["ErrorMessage"] = "There is not enough space available to add more players to this tee time.";
                    return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
                }

                // Update the reservation details
                reservation.NumberOfPlayers = UpdatedNumberOfPlayers;
                reservation.NumberOfCarts = UpdatedNumberOfCarts;
                
                // Update the tee time total players
                reservation.TeeTime.TotalPlayersBooked = newTotalPlayers;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your reservation has been successfully updated.";
                return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating reservation: {ex.Message}";
                return RedirectToPage("./BookTeeTime", new { startDate = StartDate.ToString("yyyy-MM-dd") });
            }
        }
    }
}
