"use client";

import { useEffect, useState } from "react";
import { Map, Marker } from "react-map-gl/maplibre";
import "maplibre-gl/dist/maplibre-gl.css";
import { useRouter } from "@/i18n/navigation";
import { getNearbySpots } from "@/lib/spotsApi";
import type { NearbySpot } from "@/lib/spotsApi";

const MAP_STYLE = "https://tiles.openfreemap.org/styles/liberty";
const SOFIA = { longitude: 23.3219, latitude: 42.6977 };
const MAX_MARKERS = 6;

export function HeroMap() {
  const router = useRouter();
  const [spots, setSpots] = useState<NearbySpot[]>([]);

  useEffect(() => {
    getNearbySpots(SOFIA.latitude, SOFIA.longitude, 50)
      .then((results) => setSpots(results.slice(0, MAX_MARKERS)))
      .catch(() => {});
  }, []);

  return (
    <div className="h-full w-full overflow-hidden rounded-2xl shadow-2xl">
      <Map
        initialViewState={{ longitude: SOFIA.longitude, latitude: SOFIA.latitude, zoom: 11 }}
        mapStyle={MAP_STYLE}
        style={{ width: "100%", height: "100%" }}
        dragPan={false}
        dragRotate={false}
        scrollZoom={false}
        doubleClickZoom={false}
        touchZoomRotate={false}
        keyboard={false}
      >
        {spots.map((spot) => (
          <Marker
            key={spot.id}
            longitude={spot.longitude}
            latitude={spot.latitude}
            onClick={(e) => {
              e.originalEvent.stopPropagation();
              router.push(`/spots/${spot.id}`);
            }}
          >
            <div className="h-12 w-12 cursor-pointer overflow-hidden rounded-full border-2 border-white shadow-lg">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={spot.photoUrl} alt={spot.name} className="h-full w-full object-cover" />
            </div>
          </Marker>
        ))}
      </Map>
    </div>
  );
}
