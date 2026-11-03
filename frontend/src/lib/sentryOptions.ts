import type { Event } from "@sentry/nextjs";

// Pages whose query string is a one-time secret: password reset and email verification tokens,
// and the external sign-in code. Sentry records full URLs (request, navigation breadcrumbs, span
// data), so without this a reset link could sit readable in the Sentry project.
// Also matches the percent-encoded form (a URL nested in another URL's query, e.g. a redirect).
const SECRET_QUERY = /((?:\/|%2F)(?:reset-password|verify-email|auth(?:\/|%2F)callback))(?:\?|%3F)[^"\s\\]*/gi;

// Rewrites every string in the event at once — simpler and safer than chasing each field a URL
// can end up in. Sentry events are plain JSON (they're serialized like this to be sent anyway).
function scrubSecretQueries<T extends Event>(event: T): T {
  return JSON.parse(JSON.stringify(event).replace(SECRET_QUERY, "$1")) as T;
}

function parseSampleRate(value: string | undefined): number {
  const rate = Number(value);
  return Number.isFinite(rate) && rate >= 0 && rate <= 1 ? rate : 0.1;
}

// Shared by the client, server and edge Sentry.init calls. An empty DSN (unset locally) makes the
// SDK a no-op, so this is safe to leave wired up in every environment.
export const sentryOptions = {
  dsn: process.env.NEXT_PUBLIC_SENTRY_DSN,
  environment: process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT ?? process.env.NODE_ENV,
  sendDefaultPii: false,
  // Share of page loads/navigations (and server requests) traced for performance; 0 turns it off.
  tracesSampleRate: parseSampleRate(process.env.NEXT_PUBLIC_SENTRY_TRACES_SAMPLE_RATE),
  beforeSend: scrubSecretQueries,
  beforeSendTransaction: scrubSecretQueries,
};
