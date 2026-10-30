import type { ReactNode } from "react";

interface StatusMessageProps {
  children: ReactNode;
  // Spacing/alignment only (e.g. "p-8 text-center").
  className?: string;
}

// "Loading…" and similar transient states. role="status" is a polite live region, so screen readers
// announce it without interrupting what they're currently reading.
export function StatusMessage({ children, className = "" }: StatusMessageProps) {
  return (
    <p role="status" className={`text-sm text-dim ${className}`}>
      {children}
    </p>
  );
}
