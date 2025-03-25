using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models.TeeSheet
{
    public class TeeTime
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        public int TeeSheetId { get; set; }
        
        [ForeignKey("TeeSheetId")]
        public virtual TeeSheet TeeSheet { get; set; } = null!;
        
        public int? ReservationId { get; set; }
        
        public bool IsAvailable { get; set; } = true;
        
        public string Notes { get; set; } = string.Empty;
    }
}
