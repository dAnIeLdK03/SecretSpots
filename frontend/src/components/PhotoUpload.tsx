"use client";

import { useState } from "react";
import type { ChangeEvent } from "react";
import { useTranslations } from "next-intl";
import { uploadPhoto } from "@/lib/photosApi";
import { getErrorMessage } from "@/lib/apiClient";

interface PhotoUploadProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
}

export function PhotoUpload({ label, value, onChange, required = true }: PhotoUploadProps) {
  const t = useTranslations("PhotoUpload");
  const [preview, setPreview] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    const objectUrl = URL.createObjectURL(file);
    setPreview((prev) => {
      if (prev) URL.revokeObjectURL(prev);
      return objectUrl;
    });
    setError(null);
    setUploading(true);

    try {
      const response = await uploadPhoto(file);
      onChange(response.url);
    } catch (err) {
      setError(getErrorMessage(err, t("uploadError")));
    } finally {
      setUploading(false);
    }
  }

  const previewSrc = preview ?? (value || null);

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
        {label}
      </span>
      {previewSrc ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={previewSrc} alt="" className="h-32 w-full rounded object-cover" />
      ) : null}
      <input
        type="file"
        accept="image/*"
        required={required && !value}
        onChange={(e) => void handleFileChange(e)}
        className="rounded border px-3 py-2 text-sm"
        style={{ borderColor: "var(--fieldmap-contour)" }}
      />
      {uploading ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("uploading")}
        </p>
      ) : null}
      {error ? <p className="text-sm text-red-700 dark:text-red-400">{error}</p> : null}
    </div>
  );
}
