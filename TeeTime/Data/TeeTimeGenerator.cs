using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Models;
using TeeTime.Models.TeeSheet;

namespace TeeTime.Data
{
    public static class TeeTimeGenerator
    {
        public static async Task GenerateTeeTimesForDateRangeAsync(
            TeeTimeDbContext context, 
            DateTime startDate, 
            DateTime endDate, 
            TimeSpan dailyStartTime = default, 
            TimeSpan dailyEndTime = default,
            int intervalMinutes = 10)
        {
            if (dailyStartTime == default)
            {
                dailyStartTime = new TimeSpan(7, 0, 0); // 7:00 AM
            }

            if (dailyEndTime == default)
            {
                dailyEndTime = new TimeSpan(18, 0, 0); // 6:00 PM
            }

            // Create a list to hold all the tee times to be added
            var teeTimes = new List<Models.TeeSheet.TeeTime>();
            var teeSheets = new List<Models.TeeSheet.TeeSheet>();

            // Loop through each day in the range
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                Console.WriteLine($"Generating tee times for {date.ToShortDateString()}");

                // Check if a tee sheet already exists for this date
                var existingTeeSheet = await context.TeeSheets
                    .FirstOrDefaultAsync(ts => ts.Date.Date == date.Date);

                // If not, create a new one
                if (existingTeeSheet == null)
                {
                    existingTeeSheet = new Models.TeeSheet.TeeSheet
                    {
                        Date = date,
                        StartTime = date.Add(dailyStartTime),
                        EndTime = date.Add(dailyEndTime),
                        IntervalMinutes = intervalMinutes,
                        IsActive = true
                    };
                    
                    context.TeeSheets.Add(existingTeeSheet);
                    await context.SaveChangesAsync();
                    
                    Console.WriteLine($"Created new tee sheet for {date.ToShortDateString()} with ID {existingTeeSheet.Id}");
                }
                else
                {
                    Console.WriteLine($"Found existing tee sheet for {date.ToShortDateString()} with ID {existingTeeSheet.Id}");
                }

                // Check if tee times already exist for this date
                var existingTeeTimesCount = await context.TeeTimes
                    .CountAsync(tt => tt.StartTime.Date == date.Date);

                if (existingTeeTimesCount > 0)
                {
                    Console.WriteLine($"Skipping {date.ToShortDateString()} as it already has {existingTeeTimesCount} tee times");
                    continue;
                }

                // Get applicable standing tee time requests for this day
                var dayOfWeek = date.DayOfWeek.ToString();
                var standingRequests = await context.StandingTeeTimeRequests
                    .Where(r => r.DayOfWeek == dayOfWeek &&
                               r.StartDate <= date &&
                               r.EndDate >= date &&
                               r.ApprovedTeeTime != null &&
                               r.ApprovedByUserID != null)
                    .OrderBy(r => r.PriorityNumber)
                    .ToListAsync();

                Console.WriteLine($"Found {standingRequests.Count} standing tee time requests for {date.ToShortDateString()}");

                // Calculate number of tee times to generate for this day
                var currentTime = dailyStartTime;
                var timeIncrement = TimeSpan.FromMinutes(intervalMinutes);

                // Create a dictionary to keep track of which times are reserved for standing requests
                var reservedTimes = new Dictionary<TimeSpan, StandingTeeTimeRequest>();

                // Pre-reserve times for standing requests based on their priority
                foreach (var request in standingRequests)
                {
                    if (request.ApprovedTeeTime.HasValue)
                    {
                        // Round to nearest interval
                        var approvedTime = request.ApprovedTeeTime.Value;
                        var minutesRemainder = approvedTime.Minutes % intervalMinutes;
                        var roundedTime = approvedTime;
                        
                        if (minutesRemainder > 0)
                        {
                            if (minutesRemainder >= intervalMinutes / 2)
                            {
                                // Round up
                                roundedTime = new TimeSpan(
                                    approvedTime.Hours,
                                    approvedTime.Minutes + (intervalMinutes - minutesRemainder),
                                    0);
                            }
                            else
                            {
                                // Round down
                                roundedTime = new TimeSpan(
                                    approvedTime.Hours,
                                    approvedTime.Minutes - minutesRemainder,
                                    0);
                            }
                        }
                        
                        // Check if this time is within the allowed range
                        if (roundedTime >= dailyStartTime && roundedTime <= dailyEndTime)
                        {
                            reservedTimes[roundedTime] = request;
                            Console.WriteLine($"Reserved {roundedTime} for standing request ID {request.RequestID}");
                        }
                    }
                }

                while (currentTime <= dailyEndTime)
                {
                    var startTime = date.Date.Add(currentTime);
                    
                    // Create a new tee time
                    var teeTime = new Models.TeeSheet.TeeTime
                    {
                        StartTime = startTime,
                        TeeSheetId = existingTeeSheet.Id,
                        TotalPlayersBooked = 0,
                        Notes = string.Empty
                    };
                    
                    // Check if this time is reserved for a standing request
                    if (reservedTimes.TryGetValue(currentTime, out var standingRequest))
                    {
                        // Mark this tee time as reserved for the standing request
                        teeTime.TotalPlayersBooked = 4; // Foursome
                        teeTime.Notes = $"Standing tee time for {standingRequest.Member?.User?.FirstName} {standingRequest.Member?.User?.LastName}";
                        
                        // Create reservations for this standing tee time
                        await CreateStandingTeeTimeReservationAsync(context, standingRequest, teeTime);
                    }
                    
                    // Add to the context
                    context.TeeTimes.Add(teeTime);
                    
                    currentTime = currentTime.Add(timeIncrement);
                }
                
                // Save all tee times for this day
                await context.SaveChangesAsync();
                
                // Count how many were created
                var createdCount = await context.TeeTimes
                    .CountAsync(tt => tt.TeeSheetId == existingTeeSheet.Id);
                
                Console.WriteLine($"Created {createdCount} tee times for {date.ToShortDateString()}");
            }
        }

        private static async Task CreateStandingTeeTimeReservationAsync(
            TeeTimeDbContext context,
            StandingTeeTimeRequest standingRequest,
            Models.TeeSheet.TeeTime teeTime)
        {
            // Create a reservation record for this standing tee time
            var reservation = new Reservation
            {
                MemberID = standingRequest.MemberID,
                TeeTimeId = teeTime.Id,
                NumberOfPlayers = 4,
                ReservationMadeDate = DateTime.Now,
                Notes = $"Standing tee time (ID: {standingRequest.RequestID})",
                IsStandingTeeTime = true,
                StandingRequestID = standingRequest.RequestID
            };
            
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Created reservation for standing tee time request {standingRequest.RequestID}");
        }
    }
}
