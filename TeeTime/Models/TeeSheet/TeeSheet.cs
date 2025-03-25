using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeeTime.Models.TeeSheet
{
    public class TeeSheet
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        public int IntervalMinutes { get; set; } = 10;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<TeeTime> TeeTimes { get; set; } = new List<TeeTime>();
    }
}
