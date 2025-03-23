using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class Member
    {
        [Key]
        public int MemberID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int MembershipCategoryID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime JoinDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(10)]
        public string Status { get; set; } = "Active";

        [MaxLength(12)]
        [RegularExpression(@"[0-9][0-9][0-9]-[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]", 
            ErrorMessage = "Phone number must be in format 123-456-7890")]
        public string? MemberPhone { get; set; }

        [Required]
        public bool GoodStanding { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("MembershipCategoryID")]
        public MembershipCategory? MembershipCategory { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<StandingTeeTimeRequest> StandingTeeTimeRequests { get; set; } = new List<StandingTeeTimeRequest>();
        public ICollection<MemberUpgrade> SponsoredUpgrades1 { get; set; } = new List<MemberUpgrade>();
        public ICollection<MemberUpgrade> SponsoredUpgrades2 { get; set; } = new List<MemberUpgrade>();
    }
}
