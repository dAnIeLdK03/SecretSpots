import type { ReactNode } from "react";

interface ErrorMessageProps {
  children: ReactNode;
  // Spacing/alignment only (e.g. "mt-2", "p-8 text-center").
  className?: string;
  // For the dark-surfaced modals (CreateSpotModal), same convention as CategorySelect/MultiPhotoUpload.
  dark?: boolean;
}

// The one way an error message is shown. role="alert" makes screen readers read it out as soon as
// it appears, instead of the user having to find red text they can't see.
export function ErrorMessage({ children, className = "", dark = false }: ErrorMessageProps) {
  return (
    <p role="alert" className={`text-sm ${dark ? "text-red-400" : "text-red-700 dark:text-red-400"} ${className}`}>
      {children}
    </p>
  );
}
