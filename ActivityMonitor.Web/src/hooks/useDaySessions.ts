import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { API_URL } from "../constants";

export default function useDaySessions() {
  const params = useParams();
  const dayIso = params["day-iso"];

  // STATES
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<DaySummary | null>(null);
  const [sessions, setSessions] = useState<SessionRecord[]>([]);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 10,
    totalPages: 1,
  });

  // CALLBACKS
  const fetchDaySessions = useCallback(
    async (page: number, pageSize: number) => {
      if (!dayIso) return;
      try {
        const res = await fetch(
          `${API_URL}/sessions/days/${dayIso}?limit=${pageSize}&page=${page}`
        );
        const data = (await res.json()) as DayDetailsResponse;
        setSummary(data.summary);
        setSessions(data.sessions.data);
        setPagination({
          page: data.sessions.page,
          pageSize: data.sessions.pageSize,
          totalPages: data.sessions.totalPages,
        });
      } catch (error) {
        console.error("Failed to fetch day sessions", error);
      } finally {
        setLoading(false);
      }
    },
    [dayIso]
  );

  // FUNCTIONS
  const handlePageChange = (page: number) => {
    setPagination((prev) => ({ ...prev, page }));
  };

  const handlePageSizeChange = (pageSize: number) => {
    setPagination((prev) => ({ ...prev, pageSize, page: 1 }));
  };

  // EFFECTS
  useEffect(() => {
    fetchDaySessions(pagination.page, pagination.pageSize);
  }, [dayIso, fetchDaySessions, pagination.page, pagination.pageSize]);

  return {
    loading,
    summary,
    sessions,
    pagination,
    handlePageChange,
    handlePageSizeChange,
  };
}
