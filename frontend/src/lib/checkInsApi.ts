import { apiFetch, type PagedResponse } from "@/lib/apiClient";

export interface CreateCheckInCommand {
  photoUrl: string;
  latitude: number;
  longitude: number;
}

export interface CheckInResponse {
  id: string;
  spotId: string;
  photoUrl: string;
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
  createdAt: string;
}

export function fetchMyCheckIns(page: number, pageSize: number): Promise<PagedResponse<MyCheckInResponse>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<PagedResponse<MyCheckInResponse>>(`/checkins/me?${params.toString()}`);
}
