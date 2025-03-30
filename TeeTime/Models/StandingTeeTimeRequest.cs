using System;
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

        // Remove NotMapped attribute to store these fields in the database
        public int? Player2ID { get; set; }
        
        public int? Player3ID { get; set; }
        
        public int? Player4ID { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan DesiredTeeTime { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public int? PriorityNumber { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? ApprovedTeeTime { get; set; }

        public int? ApprovedByUserID { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ApprovedDate { get; set; }

        // Navigation properties
        [ForeignKey("MemberID")]
        public Member? Member { get; set; }

        [ForeignKey("Player2ID")]
        public Member? Player2 { get; set; }

        [ForeignKey("Player3ID")]
        public Member? Player3 { get; set; }

        [ForeignKey("Player4ID")]
        public Member? Player4 { get; set; }

        [ForeignKey("ApprovedByUserID")]
        public User? ApprovedBy { get; set; }
    }
}
