using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class StandingTeeTimeRequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        [MaxLength(10)]
        public string DayOfWeek { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan DesiredTeeTime { get; set; }

        public int? PriorityNumber { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? ApprovedTeeTime { get; set; }

        public int? ApprovedByUserID { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ApprovedDate { get; set; }

        // Navigation properties
        [ForeignKey("MemberID")]
        public Member? Member { get; set; }

        [ForeignKey("ApprovedByUserID")]
        public User? ApprovedBy { get; set; }
    }
}
