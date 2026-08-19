"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { ChevronDown, Check } from "lucide-react";
import { SPOT_CATEGORIES } from "@/lib/spotsApi";
import type { SpotCategory } from "@/lib/spotsApi";
import { CategoryIcon } from "@/components/CategoryIcon";

interface CategorySelectProps {
  value: SpotCategory;
  onChange: (category: SpotCategory) => void;
  // Same convention as MultiPhotoUpload's `dark` prop — CreateSpotModal is unconditionally dark
  // regardless of OS theme, EditSpotModal uses the light "fieldmap" surface tokens instead.
  dark?: boolean;
}

// Native <select> can't render a React icon inside <option>, so this reimplements just enough of
// a listbox to show a CategoryIcon next to every option — matching the icons already shown
// everywhere else a category appears (filter pills, cards, map popup, spot detail).
export function CategorySelect({ value, onChange, dark = false }: CategorySelectProps) {
  const t = useTranslations("Spots");
  const [isOpen, setIsOpen] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const [menuWidth, setMenuWidth] = useState<number | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    function handleClickOutside(event: MouseEvent) {
      const target = event.target as Node;
      if (buttonRef.current?.contains(target) || menuRef.current?.contains(target)) return;
      setIsOpen(false);
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") setIsOpen(false);
    }

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen]);

  useLayoutEffect(() => {
    if (!isOpen || !buttonRef.current) return;
    setMenuWidth(buttonRef.current.getBoundingClientRect().width);
  }, [isOpen]);

  const triggerClasses = dark
    ? "flex w-full items-center justify-between gap-2 rounded-lg border border-white/15 bg-white/5 px-3 py-2 text-white focus:border-emerald-400 focus:outline-none"
    : "flex w-full items-center justify-between gap-2 rounded border px-3 py-2";

  const menuClasses = dark
    ? "absolute z-10 mt-1 max-h-64 overflow-y-auto rounded-lg border border-white/15 bg-zinc-900 py-1 shadow-lg"
    : "absolute z-10 mt-1 max-h-64 overflow-y-auto rounded border py-1 shadow-lg";

  return (
    <div className="relative">
      <button
        ref={buttonRef}
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        className={triggerClasses}
        style={dark ? undefined : { borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
      >
        <span className="flex items-center gap-2">
          <CategoryIcon category={value} size={16} />
          {t(`category.${value}`)}
        </span>
        <ChevronDown size={16} className={dark ? "text-zinc-400" : ""} style={dark ? undefined : { color: "var(--fieldmap-dim)" }} />
      </button>
      {isOpen ? (
        <div
          ref={menuRef}
          role="listbox"
          className={menuClasses}
          style={{
            width: menuWidth ?? undefined,
            ...(dark ? {} : { borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }),
          }}
        >
          {SPOT_CATEGORIES.map((c) => {
            const isSelected = c === value;
            return (
              <button
                key={c}
                type="button"
                role="option"
                aria-selected={isSelected}
                onClick={() => {
                  onChange(c);
                  setIsOpen(false);
                }}
                className={
                  dark
                    ? `flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-white/10 ${isSelected ? "text-emerald-400" : "text-white"}`
                    : "flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:opacity-80"
                }
                style={dark ? undefined : { color: isSelected ? "var(--fieldmap-trail)" : "var(--fieldmap-ink)" }}
              >
                <CategoryIcon category={c} size={16} />
                <span className="flex-1">{t(`category.${c}`)}</span>
                {isSelected ? <Check size={14} /> : null}
              </button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
