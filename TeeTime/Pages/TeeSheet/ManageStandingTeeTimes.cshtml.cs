using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages
{
    [Authorize(Roles = "Committee Member")]
    public class TeeSheetManageStandingTeeTimesModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ILogger<TeeSheetManageStandingTeeTimesModel> _logger;

        public TeeSheetManageStandingTeeTimesModel(TeeTimeDbContext context, ILogger<TeeSheetManageStandingTeeTimesModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<StandingTeeTimeDisplayViewModel> PendingRequests { get; set; } = new();
        public List<StandingTeeTimeDisplayViewModel> ApprovedRequests { get; set; } = new();
        public int NextAvailablePriority { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load all standing tee time requests - avoid using navigation properties that aren't in database yet
                var allRequests = await _context.StandingTeeTimeRequests
                    .Include(r => r.Member)
                        .ThenInclude(m => m!.User)
                    // Include Player2, Player3, Player4 now that database schema is updated
                    .Include(r => r.Player2)
                        .ThenInclude(p => p!.User)
                    .Include(r => r.Player3)
                        .ThenInclude(p => p!.User)
                    .Include(r => r.Player4)
                        .ThenInclude(p => p!.User)
                    .Include(r => r.ApprovedBy)
                    .ToListAsync();

                // Map to view models and separate into pending and approved
                var requests = allRequests
                    // Add a check to ensure Member and User are not null before projecting
                    .Where(r => r.Member != null && r.Member.User != null) 
                    .Select(r => new StandingTeeTimeDisplayViewModel
                {
                    RequestID = r.RequestID,
                    // Use null-forgiving operator (!) as Where clause handles nulls
                    MemberName = $"{r.Member!.User!.FirstName} {r.Member!.User!.LastName}", 
                    
                    // Convert DayOfWeek enum to string
                    DayOfWeek = r.DayOfWeek.ToString(), 
                    
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    // Check for null before calling ToString
                    DesiredTeeTime = r.DesiredTeeTime != TimeSpan.Zero ? r.DesiredTeeTime.ToString(@"hh\:mm") : "N/A", 
                    ApprovedTeeTime = r.ApprovedTeeTime?.ToString(@"hh\:mm"),
                    PriorityNumber = r.PriorityNumber,
                    Status = r.Status ?? (r.ApprovedTeeTime.HasValue ? "Approved" : "Pending"),
                    ApprovedDate = r.ApprovedDate,
                    // ApprovedBy might be null, keep the null check
                    ApprovedBy = r.ApprovedBy != null ? $"{r.ApprovedBy.FirstName} {r.ApprovedBy.LastName}" : "", 
                    Player2Name = r.Player2 != null ? $"{r.Player2!.User!.FirstName} {r.Player2!.User!.LastName}" : "TBD", 
                    Player3Name = r.Player3 != null ? $"{r.Player3!.User!.FirstName} {r.Player3!.User!.LastName}" : "TBD",
                    Player4Name = r.Player4 != null ? $"{r.Player4!.User!.FirstName} {r.Player4!.User!.LastName}" : "TBD"
                }).ToList();

                // Separate into pending and approved
                PendingRequests = requests.Where(r => r.Status == "Pending").OrderBy(r => r.StartDate).ToList();
                ApprovedRequests = requests.Where(r => r.Status == "Approved")
                                         // Handle potential null PriorityNumber during ordering
                                         .OrderBy(r => r.PriorityNumber ?? int.MaxValue) 
                                         .ToList();

                // Calculate next available priority number
                NextAvailablePriority = ApprovedRequests.Any() 
                    ? ApprovedRequests.Max(r => r.PriorityNumber ?? 0) + 1 
                    : 1;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading standing tee time requests");
                ModelState.AddModelError(string.Empty, "An error occurred while loading the standing tee time requests.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId, string approvedTeeTime, int priorityNumber)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    return Unauthorized();
                }

                var request = await _context.StandingTeeTimeRequests.FindAsync(requestId);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToPage();
                }

                // Update the request with approval information
                request.ApprovedTeeTime = TimeSpan.Parse(approvedTeeTime);
                request.PriorityNumber = priorityNumber;
                request.ApprovedByUserID = user.UserID;
                request.ApprovedDate = DateTime.Now;
                request.Status = "Approved"; // Update status to ensure it's picked up during tee sheet generation

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Standing tee time request approved successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving standing tee time request");
                TempData["ErrorMessage"] = "An error occurred while approving the request. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDenyAsync(int requestId)
        {
            try
            {
                var request = await _context.StandingTeeTimeRequests.FindAsync(requestId);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToPage();
                }

                // Delete the request
                _context.StandingTeeTimeRequests.Remove(request);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Standing tee time request denied and removed.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error denying standing tee time request");
                TempData["ErrorMessage"] = "An error occurred while denying the request. Please try again.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostRevokeAsync(int requestId)
        {
            try
            {
                var request = await _context.StandingTeeTimeRequests.FindAsync(requestId);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToPage();
                }

                // Find all reservations associated with this standing tee time
                var reservations = await _context.Reservations
                    .Where(r => r.StandingRequestID == requestId && r.TeeTime!.StartTime > DateTime.Now)
                    .ToListAsync();

                // Remove all future reservations
                _context.Reservations.RemoveRange(reservations);

                // Either delete the request or remove the approval
                if (request.ApprovedDate.HasValue && 
                    (request.ApprovedDate.Value.AddDays(30) < DateTime.Now || reservations.Count == 0))
                {
                    // If it's been active for a while or has no future reservations, just delete it
                    _context.StandingTeeTimeRequests.Remove(request);
                }
                else
                {
                    // Otherwise, just revoke the approval 
                    request.ApprovedTeeTime = null;
                    request.ApprovedByUserID = null;
                    request.ApprovedDate = null;
                    request.PriorityNumber = null;
                    request.Status = "Pending"; // Reset status when revoking approval
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Standing tee time revoked successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking standing tee time");
                TempData["ErrorMessage"] = "An error occurred while revoking the standing tee time. Please try again.";
                return RedirectToPage();
            }
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return null;
                }

                // Get user info
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == userId);
            }
            catch
            {
                return null;
            }
        }
    }

    public class StandingTeeTimeDisplayViewModel
    {
        public int RequestID { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string DayOfWeek { get; set; } = string.Empty;
        public string DesiredTeeTime { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? PriorityNumber { get; set; }
        public string? ApprovedTeeTime { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string Player2Name { get; set; } = string.Empty;
        public string Player3Name { get; set; } = string.Empty;
        public string Player4Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
