using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data.Interfaces;
using TeeTime.Models;
using TeeTime.Models.TeeSheet; // Required for TeeSheet and TeeTime models
using Microsoft.EntityFrameworkCore; // For potential Include() usage in repositories, good practice here

namespace TeeTime.Services
{
    public class TeeSheetService // Consider creating an ITeeSheetService interface
    {
        private readonly IStandingTeeTimeRepository _standingRequestRepo;
        private readonly ITeeSheetRepository _teeSheetRepo;
        private readonly ILogger<TeeSheetService> _logger;

        // Constructor for Dependency Injection
        public TeeSheetService(
            IStandingTeeTimeRepository standingRequestRepo,
            ITeeSheetRepository teeSheetRepo,
            ILogger<TeeSheetService> logger)
        {
            _standingRequestRepo = standingRequestRepo ?? throw new ArgumentNullException(nameof(standingRequestRepo));
            _teeSheetRepo = teeSheetRepo ?? throw new ArgumentNullException(nameof(teeSheetRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates a Tee Sheet for the given date, automatically scheduling
        /// applicable Standing Tee Time Requests based on priority.
        /// This method should be called when the clerk initiates tee sheet creation.
        /// </summary>
        /// <param name="date">The date for the new Tee Sheet.</param>
        /// <returns>The created TeeSheet populated with standing reservations.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a TeeSheet for this date already exists.</exception>
        /// <exception cref="Exception">Rethrows exceptions from repository layers on critical failures.</exception>
        public async Task<Models.TeeSheet.TeeSheet> GenerateTeeSheetWithStandingRequestsAsync(DateTime date)
        {
            var targetDate = date.Date;
            _logger.LogInformation("Initiating Tee Sheet generation for: {TargetDate}", targetDate);

            if (await _teeSheetRepo.ExistsAsync(targetDate))
            {
                _logger.LogWarning("Tee Sheet generation cancelled: A Tee Sheet already exists for {TargetDate}.", targetDate);
                throw new InvalidOperationException($"A Tee Sheet already exists for {targetDate}.");
            }

            List<StandingTeeTimeRequest> activeRequests;
            try
            {
                activeRequests = await _standingRequestRepo.GetActiveRequestsForDateAsync(targetDate);
                _logger.LogInformation("Found {RequestCount} active standing requests for {TargetDate}", activeRequests.Count, targetDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve standing tee time requests for {TargetDate}", targetDate);
                throw;
            }

            var newTeeSheet = new Models.TeeSheet.TeeSheet
            {
                Date = targetDate,
                TeeTimes = new List<Models.TeeSheet.TeeTime>()
            };

            var prioritizedRequests = activeRequests
                                        .Where(req => req.PriorityNumber.HasValue)
                                        .OrderBy(req => req.PriorityNumber);

            var occupiedSlots = new HashSet<TimeSpan>();
            var requestsToProcessLater = new List<StandingTeeTimeRequest>();

            // --- Pass 1: Create initial TeeTime objects for standing requests --- 
            foreach (var request in prioritizedRequests)
            {
                TimeSpan? bookingTimeSpan = request.DesiredTeeTime;

                if (!bookingTimeSpan.HasValue)
                {
                    _logger.LogWarning("Standing Request ID {RequestId} (Priority {Priority}) has no Desired Time. Skipping.", request.RequestID, request.PriorityNumber);
                    continue;
                }

                TimeSpan timeSlot = bookingTimeSpan.Value;

                if (occupiedSlots.Contains(timeSlot))
                {
                    _logger.LogWarning("Time slot conflict during generation for Standing Request ID {RequestId} (Priority {Priority}) at {BookingTime}. Slot taken by higher priority request.", request.RequestID, request.PriorityNumber, timeSlot);
                    continue;
                }

                // Create the TeeTime initially empty (no reservations yet)
                var initialTeeTime = new Models.TeeSheet.TeeTime
                {
                    StartTime = targetDate.Add(timeSlot),
                    TotalPlayersBooked = 0, // Start with 0
                    IsAvailable = true, // Start as available
                    Notes = string.Empty, // Start with empty notes
                    Reservations = new List<Reservation>() // Start with empty list
                };
                
                newTeeSheet.TeeTimes.Add(initialTeeTime);
                occupiedSlots.Add(timeSlot);
                requestsToProcessLater.Add(request); // Keep track to add reservations later

                _logger.LogInformation("Prepared initial TeeTime slot at {BookingTime} for Standing Request ID {RequestId} (Priority {Priority}).", timeSlot, request.RequestID, request.PriorityNumber);

            } // End Pass 1 foreach loop

            // --- Generate Standard Empty TeeTime Slots ---
             _logger.LogInformation("Generating standard empty tee time slots for {TargetDate}", targetDate);
            TimeSpan firstStdTeeTime = new TimeSpan(7, 0, 0); 
            TimeSpan lastStdTeeTime = new TimeSpan(18, 0, 0);
            int[] fixedIntervals = { 0, 7, 15, 22, 30, 37, 45, 52 };
            int standardSlotsAdded = 0;
            DateTime morningStart = targetDate.Add(firstStdTeeTime);
            DateTime eveningEnd = targetDate.Add(lastStdTeeTime);

            for (int hour = firstStdTeeTime.Hours; hour <= lastStdTeeTime.Hours; hour++)
            {
                foreach (int minutes in fixedIntervals)
                {
                    TimeSpan standardTimeSpan = new TimeSpan(hour, minutes, 0);
                    DateTime standardDateTime = targetDate.Add(standardTimeSpan);
                    if (standardDateTime < morningStart || standardDateTime > eveningEnd) continue;

                    if (!occupiedSlots.Contains(standardTimeSpan))
                    {
                        var standardTeeTime = new Models.TeeSheet.TeeTime
                        {
                            StartTime = standardDateTime,
                            TotalPlayersBooked = 0,
                            IsAvailable = true,
                            Reservations = new List<Reservation>(),
                            Notes = string.Empty
                        };
                        newTeeSheet.TeeTimes.Add(standardTeeTime);
                        standardSlotsAdded++;
                    }
                }
            }
            _logger.LogInformation("Added {StandardSlotsCount} standard empty tee time slots for {TargetDate}", standardSlotsAdded, targetDate);

            newTeeSheet.TeeTimes = newTeeSheet.TeeTimes.OrderBy(tt => tt.StartTime).ToList();
            
            // --- Save Pass 1: Create TeeSheet and initial TeeTimes --- 
            Models.TeeSheet.TeeSheet savedTeeSheet;
            try
            {
                _logger.LogInformation("Attempting initial save for TeeSheet and {TeeTimeCount} TeeTimes for {TargetDate}...", newTeeSheet.TeeTimes.Count, targetDate);
                savedTeeSheet = await _teeSheetRepo.CreateTeeSheetAsync(newTeeSheet);
                _logger.LogInformation("Successfully saved initial Tee Sheet ID {TeeSheetId} for {TargetDate}.", savedTeeSheet.Id, targetDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save the initial generated Tee Sheet for {TargetDate}. Aborting further processing.", targetDate);
                throw; 
            }

            // --- Pass 2: Update TeeTimes with Reservations for Standing Requests ---
            _logger.LogInformation("Starting Pass 2: Adding reservations for {RequestCount} standing requests.", requestsToProcessLater.Count);
            foreach(var request in requestsToProcessLater)
            {
                TimeSpan? bookingTimeSpan = request.DesiredTeeTime;
                if (!bookingTimeSpan.HasValue) continue; // Should not happen based on Pass 1

                DateTime targetStartTime = targetDate.Add(bookingTimeSpan.Value);

                // Find the TeeTime that was just saved in Pass 1
                var savedTeeTime = savedTeeSheet.TeeTimes.FirstOrDefault(tt => tt.StartTime == targetStartTime);

                if (savedTeeTime == null)
                {
                    _logger.LogError("Could not find the saved TeeTime for StartTime {StartTime} for Standing Request ID {RequestId}. Reservation cannot be added.", targetStartTime, request.RequestID);
                    continue;
                }
                
                _logger.LogInformation("Processing reservations for saved TeeTime ID {TeeTimeId} (Request ID {RequestId}).", savedTeeTime.Id, request.RequestID);
                
                List<Reservation> reservationsToAdd = new List<Reservation>();
                int playersAdded = 0;

                // Helper to create reservations
                Action<int?> addResIfNeeded = (playerId) => {
                    if (playerId.HasValue && playerId.Value > 0) {
                         var reservation = new Reservation {
                             // TeeTimeId will be set by EF Core/Repo logic
                             MemberID = playerId.Value,
                             ReservationMadeDate = DateTime.UtcNow,
                             ReservationStatus = "Confirmed",
                             NumberOfPlayers = 1, // Treat each as individual
                             NumberOfCarts = 0,
                             StandingRequestID = request.RequestID,
                             Type = ReservationType.Standing,
                             TeeTimeId = savedTeeTime.Id // Explicitly set FK
                         };
                         reservationsToAdd.Add(reservation);
                         playersAdded++;
                    }
                };

                addResIfNeeded(request.MemberID);
                addResIfNeeded(request.Player2ID);
                addResIfNeeded(request.Player3ID);
                addResIfNeeded(request.Player4ID);

                if (playersAdded > 0)
                {
                    savedTeeTime.TotalPlayersBooked = playersAdded;
                    savedTeeTime.Notes = $"Booked via Standing Request ID: {request.RequestID}";
                    // Update IsAvailable based on new TotalPlayersBooked
                    // savedTeeTime.IsAvailable = savedTeeTime.TotalPlayersBooked < savedTeeTime.MaxPlayers;
                    // IsAvailable is calculated property, setting TotalPlayersBooked is enough.

                    try
                    {
                        // Call repo to update TeeTime and add reservations in a second SaveChanges
                        // We add reservations one by one here for simplicity, repo handles SaveChanges
                        foreach(var res in reservationsToAdd)
                        {
                            // Pass the updated TeeTime and the single reservation to add
                            await _teeSheetRepo.UpdateTeeTimeAndAddReservationAsync(savedTeeTime, res);
                            _logger.LogInformation("Successfully processed reservation for Member {MemberId} in TeeTime ID {TeeTimeId}.", res.MemberID, savedTeeTime.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update TeeTime ID {TeeTimeId} or add reservations for Standing Request ID {RequestId}.", savedTeeTime.Id, request.RequestID);
                        // Decide on error handling: continue? attempt rollback? log and proceed?
                    }
                }
                else
                {
                    _logger.LogWarning("No valid players found for Standing Request ID {RequestId}. No reservations added.", request.RequestID);
                }
            }

            // --- Return the TeeSheet (it might be slightly stale if updates failed, consider re-fetching?) ---
            // For now, return the object we have after processing.
            return savedTeeSheet;
        }

        /// <summary>
        /// Helper method to create and add a reservation for a single player to a TeeTime.
        /// </summary>
        private bool AddPlayerReservation(Models.TeeSheet.TeeTime teeTime, int memberId, int standingRequestId)
        {
            if (memberId <= 0) return false; // Basic validation

            var reservation = new Reservation
            {
                // TeeTimeId will be set by EF Core
                MemberID = memberId,
                ReservationMadeDate = DateTime.UtcNow, // Use UTC for server time
                ReservationStatus = "Confirmed", // Default status
                NumberOfPlayers = 1, // Each reservation represents one player here
                NumberOfCarts = 0, // Default, might need adjustment based on rules
                StandingRequestID = standingRequestId, // Link back to the request
                Type = ReservationType.Standing // Mark as generated
            };
            
            teeTime.Reservations.Add(reservation);
            return true;
        }
    }
} 