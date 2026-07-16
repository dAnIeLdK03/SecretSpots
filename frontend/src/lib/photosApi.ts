import { apiFetch } from "@/lib/apiClient";

export interface UploadPhotoResponse {
  url: string;
}

export function uploadPhoto(file: File, signal?: AbortSignal): Promise<UploadPhotoResponse> {
  const formData = new FormData();
  formData.append("file", file);

  return apiFetch<UploadPhotoResponse>("/photos", {
    method: "POST",
    body: formData,
    signal,
  });
}
