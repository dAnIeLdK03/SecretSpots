"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useFormatter, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { ApiError, getErrorMessage } from "@/lib/apiClient";
import { getAdminUser } from "@/lib/adminApi";
import type { AdminUserActivity, AdminUserDetails } from "@/lib/adminApi";
import { UserRole } from "@/lib/userRole";
import { AdminUserSpotsCarousel } from "@/components/AdminUserSpotsCarousel";

type LoadState =
  | { status: "loading" }
  | { status: "success"; user: AdminUserDetails }
  | { status: "notFound" }
  | { status: "error"; message: string };

const ACTIVITY_FIELDS: (keyof AdminUserActivity)[] = [
  "spotsApproved",
  "spotsPending",
  "spotsRejected",
  "comments",
  "checkIns",
  "ratings",
  "reportsFiled",
  "crystalBalance",
];

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col">
      <span className="text-xs text-dim">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}

function AdminUserDetailsContent({ id }: { id: string }) {
  const t = useTranslations("AdminUserDetails");
  const tUsers = useTranslations("AdminUsers");
  const format = useFormatter();
  const [state, setState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    const controller = new AbortController();

    getAdminUser(id, controller.signal)
      .then((user) => setState({ status: "success", user }))
      .catch((err) => {
        if (controller.signal.aborted) return;
        if (err instanceof ApiError && err.status === 404) {
          setState({ status: "notFound" });
        } else {
          setState({ status: "error", message: getErrorMessage(err, t("unknownError")) });
        }
      });

    return () => controller.abort();
  }, [id, t]);

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-4 p-8">
      <Link
        href="/admin/users"
        aria-label={t("backToUsers")}
        title={t("backToUsers")}
        className="self-start text-2xl leading-none hover:opacity-70 text-dim"
      >
        ←
      </Link>

      {state.status === "loading" ? (
        <p className="text-sm text-dim">{t("loading")}</p>
      ) : state.status === "notFound" ? (
        <p className="text-sm text-dim">{t("notFound")}</p>
      ) : state.status === "error" ? (
        <p className="text-sm text-red-700 dark:text-red-400">{state.message}</p>
      ) : (
        <>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h1 className="text-2xl font-semibold">{state.user.displayName}</h1>
            <span
              className={`rounded-full px-3 py-1 text-xs font-medium ${
                state.user.role === UserRole.Admin ? "bg-trail text-paper-light" : "bg-contour text-ink"
              }`}
            >
              {state.user.role === UserRole.Admin ? tUsers("roleAdmin") : tUsers("roleUser")}
            </span>
          </div>

          <section className="flex flex-col gap-3 rounded-2xl p-4 bg-card">
            <h2 className="text-sm font-semibold">{t("accountHeading")}</h2>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Field label={t("email")} value={state.user.email} />
              <Field
                label={t("emailVerified")}
                value={state.user.isEmailVerified ? t("yes") : t("no")}
              />
              <Field
                label={t("registeredAt")}
                value={format.dateTime(new Date(state.user.createdAt), { dateStyle: "medium" })}
              />
              <Field label={t("hasPassword")} value={state.user.hasPassword ? t("yes") : t("no")} />
              <Field
                label={t("linkedProviders")}
                value={state.user.linkedProviders.length > 0 ? state.user.linkedProviders.join(", ") : t("none")}
              />
            </div>
          </section>

          <section className="flex flex-col gap-3 rounded-2xl p-4 bg-card">
            <h2 className="text-sm font-semibold">{t("activityHeading")}</h2>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              {ACTIVITY_FIELDS.map((field) => (
                <Field key={field} label={t(`activity.${field}`)} value={String(state.user.activity[field])} />
              ))}
            </div>
          </section>

          <AdminUserSpotsCarousel userId={state.user.id} />
        </>
      )}
    </div>
  );
}

export default function AdminUserDetailsPage() {
  const params = useParams<{ id: string }>();
  return <AdminUserDetailsContent id={params.id} />;
}
