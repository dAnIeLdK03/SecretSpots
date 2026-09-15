import { apiFetch } from "@/lib/apiClient";

export interface NearbyBusiness {
  id: string;
  name: string;
  description: string;
  latitude: number;
  longitude: number;
  isPromoted: boolean;
  distanceKm: number;
}

export interface NearbyBusinessesResponse {
  items: NearbyBusiness[];
  totalCount: number;
}

export function getNearbyBusinesses(
  lat: number,
  lng: number,
  radiusKm: number,
  signal?: AbortSignal,
): Promise<NearbyBusinessesResponse> {
  const params = new URLSearchParams({
    lat: String(lat),
    lng: String(lng),
    radiusKm: String(radiusKm),
  });
  return apiFetch<NearbyBusinessesResponse>(`/businesses/nearby?${params.toString()}`, { signal });
}

export interface BusinessResponse {
  id: string;
  name: string;
  description: string;
  latitude: number;
  longitude: number;
  ownerUserId: string;
  isPromoted: boolean;
  createdAt: string;
}

export function getBusiness(id: string, signal?: AbortSignal): Promise<BusinessResponse> {
  return apiFetch<BusinessResponse>(`/businesses/${id}`, { signal });
}
