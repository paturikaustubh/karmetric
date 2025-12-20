import React from "react";
import { Link } from "react-router";

const Navbar: React.FC = () => {
  return (
    <nav
      style={{
        borderBottom: "2px solid rgba(32, 32, 32, 0.1)",
        padding: "0.4em 0",
      }}
    >
      <div style={{ marginLeft: "1em" }}>
        <h1
          style={{
            fontFamily: '"Roboto Condensed", sans-serif',
            fontSize: "2.5em",
            marginBlock: "0.4em",
            width: "fit-content",
          }}
        >
          <Link to="/">Activity Monitor</Link>
        </h1>
      </div>
    </nav>
  );
};

export default Navbar;
