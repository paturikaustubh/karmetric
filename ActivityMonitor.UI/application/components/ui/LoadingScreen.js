// LoadingScreen.js - ES Module

// Inject CSS Styles
const styleId = "loading-screen-styles";
if (!document.getElementById(styleId)) {
  const style = document.createElement("style");
  style.id = styleId;
  style.textContent = `
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
      gap: 0.75rem; /* gap-3 average */
      font-size: 2.25rem; /* text-4xl */
      color: white;
    }

    .loading-spinner {
      width: 3rem; /* size-12 */
      height: 3rem;
      border-radius: 9999px;
      border: 6px solid #d4d4d4; /* neutral-300 */
      border-right-color: #404040; /* neutral-700 */
      animation: loading-animation 2000ms linear infinite;
    }

    @keyframes fade-in {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    @keyframes fade-out {
      from { opacity: 1; }
      to { opacity: 0; }
    }
    
    /* Ensure loading-animation keyframes exist or reuse from style.css */
    /* If style.css has it, good. If not, safe to add here. User added 'loading-animation' to style.css recently. */
  `;
  document.head.appendChild(style);
}

export default function Loading(open, message = "Loading Data...", id = null) {
  if (open) {
    var rootEle = document.getElementById("root");
    // CREATE CONTAINER
    var loadingContainer = document.createElement("div");
    loadingContainer.className = "loading-container";

    // CREATE CONTENT
    var loadingContent = document.createElement("div");
    loadingContent.className = "loading-content";

    // CREATE ANIMATION
    var loadingAnimation = document.createElement("span");
    loadingAnimation.className = "loading-spinner";

    // APPEND ANIMATION INTO CONTENT
    loadingContent.append(message);
    loadingContent.append(loadingAnimation);
    // APPEND CONTENT TO CONTAINER
    loadingContainer.appendChild(loadingContent);

    if (id) {
      var loadingParent = document.getElementById(id);
      if (loadingParent) {
        var parentFirstChild = loadingParent.firstChild;
        loadingParent === null || loadingParent === void 0
          ? void 0
          : loadingParent.insertBefore(loadingContainer, parentFirstChild);
      }
    } else document.body.insertBefore(loadingContainer, rootEle);
  } else {
    var loadingContainer = document.querySelectorAll(".loading-container");
    if (loadingContainer) {
      loadingContainer.forEach(function (container) {
        container.style.animation = "fade-out 150ms ease-in-out forwards";
        setTimeout(function () {
          container.remove();
        }, 200);
      });
    }
  }
}
