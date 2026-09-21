import { notFound } from "next/navigation";

// Unmatched URLs under a locale would otherwise fall through to Next's default English 404;
// throwing notFound() from inside the locale layout renders [locale]/not-found.tsx instead.
export default function CatchAllPage() {
  notFound();
}
