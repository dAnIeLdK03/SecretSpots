import { create } from "zustand";

interface AppStore {
  exampleCounter: number;
  increment: () => void;
}

export const useAppStore = create<AppStore>((set) => ({
  exampleCounter: 0,
  increment: () => set((state) => ({ exampleCounter: state.exampleCounter + 1 })),
}));
