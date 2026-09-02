"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { SpotsMap } from "@/components/SpotsMap";
import type { MapViewState } from "@/components/SpotsMap";
import { CreateSpotModal } from "@/components/CreateSpotModal";
import { getNearbySpots } from "@/lib/spotsApi";
import type { NearbySpot, SpotResponse } from "@/lib/spotsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";
import { useGeolocationStore } from "@/store/useGeolocationStore";
import { Link } from "@/i18n/navigation";

const SOFIA_CENTER: MapViewState = { longitude: 23.3219, latitude: 42.6977, zoom: 12 };
const RADIUS_OPTIONS = [1, 5, 20, 50] as const;
const MOVE_THRESHOLD_DEGREES = 0.005;

interface LatLng {
  lat: number;
  lng: number;
}

export default function MapPage() {
  const t = useTranslations("Spots");
  const tAuth = useTranslations("Auth");
  const authStatus = useAuthStore((state) => state.status);
  const geoStatus = useGeolocationStore((state) => state.status);
  const geoCoords = useGeolocationStore((state) => state.coords);
  const geoErrorReason = useGeolocationStore((state) => state.errorReason);

  const [viewState, setViewState] = useState<MapViewState>(SOFIA_CENTER);
  const [radiusKm, setRadiusKm] = useState<number>(5);
  const [spots, setSpots] = useState<NearbySpot[]>([]);
  const [totalNearbyCount, setTotalNearbyCount] = useState(0);
  const [selectedSpot, setSelectedSpot] = useState<NearbySpot | null>(null);
  const [lastSearchedCenter, setLastSearchedCenter] = useState<LatLng | null>(null);
  const [showSearchHere, setShowSearchHere] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [createModalCoords, setCreateModalCoords] = useState<LatLng | null>(null);
  const [showLoginPrompt, setShowLoginPrompt] = useState(false);
  const [placingSpot, setPlacingSpot] = useState(false);

  const locating = geoStatus === "locating";

  const search = useCallback(
    async (center: LatLng, radius: number) => {
      setLoadError(null);
      try {
        const results = await getNearbySpots(center.lat, center.lng, radius);
        setSpots(results.items);
        setTotalNearbyCount(results.totalCount);
        setLastSearchedCenter(center);
        setShowSearchHere(false);
      } catch (err) {
        setLoadError(getErrorMessage(err, t("loadError")));
      }
    },
    [t],
  );

  useEffect(() => {
    // Normally already resolved by now — the request was kicked off as soon as
    // the app mounted (see AuthProvider), not when this page did. This just
    // reacts to whatever state that request is in, and requests it defensively
    // if for some reason it never started. requestLocation/refreshLocation on
    // the store guarantee only one browser geolocation request is ever in
    // flight at a time, however many places (this effect, the button below)
    // ask for it.
    if (geoStatus === "idle") {
      useGeolocationStore.getState().requestLocation();
      return;
    }

    if (geoStatus === "locating") {
      return;
    }

    if (geoStatus === "success" && geoCoords) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- reflecting store status, no user event to attach to
      setViewState({ longitude: geoCoords.lng, latitude: geoCoords.lat, zoom: 13 });
      void search(geoCoords, radiusKm);
    } else {
      // search() clears loadError as soon as it starts, so set the geolocation
      // error only after kicking it off — otherwise it would be wiped out
      // immediately by search()'s own setLoadError(null).
      void search({ lat: SOFIA_CENTER.latitude, lng: SOFIA_CENTER.longitude }, radiusKm);
      if (geoStatus === "error") {
        setLoadError(geoErrorReason === "timeout" ? t("geolocationTimeout") : t("geolocationDenied"));
      } else if (geoStatus === "unsupported") {
        setLoadError(t("geolocationUnavailable"));
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [geoStatus]);

  function handleRadiusChange(newRadius: number) {
    setRadiusKm(newRadius);
    void search({ lat: viewState.latitude, lng: viewState.longitude }, newRadius);
  }

  function handleMoveEnd() {
    if (!lastSearchedCenter) return;
    const movedEnough =
      Math.abs(viewState.latitude - lastSearchedCenter.lat) > MOVE_THRESHOLD_DEGREES ||
      Math.abs(viewState.longitude - lastSearchedCenter.lng) > MOVE_THRESHOLD_DEGREES;
    setShowSearchHere(movedEnough);
  }

  function handleUseMyLocation() {
    setLoadError(null);
    useGeolocationStore.getState().refreshLocation();
  }

  function handleMapClick(lat: number, lng: number) {
    if (!placingSpot) return;
    setPlacingSpot(false);
    setCreateModalCoords({ lat, lng });
  }

  function handleToggleAddSpot() {
    if (authStatus !== "authenticated") {
      setShowLoginPrompt(true);
      return;
    }
    setShowLoginPrompt(false);
    setPlacingSpot((wasPlacing) => !wasPlacing);
  }

  function handleSpotCreated(spot: SpotResponse) {
    const { photoUrls, ...rest } = spot;
    setSpots((prev) => [{ ...rest, photoUrl: photoUrls[0], distanceKm: 0 }, ...prev]);
    setTotalNearbyCount((prev) => prev + 1);
    setCreateModalCoords(null);
  }

  return (
    <div className="relative flex-1">
      <div className="absolute top-4 left-4 z-10 flex flex-col gap-2">
        <label
          className="flex items-center gap-2 rounded px-3 py-2 text-sm shadow"
          style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
        >
          <span>{t("radiusLabel")}</span>
          <select
            value={radiusKm}
            onChange={(e) => handleRadiusChange(Number(e.target.value))}
            className="rounded border px-2 py-1"
            style={{ borderColor: "var(--fieldmap-contour)", backgroundColor: "var(--fieldmap-paper-light)" }}
          >
            {RADIUS_OPTIONS.map((r) => (
              <option key={r} value={r}>
                {r} km
              </option>
            ))}
          </select>
        </label>
        <button
          onClick={handleUseMyLocation}
          disabled={locating}
          className="rounded px-3 py-2 text-left text-sm shadow disabled:opacity-50"
          style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
        >
          {locating ? t("locating") : t("useMyLocation")}
        </button>

        {loadError ? (
          <div className="rounded bg-red-50 dark:bg-red-950 px-3 py-2 text-sm text-red-700 dark:text-red-400 shadow">{loadError}</div>
        ) : null}

        {!loadError && spots.length < totalNearbyCount ? (
          <div
            className="rounded px-3 py-2 text-sm shadow"
            style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-dim)" }}
          >
            {t("moreSpotsNearby", { shown: spots.length, total: totalNearbyCount })}
          </div>
        ) : null}
      </div>

      {showSearchHere ? (
        <button
          onClick={() => void search({ lat: viewState.latitude, lng: viewState.longitude }, radiusKm)}
          className="absolute top-4 left-1/2 z-10 -translate-x-1/2 rounded px-4 py-2 text-sm shadow"
          style={{ backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }}
        >
          {t("searchThisArea")}
        </button>
      ) : null}

      <button
        onClick={handleToggleAddSpot}
        aria-pressed={placingSpot}
        className="absolute right-6 bottom-6 z-10 rounded-full px-4 py-3 text-sm shadow-lg"
        style={
          placingSpot
            ? { backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)", boxShadow: "0 0 0 4px rgba(181,74,36,0.25)" }
            : { backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }
        }
      >
        {placingSpot ? t("tapMapToPlace") : t("addAtMyLocation")}
      </button>

      {showLoginPrompt ? (
        <div
          className="absolute bottom-6 left-6 z-10 rounded px-4 py-3 text-sm shadow"
          style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
        >
          {t("loginRequiredToCreate")}{" "}
          <Link href="/login" className="underline">
            {tAuth("loginTitle")}
          </Link>
        </div>
      ) : null}

      <SpotsMap
        viewState={viewState}
        onViewStateChange={setViewState}
        onMoveEnd={handleMoveEnd}
        spots={spots}
        onMapClick={handleMapClick}
        selectedSpot={selectedSpot}
        onSelectSpot={setSelectedSpot}
      />

      {createModalCoords ? (
        <CreateSpotModal
          latitude={createModalCoords.lat}
          longitude={createModalCoords.lng}
          onClose={() => setCreateModalCoords(null)}
          onCreated={handleSpotCreated}
        />
      ) : null}
    </div>
  );
}
