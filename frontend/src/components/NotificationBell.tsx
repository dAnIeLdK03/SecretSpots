"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { NotificationDropdown } from "@/components/NotificationDropdown";

export function NotificationBell() {
  const t = useTranslations("Notifications");
  const [isOpen, setIsOpen] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const unreadCount = useNotificationsStore((state) => state.unreadCount());
  const loadFirstPage = useNotificationsStore((state) => state.loadFirstPage);

  useEffect(() => {
    if (!isOpen) return;

    function handleClickOutside(event: MouseEvent) {
      const target = event.target as Node;
      if (buttonRef.current?.contains(target) || dropdownRef.current?.contains(target)) {
        return;
      }
      setIsOpen(false);
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  useEffect(() => {
    loadFirstPage();
  }, [loadFirstPage]);

  useEffect(() => {
    // Polls for new notifications while the dropdown is closed, so the badge reflects reality
    // without the user having to open it. Paused while open so it doesn't reset the list/scroll
    // position out from under someone browsing or paginating it.
    if (isOpen) return;

    const intervalId = setInterval(() => loadFirstPage(), 30_000);
    return () => clearInterval(intervalId);
  }, [isOpen, loadFirstPage]);

  function toggleOpen() {
    const nextIsOpen = !isOpen;
    setIsOpen(nextIsOpen);
    if (nextIsOpen) {
      loadFirstPage();
    }
  }

  return (
    <div className="relative">
      <button
        ref={buttonRef}
        onClick={toggleOpen}
        aria-label={t("title")}
        className="relative rounded-full p-2 hover:bg-black/5 dark:hover:bg-white/5"
        style={{ color: "var(--fieldmap-dim)" }}
      >
        🔔
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-semibold text-white">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>
      {isOpen && (
        <NotificationDropdown ref={dropdownRef} anchorRef={buttonRef} onNavigate={() => setIsOpen(false)} />
      )}
    </div>
  );
}
