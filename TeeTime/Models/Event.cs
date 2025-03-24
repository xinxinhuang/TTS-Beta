using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string EventName { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }
        
        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }
        
        [MaxLength(20)]
        public string EventColor { get; set; } = "blue";
        
        // Navigation property for related tee times
        public ICollection<ScheduledGolfTime> ScheduledGolfTimes { get; set; } = new List<ScheduledGolfTime>();
    }
}
