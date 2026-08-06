import { apiFetch, apiFetchVoid } from "@/lib/apiClient";

export interface CommentResponse {
  id: string;
  spotId: string;
  userId: string;
  authorDisplayName: string;
  text: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CommentsPageResponse {
  items: CommentResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function fetchSpotComments(spotId: string, page: number, pageSize: number): Promise<CommentsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<CommentsPageResponse>(`/spots/${spotId}/comments?${params.toString()}`);
}

export function createComment(spotId: string, text: string): Promise<CommentResponse> {
  return apiFetch<CommentResponse>(`/spots/${spotId}/comments`, {
    method: "POST",
    body: JSON.stringify({ text }),
  });
}

export function updateComment(commentId: string, text: string): Promise<CommentResponse> {
  return apiFetch<CommentResponse>(`/comments/${commentId}`, {
    method: "PUT",
    body: JSON.stringify({ text }),
  });
}

export function deleteComment(commentId: string): Promise<void> {
  return apiFetchVoid(`/comments/${commentId}`, { method: "DELETE" });
}
