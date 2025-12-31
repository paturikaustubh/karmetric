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
  const [gridPagination, setGridPagination] = useState<PaginatorProps>({
    currentPage: 1,
    pageSize: 10,
    totalPages: 1,
    totalItems: 0,
    onPageChange: () => {},
    onPageSizeChange: () => {},
  });
  const [dayPagination, setDayPagination] = useState<PaginatorProps>({
    currentPage: 1,
    pageSize: 10,
    totalPages: 1,
    totalItems: 0,
    onPageChange: () => {},
    onPageSizeChange: () => {},
  });

  // ANCHOR: METHODS

  const apiCall = useCallback(
    async (page: number, pageSize: number) => {
      const res = await fetch(
        `${API_URL}/sessions/${layout}?limit=${pageSize}&page=${page}&includeActive=true`
      );
      if (!res.ok) {
        throw new Error("Failed to fetch sessions");
      }
      return await res.json();
    },
    [layout]
  );

  // ANCHOR: METHODS - GRID
  const fetchGridData = useCallback(async () => {
    try {
      setLoading(true);
      const json = await apiCall(
        gridPagination.currentPage,
        gridPagination.pageSize
      );
      setGridData(json.data);
      setGridPagination((prev) => ({
        ...prev,
        currentPage: json.page,
        pageSize: json.pageSize,
        totalPages: json.totalPages,
        totalItems: json.totalItems,
      }));
    } catch (error) {
      console.error("Failed to fetch grid sessions", error);
    } finally {
      setLoading(false);
    }
  }, [gridPagination.currentPage, gridPagination.pageSize, apiCall]);

  const handleGridPageChange = (page: number) => {
    setGridPagination((prev) => ({ ...prev, currentPage: page }));
  };

  const handleGridPageSizeChange = (pageSize: number) => {
    setGridPagination((prev) => ({ ...prev, pageSize, currentPage: 1 }));
  };

  // ANCHOR: METHODS - DAYS
  const fetchDayInitialData = useCallback(async () => {
    // Only fetch if we want to reset or ensure separate state validation
    // For now, let's assuming we just fetch the current dayPagination page
    // typically page 1 on first load.
    try {
      setLoading(true);
      const json = await apiCall(dayPagination.currentPage, 10); // Standardize page size or use dayPagination.pageSize
      setDayData(json.data);
      setDayPagination((prev) => ({
        ...prev,
        currentPage: json.page,
        pageSize: json.pageSize,
        totalPages: json.totalPages,
        totalItems: json.totalItems,
        // Ensure callbacks are preserved or just re-referenced in render
      }));
    } catch (error) {
      console.error("Failed to fetch day sessions", error);
    } finally {
      setLoading(false);
    }
  }, [dayPagination.currentPage, apiCall]); // careful with deps to avoid loops

  const handleDayPageChange = async (page: number) => {
    // This is for "Load More" specifically
    setDayPagination((prev) => ({ ...prev, currentPage: page }));
    try {
      setLoading(true);
      const data = await apiCall(page, dayPagination.pageSize);

      setDayData((prev) => [...prev, ...data.data]);
      setDayPagination((prev) => ({
        ...prev,
        currentPage: data.page,
        pageSize: data.pageSize,
        totalPages: data.totalPages,
        totalItems: data.totalItems,
      }));
    } catch (err) {
      console.error("Failed to load more days", err);
    } finally {
      setLoading(false);
    }
  };

  const handleLayoutChange = useCallback(
    (newLayout: SessionsLayout) => {
      navigate(`/sessions/${newLayout}`);
    },
    [navigate]
  );

  // ANCHOR: EFFECTS

  // 1. Grid Effect
  useEffect(() => {
    if (layout === "grid") {
      fetchGridData();
    }
  }, [layout, fetchGridData]);

  // 2. Days Effect - Initial Load
  useEffect(() => {
    if (layout === "days" && dayData.length === 0) {
      // If we switch to days and have no data, fetch page 1
      fetchDayInitialData();
    }
  }, [layout, dayData.length, fetchDayInitialData]);

  // We need to attach the correct page change handler to the returned pagination object
  // Since state is separate, we do this dynamically before returning.
  const activePagination =
    layout === "grid"
      ? {
          ...gridPagination,
          onPageChange: handleGridPageChange,
          onPageSizeChange: handleGridPageSizeChange,
        }
      : {
          ...dayPagination,
          onPageChange: handleDayPageChange,
          onPageSizeChange: () => {},
        };

  return {
    gridData,
    dayData,
    loading,
    pagination: activePagination,
    layout,
    handleLayoutChange,
  };
}
