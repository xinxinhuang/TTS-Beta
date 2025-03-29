using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<TeeTimeApiController> _logger;

        public TeeTimeApiController(TeeTimeDbContext context, ILogger<TeeTimeApiController> logger)
        {
            _context = context;
            _logger = logger;
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

                // Calculate date range (today to 14 days ahead)
                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(14);

                // Get all tee times in the date range
                var scheduledTimes = await _context.TeeTimes
                    .Where(t => t.StartTime.Date >= startDate && t.StartTime.Date <= endDate)
                    .ToListAsync();

                // Get all reservations for these times
                var reservationCounts = await _context.Reservations
                    .Where(r => r.TeeTime != null && 
                               r.TeeTime.StartTime.Date >= startDate && 
                               r.TeeTime.StartTime.Date <= endDate)
                    .GroupBy(r => r.TeeTime!.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalReservations = g.Count(),
                        PlayersCount = g.Sum(r => r.NumberOfPlayers)
                    })
                    .ToListAsync();

                // Get count of tee times for each date
                var teeTimesByDate = scheduledTimes
                    .GroupBy(t => t.StartTime.Date)
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

                // Return the results as JSON
                return Ok(new
                {
                    FullyBooked = fullyBooked,
                    LimitedAvailability = limitedAvailability
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting availability");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("generateteetimes")]
        public async Task<IActionResult> GenerateTeeTimesForDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int startHour = 7,
            [FromQuery] int endHour = 18,
            [FromQuery] int intervalMinutes = 10)
        {
            try
            {
                // Default to 7 days if no end date provided
                if (endDate == null)
                {
                    endDate = startDate.AddDays(7);
                }

                var dailyStartTime = new TimeSpan(startHour, 0, 0);
                var dailyEndTime = new TimeSpan(endHour, 0, 0);

                await TeeTimeGenerator.GenerateTeeTimesForDateRangeAsync(
                    _context,
                    startDate,
                    endDate.Value,
                    dailyStartTime,
                    dailyEndTime,
                    intervalMinutes);

                return Ok(new
                {
                    success = true,
                    message = $"Generated tee times from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                    details = $"Time range: {startHour}:00 - {endHour}:00, Interval: {intervalMinutes} minutes"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating tee times",
                    error = ex.Message
                });
            }
        }

        private async Task<Member?> GetCurrentMemberAsync()
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return null;
                }

                // Get member info
                return await _context.Members
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.UserID == userId);
            }
            catch
            {
                return null;
            }
        }
    }
}
