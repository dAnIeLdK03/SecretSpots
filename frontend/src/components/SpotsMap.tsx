"use client";

import { Map, Marker, Popup } from "react-map-gl/maplibre";
import "maplibre-gl/dist/maplibre-gl.css";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { NearbySpot } from "@/lib/spotsApi";

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
}: SpotsMapProps) {
  const t = useTranslations("Spots");

  return (
    <div className="absolute inset-0">
      <Map
        {...viewState}
        onMove={(evt) => onViewStateChange(evt.viewState)}
        onMoveEnd={onMoveEnd}
        onClick={(evt) => onMapClick(evt.lngLat.lat, evt.lngLat.lng)}
        mapStyle={MAP_STYLE}
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
            <div className="h-4 w-4 cursor-pointer rounded-full border-2 border-white bg-red-600 shadow" />
          </Marker>
        ))}

        {selectedSpot ? (
          <Popup
            longitude={selectedSpot.longitude}
            latitude={selectedSpot.latitude}
            onClose={() => onSelectSpot(null)}
            closeOnClick={false}
            anchor="bottom"
            maxWidth="280px"
          >
            <div className="flex w-64 flex-col gap-1.5 text-sm text-zinc-900">
              <span className="pr-4 font-semibold">{selectedSpot.name}</span>
              <span className="w-fit rounded-full bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-600">
                {t(`category.${selectedSpot.category}`)}
              </span>
              <p className="line-clamp-2 text-zinc-700">{selectedSpot.description}</p>
              <div className="mt-0.5 aspect-video w-full overflow-hidden rounded-lg">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={selectedSpot.photoUrl}
                  alt={selectedSpot.name}
                  className="h-full w-full object-cover"
                />
              </div>
              <div className="mt-0.5 flex items-center justify-between">
                <span className="text-xs text-zinc-500">{formatDistance(selectedSpot.distanceKm, t)}</span>
                <Link href={`/spots/${selectedSpot.id}`} className="font-medium underline">
                  {t("viewDetails")}
                </Link>
              </div>
            </div>
          </Popup>
        ) : null}
      </Map>
    </div>
  );
}
