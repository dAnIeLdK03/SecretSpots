"use client";

import { useEffect } from "react";
import { useToastStore } from "@/store/useToastStore";

const VISIBLE_MS = 3000;

// Rendered once in the locale layout. The role="status" wrapper stays mounted even with no toast,
// so screen readers announce each new message (a live region added together with its text is
// often missed).
export function Toast() {
  const toast = useToastStore((state) => state.toast);
  const dismiss = useToastStore((state) => state.dismiss);

  useEffect(() => {
    if (!toast) return;
    const timeoutId = setTimeout(dismiss, VISIBLE_MS);
    return () => clearTimeout(timeoutId);
  }, [toast, dismiss]);

  return (
    <div role="status" className="pointer-events-none fixed inset-x-0 top-4 z-[60] flex justify-center px-4">
      {toast ? (
        <p key={toast.id} className="rounded-lg bg-ink px-4 py-2 text-sm text-paper shadow-lg">
          {toast.message}
        </p>
      ) : null}
    </div>
  );
}
