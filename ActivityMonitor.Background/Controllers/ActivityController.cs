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

        [HttpGet("sessions/grid")]
        public async Task<IActionResult> GetSessionGrid([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] bool includeActive = false, [FromQuery] string date = null)
        {
            var (sessions, totalItems) = await _dbService.GetRecentSessions(page, limit, includeActive, date);
            
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

        [HttpGet("sessions/days")]
        public async Task<IActionResult> GetDays([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            var (data, totalItems) = await _dbService.GetDailySummaries(page, limit);
            var totalPages = (int)System.Math.Ceiling((double)totalItems / limit);

            return Ok(new 
            {
                data = data,
                page = page,
                pageSize = limit,
                totalPages = totalPages,
                totalItems = totalItems
            });
        }

        [HttpGet("sessions/days/{date}")]
        public async Task<IActionResult> GetDayDetails(string date, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            var data = await _dbService.GetDayDetails(date, page, limit);
            if (data == null) return NotFound(new { message = "No data found for this date" });
            return Ok(data);
        }

        [HttpGet("week")]
        public async Task<IActionResult> GetWeekSummary()
        {
            var summary = await _dbService.GetWeeklySummary();
            return Ok(summary);
        }

        [HttpPost("end-session")]
        public async Task<IActionResult> EndSession([FromQuery] string reason = "External Request")
        {
            // Forces the service to end the current session immediately
            // This is used by the installer to ensure a clean checkout before update
            
            // 1. Set Shutdown Flag to prevent auto-restart by the loop
            _activityService.PrepareForShutdown();

            // 2. Stop the current session
            await _activityService.StopSession(System.DateTime.Now, reason);
            
            return Ok(new { message = "Session ended successfully." });
        }

        [HttpDelete("stop")]
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
