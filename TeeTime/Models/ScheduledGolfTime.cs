using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class ScheduledGolfTime
    {
        [Key]
        public int ScheduledGolfTimeID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan ScheduledTime { get; set; }

        [Required]
        public int GolfSessionInterval { get; set; } = 8;

        [Required]
        public bool IsAvailable { get; set; } = true;
        
        // Reference to Event
        public int? EventID { get; set; }
        
        [ForeignKey("EventID")]
        public Event? Event { get; set; }

        // Navigation property
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
