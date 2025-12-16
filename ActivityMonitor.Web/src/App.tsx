import { BrowserRouter, Routes, Route } from "react-router-dom";
import Dashboard from "./pages/Dashboard/Dashboard";
import Sessions from "./pages/Sessions/Sessions";
import Navbar from "./components/ui/Navbar";
import NotFoundPage from "./pages/404/404";
import DaySessions from "./pages/DaySessions/DaySessions";

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <main>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/sessions" element={<Sessions />} />
          <Route path="/sessions/days/:day-iso" element={<DaySessions />} />
          {/* Simple 404 / Redirect */}
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
