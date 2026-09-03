"use client";

import { useEffect, useRef } from "react";
import type { MouseEvent } from "react";

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

// Shared by every modal (CreateSpotModal, EditSpotModal, ReportModal, CheckInModal): Escape
// closes it, Tab/Shift+Tab cycles focus without leaving it, and focus returns to whatever
// triggered it on close — the behavior a `role="dialog"` is expected to have, none of which a
// plain fixed-overlay div gets for free.
export function useModalDismiss(onClose: () => void) {
  const containerRef = useRef<HTMLDivElement>(null);

  // Read through a ref inside the effect instead of listing onClose as a dependency — most
  // callers pass a fresh arrow function every render, which would otherwise tear down and
  // re-run the focus-trap/focus-restore setup (and briefly drop focus) on every single render.
  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  });

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const previouslyFocused = document.activeElement as HTMLElement | null;

    const initialFocusable = container.querySelector<HTMLElement>(FOCUSABLE_SELECTOR);
    (initialFocusable ?? container).focus();

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.stopPropagation();
        onCloseRef.current();
        return;
      }

      if (event.key !== "Tab" || !container) return;

      const focusable = Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).filter(
        (el) => el.offsetParent !== null,
      );
      if (focusable.length === 0) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      previouslyFocused?.focus();
    };
  }, []);

  function handleOverlayClick(event: MouseEvent<HTMLDivElement>) {
    if (event.target === event.currentTarget) {
      onCloseRef.current();
    }
  }

  return { containerRef, handleOverlayClick };
}
