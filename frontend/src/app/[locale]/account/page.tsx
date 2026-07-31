"use client";

import { useEffect } from "react";
import { useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useAuthStore } from "@/store/useAuthStore";
import { useCheckInsHistoryStore } from "@/store/useCheckInsHistoryStore";
import { CheckInHistoryItem } from "@/components/CheckInHistoryItem";

export default function AccountPage() {
  const t = useTranslations("Auth");
  const tHistory = useTranslations("CheckInsHistory");
  const isAuthenticated = useRequireAuth();
  const user = useAuthStore((state) => state.user);

  const items = useCheckInsHistoryStore((state) => state.items);
  const status = useCheckInsHistoryStore((state) => state.status);
  const totalCount = useCheckInsHistoryStore((state) => state.totalCount);
  const loadFirstPage = useCheckInsHistoryStore((state) => state.loadFirstPage);
  const loadMore = useCheckInsHistoryStore((state) => state.loadMore);

  useEffect(() => {
    if (isAuthenticated) {
      loadFirstPage();
    }
  }, [isAuthenticated, loadFirstPage]);

  if (!isAuthenticated || !user) {
    return null;
  }

  const hasMore = items.length < totalCount;

  return (
    <div className="flex flex-1 flex-col items-center gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t("accountTitle")}</h1>
      <dl className="grid w-full max-w-sm grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
        <dt style={{ color: "var(--fieldmap-dim)" }}>{t("displayNameLabel")}</dt>
        <dd>{user.displayName}</dd>
        <dt style={{ color: "var(--fieldmap-dim)" }}>{t("emailLabel")}</dt>
        <dd>{user.email}</dd>
        <dt style={{ color: "var(--fieldmap-dim)" }}>{t("crystalBalanceLabel")}</dt>
        <dd>{user.crystalBalance}</dd>
      </dl>

      <div className="w-full max-w-sm">
        <h2 className="mb-2 text-sm font-semibold">{tHistory("title")}</h2>
        <div className="rounded-md border" style={{ borderColor: "var(--fieldmap-contour)" }}>
          {status === "loading" ? (
            <p className="px-4 py-6 text-center text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {tHistory("loading")}
            </p>
          ) : items.length === 0 ? (
            <p className="px-4 py-6 text-center text-sm" style={{ color: "var(--fieldmap-dim)" }}>
              {tHistory("empty")}
            </p>
          ) : (
            <ul className="divide-y divide-[var(--fieldmap-contour)]">
              {items.map((checkIn) => (
                <CheckInHistoryItem key={checkIn.id} checkIn={checkIn} />
              ))}
            </ul>
          )}

          {hasMore && (
            <button
              onClick={() => loadMore()}
              disabled={status === "loadingMore"}
              className="w-full border-t px-4 py-2 text-center text-sm disabled:opacity-50"
              style={{ borderColor: "var(--fieldmap-contour)", color: "var(--fieldmap-dim)" }}
            >
              {status === "loadingMore" ? tHistory("loadingMore") : tHistory("loadMore")}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
