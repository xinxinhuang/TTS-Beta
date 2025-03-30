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

        // Link back to the standing request if this reservation originated from one
        public int? StandingRequestID { get; set; }

        // Enum to classify the type/origin of the reservation
        [Required]
        public ReservationType Type { get; set; } = ReservationType.Regular; // Default to Regular

        // Removed [NotMapped]
        [MaxLength(200)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("MemberID")]
        public Member? Member { get; set; }

        [ForeignKey("TeeTimeId")]
        public TeeSheet.TeeTime? TeeTime { get; set; }

        // Added ForeignKey for StandingRequestID
        [ForeignKey("StandingRequestID")]
        public StandingTeeTimeRequest? StandingTeeTimeRequest { get; set; } // Removed [NotMapped]
    }
}
