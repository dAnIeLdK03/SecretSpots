import { create } from "zustand";
import { GEOLOCATION_OPTIONS } from "@/lib/geolocationOptions";

export interface Coordinates {
  lat: number;
  lng: number;
}

export type GeolocationStatus = "idle" | "locating" | "success" | "error" | "unsupported";

interface GeolocationStore {
  status: GeolocationStatus;
  coords: Coordinates | null;
  requestLocation: () => void;
}

// Requested once as early as the app mounts (see AuthProvider) instead of
// when the map page loads, so the browser permission prompt and GPS/network
// fix are already done by the time the user navigates there.
export const useGeolocationStore = create<GeolocationStore>((set, get) => ({
  status: "idle",
  coords: null,

  requestLocation: () => {
    if (get().status !== "idle") return;

    if (typeof navigator === "undefined" || !navigator.geolocation) {
      set({ status: "unsupported" });
      return;
    }

    set({ status: "locating" });
    navigator.geolocation.getCurrentPosition(
      (position) => {
        set({
          status: "success",
          coords: { lat: position.coords.latitude, lng: position.coords.longitude },
        });
      },
      () => set({ status: "error" }),
      GEOLOCATION_OPTIONS,
    );
  },
}));
