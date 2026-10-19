import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { NextIntlClientProvider } from "next-intl";
import { Geist, Geist_Mono } from "next/font/google";
import Script from "next/script";
import { routing } from "@/i18n/routing";
import { AuthProvider } from "@/components/AuthProvider";
import { Header } from "@/components/Header";
import "../globals.css";

// Both unset in local dev by default — the script is skipped entirely rather than pointed
// at a dangling local Umami instance, so running without analytics configured never breaks
// the page. Set NEXT_PUBLIC_UMAMI_SRC/NEXT_PUBLIC_UMAMI_WEBSITE_ID (e.g. in .env.local) once
// a "website" is created in the Umami dashboard (http://localhost:3001) to enable tracking.
const umamiSrc = process.env.NEXT_PUBLIC_UMAMI_SRC;
const umamiWebsiteId = process.env.NEXT_PUBLIC_UMAMI_WEBSITE_ID;

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
      <body className="h-full flex flex-col">
        <NextIntlClientProvider>
          <AuthProvider>
            <Header />
            <main className="flex flex-1 flex-col">{children}</main>
          </AuthProvider>
        </NextIntlClientProvider>
        {umamiSrc && umamiWebsiteId && (
          <Script
            src={umamiSrc}
            data-website-id={umamiWebsiteId}
            strategy="afterInteractive"
          />
        )}
      </body>
    </html>
  );
}
