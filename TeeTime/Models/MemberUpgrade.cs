using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class MemberUpgrade
    {
        [Key]
        public int ApplicationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? AlternatePhone { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? Occupation { get; set; }

        [MaxLength(100)]
        public string? CompanyName { get; set; }

        [MaxLength(255)]
        public string? CompanyAddress { get; set; }

        [MaxLength(20)]
        public string? CompanyPostalCode { get; set; }

        [MaxLength(20)]
        public string? CompanyPhone { get; set; }

        public int? Sponsor1MemberID { get; set; }

        public int? Sponsor2MemberID { get; set; }

        [Required]
        public int DesiredMembershipCategoryID { get; set; }

        [Required]
        [MaxLength(10)]
        public string Status { get; set; } = "Pending";

        [DataType(DataType.Date)]
        public DateTime? ApprovalDate { get; set; }

        public int? ApprovalByUserID { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("DesiredMembershipCategoryID")]
        public MembershipCategory? DesiredMembershipCategory { get; set; }

        [ForeignKey("Sponsor1MemberID")]
        public Member? Sponsor1 { get; set; }

        [ForeignKey("Sponsor2MemberID")]
        public Member? Sponsor2 { get; set; }

        [ForeignKey("ApprovalByUserID")]
        public User? ApprovalBy { get; set; }
    }
}
