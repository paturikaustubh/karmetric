import LoadingScreen from "./components/ui/LoadingScreen.js";

// API Configuration
const API_URL = "http://localhost:2369/api/activity";

const fetchSessions = async (page = 1, pageSize = 10) => {
  const response = await fetch(
    `${API_URL}/sessions?limit=${pageSize}&page=${page}&includeActive=true`
  ); // API currently only supports limit, assume flat list for now or update API
  const jsonData = await response.json();

  // Adapt API response to UI expected format
  // API currently returns { data: [], page: 1... } wrapper from ActivityController
  return jsonData;
};

// State
let state = {
  page: 1,
  pageSize: 10,
  totalPages: 1,
};

// DOM Elements
const tableBody = document.querySelector("table.sessions tbody");
const paginationContainer = document.getElementById("pagination");

// Render Functions
const renderTable = (data, startIndex) => {
  tableBody.innerHTML = "";
  data.forEach((session, index) => {
    const tr = document.createElement("tr");
    tr.setAttribute("data-shift", session.dataShift || "");

    tr.innerHTML = `
      <td class="sno">${startIndex + index + 1}</td>
      <td class="check-in">${session.checkIn}</td>
      <td class="shift-out">${session.shiftOut}</td>
      <td class="shift-in">${session.shiftIn}</td>
      <td class="checkout">${session.checkout}</td>
      <td class="duration">${session.duration}</td>
    `;
    tableBody.appendChild(tr);
  });
};

const renderPaginator = () => {
  paginationContainer.innerHTML = "";

  // Helper for Icon Buttons
  const createIconButton = (icon, onClick, disabled = false, title = "") => {
    const button = document.createElement("button");
    button.className = "icon-btn";
    button.innerHTML = `<span class="material-symbols-outlined">${icon}</span>`;
    button.disabled = disabled;
    button.title = title;
    if (!disabled) button.onclick = onClick;
    return button;
  };

  // Page Size Dropdown
  const pageSizeSelect = document.createElement("select");
  pageSizeSelect.className = "page-size-select";
  [5, 10, 20, 50, 100].forEach((size) => {
    const option = document.createElement("option");
    option.value = size;
    option.textContent = size;
    if (size === state.pageSize) option.selected = true;
    pageSizeSelect.appendChild(option);
  });
  pageSizeSelect.onchange = (e) => {
    state.pageSize = parseInt(e.target.value);
    state.page = 1; // Reset to page 1 on size change
    loadPage();
  };

  const rowsPerPageLabel = document.createElement("span");
  rowsPerPageLabel.textContent = "Rows per page:";
  rowsPerPageLabel.style.marginRight = "8px";
  rowsPerPageLabel.style.fontSize = "12px";

  const leftControls = document.createElement("div");
  leftControls.className = "pagination-controls left";
  leftControls.appendChild(rowsPerPageLabel);
  leftControls.appendChild(pageSizeSelect);

  // Navigation Controls
  const navControls = document.createElement("div");
  navControls.className = "pagination-controls right";

  // First & Prev
  navControls.appendChild(
    createIconButton(
      "first_page",
      () => changePage(1),
      state.page === 1,
      "First Page"
    )
  );
  navControls.appendChild(
    createIconButton(
      "chevron_left",
      () => changePage(state.page - 1),
      state.page === 1,
      "Previous Page"
    )
  );

  // Page Input
  const pageInput = document.createElement("input");
  pageInput.type = "number";
  pageInput.className = "page-input";
  pageInput.value = state.page;
  pageInput.min = 1;
  pageInput.max = state.totalPages;

  // Debounce or just enter key
  pageInput.onkeydown = (e) => {
    if (e.key === "Enter") {
      let val = parseInt(e.target.value);
      if (val < 1) val = 1;
      if (val > state.totalPages) val = state.totalPages;
      changePage(val);
    }
  };
  pageInput.onblur = (e) => {
    let val = parseInt(e.target.value);
    if (val !== state.page) {
      if (val < 1) val = 1;
      if (val > state.totalPages) val = state.totalPages;
      changePage(val);
    }
  };

  const pageLabel = document.createElement("span");
  pageLabel.className = "page-label";
  pageLabel.textContent = ` / ${state.totalPages}`;

  navControls.appendChild(pageInput);
  navControls.appendChild(pageLabel);

  // Next & Last
  navControls.appendChild(
    createIconButton(
      "chevron_right",
      () => changePage(state.page + 1),
      state.page === state.totalPages,
      "Next Page"
    )
  );
  navControls.appendChild(
    createIconButton(
      "last_page",
      () => changePage(state.totalPages),
      state.page === state.totalPages,
      "Last Page"
    )
  );

  paginationContainer.appendChild(leftControls);
  paginationContainer.appendChild(navControls);
};

const changePage = (newPage) => {
  if (newPage < 1 || newPage > state.totalPages) return;
  state.page = newPage;
  loadPage();
};

const loadPage = async () => {
  try {
    LoadingScreen(true);
    const [response] = await Promise.all([
      fetchSessions(state.page, state.pageSize),
    ]);

    state.page = response.page;
    state.totalPages = response.totalPages;

    // Calculate start index for S No.
    const startIndex = (response.page - 1) * response.pageSize;

    renderTable(response.data, startIndex);
    renderPaginator(); // Re-render to update disable states and values
  } catch (error) {
    console.error("Failed to load sessions", error);
  } finally {
    LoadingScreen(false);
  }
};

// Initialize
document.addEventListener("DOMContentLoaded", () => {
  loadPage();
});
