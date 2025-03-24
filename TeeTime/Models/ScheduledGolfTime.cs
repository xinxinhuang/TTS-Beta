using System.ComponentModel.DataAnnotations;

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
        
        [Required]
        public bool IsPublished { get; set; } = false;

        // Navigation property
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
