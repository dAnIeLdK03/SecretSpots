"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";

interface PhotoSliderProps {
  photos: string[];
  alt: string;
}

export function PhotoSlider({ photos, alt }: PhotoSliderProps) {
  const t = useTranslations("Spots");
  const [index, setIndex] = useState(0);

  if (photos.length === 0) return null;

  function goTo(newIndex: number) {
    setIndex((newIndex + photos.length) % photos.length);
  }

  return (
    <div className="relative">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={photos[index]} alt={alt} className="h-64 w-full rounded-lg object-cover" />

      {photos.length > 1 ? (
        <>
          <button
            type="button"
            onClick={() => goTo(index - 1)}
            aria-label={t("previousPhoto")}
            className="absolute top-1/2 left-2 -translate-y-1/2 rounded-full bg-black/50 px-3 py-2 text-white"
          >
            ‹
          </button>
          <button
            type="button"
            onClick={() => goTo(index + 1)}
            aria-label={t("nextPhoto")}
            className="absolute top-1/2 right-2 -translate-y-1/2 rounded-full bg-black/50 px-3 py-2 text-white"
          >
            ›
          </button>
          <div className="absolute bottom-2 left-1/2 flex -translate-x-1/2 gap-1.5">
            {photos.map((photo, i) => (
              <button
                key={`${i}-${photo}`}
                type="button"
                onClick={() => goTo(i)}
                aria-label={`${t("goToPhoto")} ${i + 1}`}
                className={`h-2 w-2 rounded-full ${i === index ? "bg-white" : "bg-white/50"}`}
              />
            ))}
          </div>
        </>
      ) : null}
    </div>
  );
}
