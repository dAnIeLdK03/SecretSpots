import { create } from "zustand";

export interface ToastMessage {
  // Changes on every show(), so showing the same text twice still restarts the timer.
  id: number;
  message: string;
}

interface ToastStore {
  toast: ToastMessage | null;
  show: (message: string) => void;
  dismiss: () => void;
}

let nextId = 0;

// One toast at a time — a newer confirmation replaces the one on screen. Global rather than
// per-page so a toast survives a navigation (e.g. "Account deleted" after redirecting home).
export const useToastStore = create<ToastStore>((set) => ({
  toast: null,
  show: (message) => set({ toast: { id: ++nextId, message } }),
  dismiss: () => set({ toast: null }),
}));
