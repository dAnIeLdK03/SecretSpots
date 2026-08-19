"use client";

import { useState } from "react";
import type { FormEvent } from "react";
import { useTranslations } from "next-intl";
import { updateSpot } from "@/lib/spotsApi";
import type { SpotCategory, SpotResponse } from "@/lib/spotsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { MultiPhotoUpload } from "@/components/MultiPhotoUpload";
import { CategorySelect } from "@/components/CategorySelect";

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
    const [photoUrls, setPhotoUrls] = useState<string[]>(spot.photoUrls);
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSubmitting(true);
        try {
            const updated = await updateSpot(spot.id, { name, description, category, photoUrls });
            onUpdate(updated);
        } catch (err) {
            setError(getErrorMessage(err, t("unknownError")));
            setSubmitting(false);
        }
    }

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
            <div
                className="w-full max-w-sm rounded-lg p-6"
                style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
            >
                <h2 className="mb-4 text-lg font-semibold">{t("editTitle")}</h2>
                <form onSubmit={handleSubmit} className="flex flex-col gap-4">
                    <label className="flex flex-col gap-1">
                        <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>{t("nameLabel")}</span>
                        <input
                            type="text"
                            required
                            maxLength={100}
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            className="rounded border px-3 py-2"
                            style={{ borderColor: "var(--fieldmap-contour)" }}
                        />
                    </label>
                    <label className="flex flex-col gap-1">
                        <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>{t("descriptionLabel")}</span>
                        <textarea
                            required
                            maxLength={2000}
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            rows={3}
                            className="rounded border px-3 py-2"
                            style={{ borderColor: "var(--fieldmap-contour)" }}
                        />
                    </label>
                    <label className="flex flex-col gap-1">
                        <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>{t("categoryLabel")}</span>
                        <CategorySelect value={category} onChange={setCategory} />
                    </label>
                    <MultiPhotoUpload label={t("photoUrlLabel")} photoUrls={photoUrls} onChange={setPhotoUrls} />
                    {error ? <p className="text-sm text-red-700">{error}</p> : null}
                    <div className="flex justify-end gap-2">
                        <button type="button" onClick={onClose} className="rounded px-4 py-2" style={{ color: "var(--fieldmap-dim)" }}>
                            {t("cancelButton")}
                        </button>
                        <button
                            type="submit"
                            disabled={submitting || photoUrls.length === 0}
                            className="rounded px-4 py-2 disabled:opacity-50"
                            style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
                        >
                            {submitting ? t("saving") : t("saveButton")}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
