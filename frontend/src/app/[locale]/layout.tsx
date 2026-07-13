import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { NextIntlClientProvider, useTranslations } from "next-intl";
import { Geist, Geist_Mono } from "next/font/google";
import { routing } from "@/i18n/routing";
import "../globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "SecretSpots",
  description: "Локален гид за скрити съкровища",
};

function Header() {
  const t = useTranslations("Layout");
  return (
    <header className="border-b border-zinc-200 px-6 py-4 dark:border-zinc-800">
      <span className="text-lg font-semibold">{t("appName")}</span>
    </header>
  );
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;

  if (!routing.locales.includes(locale as (typeof routing.locales)[number])) {
    notFound();
  }

  return (
    <html
      lang={locale}
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">
        <NextIntlClientProvider>
          <Header />
          <main className="flex flex-1 flex-col">{children}</main>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
