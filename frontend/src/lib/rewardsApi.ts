import { apiFetch } from "@/lib/apiClient";

export interface RewardResponse {
  id: string;
  businessId: string;
  title: string;
  description: string;
  crystalCost: number;
  createdAt: string;
}

export interface RewardsPageResponse {
  items: RewardResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function getBusinessRewards(
  businessId: string,
  page: number,
  pageSize: number,
  signal?: AbortSignal,
): Promise<RewardsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<RewardsPageResponse>(`/businesses/${businessId}/rewards?${params.toString()}`, { signal });
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
