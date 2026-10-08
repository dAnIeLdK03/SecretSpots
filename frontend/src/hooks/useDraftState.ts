"use client";

import { useState } from "react";

// Switching locale changes the [locale] URL segment, which Next.js treats as navigating to a
// different route — the page component (and its useState) gets torn down and recreated, so a
// half-filled login/register form loses everything the user typed. This module-level Map lives
// outside React entirely (not localStorage/sessionStorage — nothing touches disk), so it survives
// that remount for as long as the tab stays open, while still disappearing on an actual reload.
const draftStore = new Map<string, unknown>();

export function useDraftState<T>(key: string, initialValue: T): [T, (value: T) => void] {
  const [value, setValue] = useState<T>(() => (draftStore.has(key) ? (draftStore.get(key) as T) : initialValue));

  function setAndStore(next: T) {
    draftStore.set(key, next);
    setValue(next);
  }

  return [value, setAndStore];
}

export function clearDraftState(...keys: string[]): void {
  for (const key of keys) {
    draftStore.delete(key);
  }
}
