"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { getAdminUsers } from "@/lib/adminApi";
import type { AdminUser } from "@/lib/adminApi";
import { UserRole } from "@/lib/userRole";
import { usePagedList } from "@/hooks/usePagedList";
import { LoadMoreButton } from "@/components/LoadMoreButton";

const PAGE_SIZE = 20;
const DEBOUNCE_MS = 300;

export default function AdminUsersPage() {
  const t = useTranslations("AdminUsers");

  const [search, setSearch] = useState("");
  // What the list is actually filtered by — lags `search` by DEBOUNCE_MS so typing doesn't fire
  // a request per keystroke.
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timeoutId = setTimeout(() => setDebouncedSearch(search.trim()), DEBOUNCE_MS);
    return () => clearTimeout(timeoutId);
  }, [search]);

  const fetchUsers = useCallback(
    (page: number, signal: AbortSignal) => getAdminUsers(page, PAGE_SIZE, debouncedSearch || undefined, signal),
    [debouncedSearch],
  );
  const list = usePagedList<AdminUser>(fetchUsers);
  const { items, status } = list;
  const error = list.error ? getErrorMessage(list.error, t("unknownError")) : null;

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t("title")}</h1>

      <input
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder={t("searchPlaceholder")}
        className="rounded-md border px-3 py-2 text-sm"
        style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-card)" }}
      />

      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}

      {status === "loading" ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : items.length === 0 ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("empty")}
        </p>
      ) : (
        <ul className="flex flex-col gap-2">
          {items.map((item) => (
            <li key={item.id}>
              <Link
                href={`/admin/users/${item.id}`}
                className="flex flex-wrap items-center justify-between gap-2 rounded-2xl p-4 hover:opacity-80"
                style={{ backgroundColor: "var(--fieldmap-card)" }}
              >
                <div className="flex flex-col">
                  <span className="text-sm font-medium">{item.displayName}</span>
                  <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
                    {item.email}
                  </span>
                </div>
                <span
                  className="rounded-full px-3 py-1 text-xs font-medium"
                  style={
                    item.role === UserRole.Admin
                      ? { backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }
                      : { backgroundColor: "var(--fieldmap-contour)", color: "var(--fieldmap-ink)" }
                  }
                >
                  {item.role === UserRole.Admin ? t("roleAdmin") : t("roleUser")}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}

      <LoadMoreButton list={list} />
    </div>
  );
}
