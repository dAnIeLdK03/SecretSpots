import { create } from "zustand";
import { fetchMySavedSpots, type SavedSpotResponse } from "@/lib/savedSpotsApi";

const PAGE_SIZE = 20;

export type SavedSpotsListStatus = "idle" | "loading" | "loadingMore" | "error";

interface SavedSpotsListStore {
  items: SavedSpotResponse[];
  page: number;
  totalCount: number;
  status: SavedSpotsListStatus;
  loadFirstPage: () => Promise<void>;
  loadMore: () => Promise<void>;
  reset: () => void;
}

export const useSavedSpotsListStore = create<SavedSpotsListStore>((set, get) => ({
  items: [],
  page: 0,
  totalCount: 0,
  status: "idle",

  loadFirstPage: async () => {
    set({ status: "loading" });
    try {
      const result = await fetchMySavedSpots(1, PAGE_SIZE);
      set({ items: result.items, page: result.page, totalCount: result.totalCount, status: "idle" });
    } catch {
      set({ status: "error" });
    }
  },

  loadMore: async () => {
    const { page, items, totalCount, status } = get();
    if (status === "loadingMore" || items.length >= totalCount) return;

    set({ status: "loadingMore" });
    try {
      const result = await fetchMySavedSpots(page + 1, PAGE_SIZE);
      set({
        items: [...items, ...result.items],
        page: result.page,
        totalCount: result.totalCount,
        status: "idle",
      });
    } catch {
      set({ status: "error" });
    }
  },

  reset: () => set({ items: [], page: 0, totalCount: 0, status: "idle" }),
}));
