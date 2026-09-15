"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { getNearbyBusinesses } from "@/lib/businessesApi";
import type { NearbyBusiness } from "@/lib/businessesApi";
import { getErrorMessage } from "@/lib/apiClient";
import { useGeolocationStore } from "@/store/useGeolocationStore";
import { Link } from "@/i18n/navigation";

const SOFIA_CENTER = { lat: 42.6977, lng: 23.3219 };
const RADIUS_KM = 20;

function formatDistance(distanceKm: number, t: ReturnType<typeof useTranslations>): string {
  if (distanceKm < 1) {
    return t("distanceMeters", { value: Math.round(distanceKm * 1000) });
  }
  return t("distanceKm", { value: distanceKm.toFixed(1) });
}

export default function BusinessesPage() {
  const t = useTranslations("Businesses");
  const geoStatus = useGeolocationStore((state) => state.status);
  const geoCoords = useGeolocationStore((state) => state.coords);

  const [businesses, setBusinesses] = useState<NearbyBusiness[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (geoStatus === "idle") {
      useGeolocationStore.getState().requestLocation();
      return;
    }

    if (geoStatus === "locating") {
      return;
    }

    const center = geoStatus === "success" && geoCoords ? geoCoords : SOFIA_CENTER;
    const controller = new AbortController();

    // eslint-disable-next-line react-hooks/set-state-in-effect -- reacting to geoStatus settling, no user event to attach to
    setLoading(true);
    getNearbyBusinesses(center.lat, center.lng, RADIUS_KM, controller.signal)
      .then((result) => {
        setBusinesses(result.items);
        setLoadError(null);
      })
      .catch((err) => {
        if (controller.signal.aborted) return;
        setLoadError(getErrorMessage(err, t("loadError")));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [geoStatus, geoCoords, t]);

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-4 p-6">
      <div>
        <h1 className="text-2xl font-semibold">{t("title")}</h1>
        <p className="mt-1 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("subtitle")}
        </p>
      </div>

      {loading ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("loading")}
        </p>
      ) : loadError ? (
        <p className="text-sm text-red-700 dark:text-red-400">{loadError}</p>
      ) : businesses.length === 0 ? (
        <p className="text-sm" style={{ color: "var(--fieldmap-dim)" }}>
          {t("noBusinessesNearby")}
        </p>
      ) : (
        <ul className="flex flex-col gap-3">
          {businesses.map((business) => (
            <li key={business.id} className="rounded-md border p-4" style={{ borderColor: "var(--fieldmap-contour)" }}>
              <Link href={`/businesses/${business.id}`} className="font-medium underline">
                {business.name}
              </Link>
              <p className="mt-1 text-sm" style={{ color: "var(--fieldmap-dim)" }}>
                {business.description}
              </p>
              <p className="mt-2 text-xs" style={{ color: "var(--fieldmap-dim)" }}>
                {formatDistance(business.distanceKm, t)}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
