"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";

interface DirectionsLinksProps {
  latitude: number;
  longitude: number;
  className?: string;
  // The map popup's background is forced white regardless of theme (see globals.css), unlike the
  // rest of the app — this menu needs to match that instead of the usual fieldmap theme vars.
  forcedLight?: boolean;
}

// Both URLs are universal links: on a phone with the app installed they open it directly for
// turn-by-turn navigation; otherwise they fall back to the web version. No API key needed for
// either — these are plain deep-link formats, not calls to Maps/Waze APIs.
export function DirectionsLinks({ latitude, longitude, className, forcedLight = false }: DirectionsLinksProps) {
  const t = useTranslations("Spots");
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;

    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [open]);

  const googleMapsUrl = `https://www.google.com/maps/dir/?api=1&destination=${latitude},${longitude}`;
  const wazeUrl = `https://waze.com/ul?ll=${latitude},${longitude}&navigate=yes`;

  const menuStyle = forcedLight
    ? { borderColor: "#e4e4e7", backgroundColor: "#fff" }
    : { borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" };
  const itemClassName = forcedLight
    ? "block rounded px-3 py-1.5 text-sm whitespace-nowrap text-zinc-700 hover:bg-black/5"
    : "block rounded px-3 py-1.5 text-sm whitespace-nowrap hover:bg-black/5 dark:hover:bg-white/5";
  const itemStyle = forcedLight ? undefined : { color: "var(--fieldmap-ink)" };

  return (
    <div ref={containerRef} className={`relative inline-block ${className ?? ""}`}>
      <button
        type="button"
        onClick={() => setOpen((wasOpen) => !wasOpen)}
        aria-haspopup="true"
        aria-expanded={open}
        className="underline"
        style={{ color: "var(--fieldmap-trail)" }}
      >
        {t("directionsButton")}
      </button>

      {open ? (
        <div
          role="menu"
          className="absolute top-full left-0 z-20 mt-1 flex flex-col gap-0.5 rounded-md border p-1 shadow-lg"
          style={menuStyle}
        >
          <a
            href={googleMapsUrl}
            target="_blank"
            rel="noopener noreferrer"
            role="menuitem"
            onClick={() => setOpen(false)}
            className={itemClassName}
            style={itemStyle}
          >
            {t("directionsGoogleMaps")}
          </a>
          <a
            href={wazeUrl}
            target="_blank"
            rel="noopener noreferrer"
            role="menuitem"
            onClick={() => setOpen(false)}
            className={itemClassName}
            style={itemStyle}
          >
            {t("directionsWaze")}
          </a>
        </div>
      ) : null}
    </div>
  );
}
