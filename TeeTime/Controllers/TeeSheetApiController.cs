using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeeSheetController : ControllerBase
    {
        private readonly TeeTimeDbContext _context;

        public TeeSheetController(TeeTimeDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetEvents")]
        public async Task<ActionResult<IEnumerable<object>>> GetEvents(string start, string end)
        {
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
            {
                return BadRequest("Start and end dates are required");
            }

            try
            {
                DateTime startDate = DateTime.Parse(start);
                DateTime endDate = DateTime.Parse(end);

                // Get all tee times within the date range
                var teeTimes = await _context.ScheduledGolfTimes
                    .Include(t => t.Reservations)
                        .ThenInclude(r => r.Member)
                            .ThenInclude(m => m.User)
                    .Where(t => t.ScheduledDate >= startDate.Date && t.ScheduledDate <= endDate.Date)
                    .ToListAsync();

                // Get all events (blocked times) within the date range
                var events = await _context.Events
                    .Where(e => e.EventDate >= startDate.Date && e.EventDate <= endDate.Date)
                    .ToListAsync();

                // Convert tee times to calendar events
                var calendarEvents = new List<object>();

                // Add regular tee times
                foreach (var teeTime in teeTimes)
                {
                    DateTime startDateTime = teeTime.ScheduledDate.Add(teeTime.ScheduledTime);
                    DateTime endDateTime = startDateTime.AddMinutes(teeTime.GolfSessionInterval);

                    if (teeTime.Reservations != null && teeTime.Reservations.Any())
                    {
                        // Add booked tee times
                        foreach (var reservation in teeTime.Reservations)
                        {
                            string memberName = "Unknown";
                            if (reservation.Member?.User != null)
                            {
                                memberName = $"{reservation.Member.User.FirstName} {reservation.Member.User.LastName}";
                            }

                            calendarEvents.Add(new
                            {
                                id = $"reservation_{reservation.ReservationID}",
                                title = memberName,
                                start = startDateTime.ToString("o"),
                                end = endDateTime.ToString("o"),
                                backgroundColor = "#3788d8",
                                borderColor = "#2C6FB7",
                                textColor = "#ffffff",
                                extendedProps = new
                                {
                                    isReservation = true,
                                    players = reservation.NumberOfPlayers,
                                    carts = reservation.NumberOfCarts
                                }
                            });
                        }
                    }
                    else if (!teeTime.IsAvailable)
                    {
                        // Check if this is part of an event
                        var eventItem = events.FirstOrDefault(e => 
                            e.EventDate.Date == teeTime.ScheduledDate.Date && 
                            e.StartTime <= teeTime.ScheduledTime && 
                            e.EndTime >= teeTime.ScheduledTime);

                        if (eventItem != null)
                        {
                            // Skip individual blocked times that are part of an event
                            // We'll add the event as a whole later
                            continue;
                        }

                        // Add blocked tee times (not part of an event)
                        calendarEvents.Add(new
                        {
                            id = $"blocked_{teeTime.ScheduledGolfTimeID}",
                            title = "Blocked",
                            start = startDateTime.ToString("o"),
                            end = endDateTime.ToString("o"),
                            backgroundColor = "#dc3545",
                            borderColor = "#b02a37",
                            textColor = "#ffffff"
                        });
                    }
                    else
                    {
                        // Add available tee times
                        calendarEvents.Add(new
                        {
                            id = $"available_{teeTime.ScheduledGolfTimeID}",
                            title = "Available",
                            start = startDateTime.ToString("o"),
                            end = endDateTime.ToString("o"),
                            backgroundColor = "#28a745",
                            borderColor = "#208537",
                            textColor = "#ffffff"
                        });
                    }
                }

                // Add events (tournaments, etc.)
                foreach (var eventItem in events)
                {
                    DateTime startDateTime = eventItem.EventDate.Add(eventItem.StartTime);
                    DateTime endDateTime = eventItem.EventDate.Add(eventItem.EndTime);

                    calendarEvents.Add(new
                    {
                        id = $"event_{eventItem.EventID}",
                        title = eventItem.EventName,
                        start = startDateTime.ToString("o"),
                        end = endDateTime.ToString("o"),
                        backgroundColor = "#fd7e14",
                        borderColor = "#ca6510",
                        textColor = "#ffffff",
                        extendedProps = new
                        {
                            isEvent = true
                        }
                    });
                }

                return Ok(calendarEvents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
