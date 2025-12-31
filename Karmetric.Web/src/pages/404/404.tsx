import { Link } from "react-router-dom";
import "./stytles.css";

export default function NotFoundPage() {
  return (
    <section id="not-found">
      <p>Looks like you're lost 👀</p>
      <p>Here, let me help you kid...</p>
      <Link to="/" className="accent">
        <button>Take me Back</button>
      </Link>
    </section>
  );
}
