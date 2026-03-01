export interface LoyaltyTransaction {
  id: number;
  orderId?: number;
  orderNumber?: string;
  type: string;
  points: number;
  balanceAfter: number;
  description: string;
  expiresAt?: string | null;
  createdAt: string;
}

export interface LoyaltySummary {
  availablePoints: number;
  availableDiscountAmount: number;
  totalEarnedPoints: number;
  totalRedeemedPoints: number;
  pointsPerLira: number;
  recentTransactions: LoyaltyTransaction[];
}
