import { apiFetch } from "@/lib/apiClient";

export interface RatingResponse {
  spotId: string;
  value: number;
  averageRating: number;
  ratingsCount: number;
}

export interface MyRatingResponse {
  value: number | null;
}

export function rateSpot(spotId: string, value: number): Promise<RatingResponse> {
  return apiFetch<RatingResponse>(`/spots/${spotId}/ratings`, {
    method: "PUT",
    body: JSON.stringify({ value }),
  });
}

export function fetchMyRating(spotId: string, signal?: AbortSignal): Promise<MyRatingResponse> {
  return apiFetch<MyRatingResponse>(`/spots/${spotId}/ratings/me`, { signal });
}
