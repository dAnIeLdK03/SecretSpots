"use client";

import { useEffect, useRef } from "react";
import { Map, Marker, Popup } from "react-map-gl/maplibre";
import { MapPin, X } from "lucide-react";
import "maplibre-gl/dist/maplibre-gl.css";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { NearbySpot } from "@/lib/spotsApi";
import { CategoryIcon } from "./CategoryIcon";
import { DirectionsLinks } from "./DirectionsLinks";
import { collapseAttribution } from "@/lib/mapAttribution";

const MAP_STYLE = "https://tiles.openfreemap.org/styles/liberty";

// Keeps the map framed on Bulgaria — the whole app is scoped to Bulgarian spots, so panning or
// zooming out to see the rest of the world/Sofia-scale-only tiles isn't useful. Bounds are
// Bulgaria's bbox with a small buffer so border-area spots aren't clipped.
const BULGARIA_BOUNDS: [[number, number], [number, number]] = [
  [22.3, 41.2],
  [28.65, 44.25],
];

export interface MapViewState {
  longitude: number;
  latitude: number;
  zoom: number;
}

interface SpotsMapProps {
  viewState: MapViewState;
  onViewStateChange: (viewState: MapViewState) => void;
  onMoveEnd: () => void;
  spots: NearbySpot[];
  onMapClick: (lat: number, lng: number) => void;
  selectedSpot: NearbySpot | null;
  onSelectSpot: (spot: NearbySpot | null) => void;
  // Exact point to flag with a prominent pin (e.g. arriving from a spot's "View on map" link).
  highlight?: { lat: number; lng: number } | null;
}

function formatDistance(distanceKm: number, t: ReturnType<typeof useTranslations>): string {
  if (distanceKm < 1) {
    return t("distanceMeters", { value: Math.round(distanceKm * 1000) });
  }
  return t("distanceKm", { value: distanceKm.toFixed(1) });
}

export function SpotsMap({
  viewState,
  onViewStateChange,
  onMoveEnd,
  spots,
  onMapClick,
  selectedSpot,
  onSelectSpot,
  highlight,
}: SpotsMapProps) {
  const t = useTranslations("Spots");
  const tCommon = useTranslations("Common");
  // globalThis.Map: plain `Map` in this file is the react-map-gl component.
  const markerButtonsRef = useRef(new globalThis.Map<string, HTMLButtonElement>());

  useEffect(() => {
    // MapLibre gives every marker's wrapper role="button" and aria-label "Map marker" when it's
    // added to the map. Our <button> inside is the real control, and a button inside a button
    // confuses screen readers. Runs after the markers' own effects, i.e. after they're added.
    for (const button of markerButtonsRef.current.values()) {
      button.parentElement?.removeAttribute("role");
      button.parentElement?.removeAttribute("aria-label");
    }
  }, [spots]);

  // Closing from the keyboard (Escape or the × button) puts focus back on the marker that opened
  // the popup, instead of dropping it to the top of the page.
  function closePopup() {
    const id = selectedSpot?.id;
    onSelectSpot(null);
    if (id) markerButtonsRef.current.get(id)?.focus();
  }

  useEffect(() => {
    if (!selectedSpot) return;
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") closePopup();
    }
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  });

  return (
    <div className="absolute inset-0">
      <Map
        {...viewState}
        locale={{ "Map.Title": t("mapLabel"), "AttributionControl.ToggleAttribution": t("mapAttributionToggle") }}
        onMove={(evt) => onViewStateChange(evt.viewState)}
        onMoveEnd={onMoveEnd}
        onClick={(evt) => onMapClick(evt.lngLat.lat, evt.lngLat.lng)}
        mapStyle={MAP_STYLE}
        // Collapsed to the (i) button so the full credits line doesn't run under the map's own
        // bottom-right controls on narrow screens; still one tap away, as the tile licenses require.
        attributionControl={{ compact: true }}
        onLoad={collapseAttribution}
        maxBounds={BULGARIA_BOUNDS}
        style={{ width: "100%", height: "100%" }}
      >
        {spots.map((spot) => (
          <Marker
            key={spot.id}
            longitude={spot.longitude}
            latitude={spot.latitude}
            onClick={(e) => {
              e.originalEvent.stopPropagation();
              onSelectSpot(spot);
            }}
          >
            <button
              type="button"
              ref={(button) => {
                if (button) markerButtonsRef.current.set(spot.id, button);
                else markerButtonsRef.current.delete(spot.id);
              }}
              aria-label={t("viewSpotLabel", { name: spot.name })}
              // 24×24 hit area (WCAG target size) around the same 16px dot.
              className="flex h-6 w-6 cursor-pointer items-center justify-center rounded-full focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500"
              // No onClick here — react-map-gl's Marker attaches its own native 'click' listener
              // (see marker.js) to the wrapper element it creates around these children, and a
              // native click bubbles up to it from this button whether triggered by mouse or by
              // keyboard (Enter/Space on a <button> fires a real click event). Stopping
              // propagation here would prevent that listener — and the onSelectSpot above —
              // from ever firing.
            >
              <span className="h-4 w-4 rounded-full border-2 border-white bg-red-600 shadow" />
            </button>
          </Marker>
        ))}

        {highlight ? (
          <Marker longitude={highlight.lng} latitude={highlight.lat} anchor="bottom">
            <MapPin
              size={40}
              strokeWidth={1.5}
              aria-hidden="true"
              className="pointer-events-none fill-red-600 text-white drop-shadow-lg"
            />
          </Marker>
        ) : null}

        {selectedSpot ? (
          <Popup
            longitude={selectedSpot.longitude}
            latitude={selectedSpot.latitude}
            onClose={() => onSelectSpot(null)}
            closeOnClick={false}
            // Our own × below: translated, and it returns focus to the marker (MapLibre's doesn't).
            closeButton={false}
            anchor="bottom"
            maxWidth="280px"
          >
            {/* Styled by the .maplibregl-popup-close-button rules in globals.css. */}
            <button
              type="button"
              onClick={closePopup}
              aria-label={tCommon("close")}
              className="maplibregl-popup-close-button"
            >
              <X size={14} className="mx-auto" aria-hidden="true" />
            </button>
            <div className="flex w-64 flex-col gap-1.5 text-sm text-zinc-900">
              <span className="pr-4 font-semibold">{selectedSpot.name}</span>
              <span className="flex w-fit items-center gap-1 rounded-full bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-600">
                <CategoryIcon category={selectedSpot.category} />
                {t(`category.${selectedSpot.category}`)}
              </span>
              <p className="line-clamp-2 text-zinc-700">{selectedSpot.description}</p>
              <div className="mt-0.5 aspect-video w-full overflow-hidden rounded-lg">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={selectedSpot.photoUrl} alt={selectedSpot.name} className="h-full w-full object-cover" />
              </div>
              <div className="mt-0.5 flex items-center justify-between">
                <span className="text-xs text-zinc-500">{formatDistance(selectedSpot.distanceKm, t)}</span>
                <Link href={`/spots/${selectedSpot.id}`} className="font-medium underline">
                  {t("viewDetails")}
                </Link>
              </div>
              <DirectionsLinks
                latitude={selectedSpot.latitude}
                longitude={selectedSpot.longitude}
                className="text-xs"
                forcedLight
              />
            </div>
          </Popup>
        ) : null}
      </Map>
    </div>
  );
}
