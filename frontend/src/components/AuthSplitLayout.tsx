import type { ReactNode } from "react";
import { HeroContourBackground } from "@/components/HeroContourBackground";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";

interface AuthSplitLayoutProps {
  title: string;
  subtitle: string;
  children: ReactNode;
}

// Split-screen: a branded "trail map" panel on one side, the form on the
// other. Used by both /login and /register, which share this shape and only
// differ in copy and the form itself.
export function AuthSplitLayout({ title, subtitle, children }: AuthSplitLayoutProps) {
  return (
    <div className="flex flex-1 flex-col sm:flex-row">
      <div
        className="relative flex min-h-[220px] flex-col items-center justify-center overflow-hidden px-10 py-12 text-center sm:w-2/5 sm:min-h-full"
        style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }}
      >
        <div className="absolute inset-0 opacity-40 [filter:invert(1)]">
          <HeroContourBackground />
        </div>
        <div className="relative mx-auto max-w-xs">
          <div className="text-lg font-semibold tracking-tight">SecretSpots</div>
          <h1 className="mt-6 text-3xl leading-tight font-extrabold text-balance">{title}</h1>
          <p className="mt-3 text-sm" style={{ color: "var(--fieldmap-contour)" }}>
            {subtitle}
          </p>
        </div>
      </div>

      <div
        className="relative flex flex-1 flex-col items-center justify-center gap-6 p-8"
        style={{ backgroundColor: "var(--fieldmap-paper)", color: "var(--fieldmap-ink)" }}
      >
        <div className="absolute top-4 right-4">
          <LocaleSwitcher />
        </div>
        {children}
      </div>
    </div>
  );
}
