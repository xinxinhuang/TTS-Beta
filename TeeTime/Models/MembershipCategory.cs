using System.ComponentModel.DataAnnotations;

namespace TeeTime.Models
{
    public class MembershipCategory
    {
        [Key]
        public int MembershipCategoryID { get; set; }

        [Required]
        [MaxLength(50)]
        public string MembershipName { get; set; } = string.Empty;

        [Required]
        public bool CanSponsor { get; set; }

        [Required]
        public bool CanMakeStandingTeeTime { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Member> Members { get; set; } = new List<Member>();
        public ICollection<MemberUpgrade> MemberUpgrades { get; set; } = new List<MemberUpgrade>();
    }
}
