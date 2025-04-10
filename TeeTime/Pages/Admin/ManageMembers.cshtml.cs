using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models;

namespace TeeTime.Pages.Admin
{
    [Authorize(Roles = "Clerk,Pro Shop Staff,Committee")]
    public class ManageMembersModel : PageModel
    {
        private readonly TeeTimeDbContext _context;
        
        public ManageMembersModel(TeeTimeDbContext context)
        {
            _context = context;
        }
        
        public List<Member> Members { get; set; } = new();
        public string SearchTerm { get; set; } = "";
        
        public async Task OnGetAsync(string searchTerm = "")
        {
            SearchTerm = searchTerm;
            var query = _context.Members
                .Include(m => m.User)
                .Include(m => m.MembershipCategory)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => m.User != null && 
                                        (m.User.FirstName != null && m.User.FirstName.Contains(searchTerm) || 
                                         m.User.LastName != null && m.User.LastName.Contains(searchTerm) ||
                                         m.User.Email != null && m.User.Email.Contains(searchTerm)));
            }
            
            Members = await query.Where(m => m.User != null)
                                 .OrderBy(m => m.User!.LastName)
                                 .ToListAsync();
        }
    }
} 