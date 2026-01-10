type DataShift = "in" | "out" | "in-out";

interface SessionRecord {
  id: number;
  checkIn: string;
  checkInDateIso: string;
  shiftOut: string;
  shiftOutDateIso: string;
  shiftIn: string;
  shiftInDateIso: string;
  checkout: string;
  checkoutDateIso: string;
  duration: string;
  dataShift: DataShift;
}

interface SessionsGridResponse {
  data: SessionRecord[];
  page: number;
  pageSize: number;
  totalPages: number;
  totalItems: number;
}

interface StatusResponse {
  status: "In" | "Out";
  duration: string;
  startTime: string;
}

interface TodaySummary {
  date: string;
  totalDuration: string;
  sessionCount: number;
  firstCheckIn: string;
  latestCheckOut: string;
}

interface ChartDataPoint {
  axisLabel: string;
  value: number;
  label: string;
}

interface WeekSummary {
  totalDuration: string;
  chartData: ChartDataPoint[];
  isLatestWeek: boolean;
  isFirstWeek: boolean;
}

type SessionsLayout = "grid" | "days";

interface DaySummary {
  dateIso: string;
  date: string;
  day: string;
  totalDuration: string;
  sessionCount: number;
  dataShift: DataShift;
}

interface DaysResponse {
  data: DaySummary[];
  page: number;
  pageSize: number;
  totalPages: number;
  totalItems: number;
}

interface DayDetailsResponse {
  summary: DaySummary;
  sessions: {
    data: SessionRecord[];
    page: number;
    pageSize: number;
    totalPages: number;
    totalItems: number;
  };
}

interface PaginatorProps {
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalItems: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
}
