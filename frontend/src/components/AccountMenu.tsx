"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { getRefreshToken, clearRefreshToken } from "@/lib/refreshTokenStorage";
import { logout } from "@/lib/authApi";
import { Avatar } from "@/components/Avatar";
import { NotificationItem } from "@/components/NotificationItem";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";

interface NavItem {
  href: string;
  label: string;
}

interface AccountMenuProps {
  displayName: string;
  crystalBalance: number;
  // Rendered inside the dropdown, hidden at the sm breakpoint and up — lets this menu double as
  // the mobile nav so a separate hamburger button isn't needed once an avatar is on screen.
  mobileNavItems?: NavItem[];
}

export function AccountMenu({ displayName, crystalBalance, mobileNavItems = [] }: AccountMenuProps) {
  const t = useTranslations("Layout");
  const tHome = useTranslations("Home");
  const tAuth = useTranslations("Auth");
  const tNotifications = useTranslations("Notifications");
  const clearSession = useAuthStore((state) => state.clearSession);

  const items = useNotificationsStore((state) => state.items);
  const notificationsStatus = useNotificationsStore((state) => state.status);
  const totalCount = useNotificationsStore((state) => state.totalCount);
  const unreadCount = useNotificationsStore((state) => state.unreadCount());
  const loadFirstPage = useNotificationsStore((state) => state.loadFirstPage);
  const loadMore = useNotificationsStore((state) => state.loadMore);
  const resetNotifications = useNotificationsStore((state) => state.reset);
  const resetCheckInsHistory = useCheckInsHistoryStore((state) => state.reset);

  const [isOpen, setIsOpen] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState<{ top: number; right: number } | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    function handleClickOutside(event: MouseEvent) {
      const target = event.target as Node;
      if (buttonRef.current?.contains(target) || menuRef.current?.contains(target)) return;
      setIsOpen(false);
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  useLayoutEffect(() => {
    if (!isOpen) return;
    const button = buttonRef.current;
    if (!button) return;
    const rect = button.getBoundingClientRect();
    setPosition({ top: rect.bottom + 8, right: window.innerWidth - rect.right });
  }, [isOpen]);

  useEffect(() => {
    loadFirstPage();
  }, [loadFirstPage]);

  useEffect(() => {
    // Polls for new notifications while the menu is closed, so the badge reflects reality
    // without the user having to open it. Paused while open so it doesn't reset the
    // list/scroll position out from under someone browsing or paginating it.
    if (isOpen) return;

    const intervalId = setInterval(() => loadFirstPage(), 30_000);
    return () => clearInterval(intervalId);
  }, [isOpen, loadFirstPage]);

  function closeMenu() {
    setIsOpen(false);
  }

  function toggleOpen() {
    const nextIsOpen = !isOpen;
    setIsOpen(nextIsOpen);
    if (nextIsOpen) {
      loadFirstPage();
    }
  }

  function handleLogout() {
    const refreshToken = getRefreshToken();
    clearRefreshToken();
    clearSession();
    resetNotifications();
    resetCheckInsHistory();
    if (refreshToken) {
      logout(refreshToken).catch(() => {});
    }
    closeMenu();
  }

  const hasMoreNotifications = items.length < totalCount;

  return (
    <div className="relative">
      <button
        ref={buttonRef}
        type="button"
        onClick={toggleOpen}
        aria-label={t("menuLabel")}
        aria-expanded={isOpen}
        className="relative flex items-center gap-1.5 rounded-full border py-1 pr-2 pl-1 hover:bg-black/5"
        style={{ borderColor: "var(--fieldmap-contour)" }}
      >
        <Avatar name={displayName} size={28} />
        {unreadCount > 0 && (
          <span className="absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-semibold text-white">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
        <span aria-hidden="true" className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
          ▾
        </span>
      </button>

      {isOpen &&
        position &&
        createPortal(
          <div
            ref={menuRef}
            className="fixed z-50 max-h-[80vh] w-72 overflow-y-auto rounded-md border py-2 text-sm shadow-lg"
            style={{
              top: position.top,
              right: position.right,
              borderColor: "var(--fieldmap-contour)",
              backgroundColor: "var(--fieldmap-paper-light)",
            }}
          >
            {mobileNavItems.length > 0 && (
              <nav
                className="mb-2 flex flex-col border-b pb-2 sm:hidden"
                style={{ borderColor: "var(--fieldmap-contour)" }}
              >
                {mobileNavItems.map((item) => (
                  <Link key={item.href} href={item.href} onClick={closeMenu} className="px-4 py-2 hover:bg-black/5">
                    {item.label}
                  </Link>
                ))}
              </nav>
            )}

            <div className="mb-2 border-b pb-2" style={{ borderColor: "var(--fieldmap-contour)" }}>
              <div className="px-4 pb-1 text-xs font-semibold uppercase" style={{ color: "var(--fieldmap-dim)" }}>
                {tNotifications("title")}
              </div>

              {notificationsStatus === "loading" ? (
                <p className="px-4 py-3 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                  {tNotifications("loading")}
                </p>
              ) : items.length === 0 ? (
                <p className="px-4 py-3 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                  {tNotifications("empty")}
                </p>
              ) : (
                <ul className="divide-y" style={{ borderColor: "var(--fieldmap-contour)" }}>
                  {items.map((notification) => (
                    <li key={notification.id}>
                      <NotificationItem notification={notification} onNavigate={closeMenu} />
                    </li>
                  ))}
                </ul>
              )}

              {hasMoreNotifications && (
                <button
                  onClick={() => loadMore()}
                  disabled={notificationsStatus === "loadingMore"}
                  className="w-full px-4 py-2 text-center text-sm disabled:opacity-50"
                  style={{ color: "var(--fieldmap-dim)" }}
                >
                  {notificationsStatus === "loadingMore" ? tNotifications("loadingMore") : tNotifications("loadMore")}
                </button>
              )}
            </div>

            <Link href="/map" onClick={closeMenu} className="block px-4 py-2 hover:bg-black/5">
              {tHome("addASpot")}
            </Link>

            <div className="my-2 border-b" style={{ borderColor: "var(--fieldmap-contour)" }} />

            <div className="px-4 py-1">
              <div className="pb-1 text-xs font-semibold uppercase" style={{ color: "var(--fieldmap-dim)" }}>
                {t("settingsLabel")}
              </div>
              <LocaleSwitcher />
            </div>

            <div className="my-2 border-b" style={{ borderColor: "var(--fieldmap-contour)" }} />

            <Link href="/account" onClick={closeMenu} className="block px-4 py-2 hover:bg-black/5">
              {displayName} ({crystalBalance} {tAuth("crystalBalanceLabel")})
            </Link>
            <button onClick={handleLogout} className="block w-full px-4 py-2 text-left hover:bg-black/5">
              {tAuth("logoutButton")}
            </button>
          </div>,
          document.body,
        )}
    </div>
  );
}
