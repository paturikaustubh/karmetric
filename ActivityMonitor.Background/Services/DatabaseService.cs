using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ActivityMonitor.Background.Models;
using Dapper;

namespace ActivityMonitor.Background.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "ActivityMonitor");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            _dbPath = Path.Combine(appFolder, "ActivityLog.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            // StartReason differentiates "Check In" vs "Shifting In" (Midnight split)
            // EndReason differentiates "Check Out" vs "Shifting Out" vs "Idle"
            var sql = @"
                CREATE TABLE IF NOT EXISTS Sessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    DurationSeconds INTEGER DEFAULT 0,
                    StartReason TEXT DEFAULT 'Check In',
                    EndReason TEXT
                );";
            conn.Execute(sql);
        }

        public async Task<long> StartSession(DateTime startTime, string startReason)
        {
            using var conn = new SQLiteConnection(_connectionString);
            var sql = @"
                INSERT INTO Sessions (StartTime, StartReason) 
                VALUES (@StartTime, @StartReason);
                SELECT last_insert_rowid();";
            
            // Store as ISO 8601
            var id = await conn.ExecuteScalarAsync<long>(sql, new { StartTime = startTime.ToString("o"), StartReason = startReason });
            return id;
        }

        public async Task EndSession(long id, DateTime endTime, string endReason)
        {
            using var conn = new SQLiteConnection(_connectionString);
            
            // Calculate duration
            var startStr = await conn.ExecuteScalarAsync<string>("SELECT StartTime FROM Sessions WHERE Id = @Id", new { Id = id });
            if (DateTime.TryParse(startStr, out var startTime))
            {
                var duration = (long)(endTime - startTime).TotalSeconds;
                var sql = @"
                    UPDATE Sessions 
                    SET EndTime = @EndTime, EndReason = @EndReason, DurationSeconds = @Duration
                    WHERE Id = @Id";
                await conn.ExecuteAsync(sql, new { 
                    EndTime = endTime.ToString("o"), 
                    EndReason = endReason, 
                    Duration = duration, 
                    Id = id 
                });
            }
        }

        public async Task<DashboardSummary> GetTodaySummary()
        {
            using var conn = new SQLiteConnection(_connectionString);
            var today = DateTime.Today;
            // Filter by StartTime >= Today 00:00
            var sql = @"SELECT * FROM Sessions WHERE StartTime >= @Today ORDER BY StartTime ASC";
            
            var sessions = await conn.QueryAsync<Session>(sql, new { Today = today.ToString("o") });
            var sessionList = sessions.ToList();

            if (!sessionList.Any()) return new DashboardSummary { Date = today.ToString("d MMM, yy"), TotalDuration = "0h 0m", SessionCount = 0 };

            var totalSeconds = sessionList.Sum(s => s.DurationSeconds);
            var ts = TimeSpan.FromSeconds(totalSeconds);
            
            // Fix: Exclude "Shifting In" from count to avoid double-counting midnight splits
            var count = sessionList.Count(s => s.StartReason != "Shifting In");
            
            return new DashboardSummary
            {
                Date = today.ToString("d MMM, yy"),
                TotalDuration = $"{(int)ts.TotalHours}h {ts.Minutes}m",
                SessionCount = count,
                FirstCheckIn = DateTime.Parse(sessionList.First().StartTime).ToString("HH:mm"), // Format used in UI
                LatestCheckOut = sessionList.Last().EndTime != null ? DateTime.Parse(sessionList.Last().EndTime).ToString("HH:mm") : "-"
            };
        }

        public async Task<(IEnumerable<SessionDto> Data, int TotalItems)> GetRecentSessions(int page = 1, int pageSize = 10, bool includeActive = false)
        {
            using var conn = new SQLiteConnection(_connectionString);
            string whereClause = includeActive ? "" : "WHERE EndTime IS NOT NULL";
            
            // Count Query
            var countSql = $"SELECT COUNT(*) FROM Sessions {whereClause}";
            var totalItems = await conn.ExecuteScalarAsync<int>(countSql);
            
            // Data Query
            var offset = (page - 1) * pageSize;
            var dataSql = $@"SELECT * FROM Sessions {whereClause} ORDER BY Id DESC LIMIT @Limit OFFSET @Offset";
            
            var sessions = await conn.QueryAsync<Session>(dataSql, new { Limit = pageSize, Offset = offset });

            return (sessions.Select(s => MapToDto(s)), totalItems);
        }

        private SessionDto MapToDto(Session s)
        {
            var start = DateTime.Parse(s.StartTime);
            var end = s.EndTime != null ? DateTime.Parse(s.EndTime) : (DateTime?)null;
            var duration = end.HasValue ? (end.Value - start) : TimeSpan.Zero;
            var durationStr = $"{(int)duration.TotalHours}h {duration.Minutes}m";

            // Logic derived from Log Analysis:
            // 1. Check In: Always StartTime
            // 2. Shift Out: If EndReason == "Shifting Out" -> EndTime
            // 3. Switch In: If StartReason == "Shifting In" -> StartTime (Override CheckIn?)
            // 4. Checkout: If EndReason != "Shifting Out" -> EndTime
            
            var dto = new SessionDto
            {
                Id = s.Id,
                Duration = durationStr,
                // Default Mapping
                CheckIn = start.ToString("d MMM, yy - HH:mm"),
                Checkout = end.HasValue && s.EndReason != "Shifting Out" ? end.Value.ToString("d MMM, yy - HH:mm") : "-",
                ShiftOut = s.EndReason == "Shifting Out" && end.HasValue ? end.Value.ToString("d MMM, yy - HH:mm") : "-",
                ShiftIn = s.StartReason == "Shifting In" ? start.ToString("d MMM, yy - HH:mm") : "-"
            };

            // Calculate DataShift for UI Brackets
            if (s.EndReason == "Shifting Out" && s.StartReason == "Check In") dto.DataShift = "out"; // Start -> Split (Top half of bracket?) No wait, "Shifting Out" creates the BOTTOM half of bracket in UI terms (7-shape) but logically it's the start of a split...
            // User requested: "for in it is J and for out it is 7".
            // Let's look at the UI CSS logic:
            // data-shift="out" -> 7-shape (Top Horizontal -> Vertical Down). This visually connects to the row BELOW.
            // So if I am "Shifting Out", I am connecting to the NEXT session (tomorrow).
            // data-shift="in" -> J-shape (Vertical Top -> Bottom Horizontal). This visually connects from the row ABOVE.
            // So if I am "Shifting In", I am connecting from the PREVIOUS session (yesterday).

            if (s.EndReason == "Shifting Out") dto.DataShift = "out";
            else if (s.StartReason == "Shifting In") dto.DataShift = "in"; // Or "in-out" if both?
            
            // Correction based on Single Row Logic:
            // "Shifting Out" means this session ENDS abruptly to continue tomorrow. Visual: arrow pointing DOWN to next row. -> "out" (7-shape) matches.
            // "Shifting In" means this session STARTED abruptly from yesterday. Visual: arrow pointing UP from prev row. -> "in" (J-shape) matches.
            
            if (s.StartReason == "Shifting In" && s.EndReason == "Shifting Out") dto.DataShift = "in-out";

            return dto;
        }

        public async Task<WeekSummary> GetWeeklySummary()
        {
            using var conn = new SQLiteConnection(_connectionString);
            // Get last 7 days
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-6);
            var sql = @"SELECT * FROM Sessions WHERE StartTime >= @StartOfWeek";
            
            var sessions = await conn.QueryAsync<Session>(sql, new { StartOfWeek = startOfWeek.ToString("o") });
            
            var chartData = new List<ChartDataPoint>();
            var totalDurationSeconds = 0L;

            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                var daySessions = sessions.Where(s => DateTime.Parse(s.StartTime).Date == date).ToList();
                var daySeconds = daySessions.Sum(s => s.DurationSeconds);
                totalDurationSeconds += daySeconds;

                var ts = TimeSpan.FromSeconds(daySeconds);
                chartData.Add(new ChartDataPoint
                {
                    AxisLabel = date.ToString("ddd"), // "Mon"
                    Value = Math.Round(ts.TotalHours, 1),
                    Label = $"{(int)ts.TotalHours}h {ts.Minutes}m"
                });
            }

            var totalTs = TimeSpan.FromSeconds(totalDurationSeconds);
            return new WeekSummary
            {
                TotalDuration = $"{(int)totalTs.TotalHours}h {totalTs.Minutes}m",
                ChartData = chartData
            };
        }

        public async Task<string> GetTrueSessionStartTime(long sessionId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            var currentId = sessionId;
            var session = await conn.QuerySingleOrDefaultAsync<Session>("SELECT * FROM Sessions WHERE Id = @Id", new { Id = currentId });
            
            if (session == null) return null;

            // Safety limit to prevent infinite loops (e.g. 50 days back)
            for (int i = 0; i < 50; i++) 
            {
                if (session.StartReason != "Shifting In") 
                {
                    // Found the real start
                    return session.StartTime;
                }

                // Find the previous session that ended exactly when this one started
                // Logic: EndTime of prev == StartTime of curr AND EndReason == "Shifting Out"
                var sql = @"SELECT * FROM Sessions 
                            WHERE EndTime = @StartTime 
                            AND EndReason = 'Shifting Out' 
                            ORDER BY Id DESC LIMIT 1";
                
                var prevSession = await conn.QuerySingleOrDefaultAsync<Session>(sql, new { StartTime = session.StartTime });
                
                if (prevSession == null)
                {
                    // Orphaned split? Just return current start.
                    return session.StartTime;
                }

                session = prevSession;
            }
            
            return session.StartTime;
        }
    }
}
