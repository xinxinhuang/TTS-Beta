using System;
using System.ComponentModel.DataAnnotations;
using TeeTime.Models;

namespace TeeTime.ViewModels
{
    public class TeeTimeBookingViewModel
    {
        [Required(ErrorMessage = "Please select a date")]
        [DataType(DataType.Date)]
        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Please select a tee time")]
        [Display(Name = "Tee Time")]
        public int ScheduledGolfTimeID { get; set; }

        [Required]
        [Range(1, 4, ErrorMessage = "Number of players must be between 1 and 4")]
        [Display(Name = "Number of Players")]
        public int NumberOfPlayers { get; set; } = 1;

        [Required]
        [Range(0, 2, ErrorMessage = "Number of carts must be between 0 and 2")]
        [Display(Name = "Number of Carts")]
        public int NumberOfCarts { get; set; } = 0;

        // Navigation property
        public required ScheduledGolfTime ScheduledGolfTime { get; set; }
        
        // Confirmation details
        public required string ConfirmationNumber { get; set; }
        public bool IsConfirmed { get; set; }
    }
}
