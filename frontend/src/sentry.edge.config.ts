import * as Sentry from "@sentry/nextjs";

// Empty DSN (unset locally) makes the SDK a no-op — safe to leave wired up in every environment.
Sentry.init({
  dsn: process.env.NEXT_PUBLIC_SENTRY_DSN,
  environment: process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT ?? process.env.NODE_ENV,
  sendDefaultPii: false,
});
