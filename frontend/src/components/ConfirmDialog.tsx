"use client";

import { useId } from "react";
import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { useModalDismiss } from "@/hooks/useModalDismiss";

interface ConfirmDialogProps {
  title: string;
  message: string;
  confirmLabel: string;
  onConfirm: () => void;
  onCancel: () => void;
  // Extra fields the confirmation needs (e.g. the password for deleting an account).
  children?: ReactNode;
}

// Replaces window.confirm: same look as the other modals, translated buttons, Escape/overlay
// cancel. Cancel comes first so it gets the initial focus — a stray Enter never confirms a
// destructive action (unless `children` adds a field, which then gets focus instead).
export function ConfirmDialog({ title, message, confirmLabel, onConfirm, onCancel, children }: ConfirmDialogProps) {
  const t = useTranslations("Common");
  const titleId = useId();
  const messageId = useId();
  const { containerRef, handleOverlayClick } = useModalDismiss(onCancel);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" onClick={handleOverlayClick}>
      <div
        ref={containerRef}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={messageId}
        tabIndex={-1}
        className="w-full max-w-sm rounded-lg bg-paper-light p-6 text-ink outline-none"
      >
        <h2 id={titleId} className="mb-2 text-lg font-semibold">
          {title}
        </h2>
        <p id={messageId} className="text-sm text-dim">
          {message}
        </p>
        {children ? <div className="mt-4">{children}</div> : null}
        <div className="mt-6 flex justify-end gap-2">
          <button type="button" onClick={onCancel} className="rounded border border-contour px-4 py-2 text-sm">
            {t("cancel")}
          </button>
          <button type="button" onClick={onConfirm} className="rounded bg-red-700 px-4 py-2 text-sm text-white">
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
