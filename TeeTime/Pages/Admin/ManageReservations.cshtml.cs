using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;
using TeeTime.Services;

namespace TeeTime.Pages.Admin
{
    [Authorize(Roles = "Clerk,Pro Shop Staff,Committee")]
    public class ManageReservationsModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        private readonly ITeeTimeService _teeTimeService;
        
        public ManageReservationsModel(TeeTimeDbContext context, ITeeTimeService teeTimeService)
        {
            _context = context;
            _teeTimeService = teeTimeService;
        }
        
        public List<Reservation> Reservations { get; set; } = new();
        public string SearchTerm { get; set; } = "";
        public DateTime? DateFilter { get; set; }
        
        [BindProperty]
        public int ReservationToCancel { get; set; }
        
        [BindProperty]
        public int ReservationToEdit { get; set; }
        
        [BindProperty]
        public int NumberOfPlayers { get; set; }
        
        [BindProperty]
        public int NumberOfCarts { get; set; }
        
        public async Task OnGetAsync(string searchTerm = "", DateTime? dateFilter = null)
        {
            SearchTerm = searchTerm;
            DateFilter = dateFilter;
            
            await LoadReservationsAsync();
        }
        
        private async Task LoadReservationsAsync()
        {
            var query = _context.Reservations
                .Include(r => r.Member)
                    .ThenInclude(m => m!.User)
                .Include(r => r.TeeTime)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(r => 
                    r.Member!.User!.FirstName!.Contains(SearchTerm) || 
                    r.Member.User.LastName!.Contains(SearchTerm) ||
                    r.Member.User.Email!.Contains(SearchTerm));
            }
            
            if (DateFilter.HasValue)
            {
                var date = DateFilter.Value.Date;
                query = query.Where(r => r.TeeTime!.StartTime.Date == date);
            }
            
            Reservations = await query
                .OrderByDescending(r => r.TeeTime!.StartTime)
                .ToListAsync();
        }
        
        public async Task<IActionResult> OnPostCancelReservationAsync()
        {
            if (ReservationToCancel <= 0)
            {
                TempData["ErrorMessage"] = "Invalid reservation ID";
                return RedirectToPage();
            }
            
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Member)
                        .ThenInclude(m => m!.User)
                    .FirstOrDefaultAsync(r => r.ReservationID == ReservationToCancel);
                    
                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found";
                    return RedirectToPage();
                }
                
                reservation.ReservationStatus = "Cancelled";
                _context.Reservations.Update(reservation);
                await _context.SaveChangesAsync();
                
                string memberName = reservation.Member?.User != null 
                    ? $"{reservation.Member.User.FirstName} {reservation.Member.User.LastName}"
                    : "Member";
                    
                TempData["SuccessMessage"] = $"Reservation for {memberName} has been cancelled";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error cancelling reservation: {ex.Message}";
            }
            
            return RedirectToPage(new { searchTerm = SearchTerm, dateFilter = DateFilter?.ToString("yyyy-MM-dd") });
        }
        
        public async Task<IActionResult> OnPostUpdateReservationAsync()
        {
            if (ReservationToEdit <= 0)
            {
                TempData["ErrorMessage"] = "Invalid reservation ID";
                return RedirectToPage();
            }
            
            try
            {
                // Validate the new values
                if (NumberOfPlayers < 1 || NumberOfPlayers > 4)
                {
                    TempData["ErrorMessage"] = "Number of players must be between 1 and 4";
                    return RedirectToPage();
                }
                
                if (NumberOfCarts < 0 || NumberOfCarts > 2)
                {
                    TempData["ErrorMessage"] = "Number of carts must be between 0 and 2";
                    return RedirectToPage();
                }
                
                var reservation = await _context.Reservations
                    .Include(r => r.Member)
                        .ThenInclude(m => m!.User)
                    .Include(r => r.TeeTime)
                    .FirstOrDefaultAsync(r => r.ReservationID == ReservationToEdit);
                    
                if (reservation == null)
                {
                    TempData["ErrorMessage"] = "Reservation not found";
                    return RedirectToPage();
                }
                
                // Update the reservation
                reservation.NumberOfPlayers = NumberOfPlayers;
                reservation.NumberOfCarts = NumberOfCarts;
                
                _context.Reservations.Update(reservation);
                await _context.SaveChangesAsync();
                
                string memberName = reservation.Member?.User != null 
                    ? $"{reservation.Member.User.FirstName} {reservation.Member.User.LastName}"
                    : "Member";
                    
                TempData["SuccessMessage"] = $"Reservation for {memberName} has been updated";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating reservation: {ex.Message}";
            }
            
            return RedirectToPage(new { searchTerm = SearchTerm, dateFilter = DateFilter?.ToString("yyyy-MM-dd") });
        }
    }
} 