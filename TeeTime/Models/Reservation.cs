using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeeTime.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        public int TeeTimeId { get; set; }

        [Required]
        public DateTime ReservationMadeDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string ReservationStatus { get; set; } = "Confirmed";

        [Required]
        [Range(1, 4, ErrorMessage = "Number of players must be between 1 and 4")]
        public int NumberOfPlayers { get; set; }

        [Required]
        [Range(0, 4, ErrorMessage = "Number of carts must be between 0 and 4")]
        public int NumberOfCarts { get; set; }

        // These properties might not exist in the current schema
        // Using NotMapped attribute to indicate they should be ignored in migrations
        [NotMapped]
        public bool IsStandingTeeTime { get; set; } = false;

        [NotMapped]
        public int? StandingRequestID { get; set; }

        [NotMapped]
        [MaxLength(200)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("MemberID")]
        public Member? Member { get; set; }

        [ForeignKey("TeeTimeId")]
        public TeeSheet.TeeTime? TeeTime { get; set; }

        // Don't map this as we don't have the column yet
        [NotMapped]
        public StandingTeeTimeRequest? StandingTeeTimeRequest { get; set; }
    }
}
