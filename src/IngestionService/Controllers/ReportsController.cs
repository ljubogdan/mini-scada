using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ScadaDbContext _context;

        public ReportsController(ScadaDbContext context)
        {
            _context = context;
        }

        [HttpGet("measurements")]
        public async Task<IActionResult> GetMeasurements(
            [FromQuery] Guid? sensorId, 
            [FromQuery] DateTime? startTime, 
            [FromQuery] DateTime? endTime,
            [FromQuery] int limit = 100)
        {
            var query = _context.Measurements.AsQueryable();

            if (sensorId.HasValue)
            {
                query = query.Where(m => m.SensorId == sensorId.Value);
            }

            if (startTime.HasValue)
            {
                query = query.Where(m => m.Timestamp >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(m => m.Timestamp <= endTime.Value);
            }

            var results = await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("consensus")]
        public async Task<IActionResult> GetConsensus(
            [FromQuery] DateTime? startTime, 
            [FromQuery] DateTime? endTime,
            [FromQuery] int limit = 100)
        {
            var query = _context.Consensuses.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(c => c.Timestamp >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(c => c.Timestamp <= endTime.Value);
            }

            var results = await query
                .OrderByDescending(c => c.Timestamp)
                .Take(limit)
                .ToListAsync();

            return Ok(results);
        }
    }
}
