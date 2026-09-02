import { apiFetch, apiFetchVoid } from "@/lib/apiClient";

export function getPushPublicKey(): Promise<{ publicKey: string }> {
  return apiFetch<{ publicKey: string }>("/notifications/push-public-key");
}

export function subscribeToPush(endpoint: string, p256dh: string, auth: string): Promise<void> {
  return apiFetchVoid("/notifications/push-subscriptions", {
    method: "POST",
    body: JSON.stringify({ endpoint, p256dh, auth }),
  });
}

export function unsubscribeFromPush(endpoint: string): Promise<void> {
  return apiFetchVoid("/notifications/push-subscriptions", {
    method: "DELETE",
    body: JSON.stringify({ endpoint }),
  });
}
