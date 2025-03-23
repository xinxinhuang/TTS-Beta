using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages.Membership
{
    public class ApplicationReviewModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public ApplicationReviewModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        public IList<MemberUpgrade> PendingApplications { get; set; } = new List<MemberUpgrade>();
        public IList<MemberUpgrade> ProcessedApplications { get; set; } = new List<MemberUpgrade>();
        public User? CurrentUser { get; set; }
        public bool IsCommitteeMember { get; set; }
        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the authenticated user's ID
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = int.Parse(userIdClaim);

            // Get the current logged-in user
            CurrentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (CurrentUser == null)
            {
                return NotFound("User not found in the database.");
            }

            // Verify the user is a committee member
            IsCommitteeMember = CurrentUser.Role?.RoleDescription == "Committee Member";
            if (!IsCommitteeMember)
            {
                return RedirectToPage("/Dashboard", new { message = "You do not have permission to access this page." });
            }

            // Get all pending applications
            PendingApplications = await _context.MemberUpgrades
                .Include(mu => mu.User)
                .Include(mu => mu.User!.MembershipCategory)
                .Include(mu => mu.DesiredMembershipCategory)
                .Include(mu => mu.Sponsor1).ThenInclude(s => s!.User)
                .Include(mu => mu.Sponsor2).ThenInclude(s => s!.User)
                .Where(mu => mu.Status == "Pending")
                .OrderByDescending(mu => mu.ApplicationDate)
                .ToListAsync();

            // Get recently processed applications (last 30 days)
            ProcessedApplications = await _context.MemberUpgrades
                .Include(mu => mu.User)
                .Include(mu => mu.User!.MembershipCategory)
                .Include(mu => mu.DesiredMembershipCategory)
                .Include(mu => mu.ApprovalBy)
                .Where(mu => mu.Status != "Pending" && mu.ApprovalDate >= DateTime.Now.AddDays(-30))
                .OrderByDescending(mu => mu.ApprovalDate)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            // Check authentication and committee role
            if (!await VerifyCommitteeMember())
            {
                return RedirectToPage("/Dashboard", new { message = "You do not have permission to perform this action." });
            }

            var application = await _context.MemberUpgrades
                .Include(mu => mu.User)
                .Include(mu => mu.DesiredMembershipCategory)
                .FirstOrDefaultAsync(mu => mu.ApplicationID == id);

            if (application == null)
            {
                return NotFound();
            }

            // Get current user ID
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToPage("/Account/Login");
            }
            var userId = int.Parse(userIdClaim);

            // Update application status
            application.Status = "Approved";
            application.ApprovalDate = DateTime.Now;
            application.ApprovalByUserID = userId;

            // Update user's membership category
            var user = application.User;
            if (user != null && application.DesiredMembershipCategory != null)
            {
                user.MembershipCategoryID = application.DesiredMembershipCategoryID;

                // Update member's membership category
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserID == user.UserID);
                if (member != null)
                {
                    member.MembershipCategoryID = application.DesiredMembershipCategoryID;
                }

                await _context.SaveChangesAsync();

                StatusMessage = $"You have approved {user.FirstName} {user.LastName}'s application to become a {application.DesiredMembershipCategory.MembershipName} member.";
            }
            else
            {
                StatusMessage = "Application approved, but user or membership category information was missing.";
            }
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            // Check authentication and committee role
            if (!await VerifyCommitteeMember())
            {
                return RedirectToPage("/Dashboard", new { message = "You do not have permission to perform this action." });
            }

            var application = await _context.MemberUpgrades
                .Include(mu => mu.User)
                .FirstOrDefaultAsync(mu => mu.ApplicationID == id);

            if (application == null)
            {
                return NotFound();
            }

            // Get current user ID
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToPage("/Account/Login");
            }
            var userId = int.Parse(userIdClaim);

            // Update application status
            application.Status = "Rejected";
            application.ApprovalDate = DateTime.Now;
            application.ApprovalByUserID = userId;

            await _context.SaveChangesAsync();

            if (application.User != null)
            {
                StatusMessage = $"You have rejected {application.User.FirstName} {application.User.LastName}'s application.";
            }
            else
            {
                StatusMessage = "Application rejected successfully.";
            }
            
            return RedirectToPage();
        }

        private async Task<bool> VerifyCommitteeMember()
        {
            // Check if user is authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return false;
            }

            // Get the authenticated user's ID
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return false;
            }

            var userId = int.Parse(userIdClaim);

            // Verify the user is a committee member
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            return user?.Role?.RoleDescription == "Committee Member";
        }
    }
}
