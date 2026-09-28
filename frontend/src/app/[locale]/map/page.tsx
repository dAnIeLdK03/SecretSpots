"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import dynamic from "next/dynamic";
import { useSearchParams } from "next/navigation";
import type { MapViewState } from "@/components/SpotsMap";
import { CreateSpotModal } from "@/components/CreateSpotModal";
import { CreateTrailModal } from "@/components/CreateTrailModal";
import { MapSearchBox } from "@/components/MapSearchBox";
import { getNearbySpots } from "@/lib/spotsApi";
import type { NearbySpot, SpotResponse, SpotSearchResult } from "@/lib/spotsApi";
import { getNearbyTrails, deleteTrail as deleteTrailRequest } from "@/lib/trailsApi";
import type { NearbyTrail, TrailPoint, TrailResponse } from "@/lib/trailsApi";
import { getErrorMessage } from "@/lib/apiClient";
import { haversineMeters } from "@/lib/haversine";
import { useAuthStore } from "@/store/useAuthStore";
import { useGeolocationStore } from "@/store/useGeolocationStore";
import { Link } from "@/i18n/navigation";

const SOFIA_CENTER: MapViewState = { longitude: 23.3219, latitude: 42.6977, zoom: 12 };
const RADIUS_OPTIONS = [1, 5, 20, 50] as const;
const MOVE_THRESHOLD_DEGREES = 0.005;

// MapLibre is a large dependency and touches window/canvas, so it's excluded from SSR and kept
// out of every route's initial bundle except this one, which is the only page that needs it
// mounted immediately (see also HeroMap's own dynamic import on the landing page).
const SpotsMap = dynamic(() => import("@/components/SpotsMap").then((m) => m.SpotsMap), { ssr: false });

interface LatLng {
  lat: number;
  lng: number;
}

const TARGET_ZOOM = 15;

// Set by the spot detail page's "View on map" link. Returns null for missing or out-of-range
// values so a hand-edited URL falls back to the normal geolocation flow instead of a broken map.
function parseTarget(params: URLSearchParams): { lat: number; lng: number; spotId: string | null } | null {
  const lat = Number(params.get("lat"));
  const lng = Number(params.get("lng"));
  if (!params.has("lat") || !params.has("lng")) return null;
  if (!Number.isFinite(lat) || !Number.isFinite(lng) || Math.abs(lat) > 90 || Math.abs(lng) > 180) return null;
  return { lat, lng, spotId: params.get("spot") };
}

