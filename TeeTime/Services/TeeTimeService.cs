using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;
using TeeTime.Models.TeeSheet;

namespace TeeTime.Services
{
    public interface ITeeTimeService
    {
        Task<Dictionary<DateTime, List<Models.TeeSheet.TeeTime>>> GetTeeSheetDataAsync(DateTime startDate, DateTime endDate);
        Dictionary<int, string> GetEventsForTeeTimesAsync(IEnumerable<Models.TeeSheet.TeeTime> teeTimes);
        Task<List<Reservation>> GetUserReservationsAsync(int memberId);
        Task<Models.TeeSheet.TeeTime?> GetTeeTimeByIdAsync(int teeTimeId);
        Task<bool> IsTimeFullyBookedAsync(int teeTimeId);
        Task<bool> IsDateFullyBookedAsync(DateTime date);
        Task<List<Models.TeeSheet.TeeTime>> GetAvailableTeeTimesAsync(DateTime date, Member member);
        Task<Reservation> CreateReservationAsync(int memberId, int teeTimeId, int numberOfPlayers, int numberOfCarts);
        Task<bool> CancelReservationAsync(int reservationId, int memberId);
        Task<Member?> GetMemberByUserIdAsync(int userId);
    }

    public class TeeTimeService : ITeeTimeService
    {
        private readonly TeeTimeDbContext _context;

