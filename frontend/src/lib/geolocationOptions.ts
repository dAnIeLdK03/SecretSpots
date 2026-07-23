// Browser defaults are maximumAge: 0 (always forces a brand-new GPS/network fix,
// never reuses even a few-seconds-old position) and timeout: Infinity (no fallback
// if acquisition stalls) — both make getCurrentPosition feel far slower than it
// needs to for this app's accuracy needs (finding spots "near" you, not turn-by-turn).
export const GEOLOCATION_OPTIONS: PositionOptions = {
  enableHighAccuracy: false,
  timeout: 10_000,
  maximumAge: 60_000,
};

// Check-in verifies physical presence right now, so it keeps maximumAge: 0 (no
// cached/stale position) — only the missing timeout is a bug fix here.
export const CHECKIN_GEOLOCATION_OPTIONS: PositionOptions = {
  enableHighAccuracy: false,
  timeout: 10_000,
  maximumAge: 0,
};
