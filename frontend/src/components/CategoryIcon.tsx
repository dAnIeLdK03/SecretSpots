import type { LucideIcon } from "lucide-react";
import {
  Trees, Telescope, Coffee, Ghost, Waves, Mountain, Landmark,
  Palmtree, Droplets, Church, Tent, Gem, TrainFrontTunnel, Castle,
} from "lucide-react";
import type { SpotCategory } from "@/lib/spotsApi";

const CATEGORY_ICONS: Record<SpotCategory, LucideIcon> = {
  Nature: Trees,
  Viewpoint: Telescope,
  Cafe: Coffee,
  Abandoned: Ghost,
  Waterfall: Waves,
  Cave: Mountain,
  Landmark: Landmark,
  BeachOrLake: Palmtree,
  Spring: Droplets,
  MonasteryOrChurch: Church,
  CampingSpot: Tent,
  RockFormation: Gem,
  RailwayTunnel: TrainFrontTunnel,
  FortressRuins: Castle,
};

export function CategoryIcon({ category, size = 14, className }: { category: SpotCategory; size?: number; className?: string }) {
  const Icon = CATEGORY_ICONS[category];
  return <Icon size={size} className={className} aria-hidden="true" />;
}
