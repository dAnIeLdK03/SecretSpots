import type { MapLibreEvent } from "maplibre-gl";

// MapLibre's compact attribution starts expanded and only collapses once the map is dragged, so on
// a phone the full credits line sits over the bottom of the map (and the hero preview never moves).
// Collapsing it through its own (i) toggle keeps the credits one tap away, as the tile licenses need.
export function collapseAttribution(event: MapLibreEvent) {
  const container = event.target.getContainer();
  if (container.querySelector(".maplibregl-ctrl-attrib.maplibregl-compact-show")) {
    container.querySelector<HTMLButtonElement>(".maplibregl-ctrl-attrib-button")?.click();
  }
}
