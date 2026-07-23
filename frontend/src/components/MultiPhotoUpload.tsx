"use client";

import { useState } from "react";
import type { ChangeEvent } from "react";
import { useTranslations } from "next-intl";
import { uploadPhoto } from "@/lib/photosApi";

interface MultiPhotoUploadProps {
  label: string;
  photoUrls: string[];
  onChange: (photoUrls: string[]) => void;
  maxCount?: number;
  // Forces the dark-on-dark styling used inside CreateSpotModal's always-dark theme, instead of
  // the default light/dark-mode-media-query classes (which assume a light surface by default —
  // wrong when the surrounding modal is unconditionally dark regardless of OS theme).
  dark?: boolean;
}

export function MultiPhotoUpload({ label, photoUrls, onChange, maxCount = 5, dark = false }: MultiPhotoUploadProps) {
  const t = useTranslations("PhotoUpload");
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleFilesChange(event: ChangeEvent<HTMLInputElement>) {
    const selected = Array.from(event.target.files ?? []);
    event.target.value = "";
    if (selected.length === 0) return;

    // Silently cap at the remaining slots — the input becomes hidden at maxCount anyway,
    // this only matters if the user selects more files than are left in one go.
    const filesToUpload = selected.slice(0, maxCount - photoUrls.length);

    setError(null);
    setUploading(true);
    const uploadedUrls: string[] = [];
    let uploadFailed = false;
    for (const file of filesToUpload) {
      try {
        const response = await uploadPhoto(file);
        uploadedUrls.push(response.url);
      } catch {
        uploadFailed = true;
      }
    }
    if (uploadedUrls.length > 0) {
      onChange([...photoUrls, ...uploadedUrls]);
    }
    if (uploadFailed) {
      setError(t("uploadError"));
    }
    setUploading(false);
  }

  function handleRemove(index: number) {
    onChange(photoUrls.filter((_, i) => i !== index));
  }

  function handleMove(index: number, direction: -1 | 1) {
    const target = index + direction;
    if (target < 0 || target >= photoUrls.length) return;
    const next = [...photoUrls];
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next);
  }

  const canAddMore = photoUrls.length < maxCount;

  const labelClass = dark ? "text-zinc-300" : "text-zinc-600 dark:text-zinc-400";
  const moveButtonClass = dark ? "text-zinc-300" : "text-zinc-600 dark:text-zinc-400";
  const removeButtonClass = dark ? "text-red-400" : "text-red-600 dark:text-red-400";
  const fileInputClass = dark
    ? "rounded-lg border border-white/15 bg-white/5 px-3 py-2 text-sm text-white file:mr-2 file:rounded-full file:border-0 file:bg-white/10 file:px-3 file:py-1 file:text-white"
    : "rounded border border-zinc-300 px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900";
  const mutedTextClass = dark ? "text-zinc-400" : "text-zinc-500";

  return (
    <div className="flex flex-col gap-2">
      <span className={`text-sm ${labelClass}`}>{label}</span>

      {photoUrls.length > 0 ? (
        <ul className="grid grid-cols-3 gap-2">
          {photoUrls.map((url, index) => (
            <li key={`${index}-${url}`} className="flex flex-col gap-1">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={url} alt="" className="h-20 w-full rounded object-cover" />
              <div className="flex items-center justify-between text-xs">
                <div className="flex gap-1">
                  <button
                    type="button"
                    onClick={() => handleMove(index, -1)}
                    disabled={index === 0}
                    aria-label={t("moveLeft")}
                    className={`${moveButtonClass} disabled:opacity-30`}
                  >
                    ◀
                  </button>
                  <button
                    type="button"
                    onClick={() => handleMove(index, 1)}
                    disabled={index === photoUrls.length - 1}
                    aria-label={t("moveRight")}
                    className={`${moveButtonClass} disabled:opacity-30`}
                  >
                    ▶
                  </button>
                </div>
                <button type="button" onClick={() => handleRemove(index)} className={removeButtonClass}>
                  {t("remove")}
                </button>
              </div>
            </li>
          ))}
        </ul>
      ) : null}

      {canAddMore ? (
        <input type="file" accept="image/*" multiple onChange={(e) => void handleFilesChange(e)} className={fileInputClass} />
      ) : (
        <p className={`text-xs ${mutedTextClass}`}>{t("maxPhotosReached")}</p>
      )}

      {uploading ? <p className={`text-sm ${labelClass}`}>{t("uploading")}</p> : null}
      {error ? <p className={`text-sm ${removeButtonClass}`}>{error}</p> : null}
    </div>
  );
}
