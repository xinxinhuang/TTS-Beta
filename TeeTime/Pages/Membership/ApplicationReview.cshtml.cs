using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
        public User CurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // In a real app, get the current logged-in user with committee role
            CurrentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Role.RoleDescription == "Committee Member");

            if (CurrentUser == null)
            {
                return NotFound("No committee member found. Please create a committee member first.");
            }

            PendingApplications = await _context.MemberUpgrades
                .Include(mu => mu.User)
                .Include(mu => mu.DesiredMembershipCategory)
                .Include(mu => mu.Sponsor1).ThenInclude(s => s.User)
                .Include(mu => mu.Sponsor2).ThenInclude(s => s.User)
                .Where(mu => mu.Status == "Pending")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var application = await _context.MemberUpgrades
                .Include(mu => mu.User)
                .FirstOrDefaultAsync(mu => mu.ApplicationID == id);

            if (application == null)
            {
                return NotFound();
            }

            // Get committee member (in a real app, this would be from authentication)
            var committeeUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Role.RoleDescription == "Committee Member");

            if (committeeUser == null)
            {
                return NotFound("Committee member not found");
            }

            // Update application status
            application.Status = "Approved";
            application.ApprovalDate = DateTime.Now;
            application.ApprovalByUserID = committeeUser.UserID;

            // Update user's membership category
            var user = application.User;
            user.MembershipCategoryID = application.DesiredMembershipCategoryID;

            // Update member's membership category
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserID == user.UserID);
            if (member != null)
            {
                member.MembershipCategoryID = application.DesiredMembershipCategoryID;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./ApplicationReview");
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var application = await _context.MemberUpgrades.FindAsync(id);

            if (application == null)
            {
                return NotFound();
            }

            // Get committee member (in a real app, this would be from authentication)
            var committeeUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Role.RoleDescription == "Committee Member");

            if (committeeUser == null)
            {
                return NotFound("Committee member not found");
            }

            // Update application status
            application.Status = "Rejected";
            application.ApprovalDate = DateTime.Now;
            application.ApprovalByUserID = committeeUser.UserID;

            await _context.SaveChangesAsync();

            return RedirectToPage("./ApplicationReview");
        }
    }
}
