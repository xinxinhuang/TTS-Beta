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

        // Navigation properties
        [ForeignKey("MemberID")]
        public Member? Member { get; set; }

        [ForeignKey("TeeTimeId")]
        public TeeSheet.TeeTime? TeeTime { get; set; }
    }
}
