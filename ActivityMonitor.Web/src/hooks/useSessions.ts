import { useCallback, useEffect, useState } from "react";
import { API_URL } from "../constants";
import { useParams, useNavigate } from "react-router-dom";

export default function useSessions() {
  const { layoutStr } = useParams<{ layoutStr: string }>();
  const navigate = useNavigate();

  const layout: SessionsLayout = (
    layoutStr === "days" ? "days" : "grid"
  ) as SessionsLayout;

  const [gridData, setGridData] = useState<SessionRecord[]>([]);
  const [dayData, setDayData] = useState<DaySummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [pagination, setPagination] = useState<PaginatorProps>({
    currentPage: 1,
    pageSize: 10,
    totalPages: 1,
    totalItems: 0,
    onPageChange: () => {},
    onPageSizeChange: () => {},
  });

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
          currentPage: json.page,
          pageSize: json.pageSize,
          totalPages: json.totalPages,
          totalItems: json.totalItems,
          onPageChange: handlePageChange,
          onPageSizeChange: handlePageSizeChange,
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
    setPagination((prev) => ({ ...prev, currentPage: page }));
  };

  const handlePageSizeChange = (pageSize: number) => {
    setPagination((prev) => ({ ...prev, pageSize, currentPage: 1 }));
  };

  const handleLayoutChange = (newLayout: SessionsLayout) => {
    navigate(`/sessions/${newLayout}`);
  };

  // ANCHOR: EFFECTS

  useEffect(() => {
    fetchSessions(pagination.currentPage, pagination.pageSize);
  }, [fetchSessions, pagination.currentPage, pagination.pageSize]);

  return {
    gridData,
    dayData,
    loading,
    pagination,
    layout,
    handleLayoutChange,
  };
}
