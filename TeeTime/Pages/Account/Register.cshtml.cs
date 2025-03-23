using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(TeeTimeDbContext context, ILogger<RegisterModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public RegisterInputModel RegisterInput { get; set; } = new RegisterInputModel();

        public SelectList? MembershipCategories { get; set; }

        public async Task OnGetAsync()
        {
            // Load membership categories for dropdown
            MembershipCategories = new SelectList(
                await _context.MembershipCategories.ToListAsync(),
                "MembershipCategoryID", 
                "MembershipName");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Reload membership categories in case of validation error
            MembershipCategories = new SelectList(
                await _context.MembershipCategories.ToListAsync(),
                "MembershipCategoryID", 
                "MembershipName");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == RegisterInput.Email))
            {
                ModelState.AddModelError(string.Empty, "Email is already taken.");
                return Page();
            }

            // Hash the password
            string hashedPassword = HashPassword(RegisterInput.Password);

            // Create new user
            var user = new User
            {
                FirstName = RegisterInput.FirstName,
                LastName = RegisterInput.LastName,
                Email = RegisterInput.Email,
                PasswordHash = hashedPassword,
                RoleID = 1, // Default: Member
                MembershipCategoryID = RegisterInput.MembershipCategoryID
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create member record
            var member = new Member
            {
                UserID = user.UserID,
                MembershipCategoryID = user.MembershipCategoryID,
                JoinDate = DateTime.Now,
                Status = "Active",
                GoodStanding = true
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Email} created a new account with membership level {MembershipLevel}.", 
                user.Email, user.MembershipCategoryID);

            // Redirect to login page
            TempData["SuccessMessage"] = "Registration successful! Please log in with your new account.";
            return RedirectToPage("./Login");
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public class RegisterInputModel
        {
            [Required(ErrorMessage = "First name is required")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Membership level is required")]
            [Display(Name = "Membership Level")]
            public int MembershipCategoryID { get; set; }
        }
    }
}
