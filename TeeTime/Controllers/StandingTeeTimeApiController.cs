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
    [Route("api/standingteetimes")]
    [ApiController]
    [Authorize]
    public class StandingTeeTimeApiController : ControllerBase
    {
        private readonly TeeTimeDbContext _context;
        private readonly ILogger<StandingTeeTimeApiController> _logger;

        public StandingTeeTimeApiController(TeeTimeDbContext context, ILogger<StandingTeeTimeApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyStandingRequests()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Find member by user ID
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.UserID.ToString() == userId);

                if (member == null)
                {
                    return NotFound("Member record not found.");
                }

                // Check eligibility
                if (!await IsEligibleForStandingTeeTime(member))
                {
                    return BadRequest("You are not eligible for standing tee times. Only Shareholder members may request standing tee times.");
                }

                // Get requests for this member
                var requests = await _context.StandingTeeTimeRequests
                    .Where(r => r.MemberID == member.MemberID)
                    .OrderByDescending(r => r.StartDate)
                    .Select(r => new
                    {
                        r.RequestID,
                        r.DayOfWeek,
                        r.StartDate,
                        r.EndDate,
                        RequestedTime = r.DesiredTeeTime.ToString(@"hh\:mm"),
                        r.PriorityNumber,
                        Status = r.ApprovedTeeTime.HasValue ? "Approved" : "Pending",
                        ApprovedTime = r.ApprovedTeeTime != null ? r.ApprovedTeeTime.Value.ToString(@"hh\:mm") : null,
                        ApprovedDate = r.ApprovedDate
                    }).ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving standing tee time requests");
                return StatusCode(500, "An error occurred while retrieving the standing tee time requests.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RequestStandingTeeTime([FromBody] StandingTeeTimeRequestViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Find member by user ID
                var member = await _context.Members
                    .Include(m => m.MembershipCategory)
                    .FirstOrDefaultAsync(m => m.UserID.ToString() == userId);

                if (member == null)
                {
                    return NotFound("Member record not found.");
                }

                // Check eligibility
                if (!await IsEligibleForStandingTeeTime(member))
                {
                    return BadRequest("You are not eligible for standing tee times. Only Shareholder members may request standing tee times.");
                }

                // Validate player IDs are unique
                var playerIds = new List<int> { model.Player2ID, model.Player3ID, model.Player4ID };
                if (playerIds.Contains(member.MemberID) || playerIds.Distinct().Count() != 3)
                {
                    return BadRequest("All players in the foursome must be different.");
                }

                // Validate dates
                if (model.EndDate < model.StartDate)
                {
                    return BadRequest("End date must be after start date.");
                }

                // **New Validation**
                if (string.IsNullOrEmpty(model.DayOfWeek))
                {
                    return BadRequest("Day of Week is required.");
                }

                if (string.IsNullOrEmpty(model.DesiredTeeTime) || !TimeSpan.TryParse(model.DesiredTeeTime, out var desiredTimeSpan))
                {
                    return BadRequest("Valid Desired Tee Time in HH:mm format is required.");
                }
                // **End New Validation**

                // Create the request - without Player IDs for now
                var request = new StandingTeeTimeRequest
                {
                    MemberID = member.MemberID,
                    // Player IDs are commented out until database schema is updated
                    // Player2ID = model.Player2ID,
                    // Player3ID = model.Player3ID,
                    // Player4ID = model.Player4ID,
                    DayOfWeek = model.DayOfWeek, // Safe after validation
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    DesiredTeeTime = desiredTimeSpan, // Safe after validation
                };

                // Save player IDs in temporary table or cache if needed
                // This is just a placeholder until the schema is updated

                _context.StandingTeeTimeRequests.Add(request);
                await _context.SaveChangesAsync();

                return Ok(new { RequestID = request.RequestID, Message = "Standing tee time request submitted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting standing tee time request");
                return StatusCode(500, "An error occurred while submitting the standing tee time request.");
            }
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Committee")]
        public async Task<IActionResult> GetAllPendingRequests()
        {
            try
            {
                // Get all pending requests
                var requests = await _context.StandingTeeTimeRequests
                    .Where(r => r.ApprovedTeeTime == null)
                    .OrderBy(r => r.StartDate)
                    .Select(r => new
                    {
                        r.RequestID,
                        r.MemberID,
                        MemberName = _context.Members
                            .Where(m => m.MemberID == r.MemberID)
                            .Join(_context.Users,
                                m => m.UserID,
                                u => u.UserID,
                                (m, u) => $"{u.FirstName} {u.LastName}")
                            .FirstOrDefault() ?? "Unknown",
                        r.DayOfWeek,
                        r.StartDate,
                        r.EndDate,
                        RequestedTime = r.DesiredTeeTime.ToString(@"hh\:mm"),
                        // Don't include Player2, Player3, Player4 details yet
                    }).ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending standing tee time requests");
                return StatusCode(500, "An error occurred while retrieving the pending standing tee time requests.");
            }
        }

        [HttpGet("admin/approved")]
        [Authorize(Roles = "Committee")]
        public async Task<IActionResult> GetAllApprovedRequests()
        {
            try
            {
                // Get all approved requests
                var requests = await _context.StandingTeeTimeRequests
                    .Where(r => r.ApprovedTeeTime != null)
                    .OrderBy(r => r.DayOfWeek)
                    .ThenBy(r => r.ApprovedTeeTime)
                    .Select(r => new
                    {
                        r.RequestID,
                        r.MemberID,
                        MemberName = _context.Members
                            .Where(m => m.MemberID == r.MemberID)
                            .Join(_context.Users,
                                m => m.UserID,
                                u => u.UserID,
                                (m, u) => $"{u.FirstName} {u.LastName}")
                            .FirstOrDefault() ?? "Unknown",
                        r.DayOfWeek,
                        r.StartDate,
                        r.EndDate,
                        ApprovedTime = r.ApprovedTeeTime.Value.ToString(@"hh\:mm"),
                        r.PriorityNumber
                        // Don't include Player2, Player3, Player4 details yet
                    }).ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving approved standing tee time requests");
                return StatusCode(500, "An error occurred while retrieving the approved standing tee time requests.");
            }
        }

        [HttpPost("admin/approve")]
        [Authorize(Roles = "Committee")]
        public async Task<IActionResult> ApproveRequest([FromBody] ApproveRequestViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int approverUserId))
                {
                    return Unauthorized();
                }

                // Find the request
                var request = await _context.StandingTeeTimeRequests
                    .FirstOrDefaultAsync(r => r.RequestID == model.RequestID);

                if (request == null)
                {
                    return NotFound("Standing tee time request not found.");
                }

                // Validate ApprovedTeeTime format before parsing
                if (!TimeSpan.TryParse(model.ApprovedTeeTime, out var approvedTimeSpan))
                {
                    return BadRequest("Invalid Approved Tee Time format. Please use HH:mm format.");
                }

                // Update with approval
                request.ApprovedTeeTime = approvedTimeSpan;
                request.ApprovedByUserID = approverUserId;
                request.ApprovedDate = DateTime.Now;
                request.PriorityNumber = model.PriorityNumber;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Standing tee time request approved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving standing tee time request");
                return StatusCode(500, "An error occurred while approving the standing tee time request.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Committee")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var request = await _context.StandingTeeTimeRequests
                    .FirstOrDefaultAsync(r => r.RequestID == id);

                if (request == null)
                {
                    return NotFound("Standing tee time request not found.");
                }

                _context.StandingTeeTimeRequests.Remove(request);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Standing tee time request deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting standing tee time request");
                return StatusCode(500, "An error occurred while deleting the standing tee time request.");
            }
        }

        private async Task<bool> IsEligibleForStandingTeeTime(Member member)
        {
            if (member == null || member.MembershipCategoryID == 0)
            {
                return false;
            }

            // Get membership category
            var membershipCategory = await _context.MembershipCategories
                .FirstOrDefaultAsync(mc => mc.MembershipCategoryID == member.MembershipCategoryID);

            if (membershipCategory == null)
            {
                return false;
            }

            // Check if membership is Shareholder or similar
            return membershipCategory.MembershipName.Contains("Shareholder") || 
                   membershipCategory.MembershipName.Contains("Gold Associate");
        }
    }

    public class StandingTeeTimeRequestViewModel
    {
        public int Player2ID { get; set; }
        public int Player3ID { get; set; }
        public int Player4ID { get; set; }
        public string? DayOfWeek { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? DesiredTeeTime { get; set; }
    }

    public class ApproveRequestViewModel
    {
        public int RequestID { get; set; }
        public string? ApprovedTeeTime { get; set; }
        public int PriorityNumber { get; set; }
    }
}
