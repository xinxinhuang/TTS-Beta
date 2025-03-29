using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(TeeTimeDbContext context, ILogger<DashboardModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string UserFullName { get; set; } = string.Empty;
        public string MembershipLevel { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public bool CanBookTeeTime { get; set; }
        public bool CanRequestStandingTeeTime { get; set; }
        public bool CanApplyForUpgrade { get; set; }
        public bool IsCommitteeMember { get; set; }
        
        // Property to hold the count of pending standing tee time requests
        public int PendingStandingRequestsCount { get; set; }

        // Properties for showing the membership upgrade notification
        public bool ShowUpgradeNotification { get; set; }
        public required MemberUpgrade RecentUpgrade { get; set; }
        [TempData]
        public bool DismissUpgradeNotification { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users
                .Include(u => u.MembershipCategory)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null || user.MembershipCategory == null)
            {
                return NotFound();
            }

            UserFullName = $"{user.FirstName} {user.LastName}";
            MembershipLevel = user.MembershipCategory.MembershipName;
            UserRole = user.Role?.RoleDescription ?? "Member";
            
            // Check if user is a committee member using standard role check
            IsCommitteeMember = User.IsInRole("Committee Member"); // Match SeedData RoleDescription

            // Set permissions based on membership level
            CanBookTeeTime = MembershipLevel != "Copper"; // All except Copper can book tee times
            CanRequestStandingTeeTime = user.MembershipCategory.CanMakeStandingTeeTime;
            CanApplyForUpgrade = MembershipLevel == "Gold"; // Only Gold members can upgrade to Shareholder/Associate

            // Check for recent membership upgrade (approved in the last 7 days)
            if (!DismissUpgradeNotification)
            {
                var recentUpgrade = await _context.MemberUpgrades
                    .Include(mu => mu.DesiredMembershipCategory)
                    .Where(mu => mu.UserID == userId && 
                                 mu.Status == "Approved" && 
                                 mu.ApprovalDate.HasValue && 
                                 mu.ApprovalDate.Value >= DateTime.Now.AddDays(-7))
                    .OrderByDescending(mu => mu.ApprovalDate)
                    .FirstOrDefaultAsync();

                if (recentUpgrade != null)
                {
                    ShowUpgradeNotification = true;
                    RecentUpgrade = recentUpgrade;
                }
            }

            // If user is a committee member, get count of pending standing tee time requests
            if (IsCommitteeMember)
            {
                PendingStandingRequestsCount = await _context.StandingTeeTimeRequests
                    .CountAsync(r => r.ApprovedTeeTime == null);
            }

            return Page();
        }

        public IActionResult OnPostDismissNotification()
        {
            DismissUpgradeNotification = true;
            return RedirectToPage();
        }
    }
}
