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

        // Make player IDs nullable and NotMapped until schema is updated
        [NotMapped]
        public int? Player2ID { get; set; }
        
        [NotMapped]
        public int? Player3ID { get; set; }
        
        [NotMapped]
        public int? Player4ID { get; set; }

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

        [NotMapped]
        public Member? Player2 { get; set; }

        [NotMapped]
        public Member? Player3 { get; set; }

        [NotMapped]
        public Member? Player4 { get; set; }

        [ForeignKey("ApprovedByUserID")]
        public User? ApprovedBy { get; set; }
    }
}
