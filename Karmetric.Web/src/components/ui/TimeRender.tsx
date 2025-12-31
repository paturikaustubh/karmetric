import { Link } from "react-router-dom";

export default function TimeRender({
  label,
  redirect,
}: {
  label: string;
  redirect: string;
}) {
  const timeArray = label.split("-");
  return timeArray.length === 2 ? (
    <span>
      <Link
        to={`/sessions/days/${redirect}`}
        style={{ textDecoration: "underline" }}
      >
        {timeArray[0].trim()}
      </Link>
      {" - "}
      {timeArray[1].trim()}
    </span>
  ) : (
    "-"
  );
}
