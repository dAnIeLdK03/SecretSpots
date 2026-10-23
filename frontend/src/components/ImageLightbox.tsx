"use client";

import { useTranslations } from "next-intl";
import { X } from "lucide-react";
import { useModalDismiss } from "@/hooks/useModalDismiss";

interface ImageLightboxProps {
  src: string;
  alt: string;
  onClose: () => void;
}

export function ImageLightbox({ src, alt, onClose }: ImageLightboxProps) {
  const t = useTranslations("AdminSpots");
  const { containerRef, handleOverlayClick } = useModalDismiss(onClose);

  return (
    <div
      ref={containerRef}
      role="dialog"
      aria-modal="true"
      aria-label={alt}
      tabIndex={-1}
      onClick={handleOverlayClick}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4 outline-none"
    >
      <button
        type="button"
        onClick={onClose}
        aria-label={t("closeLabel")}
        className="absolute top-4 right-4 rounded-full p-2 text-white/80 hover:bg-white/10 hover:text-white"
      >
        <X size={20} />
      </button>
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={src} alt={alt} className="max-h-[90vh] max-w-full rounded-lg object-contain shadow-2xl" />
    </div>
  );
}
