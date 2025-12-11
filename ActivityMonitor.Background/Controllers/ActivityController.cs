using Microsoft.AspNetCore.Mvc;
using ActivityMonitor.Background.Services;
using ActivityMonitor.Background.Models;
using System.Threading.Tasks;

namespace ActivityMonitor.Background.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly ActivityService _activityService;
        private readonly DatabaseService _dbService;

        public ActivityController(ActivityService activityService, DatabaseService dbService)
        {
            _activityService = activityService;
            _dbService = dbService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var startTime = await _activityService.GetCurrentSessionOriginalStartTime();
            return Ok(new StatusResponse
            {
                Status = _activityService.CurrentStatus, // "In", "Out"
                Duration = _activityService.CurrentSessionDuration,
                StartTime = startTime ?? "-"
            });
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodaySummary()
        {
            var summary = await _dbService.GetTodaySummary();
            return Ok(summary);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetRecentSessions([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] bool includeActive = false)
        {
            var (sessions, totalItems) = await _dbService.GetRecentSessions(page, limit, includeActive);
            
            var totalPages = (int)System.Math.Ceiling((double)totalItems / limit);

            return Ok(new 
            {
                data = sessions,
                page = page,
                pageSize = limit,
                totalPages = totalPages, 
                totalItems = totalItems
            });
        }

        [HttpGet("week")]
        public async Task<IActionResult> GetWeekSummary()
        {
            var summary = await _dbService.GetWeeklySummary();
            return Ok(summary);
        }

        [HttpPost("stop")]
        public IActionResult StopService([FromServices] Microsoft.Extensions.Hosting.IHostApplicationLifetime appLifetime)
        {
            // Trigger graceful shutdown
            Task.Run(async () => 
            {
                await Task.Delay(100); // Give time for response to send
                appLifetime.StopApplication();
            });
            return Ok(new { message = "Stopping service..." });
        }
    }
}
