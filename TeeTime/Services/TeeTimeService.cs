using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Services
{
    public interface ITeeTimeService
    {
        Task<Dictionary<DateTime, List<ScheduledGolfTime>>> GetTeeSheetDataAsync(DateTime startDate, DateTime endDate);
        Dictionary<int, string> GetEventsForTeeTimesAsync(IEnumerable<ScheduledGolfTime> teeTimes);
        Task<List<Reservation>> GetUserReservationsAsync(int memberId);
        Task<ScheduledGolfTime?> GetTeeTimeByIdAsync(int teeTimeId);
        Task<bool> IsTimeFullyBookedAsync(int scheduledGolfTimeId);
        Task<bool> IsDateFullyBookedAsync(DateTime date);
        Task<List<ScheduledGolfTime>> GetAvailableTeeTimesAsync(DateTime date, Member member);
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

        public async Task<Dictionary<DateTime, List<ScheduledGolfTime>>> GetTeeSheetDataAsync(DateTime startDate, DateTime endDate)
        {
            var teeTimes = await _context.ScheduledGolfTimes
                .Include(t => t.Reservations)
                    .ThenInclude(r => r.Member)
                        .ThenInclude(m => m!.User)
                .Include(t => t.Event)
                .Where(t => t.ScheduledDate >= startDate && t.ScheduledDate < endDate)
                .OrderBy(t => t.ScheduledDate)
                .ThenBy(t => t.ScheduledTime)
                .ToListAsync();

            // Group by date
            var teeSheets = new Dictionary<DateTime, List<ScheduledGolfTime>>();
            foreach (var teeTime in teeTimes)
            {
                var date = teeTime.ScheduledDate.Date;
                if (!teeSheets.ContainsKey(date))
                {
                    teeSheets[date] = new List<ScheduledGolfTime>();
                }
                teeSheets[date].Add(teeTime);
            }

            return teeSheets;
        }

        public Dictionary<int, string> GetEventsForTeeTimesAsync(IEnumerable<ScheduledGolfTime> teeTimes)
        {
            var events = new Dictionary<int, string>();

            foreach (var teeTime in teeTimes)
            {
                if (!teeTime.IsAvailable && teeTime.EventID.HasValue)
                {
                    events[teeTime.ScheduledGolfTimeID] = teeTime.Event?.EventName ?? "Special Event";
                }
                else if (!teeTime.IsAvailable && teeTime.Reservations != null && !teeTime.Reservations.Any())
                {
                    events[teeTime.ScheduledGolfTimeID] = "Blocked";
                }
            }

            return events;
        }

        public async Task<List<Reservation>> GetUserReservationsAsync(int memberId)
        {
            return await _context.Reservations
                .Where(r => r.MemberID == memberId && r.ReservationStatus != "Cancelled")
                .Include(r => r.ScheduledGolfTime)
                .OrderBy(r => r.ScheduledGolfTime!.ScheduledDate)
                .ThenBy(r => r.ScheduledGolfTime!.ScheduledTime)
                .ToListAsync();
        }

        public async Task<ScheduledGolfTime?> GetTeeTimeByIdAsync(int teeTimeId)
        {
            return await _context.ScheduledGolfTimes.FindAsync(teeTimeId);
        }

        public async Task<bool> IsTimeFullyBookedAsync(int scheduledGolfTimeId)
        {
            // This is a simplified check - you may need to consider GolfSessionInterval and other factors
            int bookedPlayers = await _context.Reservations
                .Where(r => r.ScheduledGolfTimeID == scheduledGolfTimeId)
                .SumAsync(r => r.NumberOfPlayers);

            // Max players per tee time (e.g., 4 golfers per tee time)
            return bookedPlayers >= 4;
        }

        public async Task<bool> IsDateFullyBookedAsync(DateTime date)
        {
            // Check if all tee times for the given date are booked
            var teeTimesForDate = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == date.Date)
                .ToListAsync();
            
            // If there are no tee times for this date yet, it's not fully booked
            if (!teeTimesForDate.Any())
                return false;
            
            // Count available tee times
            int availableTeeTimesCount = teeTimesForDate.Count(t => t.IsAvailable);
            
            // If no available tee times, the date is fully booked
            return availableTeeTimesCount == 0;
        }

        public async Task<List<ScheduledGolfTime>> GetAvailableTeeTimesAsync(DateTime date, Member member)
        {
            if (member == null)
            {
                return new List<ScheduledGolfTime>();
            }

            // Get all available tee times for the selected date
            var allTimes = await _context.ScheduledGolfTimes
                .Where(t => t.ScheduledDate.Date == date.Date && t.IsAvailable)
                .OrderBy(t => t.ScheduledTime)
                .ToListAsync();

            // Filter times based on membership level
            if (member.MembershipCategory == null)
            {
                return new List<ScheduledGolfTime>();
            }
            
            return FilterTeeTimesByMembershipLevel(allTimes, member.MembershipCategory);
        }

        private List<ScheduledGolfTime> FilterTeeTimesByMembershipLevel(List<ScheduledGolfTime> allTimes, MembershipCategory membershipCategory)
        {
            // Apply time restrictions based on membership level
            if (membershipCategory == null)
            {
                return new List<ScheduledGolfTime>();
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
                    return allTimes.Where(t => t.ScheduledTime.Hours >= 12).ToList();
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
                    return allTimes.Where(t => t.ScheduledTime.Hours >= 14).ToList();
                }
                
                // Restrict weekday morning tee times (before 10am)
                return allTimes.Where(t => t.ScheduledTime.Hours >= 10).ToList();
            }
            
            // Default case - if membership doesn't match known categories
            return allTimes;
        }

        public async Task<Reservation> CreateReservationAsync(int memberId, int teeTimeId, int numberOfPlayers, int numberOfCarts)
        {
            // Verify the selected time is still available
            var selectedTime = await _context.ScheduledGolfTimes.FindAsync(teeTimeId);
            if (selectedTime == null || !selectedTime.IsAvailable)
            {
                throw new InvalidOperationException("This tee time is no longer available");
            }

            // Create the reservation
            var reservation = new Reservation
            {
                MemberID = memberId,
                ScheduledGolfTimeID = teeTimeId,
                ReservationMadeDate = DateTime.Now,
                ReservationStatus = "Confirmed",
                NumberOfPlayers = numberOfPlayers,
                NumberOfCarts = numberOfCarts
            };

            _context.Reservations.Add(reservation);
            
            // Update the scheduled time to be unavailable if fully booked
            if (await IsTimeFullyBookedAsync(selectedTime.ScheduledGolfTimeID))
            {
                selectedTime.IsAvailable = false;
                _context.ScheduledGolfTimes.Update(selectedTime);
            }

            await _context.SaveChangesAsync();
            
            return reservation;
        }

        public async Task<bool> CancelReservationAsync(int reservationId, int memberId)
        {
            // Find the reservation
            var reservation = await _context.Reservations
                .Include(r => r.ScheduledGolfTime)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId && r.MemberID == memberId);

            if (reservation == null)
            {
                throw new InvalidOperationException("Reservation not found or you don't have permission to cancel it.");
            }

            // Update reservation status
            reservation.ReservationStatus = "Cancelled";
            _context.Reservations.Update(reservation);

            // Update the tee time to be available again
            var teeTime = reservation.ScheduledGolfTime;
            if (teeTime != null && !teeTime.IsAvailable)
            {
                teeTime.IsAvailable = true;
                _context.ScheduledGolfTimes.Update(teeTime);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Member?> GetMemberByUserIdAsync(int userId)
        {
            return await _context.Members
                .Include(m => m.MembershipCategory)
                .FirstOrDefaultAsync(m => m.UserID == userId);
        }
    }
}
