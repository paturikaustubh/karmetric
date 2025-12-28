using System;

namespace ActivityMonitor.Background.Models
{
    public class Session
    {
        public long Id { get; set; }
        public string StartTime { get; set; } // ISO 8601
        public string? EndTime { get; set; }   // ISO 8601, nullable
        public long DurationSeconds { get; set; }
        public string StartReason { get; set; } = "Check In"; // "Check In", "Shifting In"
        public string EndReason { get; set; } = "";           // "Check Out", "Shifting Out", "Idle"
    }

    public class SessionDto
    {
        public long Id { get; set; }
        public string CheckIn { get; set; }
        public string? CheckInDateIso { get; set; }
        public string ShiftOut { get; set; }
        public string? ShiftOutDateIso { get; set; }
        public string ShiftIn { get; set; }
        public string? ShiftInDateIso { get; set; }
        public string Checkout { get; set; }
        public string? CheckoutDateIso { get; set; }
        public string Duration { get; set; }
        public string DataShift { get; set; } // "in", "out", "in-out", ""
    }

    public class StatusResponse
    {
        public string Status { get; set; } // "In", "Out", "Idle"
        public string Duration { get; set; } // "HH:mm:ss"
        public string StartTime { get; set; }
    }

     public class DashboardSummary
    {
        public string Date { get; set; }
        public string TotalDuration { get; set; }
        public int SessionCount { get; set; }
        public string FirstCheckIn { get; set; }
        public string LatestCheckOut { get; set; }
    }

    public class WeekSummary
    {
        public string TotalDuration { get; set; }
        public System.Collections.Generic.List<ChartDataPoint> ChartData { get; set; }
    }

    public class ChartDataPoint
    {
        public double Value { get; set; }
        public string Label { get; set; }
        public string AxisLabel { get; set; }
    }
}
