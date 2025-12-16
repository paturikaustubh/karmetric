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

        public async Task<(IEnumerable<SessionDto> Data, int TotalItems)> GetRecentSessions(int page = 1, int pageSize = 10, bool includeActive = false, string date = null)
        {
            using var conn = new SQLiteConnection(_connectionString);
            
            var conditions = new List<string>();
            var p = new DynamicParameters();
            
            if (!includeActive) conditions.Add("EndTime IS NOT NULL");
            
            if (!string.IsNullOrEmpty(date))
            {
                // Filter by specific day (ISO YYYY-MM-DD matches StartTime string starts with...)
                conditions.Add("StartTime LIKE @DateLike");
                p.Add("DateLike", $"{date}%");
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

            // Count Query
            var countSql = $"SELECT COUNT(*) FROM Sessions {whereClause}";
            var totalItems = await conn.ExecuteScalarAsync<int>(countSql, p);
            
            // Data Query
            var offset = (page - 1) * pageSize;
            p.Add("Limit", pageSize);
            p.Add("Offset", offset);
            
            var dataSql = $@"SELECT * FROM Sessions {whereClause} ORDER BY StartTime DESC LIMIT @Limit OFFSET @Offset";
            
            var sessions = await conn.QueryAsync<Session>(dataSql, p);

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
            
            var today = DateTime.Today;
            // Get Start of week (Sunday)
            var diff = today.DayOfWeek - DayOfWeek.Sunday;
            if (diff < 0) diff += 7;
            var startOfWeek = today.AddDays(-diff).Date;
            
            // Get week range (Sun - Sat)
            var endOfWeek = startOfWeek.AddDays(7); // < EndOfWeek

            var sql = @"SELECT * FROM Sessions WHERE StartTime >= @StartOfWeek AND StartTime < @EndOfWeek";
            
            var sessions = await conn.QueryAsync<Session>(sql, new { 
                StartOfWeek = startOfWeek.ToString("o"), 
                EndOfWeek = endOfWeek.ToString("o") 
            });
            
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
                    AxisLabel = date.ToString("ddd"), // "Sun", "Mon"...
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

        public async Task<(IEnumerable<DaySummaryDto> Data, int TotalItems)> GetDailySummaries(int page = 1, int pageSize = 10)
        {
            using var conn = new SQLiteConnection(_connectionString);
            
            // 1. Get Distinct Days Count
            var countSql = "SELECT COUNT(DISTINCT substr(StartTime, 1, 10)) FROM Sessions";
            var totalItems = await conn.ExecuteScalarAsync<int>(countSql);

            // 2. Get Page of Days
            var offset = (page - 1) * pageSize;
            var daysSql = @"SELECT DISTINCT substr(StartTime, 1, 10) as DayStr
                            FROM Sessions 
                            ORDER BY DayStr DESC 
                            LIMIT @Limit OFFSET @Offset";
            
            var pagedDays = (await conn.QueryAsync<string>(daysSql, new { Limit = pageSize, Offset = offset })).ToList();

            if (!pagedDays.Any()) return (Enumerable.Empty<DaySummaryDto>(), totalItems);

            // 3. Get Sessions for these days
            var sessionsSql = "SELECT * FROM Sessions WHERE substr(StartTime, 1, 10) IN @Days ORDER BY StartTime ASC";
            var sessions = await conn.QueryAsync<Session>(sessionsSql, new { Days = pagedDays });

            // 4. Group
            var grouped = sessions
                .GroupBy(s => DateTime.Parse(s.StartTime).Date)
                .OrderByDescending(g => g.Key)
                .Select(g => 
                {
                    var date = g.Key;
                    var daySessions = g.ToList();
                    var totalSeconds = daySessions.Sum(s => s.DurationSeconds);
                    var ts = TimeSpan.FromSeconds(totalSeconds);
                    
                    var firstSession = daySessions.First();
                    var lastSession = daySessions.Last();

                    var dayShiftedIn = firstSession.StartReason == "Shifting In";
                    var dayShiftedOut = lastSession.EndReason == "Shifting Out";
                    
                    string dataShift = null;
                    if (dayShiftedIn && dayShiftedOut) dataShift = "in-out";
                    else if (dayShiftedIn) dataShift = "in";
                    else if (dayShiftedOut) dataShift = "out";

                    return new DaySummaryDto
                    {
                        DateIso = date.ToString("yyyy-MM-dd"),
                        Date = date.ToString("d MMM, yy"),
                        Day = date.ToString("dddd"),
                        TotalDuration = $"{(int)ts.TotalHours}h {ts.Minutes}m",
                        SessionCount = daySessions.Count,
                        DataShift = dataShift
                    };
                });

            return (grouped, totalItems);
        }

        public async Task<object> GetDayDetails(string dateIso, int page = 1, int pageSize = 10)
        {
            if (!DateTime.TryParse(dateIso, out var date)) return null;

            using var conn = new SQLiteConnection(_connectionString);
            
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1);
            var startStr = startOfDay.ToString("o");
            var endStr = endOfDay.ToString("o");

            // 1. Aggregation Query for Summary Metadata (Count, Duration, First/Last Reason)
            var statsSql = @"
                SELECT 
                    COUNT(*) as Count, 
                    SUM(DurationSeconds) as Duration,
                    (SELECT StartReason FROM Sessions WHERE StartTime >= @Start AND StartTime < @End ORDER BY StartTime ASC LIMIT 1) as FirstReason,
                    (SELECT EndReason FROM Sessions WHERE StartTime >= @Start AND StartTime < @End ORDER BY StartTime DESC LIMIT 1) as LastReason
                FROM Sessions 
                WHERE StartTime >= @Start AND StartTime < @End";

            var stats = await conn.QuerySingleOrDefaultAsync<dynamic>(statsSql, new { Start = startStr, End = endStr });

            if (stats == null || (long)stats.Count == 0) return null;

            var totalItems = (int)stats.Count;
            var durationSeconds = (stats.Duration != null) ? (long)stats.Duration : 0;
            var ts = TimeSpan.FromSeconds(durationSeconds);

            var dayShiftedIn = (string)stats.FirstReason == "Shifting In";
            var dayShiftedOut = (string)stats.LastReason == "Shifting Out";

            string dataShift = null;
            if (dayShiftedIn && dayShiftedOut) dataShift = "in-out";
            else if (dayShiftedIn) dataShift = "in";
            else if (dayShiftedOut) dataShift = "out";

            var summary = new DaySummaryDto
            {
                DateIso = date.ToString("yyyy-MM-dd"),
                Date = date.ToString("d MMM, yy"),
                Day = date.ToString("dddd"),
                TotalDuration = $"{(int)ts.TotalHours}h {ts.Minutes}m",
                SessionCount = totalItems,
                DataShift = dataShift
            };

            // 2. Paginated Grid Data
            var offset = (page - 1) * pageSize;
            var gridSql = @"SELECT * FROM Sessions 
                            WHERE StartTime >= @Start AND StartTime < @End 
                            ORDER BY StartTime ASC 
                            LIMIT @Limit OFFSET @Offset";

            var sessions = await conn.QueryAsync<Session>(gridSql, new { 
                Start = startStr, 
                End = endStr,
                Limit = pageSize,
                Offset = offset
            });

            var gridDetails = sessions.Select(s => MapToDto(s)).ToList();
            var totalPages = (int)System.Math.Ceiling((double)totalItems / pageSize);

            return new 
            {
                summary = summary,
                sessions = new {
                    data = gridDetails,
                    page = page,
                    pageSize = pageSize,
                    totalItems = totalItems,
                    totalPages = totalPages
                }
            };
        }
    }

    public class DaySummaryDto
    {
        public string DateIso { get; set; }
        public string Date { get; set; }
        public string Day { get; set; }
        public string TotalDuration { get; set; }
        public int SessionCount { get; set; }
        public string DataShift { get; set; } // "in", "out", "in-out"
    }
}
