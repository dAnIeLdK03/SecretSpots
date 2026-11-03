import * as Sentry from "@sentry/nextjs";
import { sentryOptions } from "@/lib/sentryOptions";

Sentry.init({
  ...sentryOptions,
  // The API is on another origin, and Sentry only adds its trace headers to same-origin requests by
  // default; listing it links a page load to the API request (and its SQL) in one trace. The API's
  // CORS policy already allows any request header.
  tracePropagationTargets: process.env.NEXT_PUBLIC_API_URL ? [process.env.NEXT_PUBLIC_API_URL] : [],
});

export const onRouterTransitionStart = Sentry.captureRouterTransitionStart;
