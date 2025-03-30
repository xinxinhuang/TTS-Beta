namespace TeeTime.Models
{
    /// <summary>
    /// Defines the origin or type of a reservation.
    /// </summary>
    public enum ReservationType
    {
        /// <summary>
        /// Reservation created automatically from an approved Standing Tee Time Request.
        /// </summary>
        Standing = 0,

        /// <summary>
        /// Reservation booked directly by a member or clerk through regular booking.
        /// </summary>
        Regular = 1,

        /// <summary>
        /// Reservation associated with a tournament or event blocking.
        /// </summary>
        Event = 2,

        /// <summary>
        /// Time slot blocked for course maintenance.
        /// </summary>
        Maintenance = 3,

        // Add other specific types as needed, e.g., League, Clinic
    }
} 