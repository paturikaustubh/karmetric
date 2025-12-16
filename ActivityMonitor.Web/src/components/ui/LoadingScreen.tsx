import React from "react";

const LoadingScreen: React.FC<{ isOpen: boolean; message?: string }> = ({
  isOpen: open,
  message = "Loading Data...",
}) => {
  if (!open) return null;

  return (
    <div className="loading-container">
      <div className="loading-content">
        {message}
        <span className="loading-spinner"></span>
      </div>
      <style>{`
        .loading-container {
          position: fixed;
          inset: 0;
          z-index: 50;
          display: flex;
          align-items: center;
          justify-content: center;
          width: 100%;
          height: 100%;
          background-color: rgba(0, 0, 0, 0.8);
          backdrop-filter: blur(5px);
          animation: fade-in 150ms ease-in-out forwards;
        }

        .loading-content {
          position: absolute;
          display: flex;
          align-items: center;
          gap: 0.75rem;
          font-size: 2.25rem;
          color: white;
        }

        .loading-spinner {
          width: 3rem;
          height: 3rem;
          border-radius: 9999px;
          border: 6px solid #d4d4d4;
          border-right-color: #404040;
          animation: loading-animation 2000ms linear infinite;
        }

        @keyframes fade-in { from { opacity: 0; } to { opacity: 1; } }
        @keyframes loading-animation { to { transform: rotate(360deg); } }
      `}</style>
    </div>
  );
};

export default LoadingScreen;
