using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ActivityMonitor.Background.Utils;

using Microsoft.Extensions.Configuration;

namespace ActivityMonitor.Background.Services
{
    public class ActivityService : BackgroundService
    {
        private readonly DatabaseService _db;
        private readonly ILogger<ActivityService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IConfiguration _configuration;
        
        private const int CheckIntervalMs = 5000;
        private const int IdleThresholdSeconds = 300; // 5 mins

        // State
        private bool _isWorking = false;
        private volatile bool _isShuttingDown = false; // New flag
        private bool _isLocked = false;
        private long _currentSessionId = 0;
        private DateTime _sessionStartTime;

        // Exposed properties for API
        public string CurrentStatus => _isWorking ? "In" : "Out";
        public string CurrentSessionDuration => _isWorking ? (DateTime.Now - _sessionStartTime).ToString(@"hh\:mm\:ss") : "00:00:00";
        public string CurrentSessionStart => _isWorking ? _sessionStartTime.ToString("HH:mm") : "-";

        public ActivityService(DatabaseService db, ILogger<ActivityService> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _appLifetime = appLifetime;
            _configuration = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // Register System Events
            try 
            {
                SystemEvents.SessionSwitch += OnSessionSwitch;
                SystemEvents.PowerModeChanged += OnPowerModeChanged;
                SystemEvents.SessionEnding += OnSessionEnding;
                _logger.LogInformation("SystemEvents registered.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register SystemEvents");
            }
            
            // Register Application Shutdown Hook
            _appLifetime.ApplicationStopping.Register(() => 
            {
                _isShuttingDown = true; // Ensure flag is set on app stop
                // Synchronous stop on shutdown
                if (_isWorking)
                {
                    StopSessionSync(DateTime.Now, "Service Shutdown");
                }
            });

            // Extra Safety: Process Exit (e.g. TaskKill)
            AppDomain.CurrentDomain.ProcessExit += (s, e) => 
            {
                _isShuttingDown = true;
                if (_isWorking)
                {
                    StopSessionSync(DateTime.Now, "Process Exit");
                }
            };

            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _isShuttingDown = true;
            try 
            {
                SystemEvents.SessionSwitch -= OnSessionSwitch;
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                SystemEvents.SessionEnding -= OnSessionEnding;
            }
            catch {}
            
            await base.StopAsync(cancellationToken);
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                _isLocked = true;
                if (_isWorking)
                {
                    // Fire and forget mechanism for event handler, but prefer Task.Run
                    Task.Run(() => StopSession(DateTime.Now, "System Lock"));
                }
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                _isLocked = false;
                if (!_isWorking)
                {
                    Task.Run(() => StartSession());
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Activity Monitor Service Started.");
            
            // Auto-start session on launch
            await StartSession();

            while (!stoppingToken.IsCancellationRequested && !_isShuttingDown)
            {
                try 
                {
                    await Task.Delay(CheckIntervalMs, stoppingToken);
                    if (_isShuttingDown) break; 

                    var idleSeconds = IdleUtils.GetIdleTimeSeconds();

                    if (_isWorking)
                    {
                        if (idleSeconds >= IdleThresholdSeconds)
                        {
                            _logger.LogInformation($"Idle threshold reached ({idleSeconds}s). Stopping Session.");

                            // Stop Session.
                            // Backdate the EndTime to when idle started
                            var endTime = DateTime.Now.AddSeconds(-idleSeconds);
                            await StopSession(endTime, "Idle Timeout");
                        }
                    }
                    else // Not Working
                    {
                        // Check if back (Active) AND distinct session is NOT locked
                        if (idleSeconds < 2 && !_isLocked)
                        {
                            await StartSession();
                        }
                    }
                }
                catch (TaskCanceledException) 
                {
                    // Ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Activity Loop");
                }
            }
        }

        public void PrepareForShutdown()
        {
            _isShuttingDown = true;
            _logger.LogInformation("Shutdown Initiated via API. No new sessions will be started.");
        }

        public async Task StartSession()
        {
            if (_isShuttingDown) return; // Prevent start if shutting down
            if (_isWorking) return;

            _sessionStartTime = DateTime.Now;
            _isWorking = true;
            _currentSessionId = await _db.StartSession(_sessionStartTime, "Check In");
            _logger.LogInformation($"Session Started: {_currentSessionId} at {_sessionStartTime}");
        }

        public async Task StopSession(DateTime endTime, string reason)
        {
            if (!_isWorking) return;

            // SAFETY: If idle time is stale (e.g. from before sleep), endTime might be calculated as BEFORE start time.
            // This causes negative durations and havoc with variable dates (Midnight Split).
            if (endTime < _sessionStartTime)
            {
                _logger.LogWarning($"StopSession: Correction applied. EndTime ({endTime}) < StartTime ({_sessionStartTime}). Reason: {reason}. Clamping to StartTime.");
                endTime = _sessionStartTime;
            }

            _logger.LogInformation($"Stopping Session: {reason} at {endTime}");
            _isWorking = false;

            // Handle Midnight Split
            if (_sessionStartTime.Date != endTime.Date)
            {
                var endOfDay = _sessionStartTime.Date.AddDays(1).AddSeconds(-1);
                await _db.EndSession(_currentSessionId, endOfDay, "Shifting Out");

                var startOfNextDay = _sessionStartTime.Date.AddDays(1);
                var nextDayId = await _db.StartSession(startOfNextDay, "Shifting In");
                
                // If the reason is "Idle Timeout", we check out the split part too?
                // Logic: Yes, we still check out.
                await _db.EndSession(nextDayId, endTime, reason == "Idle Timeout" ? "Check Out" : reason); 
            }
            else
            {
                // Normal Check Out
                await _db.EndSession(_currentSessionId, endTime, "Check Out");
            }

            _currentSessionId = 0;
        }

        // Monitor System Sleep/Resume
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (_isWorking)
                {
                     // Force logic sync? Or Task.Run and hope?
                     // Suspend is usually fast. We should try sync save.
                     StopSessionSync(DateTime.Now, "System Sleep");
                }
            }
            else if (e.Mode == PowerModes.Resume)
            {
                // Do NOT auto-start on resume. 
                // Wait for either explicit Unlock event OR User Activity Loop (if unlocked).
                // If resumed to Lock Screen, _isLocked should ideally be set, or handle via loop.
            }
        }

        // Monitor System Shutdown/Logoff
        private void OnSessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (_isWorking)
            {
                StopSessionSync(DateTime.Now, "System Shutdown");
            }
        }

        // Synchronous version for App Shutdown/Exit
        private void StopSessionSync(DateTime endTime, string reason)
        {
            // Just blocking call to Async method for simplicity in shutdown hook
            // Note: StopSession handles _currentSessionId reset.
            StopSession(endTime, reason).GetAwaiter().GetResult();
        }

        public async Task<string> GetCurrentSessionOriginalStartTime()
        {
            if (!_isWorking) return null;
            return await _db.GetTrueSessionStartTime(_currentSessionId);
        }
    }
}
