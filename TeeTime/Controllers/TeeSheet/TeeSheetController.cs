using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeeTime.Data;
using TeeTime.Models.TeeSheet;
using Microsoft.EntityFrameworkCore;

namespace TeeTime.Controllers.TeeSheet
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

        // GET: api/TeeSheet
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.TeeSheet.TeeSheet>>> GetTeeSheets()
        {
            return await _context.TeeSheets.ToListAsync();
        }

        // GET: api/TeeSheet/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Models.TeeSheet.TeeSheet>> GetTeeSheet(int id)
        {
            var teeSheet = await _context.TeeSheets.FindAsync(id);

            if (teeSheet == null)
            {
                return NotFound();
            }

            return teeSheet;
        }

        // POST: api/TeeSheet
        [HttpPost]
        public async Task<ActionResult<Models.TeeSheet.TeeSheet>> PostTeeSheet(Models.TeeSheet.TeeSheet teeSheet)
        {
            _context.TeeSheets.Add(teeSheet);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeeSheet), new { id = teeSheet.Id }, teeSheet);
        }

        // PUT: api/TeeSheet/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeeSheet(int id, Models.TeeSheet.TeeSheet teeSheet)
        {
            if (id != teeSheet.Id)
            {
                return BadRequest();
            }

            _context.Entry(teeSheet).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeeSheetExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/TeeSheet/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeeSheet(int id)
        {
            var teeSheet = await _context.TeeSheets.FindAsync(id);
            if (teeSheet == null)
            {
                return NotFound();
            }

            _context.TeeSheets.Remove(teeSheet);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeeSheetExists(int id)
        {
            return _context.TeeSheets.Any(e => e.Id == id);
        }
    }
}
