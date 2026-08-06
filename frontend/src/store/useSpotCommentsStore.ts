import { create } from "zustand";
import {
  fetchSpotComments,
  createComment as createCommentApi,
  updateComment as updateCommentApi,
  deleteComment as deleteCommentApi,
  type CommentResponse,
} from "@/lib/commentsApi";

const PAGE_SIZE = 20;

export type SpotCommentsStatus = "idle" | "loading" | "loadingMore" | "error";

interface SpotCommentsStore {
  items: CommentResponse[];
  page: number;
  totalCount: number;
  status: SpotCommentsStatus;
  loadFirstPage: (spotId: string) => Promise<void>;
  loadMore: (spotId: string) => Promise<void>;
  addComment: (spotId: string, text: string) => Promise<void>;
  editComment: (commentId: string, text: string) => Promise<void>;
  removeComment: (commentId: string) => Promise<void>;
  reset: () => void;
}

export const useSpotCommentsStore = create<SpotCommentsStore>((set, get) => ({
  items: [],
  page: 0,
  totalCount: 0,
  status: "idle",

  loadFirstPage: async (spotId) => {
    set({ status: "loading" });
    try {
      const result = await fetchSpotComments(spotId, 1, PAGE_SIZE);
      set({ items: result.items, page: result.page, totalCount: result.totalCount, status: "idle" });
    } catch {
      set({ status: "error" });
    }
  },

  loadMore: async (spotId) => {
    const { page, items, totalCount, status } = get();
    if (status === "loadingMore" || items.length >= totalCount) return;

    set({ status: "loadingMore" });
    try {
      const result = await fetchSpotComments(spotId, page + 1, PAGE_SIZE);
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

  addComment: async (spotId, text) => {
    const comment = await createCommentApi(spotId, text);
    set((state) => ({ items: [comment, ...state.items], totalCount: state.totalCount + 1 }));
  },

  editComment: async (commentId, text) => {
    const updated = await updateCommentApi(commentId, text);
    set((state) => ({ items: state.items.map((c) => (c.id === commentId ? updated : c)) }));
  },

  removeComment: async (commentId) => {
    await deleteCommentApi(commentId);
    set((state) => ({
      items: state.items.filter((c) => c.id !== commentId),
      totalCount: Math.max(0, state.totalCount - 1),
    }));
  },

  reset: () => set({ items: [], page: 0, totalCount: 0, status: "idle" }),
}));
