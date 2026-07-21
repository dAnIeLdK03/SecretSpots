import { create } from "zustand";
import { fetchMyCheckIns, type MyCheckInResponse } from "@/lib/checkInsApi";

const PAGE_SIZE = 20;

export type CheckInsHistoryStatus = "idle" | "loading" | "loadingMore" | "error";

interface CheckInsHistoryStore {
  items: MyCheckInResponse[];
  page: number;
  totalCount: number;
  status: CheckInsHistoryStatus;
  loadFirstPage: () => Promise<void>;
  loadMore: () => Promise<void>;
  reset: () => void;
}

export const useCheckInsHistoryStore = create<CheckInsHistoryStore>((set, get) => ({
  items: [],
  page: 0,
  totalCount: 0,
  status: "idle",

  loadFirstPage: async () => {
    set({ status: "loading" });
    try {
      const result = await fetchMyCheckIns(1, PAGE_SIZE);
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
      const result = await fetchMyCheckIns(page + 1, PAGE_SIZE);
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
