"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { SPOT_CATEGORIES, updateSpot } from "@/lib/spotsApi";
import type { SpotCategory, SpotResponse } from "@/lib/spotsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { PhotoUpload } from "@/components/PhotoUpload";

interface EditSpotModalProps {
    spot: SpotResponse;
    onClose: () => void;
    onUpdate: (spot: SpotResponse) => void;
}

export function EditSpotModal({ spot, onClose, onUpdate }: EditSpotModalProps) {
    const t = useTranslations("Spots");
    const [name, setName] = useState(spot.name);
    const [description, setDescription] = useState(spot.description);
    const [category, setCategory] = useState<SpotCategory>(spot.category);
    const [photoUrl, setPhotoUrl] = useState(spot.photoUrl);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSubmitting(true);
        try {
            const updated = await updateSpot(spot.id, { name, description, category, photoUrl });
            onUpdate(updated);
        } catch (err) {
            setError(getErrorMessage(err, t("unknownError")));
            setSubmitting(false);
        }
    }

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <div className="w-full max-w-sm rounded-lg bg-white p-6 dark:bg-zinc-900">
                <h2 className="mb-4 text-lg font-semibold">{t("editTitle")}</h2>
                <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                    <label className="flex flex-col gap-1">
                        <span className="text-sm text-zinc-600 dark:text-zinc-400">{t("nameLabel")}</span>
                        <input
                            type="text"
                            required
                            maxLength={100}
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            className="rounded border border-zinc-300 px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900"
                        />
                    </label>
                    <label className="flex flex-col gap-1">
                        <span className="text-sm text-zinc-600 dark:text-zinc-400">{t("descriptionLabel")}</span>
                        <textarea
                            required
                            maxLength={2000}
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            rows={3}
                            className="rounded border border-zinc-300 px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900"
                        />
                    </label>
                    <label className="flex flex-col gap-1">
                        <span className="text-sm text-zinc-600 dark:text-zinc-400">{t("categoryLabel")}</span>
                        <select
                            value={category}
                            onChange={(e) => setCategory(e.target.value as SpotCategory)}
                            className="rounded border border-zinc-300 px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900"
                        >
                            {SPOT_CATEGORIES.map((c) => (
                                <option key={c} value={c}>
                                    {t(`category.${c}`)}
                                </option>
                            ))}
                        </select>
                    </label>
                    <PhotoUpload label={t("photoUrlLabel")} value={photoUrl} onChange={setPhotoUrl} />
                    {error ? <p className="text-sm text-red-600 dark:text-red-400">{error}</p> : null}
                    <div className="flex justify-end gap-2">
                        <button type="button" onClick={onClose} className="rounded px-4 py-2 text-zinc-600 dark:text-zinc-400">
                            {t("cancelButton")}
                        </button>
                        <button
                            type="submit"
                            disabled={submitting}
                            className="rounded bg-zinc-900 px-4 py-2 text-white disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900"
                        >
                            {submitting ? t("saving") : t("saveButton")}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