        public TeeTimeService(TeeTimeDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<DateTime, List<Models.TeeSheet.TeeTime>>> GetTeeSheetDataAsync(DateTime startDate, DateTime endDate)
        {
            var teeTimes = await _context.TeeTimes
                .Include(t => t.Reservations)
                    .ThenInclude(r => r.Member)
                        .ThenInclude(m => m!.User)
                .Include(t => t.TeeSheet)
                .Where(t => t.StartTime.Date >= startDate && t.StartTime.Date < endDate)
                .OrderBy(t => t.StartTime.Date)
                .ThenBy(t => t.StartTime.TimeOfDay)
                .ToListAsync();

            // Group by date
            var teeSheets = new Dictionary<DateTime, List<Models.TeeSheet.TeeTime>>();
            foreach (var teeTime in teeTimes)
            {
                var date = teeTime.StartTime.Date;
                if (!teeSheets.ContainsKey(date))
                {
                    teeSheets[date] = new List<Models.TeeSheet.TeeTime>();
                }
                teeSheets[date].Add(teeTime);
            }

            return teeSheets;
        }

        public Dictionary<int, string> GetEventsForTeeTimesAsync(IEnumerable<Models.TeeSheet.TeeTime> teeTimes)
        {
            var events = new Dictionary<int, string>();

            foreach (var teeTime in teeTimes)
            {
                if (!teeTime.IsAvailable && !string.IsNullOrEmpty(teeTime.Notes))
                {
                    events[teeTime.Id] = teeTime.Notes;
                }
                else if (!teeTime.IsAvailable && (teeTime.Reservations == null || !teeTime.Reservations.Any()))
                {
                    events[teeTime.Id] = "Blocked";
                }
            }

            return events;
        }

        public async Task<List<Reservation>> GetUserReservationsAsync(int memberId)
        {
            return await _context.Reservations
                .Where(r => r.MemberID == memberId && r.ReservationStatus != "Cancelled")
                .Include(r => r.TeeTime)
                .OrderBy(r => r.TeeTime!.StartTime.Date)
                .ThenBy(r => r.TeeTime!.StartTime.TimeOfDay)
                .ToListAsync();
        }

        public async Task<Models.TeeSheet.TeeTime?> GetTeeTimeByIdAsync(int teeTimeId)
        {
            return await _context.TeeTimes.FindAsync(teeTimeId);
        }

        public async Task<bool> IsTimeFullyBookedAsync(int teeTimeId)
        {
            var teeTime = await _context.TeeTimes
                .Include(t => t.Reservations)
                .FirstOrDefaultAsync(t => t.Id == teeTimeId);
                
            if (teeTime == null)
                return true;
                
            return !teeTime.IsAvailable;
        }

        public async Task<bool> IsDateFullyBookedAsync(DateTime date)
        {
            // Check if all tee times for the given date are booked
            var teeTimesForDate = await _context.TeeTimes
                .Where(t => t.StartTime.Date == date.Date)
                .ToListAsync();
            
            // If there are no tee times for this date yet, it's not fully booked
            if (!teeTimesForDate.Any())
                return false;
            
            // Count available tee times
            int availableTeeTimesCount = teeTimesForDate.Count(t => t.IsAvailable);
            
            // If no available tee times, the date is fully booked
            return availableTeeTimesCount == 0;
        }

        public async Task<List<Models.TeeSheet.TeeTime>> GetAvailableTeeTimesAsync(DateTime date, Member member)
        {
            if (member == null)
            {
                return new List<Models.TeeSheet.TeeTime>();
            }

            // Get all available tee times for the selected date
            var allTimes = await _context.TeeTimes
                .Where(t => t.StartTime.Date == date.Date && t.IsAvailable)
                .OrderBy(t => t.StartTime.TimeOfDay)
                .ToListAsync();

            // Filter times based on membership level
            if (member.MembershipCategory == null)
            {
                return new List<Models.TeeSheet.TeeTime>();
            }
            
            return FilterTeeTimesByMembershipLevel(allTimes, member.MembershipCategory);
        }

        private List<Models.TeeSheet.TeeTime> FilterTeeTimesByMembershipLevel(List<Models.TeeSheet.TeeTime> allTimes, MembershipCategory membershipCategory)
        {
            // Apply time restrictions based on membership level
            if (membershipCategory == null)
            {
                return new List<Models.TeeSheet.TeeTime>();
            }

            // For example, if membership name is "Gold", show all times
            if (membershipCategory.MembershipName == "Gold")
            {
                return allTimes;
            }
            
            // For "Silver", filter out prime time slots on weekends
            else if (membershipCategory.MembershipName == "Silver")
            {
                bool isWeekend = DateTime.Today.DayOfWeek == DayOfWeek.Saturday || DateTime.Today.DayOfWeek == DayOfWeek.Sunday;
                
                if (isWeekend)
                {
                    // Restrict weekend morning tee times (before noon)
                    return allTimes.Where(t => t.StartTime.Hour >= 12).ToList();
                }
                
                return allTimes;
            }
            
            // For "Bronze", more restrictions
            else if (membershipCategory.MembershipName == "Bronze")
            {
                bool isWeekend = DateTime.Today.DayOfWeek == DayOfWeek.Saturday || DateTime.Today.DayOfWeek == DayOfWeek.Sunday;
                
                if (isWeekend)
                {
                    // Only allow afternoon tee times on weekends (after 2pm)
                    return allTimes.Where(t => t.StartTime.Hour >= 14).ToList();
                }
                
                // Restrict weekday morning tee times (before 10am)
                return allTimes.Where(t => t.StartTime.Hour >= 10).ToList();
            }
            
            // Default case - if membership doesn't match known categories
            return allTimes;
        }

        public async Task<Reservation> CreateReservationAsync(int memberId, int teeTimeId, int numberOfPlayers, int numberOfCarts)
        {
            // Use a transaction to ensure data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Console.WriteLine($"Creating reservation for member {memberId} for tee time {teeTimeId}");
                
                // Verify the selected time is still available
                var selectedTime = await _context.TeeTimes
                    .Include(t => t.Reservations)
                    .FirstOrDefaultAsync(t => t.Id == teeTimeId);
                    
                if (selectedTime == null)
                {
                    Console.WriteLine("Tee time not found");
                    throw new InvalidOperationException("Tee time not found");
                }
                
                Console.WriteLine($"Tee time found: StartTime={selectedTime.StartTime}, IsAvailable={selectedTime.IsAvailable}, TotalPlayersBooked={selectedTime.TotalPlayersBooked}");
                
                // Check if the tee time is available
                if (!selectedTime.IsAvailable)
                {
                    Console.WriteLine("Tee time is not available");
                    throw new InvalidOperationException("This tee time is no longer available");
                }
                
                // Check if adding these players would exceed the max capacity
                if (selectedTime.TotalPlayersBooked + numberOfPlayers > selectedTime.MaxPlayers)
                {
                    Console.WriteLine($"Too many players: {selectedTime.TotalPlayersBooked} + {numberOfPlayers} > {selectedTime.MaxPlayers}");
                    throw new InvalidOperationException($"This tee time can only accommodate {selectedTime.MaxPlayers - selectedTime.TotalPlayersBooked} more players");
                }

                // Create a new reservation
                var reservation = new Reservation
                {
                    MemberID = memberId,
                    TeeTimeId = teeTimeId,
                    NumberOfPlayers = numberOfPlayers,
                    NumberOfCarts = numberOfCarts,
                    ReservationMadeDate = DateTime.Now,
                    ReservationStatus = "Confirmed"
                };

                Console.WriteLine($"Adding reservation: MemberID={reservation.MemberID}, TeeTimeId={reservation.TeeTimeId}, Players={reservation.NumberOfPlayers}");
                
                // Add the reservation to the context
                _context.Reservations.Add(reservation);
                
                // Update the TeeTime's player count
                selectedTime.TotalPlayersBooked += numberOfPlayers;
                
                Console.WriteLine($"Updating tee time: New TotalPlayersBooked={selectedTime.TotalPlayersBooked}");
                
                // Update the tee time in the context
                _context.TeeTimes.Update(selectedTime);
                
                // Save changes to the database
                await _context.SaveChangesAsync();
                
                Console.WriteLine("Changes saved to database");
                
                // Commit the transaction
                await transaction.CommitAsync();
                
                Console.WriteLine("Transaction committed");
                
                return reservation;
            }
            catch (Exception ex)
            {
                // Rollback the transaction if an error occurs
                await transaction.RollbackAsync();
                Console.WriteLine($"Error in CreateReservationAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> CancelReservationAsync(int reservationId, int memberId)
        {
            // Use a transaction to ensure data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Find the reservation and ensure it belongs to this member
                var reservation = await _context.Reservations
                    .Include(r => r.TeeTime)
                    .FirstOrDefaultAsync(r => r.ReservationID == reservationId && r.MemberID == memberId);

                if (reservation == null)
                {
                    return false;
                }

                // Check if the reservation is already cancelled
                if (reservation.ReservationStatus == "Cancelled")
                {
                    return true; // Already cancelled
                }

                // Update reservation status
                reservation.ReservationStatus = "Cancelled";
                
                // Update TeeTime player count
                if (reservation.TeeTime != null)
                {
                    reservation.TeeTime.TotalPlayersBooked -= reservation.NumberOfPlayers;
                    // Ensure we don't go below zero
                    if (reservation.TeeTime.TotalPlayersBooked < 0)
                        reservation.TeeTime.TotalPlayersBooked = 0;
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<Member?> GetMemberByUserIdAsync(int userId)
        {
            return await _context.Members
                .Include(m => m.MembershipCategory)
                .FirstOrDefaultAsync(m => m.UserID == userId);
        }
    }
}
