"use client";

import { useToastStore } from "@/store/useToastStore";

// Short success confirmation after an action ("Spot approved", "Saved"). Errors keep using
// ErrorMessage next to the thing that failed — a toast that vanishes is the wrong place for them.
export function useToast(): (message: string) => void {
  return useToastStore((state) => state.show);
}
