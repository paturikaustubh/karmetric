type DataShift = "in" | "out" | "in-out";

interface SessionRecord {
  id: number;
  checkIn: string;
  shiftOut: string;
  shiftIn: string;
  checkout: string;
  duration: string;
  dataShift: DataShift;
}

interface SessionsGridResponse {
  data: SessionRecord[];
  page: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
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
