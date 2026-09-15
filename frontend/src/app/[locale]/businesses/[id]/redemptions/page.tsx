"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useTranslations, useLocale } from "next-intl";
import { Check } from "lucide-react";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useAuthStore } from "@/store/useAuthStore";
import { getBusiness } from "@/lib/businessesApi";
import type { BusinessResponse } from "@/lib/businessesApi";
import { fetchBusinessRedemptions, fulfillRedemption } from "@/lib/rewardsApi";
import type { BusinessRedemptionResponse } from "@/lib/rewardsApi";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { formatRelativeTime } from "@/lib/relativeTime";

const PAGE_SIZE = 20;

type LoadState =
  | { status: "loading" }
  | { status: "success"; business: BusinessResponse }
  | { status: "notFound" }
  | { status: "forbidden" }
  | { status: "error"; message: string };

function RedemptionRow({
  redemption,
  onFulfilled,
}: {
  redemption: BusinessRedemptionResponse;
  onFulfilled: (redemptionId: string) => void;
}) {
  const t = useTranslations("Rewards");
  const locale = useLocale();
  const [fulfilling, setFulfilling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleFulfill() {
    if (fulfilling) return;
    setFulfilling(true);
    setError(null);
    try {
      await fulfillRedemption(redemption.redemptionId);
      onFulfilled(redemption.redemptionId);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
    } finally {
      setFulfilling(false);
    }
  }

  return (
    <li className="flex flex-col gap-2 px-4 py-3 text-sm">
      <div className="flex items-start justify-between gap-3">
        <span className="flex flex-col">
          <span className="font-medium">{redemption.rewardTitle}</span>
          <span className="text-xs" style={{ color: "var(--fieldmap-dim)" }}>
            {t("redeemedByLabel", { name: redemption.redeemedByDisplayName })} ·{" "}
            {formatRelativeTime(redemption.createdAt, locale)}
          </span>
        </span>
        <span
          className="rounded px-2 py-1 font-mono text-sm font-semibold tracking-widest"
          style={{ backgroundColor: "var(--fieldmap-card)", color: "var(--fieldmap-ink)" }}
        >
          {redemption.redemptionCode}
        </span>
      </div>

      {error ? <p className="text-xs text-red-700 dark:text-red-400">{error}</p> : null}

      {redemption.isFulfilled ? (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium" style={{ color: "var(--fieldmap-trail)" }}>
          <Check size={14} />
          {t("fulfilledLabel")}
        </span>
      ) : (
        <button
          type="button"
          onClick={handleFulfill}
          disabled={fulfilling}
          className="self-start rounded px-3 py-1.5 text-xs disabled:opacity-50"
          style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }}
        >
          {fulfilling ? t("markingFulfilled") : t("markFulfilledButton")}
        </button>
      )}
    </li>
  );
}

function BusinessRedemptionsContent({ businessId }: { businessId: string }) {
  const t = useTranslations("Rewards");
  const tBusinesses = useTranslations("Businesses");
  const user = useAuthStore((state) => state.user);

  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [items, setItems] = useState<BusinessRedemptionResponse[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loadingMore, setLoadingMore] = useState(false);

  const loadPage = useCallback(
    async (pageNum: number, append: boolean) => {
      const result = await fetchBusinessRedemptions(businessId, pageNum, PAGE_SIZE);
      setItems((prev) => (append ? [...prev, ...result.items] : result.items));
      setTotalCount(result.totalCount);
      setPage(result.page);
    },
    [businessId],
  );

  useEffect(() => {
    let cancelled = false;

    getBusiness(businessId)
      .then((business) => {
        if (cancelled) return;
        if (user?.id !== business.ownerUserId) {
          setState({ status: "forbidden" });
          return;
        }
        setState({ status: "success", business });
        return loadPage(1, false);
      })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 404) {
          setState({ status: "notFound" });
        } else {
          setState({ status: "error", message: getErrorMessage(err, tBusinesses("unknownError")) });
        }
      });

    return () => {
      cancelled = true;
    };
  }, [businessId, user, loadPage, tBusinesses]);

  function handleFulfilled(redemptionId: string) {
    setItems((prev) =>
      prev.map((item) =>
        item.redemptionId === redemptionId
          ? { ...item, isFulfilled: true, fulfilledAt: new Date().toISOString() }
          : item,
      ),
    );
  }

  async function handleLoadMore() {
    setLoadingMore(true);
    try {
      await loadPage(page + 1, true);
    } finally {
      setLoadingMore(false);
    }
  }

  if (state.status === "loading") {
    return (
      <p className="p-8 text-center text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {tBusinesses("loading")}
      </p>
    );
  }

  if (state.status === "notFound") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
        <h1 className="text-xl font-semibold">{tBusinesses("notFoundTitle")}</h1>
        <p style={{ color: "var(--fieldmap-dim)" }}>{tBusinesses("notFoundMessage")}</p>
      </div>
    );
  }

  if (state.status === "forbidden") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
        <p style={{ color: "var(--fieldmap-dim)" }}>{t("notYourBusiness")}</p>
      </div>
    );
  }

  if (state.status === "error") {
    return <p className="p-8 text-center text-sm text-red-700 dark:text-red-400">{state.message}</p>;
  }

  const hasMore = items.length < totalCount;

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-4 p-6">
      <h1 className="text-2xl font-semibold">{t("manageRedemptionsTitle", { name: state.business.name })}</h1>

      <div className="rounded-md border" style={{ borderColor: "var(--fieldmap-contour)" }}>
        {items.length === 0 ? (
          <p className="px-4 py-6 text-center text-sm" style={{ color: "var(--fieldmap-dim)" }}>
            {t("noRedemptionsForBusinessYet")}
          </p>
        ) : (
          <ul className="divide-y divide-[var(--fieldmap-contour)]">
            {items.map((redemption) => (
              <RedemptionRow key={redemption.redemptionId} redemption={redemption} onFulfilled={handleFulfilled} />
            ))}
          </ul>
        )}

        {hasMore && (
          <button
            onClick={handleLoadMore}
            disabled={loadingMore}
            className="w-full border-t px-4 py-2 text-center text-sm disabled:opacity-50"
            style={{ borderColor: "var(--fieldmap-contour)", color: "var(--fieldmap-dim)" }}
          >
            {loadingMore ? t("loadingMore") : t("loadMore")}
          </button>
        )}
      </div>
    </div>
  );
}

export default function BusinessRedemptionsPage() {
  const isAuthenticated = useRequireAuth();
  const params = useParams<{ id: string }>();

  if (!isAuthenticated) {
    return null;
  }

  return <BusinessRedemptionsContent businessId={params.id} />;
}
