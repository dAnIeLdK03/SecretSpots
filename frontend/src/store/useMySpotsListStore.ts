import { create } from "zustand";
import { getMySpots } from "@/lib/spotsApi";
import type { SpotSearchResult } from "@/lib/spotsApi";

const PAGE_SIZE = 20;

export type MySpotsListStatus = "idle" | "loading" | "loadingMore" | "error";

interface MySpotsListStore {
  items: SpotSearchResult[];
  page: number;
  totalCount: number;
  status: MySpotsListStatus;
  loadFirstPage: () => Promise<void>;
  loadMore: () => Promise<void>;
  reset: () => void;
}

export const useMySpotsListStore = create<MySpotsListStore>((set, get) => ({
  items: [],
  page: 0,
  totalCount: 0,
  status: "idle",

  loadFirstPage: async () => {
    set({ status: "loading" });
    try {
      const result = await getMySpots(1, PAGE_SIZE);
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
      const result = await getMySpots(page + 1, PAGE_SIZE);
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