function MapPageContent() {
  const t = useTranslations("Spots");
  const tTrails = useTranslations("Trails");
  const searchParams = useSearchParams();
  const [target] = useState(() => parseTarget(searchParams));
  const [highlight, setHighlight] = useState(target ? { lat: target.lat, lng: target.lng } : null);
  const ignoreGeoRef = useRef(target !== null);
  const targetAppliedRef = useRef(false);
  const pendingSelectIdRef = useRef<string | null>(target?.spotId ?? null);
  const tAuth = useTranslations("Auth");
  const authStatus = useAuthStore((state) => state.status);
  const currentUserId = useAuthStore((state) => state.user?.id ?? null);
  const geoStatus = useGeolocationStore((state) => state.status);
  const geoCoords = useGeolocationStore((state) => state.coords);
  const geoErrorReason = useGeolocationStore((state) => state.errorReason);

  const [viewState, setViewState] = useState<MapViewState>(
    target ? { longitude: target.lng, latitude: target.lat, zoom: TARGET_ZOOM } : SOFIA_CENTER,
  );
  const [radiusKm, setRadiusKm] = useState<number>(5);
  const [spots, setSpots] = useState<NearbySpot[]>([]);
  const [totalNearbyCount, setTotalNearbyCount] = useState(0);
  const [selectedSpot, setSelectedSpot] = useState<NearbySpot | null>(null);
  const [lastSearchedCenter, setLastSearchedCenter] = useState<LatLng | null>(null);
  const [showSearchHere, setShowSearchHere] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [createModalCoords, setCreateModalCoords] = useState<LatLng | null>(null);
  const [loginPromptFor, setLoginPromptFor] = useState<"spot" | "trail" | null>(null);
  const [placingSpot, setPlacingSpot] = useState(false);

  const [trails, setTrails] = useState<NearbyTrail[]>([]);
  const [selectedTrail, setSelectedTrail] = useState<NearbyTrail | null>(null);
  const [drawingTrail, setDrawingTrail] = useState(false);
  const [trailPoints, setTrailPoints] = useState<TrailPoint[]>([]);
  const [showCreateTrailModal, setShowCreateTrailModal] = useState(false);
  const [trailError, setTrailError] = useState<string | null>(null);

  const locating = geoStatus === "locating";

  const searchControllerRef = useRef<AbortController | null>(null);

  const search = useCallback(
    async (center: LatLng, radius: number) => {
      // Only the latest search may write state — otherwise a slow earlier response
      // (e.g. an old radius) could land after a newer one and overwrite it.
      searchControllerRef.current?.abort();
      const controller = new AbortController();
      searchControllerRef.current = controller;

      setLoadError(null);
      try {
        const [results, trailResults] = await Promise.all([
          getNearbySpots(center.lat, center.lng, radius, undefined, controller.signal),
          getNearbyTrails(center.lat, center.lng, radius, controller.signal),
        ]);
        if (controller.signal.aborted) return;
        setSpots(results.items);
        setTotalNearbyCount(results.totalCount);
        setTrails(trailResults.items);
        if (pendingSelectIdRef.current) {
          const match = results.items.find((item) => item.id === pendingSelectIdRef.current);
          if (match) setSelectedSpot(match);
          pendingSelectIdRef.current = null;
        }
        setLastSearchedCenter(center);
        setShowSearchHere(false);
      } catch (err) {
        if (controller.signal.aborted) return;
        setLoadError(getErrorMessage(err, t("loadError")));
      }
    },
    [t],
  );

  useEffect(() => () => searchControllerRef.current?.abort(), []);

  useEffect(() => {
    // Arrived via a "View on map" link — stay on that spot instead of jumping to the user's
    // location. Geolocation is only honored again once they press "Use my location".
    if (ignoreGeoRef.current) {
      if (target && !targetAppliedRef.current) {
        targetAppliedRef.current = true;
        void search({ lat: target.lat, lng: target.lng }, radiusKm);
      }
      return;
    }

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

  function handleSearchSelect(spot: SpotSearchResult) {
    // Same flow as arriving via a "View on map" link: stay on this spot rather than letting a
    // late geolocation result pull the map away, then open its popup once nearby results land.
    ignoreGeoRef.current = true;
    setSelectedSpot(null);
    setHighlight({ lat: spot.latitude, lng: spot.longitude });
    pendingSelectIdRef.current = spot.id;
    setViewState({ longitude: spot.longitude, latitude: spot.latitude, zoom: TARGET_ZOOM });
    void search({ lat: spot.latitude, lng: spot.longitude }, radiusKm);
  }

  function handleUseMyLocation() {
    ignoreGeoRef.current = false;
    setHighlight(null);
    setLoadError(null);
    useGeolocationStore.getState().refreshLocation();
  }

  function handleMapClick(lat: number, lng: number) {
    if (drawingTrail) {
      setTrailPoints((prev) => [...prev, { latitude: lat, longitude: lng }]);
      return;
    }
    if (!placingSpot) return;
    setPlacingSpot(false);
    setCreateModalCoords({ lat, lng });
  }

  function handleToggleAddSpot() {
    if (authStatus !== "authenticated") {
      setLoginPromptFor("spot");
      return;
    }
    setLoginPromptFor(null);
    setDrawingTrail(false);
    setTrailPoints([]);
    setPlacingSpot((wasPlacing) => !wasPlacing);
  }

  function handleSpotCreated(spot: SpotResponse) {
    const { photoUrls, ...rest } = spot;
    setSpots((prev) => [{ ...rest, photoUrl: photoUrls[0], distanceKm: 0 }, ...prev]);
    setTotalNearbyCount((prev) => prev + 1);
    setCreateModalCoords(null);
  }

  function handleToggleAddTrail() {
    if (authStatus !== "authenticated") {
      setLoginPromptFor("trail");
      return;
    }
    setLoginPromptFor(null);
    setPlacingSpot(false);
    if (drawingTrail) {
      setDrawingTrail(false);
      setTrailPoints([]);
    } else {
      setDrawingTrail(true);
    }
  }

  function handleUndoTrailPoint() {
    setTrailPoints((prev) => prev.slice(0, -1));
  }

  function trailDrawingDistanceMeters(points: TrailPoint[]): number {
    let total = 0;
    for (let i = 1; i < points.length; i++) {
      total += haversineMeters(points[i - 1].latitude, points[i - 1].longitude, points[i].latitude, points[i].longitude);
    }
    return total;
  }

  function handleFinishTrail() {
    if (trailPoints.length < 2) return;
    setShowCreateTrailModal(true);
  }

  function handleTrailCreated(trail: TrailResponse) {
    setTrails((prev) => [
      {
        id: trail.id,
        name: trail.name,
        description: trail.description,
        photoUrl: trail.photoUrls[0],
        points: trail.points,
        distanceMeters: trail.distanceMeters,
        createdByUserId: trail.createdByUserId,
        createdAt: trail.createdAt,
      },
      ...prev,
    ]);
    setShowCreateTrailModal(false);
    setDrawingTrail(false);
    setTrailPoints([]);
  }

  async function handleDeleteTrail(trail: NearbyTrail) {
    if (!window.confirm(tTrails("deleteConfirm"))) return;

    setTrailError(null);
    try {
      await deleteTrailRequest(trail.id);
      setTrails((prev) => prev.filter((t) => t.id !== trail.id));
      setSelectedTrail(null);
    } catch (err) {
      setTrailError(getErrorMessage(err, tTrails("unknownError")));
    }
  }

  return (
    <div className="relative flex-1">
      <div className="absolute top-4 left-4 z-10 flex flex-col items-start gap-2">
        <MapSearchBox onSelect={handleSearchSelect} />
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

      {drawingTrail ? (
        <div
          className="absolute bottom-24 left-1/2 z-10 flex -translate-x-1/2 items-center gap-2 rounded px-3 py-2 text-sm shadow-lg"
          style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
        >
          <span>
            {trailPoints.length < 2 ? tTrails("minPointsHint") : tTrails("tapMapToAddPoint")} ({trailPoints.length})
          </span>
          <button onClick={handleUndoTrailPoint} disabled={trailPoints.length === 0} className="underline disabled:opacity-40">
            {tTrails("undoPoint")}
          </button>
          <button
            onClick={handleFinishTrail}
            disabled={trailPoints.length < 2}
            className="rounded px-2 py-1 font-medium disabled:opacity-40"
            style={{ backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)" }}
          >
            {tTrails("finishTrail")}
          </button>
          <button onClick={handleToggleAddTrail} className="underline">
            {tTrails("cancelTrail")}
          </button>
        </div>
      ) : null}

      {trailError ? (
        <div className="absolute bottom-24 left-1/2 z-10 -translate-x-1/2 rounded bg-red-50 dark:bg-red-950 px-3 py-2 text-sm text-red-700 dark:text-red-400 shadow">
          {trailError}
        </div>
      ) : null}

      <div className="absolute right-6 bottom-6 z-10 flex flex-col items-end gap-2">
        <button
          onClick={handleToggleAddTrail}
          aria-pressed={drawingTrail}
          className="rounded-full px-4 py-3 text-sm shadow-lg"
          style={
            drawingTrail
              ? { backgroundColor: "#2563eb", color: "#fff", boxShadow: "0 0 0 4px rgba(37,99,235,0.25)" }
              : { backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }
          }
        >
          {tTrails("addTrailButton")}
        </button>

        <button
          onClick={handleToggleAddSpot}
          aria-pressed={placingSpot}
          className="rounded-full px-4 py-3 text-sm shadow-lg"
          style={
            placingSpot
              ? { backgroundColor: "var(--fieldmap-trail)", color: "var(--fieldmap-paper-light)", boxShadow: "0 0 0 4px rgba(181,74,36,0.25)" }
              : { backgroundColor: "var(--fieldmap-ink)", color: "var(--fieldmap-paper-light)" }
          }
        >
          {placingSpot ? t("tapMapToPlace") : t("addAtMyLocation")}
        </button>
      </div>

      {loginPromptFor ? (
        <div
          className="absolute bottom-6 left-6 z-10 rounded px-4 py-3 text-sm shadow"
          style={{ backgroundColor: "var(--fieldmap-paper-light)", color: "var(--fieldmap-ink)" }}
        >
          {loginPromptFor === "spot" ? t("loginRequiredToCreate") : tTrails("loginRequiredToCreate")}{" "}
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
        highlight={highlight}
        trails={trails}
        selectedTrail={selectedTrail}
        onSelectTrail={setSelectedTrail}
        currentUserId={currentUserId}
        onDeleteTrail={handleDeleteTrail}
        drawingPoints={drawingTrail ? trailPoints : undefined}
      />

      {createModalCoords ? (
        <CreateSpotModal
          latitude={createModalCoords.lat}
          longitude={createModalCoords.lng}
          onClose={() => setCreateModalCoords(null)}
          onCreated={handleSpotCreated}
        />
      ) : null}

      {showCreateTrailModal ? (
        <CreateTrailModal
          points={trailPoints}
          distanceMeters={trailDrawingDistanceMeters(trailPoints)}
          onClose={() => setShowCreateTrailModal(false)}
          onCreated={handleTrailCreated}
        />
      ) : null}
    </div>
  );
}

export default function MapPage() {
  return (
    <Suspense fallback={null}>
      <MapPageContent />
    </Suspense>
  );
}
