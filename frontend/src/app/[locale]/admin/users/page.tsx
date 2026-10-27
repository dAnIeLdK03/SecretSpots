"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { getErrorMessage } from "@/lib/apiClient";
import { getAdminUsers } from "@/lib/adminApi";
import type { AdminUser } from "@/lib/adminApi";
import { UserRole } from "@/lib/userRole";

const PAGE_SIZE = 20;
const DEBOUNCE_MS = 300;

export default function AdminUsersPage() {
  const t = useTranslations("AdminUsers");

  const [search, setSearch] = useState("");
  const [items, setItems] = useState<AdminUser[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<"loading" | "idle" | "loadingMore">("loading");
  const [error, setError] = useState<string | null>(null);

  const fetchPage = useCallback(
    (pageNum: number, append: boolean) => {
      setStatus(append ? "loadingMore" : "loading");
      return getAdminUsers(pageNum, PAGE_SIZE, search.trim() || undefined)
        .then((result) => {
          setItems((prev) => (append ? [...prev, ...result.items] : result.items));
          setTotalCount(result.totalCount);
          setPage(pageNum);
          setError(null);
        })
        .catch((err) => setError(getErrorMessage(err, t("unknownError"))))
        .finally(() => setStatus("idle"));
    },
    [search, t],
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void fetchPage(1, false);
    }, DEBOUNCE_MS);

    return () => clearTimeout(timeoutId);
  }, [fetchPage]);

  const hasMore = items.length < totalCount;

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

      {hasMore ? (
        <button
          onClick={() => fetchPage(page + 1, true)}
          disabled={status === "loadingMore"}
          className="rounded border px-4 py-2 text-sm disabled:opacity-50"
          style={{ borderColor: "var(--fieldmap-contour)" }}
        >
          {status === "loadingMore" ? t("loadingMore") : t("loadMore")}
        </button>
      ) : null}
    </div>
  );
}
