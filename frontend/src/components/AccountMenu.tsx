"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useAuthStore } from "@/store/useAuthStore";
import { useNotificationsStore } from "@/store/useNotificationsStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { logout } from "@/lib/authApi";
import { Avatar } from "@/components/Avatar";

interface NavItem {
  href: string;
  label: string;
}

interface AccountMenuProps {
  displayName: string;
  // Rendered inside the dropdown, hidden at the sm breakpoint and up — lets this menu double as
  // the mobile nav so a separate hamburger button isn't needed once an avatar is on screen.
  mobileNavItems?: NavItem[];
}

export function AccountMenu({ displayName, mobileNavItems = [] }: AccountMenuProps) {
  const t = useTranslations("Layout");
  const tHome = useTranslations("Home");
  const tAuth = useTranslations("Auth");
  const isAdmin = useAuthStore((state) => state.user?.isAdmin ?? false);
  const clearSession = useAuthStore((state) => state.clearSession);
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

  function closeMenu() {
    setIsOpen(false);
  }

  function handleLogout() {
    clearSession();
    resetNotifications();
    resetCheckInsHistory();
    logout().catch(() => {});
    closeMenu();
  }

  return (
    <div className="relative">
      <button
        ref={buttonRef}
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-label={t("menuLabel")}
        aria-expanded={isOpen}
        className="flex items-center gap-1.5 rounded-full border py-1 pr-2 pl-1 hover:bg-black/5 dark:hover:bg-white/5"
        style={{ borderColor: "var(--fieldmap-contour)" }}
      >
        <Avatar name={displayName} size={28} />
        <span aria-hidden="true" className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
          ▾
        </span>
      </button>

      {isOpen &&
        position &&
        createPortal(
          <div
            ref={menuRef}
            className="fixed z-50 w-64 rounded-md border py-2 text-sm shadow-lg"
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
                  <Link key={item.href} href={item.href} onClick={closeMenu} className="px-4 py-2 hover:bg-black/5 dark:hover:bg-white/5">
                    {item.label}
                  </Link>
                ))}
              </nav>
            )}

            <Link href="/map" onClick={closeMenu} className="block px-4 py-2 hover:bg-black/5 dark:hover:bg-white/5">
              {tHome("addASpot")}
            </Link>
            <Link href="/settings" onClick={closeMenu} className="block px-4 py-2 hover:bg-black/5 dark:hover:bg-white/5">
              {t("settingsLabel")}
            </Link>

            <div className="my-2 border-b" style={{ borderColor: "var(--fieldmap-contour)" }} />

            <Link href="/account" onClick={closeMenu} className="block px-4 py-2 hover:bg-black/5 dark:hover:bg-white/5">
              {tAuth("profileLabel")}
            </Link>
            {isAdmin && (
              <Link
                href="/admin/reports"
                onClick={closeMenu}
                className="block px-4 py-2 hover:bg-black/5 dark:hover:bg-white/5"
              >
                {t("adminReportsLabel")}
              </Link>
            )}
            <button onClick={handleLogout} className="block w-full px-4 py-2 text-left hover:bg-black/5 dark:hover:bg-white/5">
              {tAuth("logoutButton")}
            </button>
          </div>,
          document.body,
        )}
    </div>
  );
}
