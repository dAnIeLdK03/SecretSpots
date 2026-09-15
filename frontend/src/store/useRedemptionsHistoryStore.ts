import { create } from "zustand";
import { fetchMyRedemptions, type MyRedemptionResponse } from "@/lib/rewardsApi";

const PAGE_SIZE = 20;

export type RedemptionsHistoryStatus = "idle" | "loading" | "loadingMore" | "error";

interface RedemptionsHistoryStore {
  items: MyRedemptionResponse[];
  page: number;
  totalCount: number;
  status: RedemptionsHistoryStatus;
  loadFirstPage: () => Promise<void>;
  loadMore: () => Promise<void>;
  reset: () => void;
}

export const useRedemptionsHistoryStore = create<RedemptionsHistoryStore>((set, get) => ({
  items: [],
  page: 0,
  totalCount: 0,
  status: "idle",

  loadFirstPage: async () => {
    set({ status: "loading" });
    try {
      const result = await fetchMyRedemptions(1, PAGE_SIZE);
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
      const result = await fetchMyRedemptions(page + 1, PAGE_SIZE);
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
