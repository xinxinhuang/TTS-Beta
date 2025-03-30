using System;
using System.Threading.Tasks;
using TeeTime.Models.TeeSheet;
using TeeTime.Models;

namespace TeeTime.Data.Interfaces
{
    public interface ITeeSheetRepository
    {
        /// <summary>
        /// Persists a new TeeSheet and its associated TeeTimes and Reservations to the database.
        /// </summary>
        /// <param name="teeSheet">The TeeSheet object to save (including its TeeTimes and their Reservations lists).</param>
        /// <returns>The saved TeeSheet, possibly updated with IDs.</returns>
        Task<TeeSheet> CreateTeeSheetAsync(TeeSheet teeSheet);

        /// <summary>
        /// Checks if a TeeSheet already exists for the specified date.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>True if a TeeSheet exists, false otherwise.</returns>
        Task<bool> ExistsAsync(DateTime date);

        /// <summary>
        /// Finds an existing TeeTime on a specific TeeSheet at a specific time.
        /// </summary>
        /// <param name="teeSheetId">The ID of the TeeSheet.</param>
        /// <param name="time">The specific time slot.</param>
        /// <returns>The TeeTime if found, otherwise null.</returns>
        Task<Models.TeeSheet.TeeTime?> FindTeeTimeAsync(int teeSheetId, TimeSpan time);

        /// <summary>
        /// Updates an existing TeeTime (e.g., TotalPlayersBooked, Notes) and adds a related Reservation.
        /// </summary>
        /// <param name="teeTime">The TeeTime entity to update.</param>
        /// <param name="reservation">The Reservation entity to add.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task UpdateTeeTimeAndAddReservationAsync(Models.TeeSheet.TeeTime teeTime, Reservation reservation);
    }
} 