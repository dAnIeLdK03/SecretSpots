"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { NotificationDropdown } from "@/components/NotificationDropdown";

export function NotificationBell() {
  const t = useTranslations("Notifications");
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const unreadCount = useNotificationsStore((state) => state.unreadCount());
  const loadFirstPage = useNotificationsStore((state) => state.loadFirstPage);

  useEffect(() => {
    if (!isOpen) return;

    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  function toggleOpen() {
    const nextIsOpen = !isOpen;
    setIsOpen(nextIsOpen);
    if (nextIsOpen) {
      loadFirstPage();
    }
  }

  return (
    <div ref={containerRef} className="relative">
      <button
        onClick={toggleOpen}
        aria-label={t("title")}
        className="relative rounded-full p-2 text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800"
      >
        🔔
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-semibold text-white">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>
      {isOpen && <NotificationDropdown onNavigate={() => setIsOpen(false)} />}
    </div>
  );
}
