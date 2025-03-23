using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public int RoleID { get; set; } = 1; // Default: Member

        [Required]
        public int MembershipCategoryID { get; set; }

        // Navigation properties
        [ForeignKey("RoleID")]
        public Role? Role { get; set; }

        [ForeignKey("MembershipCategoryID")]
        public MembershipCategory? MembershipCategory { get; set; }

        public Member? Member { get; set; }
        
        public ICollection<MemberUpgrade> ApprovedUpgrades { get; set; } = new List<MemberUpgrade>();
    }
}
