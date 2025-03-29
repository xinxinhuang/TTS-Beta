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

                // Calculate number of tee times to generate for this day
                var currentTime = dailyStartTime;
                var timeIncrement = TimeSpan.FromMinutes(intervalMinutes);

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
    }
}
