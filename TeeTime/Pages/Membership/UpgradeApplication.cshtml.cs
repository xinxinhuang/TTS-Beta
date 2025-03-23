using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages.Membership
{
    public class UpgradeApplicationModel : PageModel
    {
        private readonly TeeTimeDbContext _context;

        public UpgradeApplicationModel(TeeTimeDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MemberUpgradeViewModel UpgradeApplication { get; set; } = new();

        public SelectList MembershipCategories { get; set; }
        public SelectList GoldMembers { get; set; }
        
        public User CurrentUser { get; set; }
        public Member CurrentMember { get; set; }
        
        public bool HasPendingApplication { get; set; }
        public MemberUpgrade PendingApplication { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // For demo purposes, get the first Gold member
            // In a real application, you would get the current logged-in user
            CurrentUser = await _context.Users
                .Include(u => u.MembershipCategory)
                .Include(u => u.Member)
                .FirstOrDefaultAsync(u => u.MembershipCategory.MembershipName == "Gold");

            if (CurrentUser == null)
            {
                return NotFound("No Gold member found. Please create a Gold member first.");
            }

            CurrentMember = CurrentUser.Member;

            // Check if user already has a pending application
            HasPendingApplication = await _context.MemberUpgrades
                .AnyAsync(mu => mu.UserID == CurrentUser.UserID && mu.Status == "Pending");

            if (HasPendingApplication)
            {
                PendingApplication = await _context.MemberUpgrades
                    .Include(mu => mu.DesiredMembershipCategory)
                    .Include(mu => mu.Sponsor1)
                    .Include(mu => mu.Sponsor2)
                    .FirstOrDefaultAsync(mu => mu.UserID == CurrentUser.UserID && mu.Status == "Pending");
            }

            // Get eligible membership categories (Gold Shareholder and Gold Associate)
            var eligibleCategories = await _context.MembershipCategories
                .Where(mc => mc.MembershipName == "Gold Shareholder" || mc.MembershipName == "Gold Associate")
                .ToListAsync();
            MembershipCategories = new SelectList(eligibleCategories, "MembershipCategoryID", "MembershipName");

            // Get Gold members who can sponsor (Gold Shareholder or Gold Associate)
            var sponsors = await _context.Members
                .Include(m => m.User)
                .Include(m => m.MembershipCategory)
                .Where(m => m.MembershipCategory.CanSponsor)
                .ToListAsync();
            GoldMembers = new SelectList(sponsors, "MemberID", "User.FirstName");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await SetupSelectLists();
                return Page();
            }

            // Get current user (in a real app, this would be from authentication)
            var user = await _context.Users
                .Include(u => u.Member)
                .FirstOrDefaultAsync(u => u.MembershipCategory.MembershipName == "Gold");

            if (user == null)
            {
                return NotFound("User not found");
            }

            var memberUpgrade = new MemberUpgrade
            {
                UserID = user.UserID,
                Address = UpgradeApplication.Address,
                PostalCode = UpgradeApplication.PostalCode,
                Phone = UpgradeApplication.Phone,
                AlternatePhone = UpgradeApplication.AlternatePhone,
                DateOfBirth = UpgradeApplication.DateOfBirth,
                Occupation = UpgradeApplication.Occupation,
                CompanyName = UpgradeApplication.CompanyName,
                CompanyAddress = UpgradeApplication.CompanyAddress,
                CompanyPostalCode = UpgradeApplication.CompanyPostalCode,
                CompanyPhone = UpgradeApplication.CompanyPhone,
                Sponsor1MemberID = UpgradeApplication.Sponsor1MemberID,
                Sponsor2MemberID = UpgradeApplication.Sponsor2MemberID,
                DesiredMembershipCategoryID = UpgradeApplication.DesiredMembershipCategoryID,
                Status = "Pending",
                ApplicationDate = DateTime.Now
            };

            _context.MemberUpgrades.Add(memberUpgrade);
            await _context.SaveChangesAsync();

            return RedirectToPage("./ApplicationSubmitted");
        }

        private async Task SetupSelectLists()
        {
            var eligibleCategories = await _context.MembershipCategories
                .Where(mc => mc.MembershipName == "Gold Shareholder" || mc.MembershipName == "Gold Associate")
                .ToListAsync();
            MembershipCategories = new SelectList(eligibleCategories, "MembershipCategoryID", "MembershipName");

            var sponsors = await _context.Members
                .Include(m => m.User)
                .Include(m => m.MembershipCategory)
                .Where(m => m.MembershipCategory.CanSponsor)
                .ToListAsync();
            GoldMembers = new SelectList(sponsors, "MemberID", "User.FirstName");
        }

        public async Task<IActionResult> OnPostWithdrawAsync(int applicationId)
        {
            // Find the application
            var application = await _context.MemberUpgrades
                .FirstOrDefaultAsync(mu => mu.ApplicationID == applicationId && mu.Status == "Pending");

            if (application == null)
            {
                return NotFound("Application not found or already processed.");
            }

            // Delete the application
            _context.MemberUpgrades.Remove(application);
            await _context.SaveChangesAsync();

            // Redirect back to the same page to show the form for a new application
            return RedirectToPage();
        }
    }

    public class MemberUpgradeViewModel
    {
        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Postal Code")]
        [RegularExpression(@"^[A-Za-z]\d[A-Za-z]\s\d[A-Za-z]\d$", ErrorMessage = "Postal code must be in format 'A1A 1A1'")]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Alternate Phone")]
        public string? AlternatePhone { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-30);

        [Display(Name = "Occupation")]
        public string? Occupation { get; set; }

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "Company Address")]
        public string? CompanyAddress { get; set; }

        [Display(Name = "Company Postal Code")]
        [RegularExpression(@"^[A-Za-z]\d[A-Za-z]\s\d[A-Za-z]\d$", ErrorMessage = "Company postal code must be in format 'A1A 1A1'")]
        public string? CompanyPostalCode { get; set; }

        [Display(Name = "Company Phone")]
        public string? CompanyPhone { get; set; }

        [Display(Name = "First Sponsor")]
        public int? Sponsor1MemberID { get; set; }

        [Display(Name = "Second Sponsor")]
        public int? Sponsor2MemberID { get; set; }

        [Required]
        [Display(Name = "Desired Membership")]
        public int DesiredMembershipCategoryID { get; set; }
    }
}
