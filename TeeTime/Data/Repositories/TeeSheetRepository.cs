using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data.Interfaces;
using TeeTime.Models.TeeSheet;
using TeeTime.Models;

namespace TeeTime.Data.Repositories
{
    public class TeeSheetRepository : ITeeSheetRepository
    {
        private readonly TeeTimeDbContext _context;
        private readonly ILogger<TeeSheetRepository> _logger;

        public TeeSheetRepository(TeeTimeDbContext context, ILogger<TeeSheetRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Persists a new TeeSheet and its associated TeeTimes and Reservations to the database.
        /// EF Core automatically handles cascading saves if relationships are configured correctly.
        /// </summary>
        /// <param name="teeSheet">The TeeSheet object to save.</param>
        /// <returns>The saved TeeSheet.</returns>
        public async Task<TeeSheet> CreateTeeSheetAsync(TeeSheet teeSheet)
        {
            // Assume teeSheet object includes its TeeTimes, and each TeeTime includes its Reservations
            _context.TeeSheets.Add(teeSheet);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update failed during TeeSheet creation. Error: {ErrorMessage}, Inner Exception: {InnerException}", dbEx.Message, dbEx.InnerException?.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic error during SaveChanges in CreateTeeSheetAsync.");
                throw;
            }
            return teeSheet; // The teeSheet object will be updated with IDs by EF Core if save succeeds
        }

        /// <summary>
        /// Checks if a TeeSheet already exists for the specified date.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>True if a TeeSheet exists, false otherwise.</returns>
        public async Task<bool> ExistsAsync(DateTime date)
        {
            var targetDate = date.Date;
            return await _context.TeeSheets.AnyAsync(ts => ts.Date == targetDate);
        }

        /// <summary>
        /// Finds an existing TeeTime on a specific TeeSheet at a specific time.
        /// Note: This currently compares the full DateTime. If only Time comparison is needed,
        /// adjust the query or the TeeTime model.
        /// </summary>
        /// <param name="teeSheetId">The ID of the TeeSheet.</param>
        /// <param name="time">The specific time slot (as TimeSpan).</param>
        /// <returns>The TeeTime if found, otherwise null.</returns>
        public async Task<Models.TeeSheet.TeeTime?> FindTeeTimeAsync(int teeSheetId, TimeSpan time)
        {
            // This finds based on the exact StartTime (which includes Date).
            // If TeeTimes were always on the same day as their TeeSheet, 
            // we could potentially construct the DateTime here.
            // For now, assuming the service layer provides the correct DateTime if needed,
            // or relying on the service layer's HashSet check for initial generation.
            // A more robust approach might involve querying by TeeSheetId and Time part only if needed.
            
            // Let's refine this - we really only care about the time on that sheet's date.
            var teeSheetDate = await _context.TeeSheets
                                        .Where(ts => ts.Id == teeSheetId)
                                        .Select(ts => ts.Date)
                                        .FirstOrDefaultAsync();

            if (teeSheetDate == default(DateTime)){
                return null; // TeeSheet not found
            }

            var targetDateTime = teeSheetDate.Date.Add(time);

            return await _context.TeeTimes
                .FirstOrDefaultAsync(tt => tt.TeeSheetId == teeSheetId && tt.StartTime == targetDateTime);
        }

        /// <summary>
        /// Updates an existing TeeTime and adds a related Reservation.
        /// Ensures changes are tracked and saved.
        /// </summary>
        public async Task UpdateTeeTimeAndAddReservationAsync(Models.TeeSheet.TeeTime teeTime, Reservation reservation)
        {
            // Explicitly add the reservation to the context
            // _context.Reservations.Add(reservation); // <<< TEMPORARILY COMMENTED OUT
            
            // Mark the TeeTime as modified (EF Core might detect changes anyway, but this is explicit)
            _context.Entry(teeTime).State = EntityState.Modified;

            // Ensure the reservation is linked to the teeTime's ID if not already set by relationship fixup
            // (Shouldn't be necessary if navigation properties are used correctly, but belts and suspenders)
            // <<< COMMENTED OUT as we are not adding reservation now
            // if (reservation.TeeTimeId == 0 && teeTime.Id != 0)
            // {
            //      reservation.TeeTimeId = teeTime.Id;
            // }
            // else if (reservation.TeeTimeId != teeTime.Id && teeTime.Id != 0)
            // {
            //      _logger.LogWarning("Reservation TeeTimeId {ResTeeTimeId} does not match TeeTime Id {TeeTimeId} during update.", reservation.TeeTimeId, teeTime.Id);
            //      // Optionally correct it: reservation.TeeTimeId = teeTime.Id;
            // }
            
            // --- DEBUG: Log Change Tracker State ---
            var entry = _context.Entry(teeTime);
            bool isBookedModified = entry.Property(e => e.TotalPlayersBooked).IsModified;
            int? originalBooked = isBookedModified ? entry.Property(e => e.TotalPlayersBooked).OriginalValue : null;
            bool isNotesModified = entry.Property(e => e.Notes).IsModified;
            string? originalNotes = isNotesModified ? entry.Property(e => e.Notes).OriginalValue : null;
            _logger.LogInformation("UpdateRepo: ChangeTracker State for TeeTime ID {TeeTimeId}: EntityState={State}, IsBookedModified={IsBookedMod} (Original={OrigBooked}, Current={CurrBooked}), IsNotesModified={IsNotesMod} (Original=\'{OrigNotes}', Current=\'{CurrNotes}')",
                                teeTime.Id, 
                                entry.State, 
                                isBookedModified, 
                                originalBooked, 
                                teeTime.TotalPlayersBooked, // Current value
                                isNotesModified,
                                originalNotes,
                                teeTime.Notes); // Current value
            // --- END DEBUG ---

            _logger.LogInformation("UpdateRepo (TeeTime Only Test): Attempting SaveChanges. TeeTime ID: {TeeTimeId}, PlayersBooked: {Players}, Notes: \'{Notes}\'",
                                teeTime.Id, teeTime.TotalPlayersBooked, teeTime.Notes /*, reservation.ReservationID, reservation.MemberID, reservation.TeeTimeId*/); // <<< Reservation details removed from log

            try
            {
                 await _context.SaveChangesAsync();
                 _logger.LogInformation("UpdateRepo: SaveChanges SUCCEEDED for TeeTime ID: {TeeTimeId}.", teeTime.Id);
            }
            catch (DbUpdateException dbEx) 
            {
                _logger.LogError(dbEx, "Database update failed during UpdateTeeTimeAndAddReservationAsync. Error: {ErrorMessage}, Inner Exception: {InnerException}", dbEx.Message, dbEx.InnerException?.Message);
                throw; 
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Generic error during SaveChanges in UpdateTeeTimeAndAddReservationAsync.");
                throw; 
            }
        }
    }
} 