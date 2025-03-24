using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        
        [Required]
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
        
        [DataType(DataType.DateTime)]
        [NotMapped]
        public DateTime StartDateTime => EventDate.Add(StartTime);
        
        [DataType(DataType.DateTime)]
        [NotMapped]
        public DateTime EndDateTime => EventDate.Add(EndTime);
    }
}
