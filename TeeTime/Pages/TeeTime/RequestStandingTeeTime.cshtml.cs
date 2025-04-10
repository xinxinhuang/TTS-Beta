using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    [Authorize]
    public class TeeTimeRequestStandingTeeTimeModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ILogger<TeeTimeRequestStandingTeeTimeModel> _logger;

        public TeeTimeRequestStandingTeeTimeModel(TeeTimeDbContext context, ILogger<TeeTimeRequestStandingTeeTimeModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public StandingTeeTimeRequestInput RequestData { get; set; } = new();

        public bool IsEligible { get; set; }
        public string CurrentMemberName { get; set; } = string.Empty;
        public List<StandingTeeTimeRequest> ExistingRequests { get; set; } = new();
        public SelectList? MemberList { get; set; }
        public string StatusMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return Unauthorized();
            }

            // Set default values
            CurrentMemberName = $"{member.User?.FirstName} {member.User?.LastName}";
            RequestData.StartDate = DateTime.Today;
            RequestData.EndDate = DateTime.Today.AddMonths(3);

            // Check if the member is eligible (Shareholder or Associate)
            IsEligible = CheckEligibility(member);

            // If eligible, load other data
            if (IsEligible)
            {
                await LoadMemberListAsync();
                await LoadExistingRequestsAsync(member.MemberID);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            IsEligible = CheckEligibility(member);
            if (!IsEligible)
            {
                StatusMessage = "You are not eligible to request standing tee times. Only Shareholder members can request standing tee times.";
                return Page();
            }

            // Validate that all player IDs are different
            var playerIds = new List<int> { RequestData.MemberID, RequestData.Player2ID, RequestData.Player3ID, RequestData.Player4ID };
            if (playerIds.Distinct().Count() != 4)
            {
                ModelState.AddModelError(string.Empty, "All players in the foursome must be different.");
            }

            // Validate start date is before end date
            if (RequestData.EndDate < RequestData.StartDate)
            {
                ModelState.AddModelError(string.Empty, "End date must be after start date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadMemberListAsync();
                await LoadExistingRequestsAsync(member.MemberID);
                return Page();
            }

            // Store player IDs in TempData for future use
            // This is a workaround until the database schema is updated
            TempData["Player2ID"] = RequestData.Player2ID;
            TempData["Player3ID"] = RequestData.Player3ID;
            TempData["Player4ID"] = RequestData.Player4ID;

            // Create request with only the fields that exist in the database
            var request = new StandingTeeTimeRequest
            {
                MemberID = member.MemberID,
                // Include Player IDs now that the database schema has been updated
                Player2ID = RequestData.Player2ID,
                Player3ID = RequestData.Player3ID,
                Player4ID = RequestData.Player4ID,
                
                // Parse DayOfWeek string to enum
                DayOfWeek = Enum.Parse<DayOfWeek>(RequestData.DayOfWeek, true),
                
                StartDate = RequestData.StartDate,
                EndDate = RequestData.EndDate,
                DesiredTeeTime = TimeSpan.Parse(RequestData.DesiredTeeTime)
            };

            _context.StandingTeeTimeRequests.Add(request);
            await _context.SaveChangesAsync();

            StatusMessage = "Your standing tee time request has been submitted. You will be notified when it is reviewed.";

            // Use simple string interpolation first, then pass as a single parameter to avoid CA2017 warning
            var memberName = member?.User != null ? $"{member.User.FirstName} {member.User.LastName}" : "Unknown";
            var logMessage = $"Standing tee time request submitted: Member={memberName}, " +
                            $"DayOfWeek={request.DayOfWeek}, " +
                            $"Start={request.StartDate:yyyy-MM-dd}, " +
                            $"End={request.EndDate:yyyy-MM-dd}, " +
                            $"Players=[{request.Player2ID},{request.Player3ID},{request.Player4ID}], " +
                            $"Time={request.DesiredTeeTime}";
            
            _logger.LogInformation(logMessage);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelRequestAsync(int requestId)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return Unauthorized();
            }

            // Find the request and verify ownership
            var request = await _context.StandingTeeTimeRequests
                .FirstOrDefaultAsync(r => r.RequestID == requestId && r.MemberID == member.MemberID);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found or you do not have permission to cancel it.";
                return RedirectToPage();
            }

            // Only allow cancellation of pending requests
            if (request.ApprovedTeeTime.HasValue)
            {
                TempData["ErrorMessage"] = "Cannot cancel an approved standing tee time request.";
                return RedirectToPage();
            }

            try
            {
                _context.StandingTeeTimeRequests.Remove(request);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Standing tee time request cancelled successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling standing tee time request");
                TempData["ErrorMessage"] = "An error occurred while cancelling your request. Please try again.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteRequestAsync(int requestId)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return Unauthorized();
            }

            // Find the request and verify ownership
            var request = await _context.StandingTeeTimeRequests
                .FirstOrDefaultAsync(r => r.RequestID == requestId && r.MemberID == member.MemberID);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found or you do not have permission to delete it.";
                return RedirectToPage();
            }

            try
            {
                // Find all reservations associated with this standing tee time
                var reservations = await _context.Reservations
                    .Where(r => r.StandingRequestID == requestId)
                    .ToListAsync();

                // Remove all reservations
                _context.Reservations.RemoveRange(reservations);

                // Delete the standing tee time request
                _context.StandingTeeTimeRequests.Remove(request);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Standing tee time request deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting standing tee time request");
                TempData["ErrorMessage"] = "An error occurred while deleting your request. Please try again.";
            }

            return RedirectToPage();
        }

        private bool CheckEligibility(Member member)
        {
            // Check if member has Shareholder or Gold Associate membership
            if (member.MembershipCategory == null)
            {
                // Log if MembershipCategory was not loaded - indicates an issue in GetCurrentMemberAsync
                _logger.LogWarning("CheckEligibilityAsync: member.MembershipCategory is null for MemberID {MemberID}. Make sure it's included in the query.", member.MemberID);
                return false;
            }

            // Directly use the loaded MembershipCategory
            var categoryName = member.MembershipCategory.MembershipName;
            
            // Perform case-insensitive comparison for robustness
            bool isEligible = categoryName.Contains("Shareholder", StringComparison.OrdinalIgnoreCase) || 
                              categoryName.Contains("Gold Associate", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("CheckEligibility for MemberID {MemberID}: Category={CategoryName}, IsEligible={IsEligible}", 
                member.MemberID, categoryName, isEligible);

            return isEligible;
        }

        private async Task LoadMemberListAsync()
        {
            // Get all members for the player selection dropdowns
            var members = await _context.Members
                .Include(m => m.User)
                .Where(m => m.User != null)
                .OrderBy(m => m.User!.LastName)
                .ThenBy(m => m.User!.FirstName)
                .Select(m => new
                {
                    MemberID = m.MemberID,
                    FullName = $"{m.User!.FirstName} {m.User.LastName} ({m.MemberID})"
                })
                .ToListAsync();

            MemberList = new SelectList(members, "MemberID", "FullName");
        }

        private async Task LoadExistingRequestsAsync(int memberId)
        {
            // Get existing standing tee time requests for this member
            // Using projection to select only the columns that exist in the database
            var requestData = await _context.StandingTeeTimeRequests
                .Where(r => r.MemberID == memberId)
                .OrderByDescending(r => r.StartDate)
                .Select(r => new 
                {
                    r.RequestID,
                    r.MemberID,
                    r.DayOfWeek,
                    r.StartDate, 
                    r.EndDate,
                    r.DesiredTeeTime,
                    r.PriorityNumber,
                    r.ApprovedTeeTime,
                    r.ApprovedByUserID,
                    r.ApprovedDate
                })
                .ToListAsync();
                
            // Convert to StandingTeeTimeRequest objects
            ExistingRequests = requestData.Select(r => new StandingTeeTimeRequest
            {
                RequestID = r.RequestID,
                MemberID = r.MemberID,
                DayOfWeek = r.DayOfWeek,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                DesiredTeeTime = r.DesiredTeeTime,
                PriorityNumber = r.PriorityNumber,
                ApprovedTeeTime = r.ApprovedTeeTime,
                ApprovedByUserID = r.ApprovedByUserID,
                ApprovedDate = r.ApprovedDate
                // Player2ID, Player3ID, and Player4ID are intentionally not set here
                // since they don't exist in the database yet
            }).ToList();
        }

        private async Task<Member?> GetCurrentMemberAsync()
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return null;
                }

                // Get member info - **Include MembershipCategory**
                return await _context.Members
                    .Include(m => m.User)
                    .Include(m => m.MembershipCategory) // Added Include
                    .FirstOrDefaultAsync(m => m.UserID == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current member");
                return null;
            }
        }
    }

    public class StandingTeeTimeRequestInput
    {
        [Required]
        public int MemberID { get; set; }

        [Required]
        public int Player2ID { get; set; }

        [Required]
        public int Player3ID { get; set; }

        [Required]
        public int Player4ID { get; set; }

        [Required]
        [Display(Name = "Day of Week")]
        public string DayOfWeek { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Desired Tee Time")]
        public string DesiredTeeTime { get; set; } = string.Empty;
    }
}
