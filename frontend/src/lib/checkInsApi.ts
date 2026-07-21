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

export interface MyCheckInResponse {
  id: string;
  spotId: string;
  spotName: string;
  photoUrl: string;
  crystalsAwarded: number;
  createdAt: string;
}

export interface CheckInsPageResponse {
  items: MyCheckInResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function fetchMyCheckIns(page: number, pageSize: number): Promise<CheckInsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<CheckInsPageResponse>(`/checkins/me?${params.toString()}`);
}
