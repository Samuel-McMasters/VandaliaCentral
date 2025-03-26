using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MondayMinuteApi.Data;
using MondayMinuteApi.Models;

namespace MondayMinuteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MondayMinuteController : ControllerBase
    {
        private readonly MondayMinuteContext _context;

        public MondayMinuteController(MondayMinuteContext context)
        {
            _context = context;
        }

        //GET: api/MondayMinutes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MondayMinute>>> GetMondayMinutes()
        {
            return await _context.MondayMinutes.ToListAsync();
        }

        //POST: api/MondayMinutes
        [HttpPost]
        public async Task<ActionResult<MondayMinute>> PostMondayMinute(MondayMinute mondayMinute)
        {
            _context.MondayMinutes.Add(mondayMinute);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMondayMinutes), new { id = mondayMinute.Id }, mondayMinute);
        }


        //PUT: api/MondayMinutes
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMondayMinute(int id, MondayMinute mondayMinute)
        {
            if (id != mondayMinute.Id)
            {
                return BadRequest();
            }

            _context.Entry(mondayMinute).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.MondayMinutes.Any(e => e.Id == id))
                    return NotFound();

                throw;
            }

            return NoContent();


        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMondayMinute(int id)
        {
            var mondayMinute = await _context.MondayMinutes.FindAsync(id);

            if (mondayMinute == null)
                return NotFound();

            _context.MondayMinutes.Remove(mondayMinute);
            await _context.SaveChangesAsync();
            return NoContent();
        }


    }
}
