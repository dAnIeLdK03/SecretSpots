"use client";

import { Map, Marker, Popup } from "react-map-gl/maplibre";
import "maplibre-gl/dist/maplibre-gl.css";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import type { NearbySpot } from "@/lib/spotsApi";

const MAP_STYLE = "https://tiles.openfreemap.org/styles/liberty";

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
          >
            <div className="flex max-w-[220px] flex-col gap-1 text-sm text-zinc-900">
              <span className="font-semibold">{selectedSpot.name}</span>
              <span className="text-zinc-600">{t(`category.${selectedSpot.category}`)}</span>
              <span>{selectedSpot.description}</span>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={selectedSpot.photoUrl}
                alt={selectedSpot.name}
                className="mt-1 max-h-32 rounded object-cover"
              />
              <span className="text-zinc-500">{selectedSpot.distanceKm.toFixed(1)} km</span>
              <Link href={`/spots/${selectedSpot.id}`} className="mt-1 font-medium underline">
                {t("viewDetails")}
              </Link>
            </div>
          </Popup>
        ) : null}
      </Map>
    </div>
  );
}
