using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;
using TeeTime.Services;
using System.Security.Claims;

namespace TeeTime.Pages
{
    [Authorize] // Requires login
    public class TeeTimeViewReservationModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ITeeTimeService _teeTimeService;

        public TeeTimeViewReservationModel(TeeTimeDbContext context, ITeeTimeService teeTimeService)
        {
            _context = context;
            _teeTimeService = teeTimeService;
        }

        public Reservation? Reservation { get; set; }
        public Member? Member { get; set; }
        public Models.TeeSheet.TeeTime? TeeTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id, string? returnUrl = null)
        {
            Console.WriteLine($"ViewReservation.OnGetAsync called with id={id}");
            
            // Get the current user ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"User ID from claims: {userIdString}");
            
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                Console.WriteLine("Could not parse user ID from claims");
                return RedirectToPage("/Account/Login");
            }

            // Find the member associated with this user
            var member = await _teeTimeService.GetMemberByUserIdAsync(userId);
            if (member == null)
            {
                Console.WriteLine($"Member not found for user ID {userId}");
                return RedirectToPage("/Account/Login");
            }

            Console.WriteLine($"Found member with ID {member.MemberID}");
            Member = member;

            // Load the reservation with related data
            Reservation = await _context.Reservations
                .Include(r => r.TeeTime)
                .Include(r => r.Member)
                    .ThenInclude(m => m!.User)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (Reservation == null)
            {
                Console.WriteLine($"Reservation with ID {id} not found");
                ErrorMessage = "Reservation not found.";
                return Page();
            }

            Console.WriteLine($"Found reservation: ID={Reservation.ReservationID}, TeeTimeId={Reservation.TeeTimeId}, MemberID={Reservation.MemberID}");

            // Check if this reservation belongs to the current member or if the user is staff
            if (Reservation.MemberID != member.MemberID && 
                !User.IsInRole("Clerk") && 
                !User.IsInRole("Pro Shop Staff") && 
                !User.IsInRole("Committee Member"))
            {
                Console.WriteLine("User does not have permission to view this reservation");
                ErrorMessage = "You do not have permission to view this reservation.";
                return Page();
            }

            TeeTime = Reservation.TeeTime;
            Console.WriteLine($"Tee time details: ID={TeeTime?.Id}, StartTime={TeeTime?.StartTime}, TotalPlayersBooked={TeeTime?.TotalPlayersBooked}");

            // If we got here, everything is fine
            return Page();
        }

        public async Task<IActionResult> OnPostCancelReservationAsync(int id, string? returnUrl = null)
        {
            // Get the current user's member ID
            var member = await _teeTimeService.GetMemberByUserIdAsync(int.Parse(User.FindFirst("UserId")?.Value ?? "0"));
            if (member == null)
            {
                return RedirectToPage("/Account/Login");
            }

            try
            {
                bool success = await _teeTimeService.CancelReservationAsync(id, member.MemberID);
                
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
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToPage("./BookTeeTime");
        }
    }
}
