import { apiFetch, apiFetchVoid } from "@/lib/apiClient";

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
  redemptionCode: string;
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
  redemptionCode: string;
  isFulfilled: boolean;
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

export interface BusinessRedemptionResponse {
  redemptionId: string;
  rewardId: string;
  rewardTitle: string;
  redeemedByDisplayName: string;
  crystalsSpent: number;
  redemptionCode: string;
  isFulfilled: boolean;
  fulfilledAt: string | null;
  createdAt: string;
}

export interface BusinessRedemptionsPageResponse {
  items: BusinessRedemptionResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function fetchBusinessRedemptions(
  businessId: string,
  page: number,
  pageSize: number,
): Promise<BusinessRedemptionsPageResponse> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return apiFetch<BusinessRedemptionsPageResponse>(`/businesses/${businessId}/redemptions?${params.toString()}`);
}

export function fulfillRedemption(redemptionId: string): Promise<void> {
  return apiFetchVoid(`/redemptions/${redemptionId}/fulfill`, { method: "POST" });
}
