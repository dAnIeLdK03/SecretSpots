import { create } from "zustand";
import { GEOLOCATION_OPTIONS } from "@/lib/geolocationOptions";

export interface Coordinates {
  lat: number;
  lng: number;
}

export type GeolocationStatus = "idle" | "locating" | "success" | "error" | "unsupported";

// Mirrors GeolocationPositionError.code — kept separate from "error" status so
// the UI can tell "you said no" apart from "the fix just took too long"
// (very common without a GPS/WiFi location backend, e.g. in a desktop Linux
// VM), which is worth a different message and is safe to just retry.
export type GeolocationErrorReason = "denied" | "unavailable" | "timeout" | null;

interface GeolocationStore {
  status: GeolocationStatus;
  coords: Coordinates | null;
  errorReason: GeolocationErrorReason;
  requestLocation: () => void;
  refreshLocation: () => void;
}

function reasonFromErrorCode(code: number): GeolocationErrorReason {
  switch (code) {
    case GeolocationPositionError.PERMISSION_DENIED:
      return "denied";
    case GeolocationPositionError.TIMEOUT:
      return "timeout";
    default:
      return "unavailable";
  }
}

function fetchLocation(set: (partial: Partial<GeolocationStore>) => void) {
  if (typeof navigator === "undefined" || !navigator.geolocation) {
    set({ status: "unsupported", errorReason: null });
    return;
  }

  set({ status: "locating", errorReason: null });
  navigator.geolocation.getCurrentPosition(
    (position) => {
      set({
        status: "success",
        errorReason: null,
        coords: { lat: position.coords.latitude, lng: position.coords.longitude },
      });
    },
    (error) => set({ status: "error", errorReason: reasonFromErrorCode(error.code) }),
    GEOLOCATION_OPTIONS,
  );
}

// The only place that should ever call navigator.geolocation.getCurrentPosition
// for "where is the user" purposes — everything (AuthProvider's proactive
// request, the map page's fallback, its "use my location" button) goes through
// this single store so two browser geolocation requests can never run at once
// and interfere with each other.
export const useGeolocationStore = create<GeolocationStore>((set, get) => ({
  status: "idle",
  coords: null,
  errorReason: null,

  // Requested once as early as the app mounts (see AuthProvider) instead of
  // when the map page loads, so the fix is usually already done by the time
  // the user navigates there. A no-op once a request has started, so it's
  // safe to call defensively from multiple places.
  requestLocation: () => {
    if (get().status !== "idle") return;
    fetchLocation(set);
  },

  // Always starts a brand-new fix (e.g. the map's "use my location" button,
  // or retrying after a denied/failed request) — but never while one is
  // already in flight, so it can't race the automatic request above.
  refreshLocation: () => {
    if (get().status === "locating") return;
    fetchLocation(set);
  },
}));
