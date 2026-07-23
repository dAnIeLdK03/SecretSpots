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
import { Link } from "@/i18n/navigation";

const SOFIA_CENTER: MapViewState = { longitude: 23.3219, latitude: 42.6977, zoom: 12 };
const RADIUS_OPTIONS = [1, 5, 20, 50] as const;
const MOVE_THRESHOLD_DEGREES = 0.005;

interface LatLng {
  lat: number;
  lng: number;
}

export default function Home() {
  const t = useTranslations("Spots");
  const tAuth = useTranslations("Auth");
  const authStatus = useAuthStore((state) => state.status);

  const [viewState, setViewState] = useState<MapViewState>(SOFIA_CENTER);
  const [radiusKm, setRadiusKm] = useState<number>(5);
  const [spots, setSpots] = useState<NearbySpot[]>([]);
  const [selectedSpot, setSelectedSpot] = useState<NearbySpot | null>(null);
  const [lastSearchedCenter, setLastSearchedCenter] = useState<LatLng | null>(null);
  const [showSearchHere, setShowSearchHere] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [createModalCoords, setCreateModalCoords] = useState<LatLng | null>(null);
  const [showLoginPrompt, setShowLoginPrompt] = useState(false);
  const [locating, setLocating] = useState(false);

  const search = useCallback(
    async (center: LatLng, radius: number) => {
      setLoadError(null);
      try {
        const results = await getNearbySpots(center.lat, center.lng, radius);
        setSpots(results);
        setLastSearchedCenter(center);
        setShowSearchHere(false);
      } catch (err) {
        setLoadError(getErrorMessage(err, t("loadError")));
      }
    },
    [t],
  );

  useEffect(() => {
    if (typeof navigator === "undefined" || !navigator.geolocation) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- initial fetch on mount, no user event to attach to
      void search({ lat: SOFIA_CENTER.latitude, lng: SOFIA_CENTER.longitude }, radiusKm);
      return;
    }

    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const center = { lat: position.coords.latitude, lng: position.coords.longitude };
        setViewState({ longitude: center.lng, latitude: center.lat, zoom: 13 });
        setLocating(false);
        void search(center, radiusKm);
      },
      () => {
        setLocating(false);
        void search({ lat: SOFIA_CENTER.latitude, lng: SOFIA_CENTER.longitude }, radiusKm);
      },
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
    if (typeof navigator === "undefined" || !navigator.geolocation) {
      setLoadError(t("geolocationUnavailable"));
      return;
    }
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const center = { lat: position.coords.latitude, lng: position.coords.longitude };
        setViewState({ longitude: center.lng, latitude: center.lat, zoom: 13 });
        setLocating(false);
        void search(center, radiusKm);
      },
      () => {
        setLocating(false);
        setLoadError(t("geolocationDenied"));
      },
    );
  }

  function requireAuthThen(coords: LatLng) {
    if (authStatus !== "authenticated") {
      setShowLoginPrompt(true);
      return;
    }
    setShowLoginPrompt(false);
    setCreateModalCoords(coords);
  }

  function handleMapClick(lat: number, lng: number) {
    requireAuthThen({ lat, lng });
  }

  function handleAddAtMyLocation() {
    if (typeof navigator === "undefined" || !navigator.geolocation) {
      setLoadError(t("geolocationUnavailable"));
      return;
    }
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLocating(false);
        requireAuthThen({ lat: position.coords.latitude, lng: position.coords.longitude });
      },
      () => {
        setLocating(false);
        setLoadError(t("geolocationDenied"));
      },
    );
  }

  function handleSpotCreated(spot: SpotResponse) {
    setSpots((prev) => [{ ...spot, distanceKm: 0 }, ...prev]);
    setCreateModalCoords(null);
  }

  return (
    <div className="relative flex-1">
      <div className="absolute top-4 left-4 z-10 flex flex-col gap-2">
        <label className="flex items-center gap-2 rounded bg-white px-3 py-2 text-sm shadow dark:bg-zinc-900">
          <span>{t("radiusLabel")}</span>
          <select
            value={radiusKm}
            onChange={(e) => handleRadiusChange(Number(e.target.value))}
            className="rounded border border-zinc-300 px-2 py-1 dark:border-zinc-700 dark:bg-zinc-900"
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
          className="rounded bg-white px-3 py-2 text-left text-sm shadow disabled:opacity-50 dark:bg-zinc-900"
        >
          {locating ? t("locating") : t("useMyLocation")}
        </button>
      </div>

      {showSearchHere ? (
        <button
          onClick={() => void search({ lat: viewState.latitude, lng: viewState.longitude }, radiusKm)}
          className="absolute top-4 left-1/2 z-10 -translate-x-1/2 rounded bg-zinc-900 px-4 py-2 text-sm text-white shadow dark:bg-zinc-100 dark:text-zinc-900"
        >
          {t("searchThisArea")}
        </button>
      ) : null}

      <button
        onClick={handleAddAtMyLocation}
        disabled={locating}
        className="absolute right-6 bottom-6 z-10 rounded-full bg-zinc-900 px-4 py-3 text-sm text-white shadow-lg disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900"
      >
        {locating ? t("locating") : t("addAtMyLocation")}
      </button>

      {showLoginPrompt ? (
        <div className="absolute bottom-6 left-6 z-10 rounded bg-white px-4 py-3 text-sm shadow dark:bg-zinc-900">
          {t("loginRequiredToCreate")}{" "}
          <Link href="/login" className="underline">
            {tAuth("loginTitle")}
          </Link>
        </div>
      ) : null}

      {loadError ? (
        <div className="absolute top-20 left-4 z-10 rounded bg-red-50 px-3 py-2 text-sm text-red-700 shadow dark:bg-red-950 dark:text-red-300">
          {loadError}
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
