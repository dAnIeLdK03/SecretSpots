"use client";

import { useTranslations } from "next-intl";
import { useRequireAuth } from "@/hooks/useRequireAuth";
import { useAuthStore } from "@/store/useAuthStore";

export default function AccountPage() {
  const t = useTranslations("Auth");
  const isAuthenticated = useRequireAuth();
  const user = useAuthStore((state) => state.user);

  if (!isAuthenticated || !user) {
    return null;
  }

  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t("accountTitle")}</h1>
      <dl className="grid w-full max-w-sm grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
        <dt className="text-zinc-600 dark:text-zinc-400">{t("displayNameLabel")}</dt>
        <dd>{user.displayName}</dd>
        <dt className="text-zinc-600 dark:text-zinc-400">{t("emailLabel")}</dt>
        <dd>{user.email}</dd>
        <dt className="text-zinc-600 dark:text-zinc-400">{t("crystalBalanceLabel")}</dt>
        <dd>{user.crystalBalance}</dd>
      </dl>
    </div>
  );
}
