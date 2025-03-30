using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeeTime.Models;

namespace TeeTime.Data.Interfaces
{
    public interface IStandingTeeTimeRepository
    {
        /// <summary>
        /// Finds all approved Standing Tee Time Requests that are active
        /// for the given date and day of the week.
        /// </summary>
        /// <param name="date">The target date.</param>
        /// <returns>A list of matching standing requests.</returns>
        Task<List<StandingTeeTimeRequest>> GetActiveRequestsForDateAsync(DateTime date);
    }
} 