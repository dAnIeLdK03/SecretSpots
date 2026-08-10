"use client";

import * as Sentry from "@sentry/nextjs";
import { useEffect } from "react";

// Renders only when React itself fails to render the root layout, so it sits outside the
// NextIntlClientProvider tree — no translation keys are reachable here, hence the hardcoded text.
export default function GlobalError({
  error,
}: {
  error: Error & { digest?: string };
}) {
  useEffect(() => {
    Sentry.captureException(error);
  }, [error]);

  return (
    <html lang="bg">
      <body>
        <p>Нещо се обърка. Моля, презаредете страницата.</p>
      </body>
    </html>
  );
}
