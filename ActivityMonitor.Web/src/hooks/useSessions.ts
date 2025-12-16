import { useCallback, useEffect, useState } from "react";
import { API_URL } from "../constants";

export default function useSessions() {
  const [gridData, setGridData] = useState<SessionRecord[]>([]);
  const [dayData, setDayData] = useState<DaySummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 10,
    totalPages: 1,
  });
  const [layout, setLayout] = useState<SessionsLayout>("grid");

  // CALLBACKS
  const fetchSessions = useCallback(
    async (page: number, pageSize: number) => {
      try {
        setLoading(true);
        const res = await fetch(
          `${API_URL}/sessions/${layout}?limit=${pageSize}&page=${page}&includeActive=true`
        );
        const json = await res.json();

        if (layout === "days") {
          setDayData(json.data);
        } else {
          setGridData(json.data);
        }
        setPagination({
          page: json.page,
          pageSize: json.pageSize,
          totalPages: json.totalPages,
        });
      } catch (error) {
        console.error("Failed to fetch sessions", error);
      } finally {
        setLoading(false);
      }
    },
    [layout]
  );

  // ANCHOR: METHODS
  const handlePageChange = (page: number) => {
    setPagination((prev) => ({ ...prev, page }));
  };

  const handlePageSizeChange = (pageSize: number) => {
    setPagination((prev) => ({ ...prev, pageSize, page: 1 }));
  };

  const handleLayoutChange = (layout: SessionsLayout) => {
    setLayout(layout);
  };

  // ANCHOR: EFFECTS

  useEffect(() => {
    fetchSessions(pagination.page, pagination.pageSize);
  }, [fetchSessions, pagination.page, pagination.pageSize]);

  return {
    gridData,
    dayData,
    loading,
    pagination,
    layout,
    handlePageChange,
    handlePageSizeChange,
    handleLayoutChange,
  };
}
