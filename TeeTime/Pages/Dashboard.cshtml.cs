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
        public bool CanBookTeeTime { get; set; }
        public bool CanRequestStandingTeeTime { get; set; }
        public bool CanApplyForUpgrade { get; set; }

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

            var user = await _context.Users
                .Include(u => u.MembershipCategory)
                .FirstOrDefaultAsync(u => u.UserID == int.Parse(userIdClaim));

            if (user == null || user.MembershipCategory == null)
            {
                return NotFound();
            }

            UserFullName = $"{user.FirstName} {user.LastName}";
            MembershipLevel = user.MembershipCategory.MembershipName;

            // Set permissions based on membership level
            CanBookTeeTime = MembershipLevel != "Copper"; // All except Copper can book tee times
            CanRequestStandingTeeTime = user.MembershipCategory.CanMakeStandingTeeTime;
            CanApplyForUpgrade = MembershipLevel == "Gold"; // Only Gold members can upgrade to Shareholder/Associate

            return Page();
        }
    }
}
