export interface ReferralTransaction {
  id: number;
  type: string;
  points: number;
  orderId?: number;
  orderNumber?: string;
  description: string;
  relatedUserName?: string | null;
  createdAt: string;
}

export interface ReferralSummary {
  referralCode: string;
  totalReferrals: number;
  successfulReferrals: number;
  pendingReferrals: number;
  totalRewardPoints: number;
  referrerRewardPoints: number;
  referredRewardPoints: number;
  referredByCode?: string | null;
  recentTransactions: ReferralTransaction[];
}
