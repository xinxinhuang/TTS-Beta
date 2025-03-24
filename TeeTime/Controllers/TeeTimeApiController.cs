using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Controllers
{
    [Route("api/teetime")]
    [ApiController]
    [Authorize]
    public class TeeTimeApiController : ControllerBase
    {
        private readonly TeeTimeDbContext _context;

        public TeeTimeApiController(TeeTimeDbContext context)
        {
            _context = context;
        }

        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability()
        {
            try
            {
                // Get current member
                var member = await GetCurrentMemberAsync();
                if (member == null)
                {
                    return Unauthorized();
                }

                // Calculate date range (today to 30 days ahead for the calendar view)
                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(30);

                // Get all scheduled tee times in the date range
                var scheduledTimes = await _context.ScheduledGolfTimes
                    .Where(t => t.ScheduledDate >= startDate && t.ScheduledDate <= endDate)
                    .ToListAsync();

                // Get all reservations for these times
                var reservationCounts = await _context.Reservations
                    .Where(r => r.ScheduledGolfTime != null && 
                               r.ScheduledGolfTime.ScheduledDate >= startDate && 
                               r.ScheduledGolfTime.ScheduledDate <= endDate)
                    .GroupBy(r => r.ScheduledGolfTime.ScheduledDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalReservations = g.Count(),
                        PlayersCount = g.Sum(r => r.NumberOfPlayers)
                    })
                    .ToListAsync();

                // Get count of tee times for each date
                var teeTimesByDate = scheduledTimes
                    .GroupBy(t => t.ScheduledDate.Date)
                    .ToDictionary(
                        g => g.Key,
                        g => new { 
                            TotalSlots = g.Count(),
                            AvailableSlots = g.Count(t => t.IsAvailable)
                        }
                    );

                // Calculate fully booked and limited availability dates
                var fullyBooked = new List<string>();
                var limitedAvailability = new List<string>();

                foreach (var dateEntry in teeTimesByDate)
                {
                    var date = dateEntry.Key;
                    var formattedDate = date.ToString("yyyy-MM-dd");
                    
                    // Date is fully booked if no available slots
                    if (dateEntry.Value.AvailableSlots == 0)
                    {
                        fullyBooked.Add(formattedDate);
                        continue;
                    }

                    // Check if date has limited availability (less than 25% available)
                    var availabilityPercentage = (double)dateEntry.Value.AvailableSlots / dateEntry.Value.TotalSlots;
                    if (availabilityPercentage <= 0.25)
                    {
                        limitedAvailability.Add(formattedDate);
                    }
                }

                // Get date summaries for the tooltips
                var dateSummaries = new Dictionary<string, object>();
                foreach (var dateEntry in teeTimesByDate)
                {
                    var date = dateEntry.Key;
                    var formattedDate = date.ToString("yyyy-MM-dd");
                    
                    dateSummaries[formattedDate] = new {
                        totalSlots = dateEntry.Value.TotalSlots,
                        availableSlots = dateEntry.Value.AvailableSlots,
                        percentAvailable = Math.Round((double)dateEntry.Value.AvailableSlots / dateEntry.Value.TotalSlots * 100)
                    };
                }
                
                // Return the results as JSON
                return Ok(new
                {
                    fullyBooked,
                    limitedAvailability,
                    dateSummaries
                });
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 error
                return StatusCode(500, new { error = "An error occurred while processing your request." });
            }
        }

        private async Task<Member> GetCurrentMemberAsync()
        {
            // Get the current user ID from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return null;
            }

            // Find the member associated with this user
            return await _context.Members
                .Include(m => m.MembershipCategory)
                .FirstOrDefaultAsync(m => m.UserID == userId);
        }
    }
}
