import { apiFetch } from "@/lib/apiClient";

export interface RewardResponse {
  id: string;
  businessId: string;
  title: string;
  description: string;
  crystalCost: number;
  createdAt: string;
}

export function getBusinessRewards(businessId: string, signal?: AbortSignal): Promise<RewardResponse[]> {
  return apiFetch<RewardResponse[]>(`/businesses/${businessId}/rewards`, { signal });
}

export interface RewardRedemptionResponse {
  redemptionId: string;
  rewardId: string;
  crystalsSpent: number;
  newCrystalBalance: number;
  createdAt: string;
}

export function redeemReward(rewardId: string): Promise<RewardRedemptionResponse> {
  return apiFetch<RewardRedemptionResponse>(`/rewards/${rewardId}/redeem`, { method: "POST" });
}

export interface MyRedemptionResponse {
  redemptionId: string;
  rewardId: string;
  rewardTitle: string;
  businessId: string;
  businessName: string;
  crystalsSpent: number;
  createdAt: string;
}

export interface RedemptionsPageResponse {
  items: MyRedemptionResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function fetchMyRedemptions(page: number, pageSize: number): Promise<RedemptionsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<RedemptionsPageResponse>(`/redemptions/me?${params.toString()}`);
}
