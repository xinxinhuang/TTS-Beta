using System;
using System.Collections.Generic;
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
        
        public int? EventID { get; set; }
        
        [ForeignKey("EventID")]
        public virtual Models.Event? Event { get; set; }
        
        public virtual ICollection<Models.Reservation> Reservations { get; set; } = new List<Models.Reservation>();
        
        public int TotalPlayersBooked { get; set; } = 0;
        
        [NotMapped]
        public int MaxPlayers { get; } = 4;
        
        public bool IsAvailable 
        { 
            get => TotalPlayersBooked < MaxPlayers;
            set { /* Setter kept for EF compatibility */ }
        }
        
        public string Notes { get; set; } = string.Empty;
    }
}
