using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data.Interfaces;
using TeeTime.Models;

namespace TeeTime.Data.Repositories
{
    public class StandingTeeTimeRepository : IStandingTeeTimeRepository
    {
        private readonly TeeTimeDbContext _context;

        public StandingTeeTimeRepository(TeeTimeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Finds all approved Standing Tee Time Requests that are active
        /// for the given date and day of the week.
        /// </summary>
        /// <param name="date">The target date.</param>
        /// <returns>A list of matching standing requests.</returns>
        public async Task<List<StandingTeeTimeRequest>> GetActiveRequestsForDateAsync(DateTime date)
        {
            var targetDate = date.Date;
            var targetDayOfWeek = date.DayOfWeek;
            const string approvedStatus = "Approved"; // Define constant for status

            // Ensure the query compares only the Date part for StartDate and EndDate
            return await _context.StandingTeeTimeRequests
                .Where(req => req.Status == approvedStatus &&
                              req.DayOfWeek == targetDayOfWeek &&
                              req.StartDate.Date <= targetDate &&
                              req.EndDate.Date >= targetDate)
                .ToListAsync(); 
        }
    }
} 