import { create } from "zustand";
import {
  fetchNotifications,
  markNotificationRead,
  type NotificationResponse,
} from "@/lib/notificationsApi";

const PAGE_SIZE = 20;

export type NotificationsStatus = "idle" | "loading" | "loadingMore" | "error";

interface NotificationsStore {
  items: NotificationResponse[];
  page: number;
  totalCount: number;
  status: NotificationsStatus;
  unreadCount: () => number;
  loadFirstPage: () => Promise<void>;
  loadMore: () => Promise<void>;
  markAsRead: (id: string) => Promise<void>;
  reset: () => void;
}

export const useNotificationsStore = create<NotificationsStore>((set, get) => ({
  items: [],
  page: 0,
  totalCount: 0,
  status: "idle",
  unreadCount: () => get().items.filter((n) => !n.isRead).length,

  loadFirstPage: async () => {
    set({ status: "loading" });
    try {
      const result = await fetchNotifications(1, PAGE_SIZE);
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
      const result = await fetchNotifications(page + 1, PAGE_SIZE);
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

  markAsRead: async (id: string) => {
    const target = get().items.find((n) => n.id === id);
    if (!target || target.isRead) return;

    set({ items: get().items.map((n) => (n.id === id ? { ...n, isRead: true } : n)) });
    try {
      await markNotificationRead(id);
    } catch {
      set({ items: get().items.map((n) => (n.id === id ? { ...n, isRead: false } : n)) });
    }
  },

  reset: () => set({ items: [], page: 0, totalCount: 0, status: "idle" }),
}));
