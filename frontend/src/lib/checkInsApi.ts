import { apiFetch } from "@/lib/apiClient";

export interface CreateCheckInCommand {
  photoUrl: string;
  latitude: number;
  longitude: number;
}

export interface CheckInResponse {
  id: string;
  spotId: string;
  photoUrl: string;
  crystalsAwarded: number;
  newCrystalBalance: number;
  createdAt: string;
}

export function createCheckIn(spotId: string, command: CreateCheckInCommand): Promise<CheckInResponse> {
  return apiFetch<CheckInResponse>(`/spots/${spotId}/checkins`, {
    method: "POST",
    body: JSON.stringify(command),
  });
}
