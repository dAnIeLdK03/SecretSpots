"use client";

import { Map, Marker, Popup, Source, Layer } from "react-map-gl/maplibre";
import { MapPin } from "lucide-react";
import "maplibre-gl/dist/maplibre-gl.css";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { NearbySpot } from "@/lib/spotsApi";
import type { NearbyTrail, TrailPoint } from "@/lib/trailsApi";
import { CategoryIcon } from "./CategoryIcon";

const MAP_STYLE = "https://tiles.openfreemap.org/styles/liberty";
const TRAILS_LAYER_ID = "trails-line";

// The liberty style already renders OSM footpaths/tracks (highway=path/footway/track), just as
// thin white dashes only from zoom 14 — easy to miss against most basemap colors and too subtle
// to actually trace a hiking route by eye. This re-highlights the same "openmaptiles"/
// "transportation" source-layer data the style itself uses (no separate fetch/Source needed)
// with a bold dashed line, one zoom level earlier, so a real trail is visible while placing
// points for it.
const HIKING_PATHS_LAYER_ID = "hiking-paths-highlight";

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
  trails?: NearbyTrail[];
  selectedTrail?: NearbyTrail | null;
  onSelectTrail?: (trail: NearbyTrail | null) => void;
  currentUserId?: string | null;
  onDeleteTrail?: (trail: NearbyTrail) => void;
  // In-progress points for a trail being drawn (not yet submitted) — rendered as its own line/dots
  // so the user can see the shape build up before hitting "Finish".
  drawingPoints?: TrailPoint[];
}

function formatDistance(distanceKm: number, t: ReturnType<typeof useTranslations>): string {
  if (distanceKm < 1) {
    return t("distanceMeters", { value: Math.round(distanceKm * 1000) });
  }
  return t("distanceKm", { value: distanceKm.toFixed(1) });
}

function trailsToFeatureCollection(trails: NearbyTrail[]) {
  return {
    type: "FeatureCollection" as const,
    features: trails.map((trail) => ({
      type: "Feature" as const,
      properties: { id: trail.id },
      geometry: {
        type: "LineString" as const,
        coordinates: trail.points.map((p) => [p.longitude, p.latitude]),
      },
    })),
  };
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
  trails = [],
  selectedTrail = null,
  onSelectTrail,
  currentUserId,
  onDeleteTrail,
  drawingPoints = [],
}: SpotsMapProps) {
  const t = useTranslations("Spots");
  const tTrails = useTranslations("Trails");

  return (
    <div className="absolute inset-0">
      <Map
        {...viewState}
        onMove={(evt) => onViewStateChange(evt.viewState)}
        onMoveEnd={onMoveEnd}
        interactiveLayerIds={onSelectTrail ? [TRAILS_LAYER_ID] : undefined}
        onClick={(evt) => {
          const trailFeature = evt.features?.find((f) => f.layer.id === TRAILS_LAYER_ID);
          const trailId = trailFeature?.properties?.id as string | undefined;
          const trail = trailId ? trails.find((tr) => tr.id === trailId) : undefined;
          if (trail && onSelectTrail) {
            onSelectTrail(trail);
            return;
          }
          onMapClick(evt.lngLat.lat, evt.lngLat.lng);
        }}
        mapStyle={MAP_STYLE}
        maxBounds={BULGARIA_BOUNDS}
        style={{ width: "100%", height: "100%" }}
      >
        <Layer
          id={HIKING_PATHS_LAYER_ID}
          type="line"
          source="openmaptiles"
          source-layer="transportation"
          minzoom={13}
          filter={["match", ["get", "class"], ["path", "track"], true, false]}
          layout={{ "line-cap": "round", "line-join": "round" }}
          paint={{
            "line-color": "#b45a24",
            "line-width": ["interpolate", ["linear"], ["zoom"], 13, 1.5, 18, 4],
            "line-dasharray": [2, 1.5],
          }}
        />

        {trails.length > 0 ? (
          <Source id="trails" type="geojson" data={trailsToFeatureCollection(trails)}>
            <Layer
              id={TRAILS_LAYER_ID}
              type="line"
              layout={{ "line-cap": "round", "line-join": "round" }}
              paint={{ "line-color": "#2563eb", "line-width": 4, "line-opacity": 0.85 }}
            />
          </Source>
        ) : null}

        {drawingPoints.length > 1 ? (
          <Source
            id="drawing-trail"
            type="geojson"
            data={{
              type: "Feature",
              properties: {},
              geometry: {
                type: "LineString",
                coordinates: drawingPoints.map((p) => [p.longitude, p.latitude]),
              },
            }}
          >
            <Layer
              type="line"
              layout={{ "line-cap": "round", "line-join": "round" }}
              paint={{ "line-color": "#f59e0b", "line-width": 4, "line-dasharray": [1, 1.5] }}
            />
          </Source>
        ) : null}

        {drawingPoints.map((p, index) => (
          <Marker key={index} longitude={p.longitude} latitude={p.latitude}>
            <div className="h-2.5 w-2.5 rounded-full border-2 border-white bg-amber-500 shadow" />
          </Marker>
        ))}

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
              aria-label={t("viewSpotLabel", { name: spot.name })}
              className="h-4 w-4 cursor-pointer rounded-full border-2 border-white bg-red-600 shadow focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500"
              // No onClick here — react-map-gl's Marker attaches its own native 'click' listener
              // (see marker.js) to the wrapper element it creates around these children, and a
              // native click bubbles up to it from this button whether triggered by mouse or by
              // keyboard (Enter/Space on a <button> fires a real click event). Stopping
              // propagation here would prevent that listener — and the onSelectSpot above —
              // from ever firing.
            />
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
            anchor="bottom"
            maxWidth="280px"
          >
            <div className="flex w-64 flex-col gap-1.5 text-sm text-zinc-900">
              <span className="pr-4 font-semibold">{selectedSpot.name}</span>
              <span className="w-fit rounded-full bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-600 flex items-center gap-1">
                <CategoryIcon category={selectedSpot.category} />
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

        {selectedTrail ? (
          <Popup
            longitude={selectedTrail.points[Math.floor(selectedTrail.points.length / 2)].longitude}
            latitude={selectedTrail.points[Math.floor(selectedTrail.points.length / 2)].latitude}
            onClose={() => onSelectTrail?.(null)}
            closeOnClick={false}
            anchor="bottom"
            maxWidth="280px"
          >
            <div className="flex w-64 flex-col gap-1.5 text-sm text-zinc-900">
              <span className="pr-4 font-semibold">{selectedTrail.name}</span>
              <p className="line-clamp-2 text-zinc-700">{selectedTrail.description}</p>
              <div className="mt-0.5 aspect-video w-full overflow-hidden rounded-lg">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={selectedTrail.photoUrl} alt={selectedTrail.name} className="h-full w-full object-cover" />
              </div>
              <div className="mt-0.5 flex items-center justify-between">
                <span className="text-xs text-zinc-500">
                  {tTrails("distanceLabel", { value: (selectedTrail.distanceMeters / 1000).toFixed(1) })}
                </span>
                {currentUserId && currentUserId === selectedTrail.createdByUserId ? (
                  <button
                    type="button"
                    onClick={() => onDeleteTrail?.(selectedTrail)}
                    className="font-medium text-red-700 underline"
                  >
                    {tTrails("deleteButton")}
                  </button>
                ) : null}
              </div>
            </div>
          </Popup>
        ) : null}
      </Map>
    </div>
  );
}
