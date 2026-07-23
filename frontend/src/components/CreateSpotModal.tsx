"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { createSpot, SPOT_CATEGORIES } from "@/lib/spotsApi";
import type { SpotCategory, SpotResponse } from "@/lib/spotsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { MultiPhotoUpload } from "@/components/MultiPhotoUpload";

interface CreateSpotModalProps {
  latitude: number;
  longitude: number;
  onClose: () => void;
  onCreated: (spot: SpotResponse) => void;
}

const FIELD_CLASSES =
  "rounded-lg border border-white/15 bg-white/5 px-3 py-2 text-white placeholder:text-zinc-500 focus:border-emerald-400 focus:outline-none";

export function CreateSpotModal({ latitude, longitude, onClose, onCreated }: CreateSpotModalProps) {
  const t = useTranslations("Spots");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [category, setCategory] = useState<SpotCategory>(SPOT_CATEGORIES[0]);
  const [photoUrls, setPhotoUrls] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const spot = await createSpot({ name, description, category, photoUrls, latitude, longitude });
      onCreated(spot);
    } catch (err) {
      setError(getErrorMessage(err, t("unknownError")));
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="relative max-h-[90vh] w-full max-w-sm overflow-y-auto rounded-2xl border border-white/10 bg-zinc-900 p-6 text-white">
        <button
          type="button"
          onClick={onClose}
          aria-label={t("cancelButton")}
          className="absolute top-4 right-4 text-zinc-400 hover:text-white"
        >
          ✕
        </button>
        <h2 className="mb-5 text-xl font-bold">{t("createTitle")}</h2>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1">
            <span className="text-sm text-zinc-300">{t("nameLabel")}</span>
            <input
              type="text"
              required
              maxLength={100}
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={FIELD_CLASSES}
            />
          </label>
          <label className="flex flex-col gap-1">
            <span className="text-sm text-zinc-300">{t("descriptionLabel")}</span>
            <textarea
              required
              maxLength={2000}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              className={FIELD_CLASSES}
            />
          </label>
          <div className="flex flex-col gap-1">
            <span className="text-sm text-zinc-300">{t("locationLabel")}</span>
            <p className="rounded-lg border border-white/15 bg-white/5 px-3 py-2 text-sm text-zinc-400">
              📍 {latitude.toFixed(5)}, {longitude.toFixed(5)}
            </p>
          </div>
          <label className="flex flex-col gap-1">
            <span className="text-sm text-zinc-300">{t("categoryLabel")}</span>
            <select
              value={category}
              onChange={(e) => setCategory(e.target.value as SpotCategory)}
              className={FIELD_CLASSES}
            >
              {SPOT_CATEGORIES.map((c) => (
                <option key={c} value={c} className="text-zinc-900">
                  {t(`category.${c}`)}
                </option>
              ))}
            </select>
          </label>
          <MultiPhotoUpload label={t("photoUrlLabel")} photoUrls={photoUrls} onChange={setPhotoUrls} dark />
          {error ? <p className="text-sm text-red-400">{error}</p> : null}
          <div className="mt-2 flex justify-end gap-2">
            <button type="button" onClick={onClose} className="rounded-full px-4 py-2 text-sm text-zinc-300 hover:text-white">
              {t("cancelButton")}
            </button>
            <button
              type="submit"
              disabled={submitting || photoUrls.length === 0}
              className="rounded-full bg-emerald-500 px-5 py-2 text-sm font-medium text-white hover:bg-emerald-400 disabled:opacity-50"
            >
              {submitting ? t("creating") : t("createButton")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
