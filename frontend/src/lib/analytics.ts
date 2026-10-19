// Umami's tracker script (loaded conditionally in the root layout) defines window.umami once it
// loads — absent entirely when analytics isn't configured (see layout.tsx) or the script is
// blocked/fails to load, so every call site goes through this instead of touching window.umami
// directly.
declare global {
  interface Window {
    umami?: {
      track: (eventName: string, eventData?: Record<string, unknown>) => void;
    };
  }
}

export function trackEvent(name: string, data?: Record<string, unknown>): void {
  if (typeof window === "undefined" || !window.umami) return;
  window.umami.track(name, data);
}
