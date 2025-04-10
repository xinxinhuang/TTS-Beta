using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;
using TeeTime.Services;

namespace TeeTime.Pages.Admin
{
    [Authorize(Roles = "Clerk,Pro Shop Staff,Committee")]
    public class BookForMemberModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ITeeTimeService _teeTimeService;
        
        public BookForMemberModel(TeeTimeDbContext context, ITeeTimeService teeTimeService)
        {
            _context = context;
            _teeTimeService = teeTimeService;
        }
        
        [BindProperty]
        [Required(ErrorMessage = "Please select a member")]
        public int SelectedMemberId { get; set; }
        
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
        
        public List<SelectListItem> MembersList { get; set; } = new();
        public DateTime StartDate { get; set; } = DateTime.Today;
        public Dictionary<DateTime, List<Models.TeeSheet.TeeTime>> TeeSheets { get; set; } = new();
        public Dictionary<int, string> Events { get; set; } = new();
        
        public async Task<IActionResult> OnGetAsync(int? memberId = null, DateTime? date = null)
        {
            // Load members for dropdown
            await LoadMembersListAsync();
            
            // Set selected member if provided
            if (memberId.HasValue)
            {
                SelectedMemberId = memberId.Value;
            }
            
            // Load tee sheet data
            StartDate = date ?? DateTime.Today;
            SelectedDate = StartDate;
            await LoadTeeSheetDataAsync(StartDate);
            
            return Page();
        }
        
        private async Task LoadMembersListAsync()
        {
            var members = await _context.Members
                .Include(m => m.User)
                .Include(m => m.MembershipCategory)
                .Where(m => m.User != null)
                .OrderBy(m => m.User!.LastName)
                .ThenBy(m => m.User!.FirstName)
                .ToListAsync();
                
            MembersList = members.Select(m => new SelectListItem
            {
                Value = m.MemberID.ToString(),
                Text = $"{m.User?.LastName ?? "Unknown"}, {m.User?.FirstName ?? "Unknown"} - {m.MembershipCategory?.MembershipName ?? "Unknown"}"
            }).ToList();
            
            // Add an empty item at the beginning
            MembersList.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Select Member --",
                Selected = true
            });
        }
        
        private async Task LoadTeeSheetDataAsync(DateTime startDate)
        {
            // Load all tee times for the week
            var endDate = startDate.AddDays(7);
            TeeSheets = await _teeTimeService.GetTeeSheetDataAsync(startDate, endDate);
            
            // Get events for all tee times
            var allTeeTimes = TeeSheets.Values.SelectMany(list => list).ToList();
            Events = _teeTimeService.GetEventsForTeeTimesAsync(allTeeTimes);
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || SelectedMemberId <= 0 || SelectedTimeId <= 0)
            {
                await LoadMembersListAsync();
                await LoadTeeSheetDataAsync(StartDate);
                return Page();
            }
            
            try
            {
                // Use the service to create the reservation
                var reservation = await _teeTimeService.CreateReservationAsync(
                    SelectedMemberId, 
                    SelectedTimeId, 
                    NumberOfPlayers, 
                    NumberOfCarts);
                
                // Get member details for the success message
                var member = await _context.Members
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.MemberID == SelectedMemberId);
                
                string memberName = member?.User != null 
                    ? $"{member.User.FirstName} {member.User.LastName}"
                    : "Member";
                
                // Store success message in TempData
                TempData["SuccessMessage"] = $"Tee time successfully booked for {memberName}. Confirmation #: TT{reservation.ReservationID:D6}";
                
                // Redirect to the ManageReservations page
                return RedirectToPage("./ManageReservations");
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error booking tee time: {ex.Message}");
                
                // Add error to ModelState
                ModelState.AddModelError("", $"Failed to book tee time: {ex.Message}");
                
                // Reload data for the view
                await LoadMembersListAsync();
                await LoadTeeSheetDataAsync(StartDate);
                
                return Page();
            }
        }
    }
} 