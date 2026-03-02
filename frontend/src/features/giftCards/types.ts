export interface GiftCard {
  id: number;
  code: string;
  maskedCode: string;
  initialBalance: number;
  currentBalance: number;
  currency: string;
  isActive: boolean;
  expiresAt?: string | null;
  isAssigned: boolean;
  assignedAt?: string | null;
  lastUsedAt?: string | null;
  description?: string | null;
  assignedUserEmail?: string | null;
  createdAt: string;
}

export interface GiftCardTransaction {
  id: number;
  giftCardId: number;
  giftCardCode: string;
  maskedGiftCardCode: string;
  orderId?: number;
  orderNumber?: string | null;
  type: 'Issued' | 'Redeemed' | 'Restored';
  amount: number;
  balanceAfter: number;
  description: string;
  createdAt: string;
}

export interface GiftCardSummary {
  totalAvailableBalance: number;
  activeCardCount: number;
  cards: GiftCard[];
  recentTransactions: GiftCardTransaction[];
}

export interface CreateGiftCardRequest {
  code?: string;
  initialBalance: number;
  validDays?: number;
  expiresAt?: string;
  description?: string;
}

export interface UpdateGiftCardRequest {
  isActive?: boolean;
  expiresAt?: string;
  description?: string;
}

export interface ValidateGiftCardRequest {
  code: string;
  orderTotal: number;
}

export interface GiftCardValidationResult {
  isValid: boolean;
  errorMessage?: string | null;
  giftCardId: number;
  code: string;
  maskedCode: string;
  availableBalance: number;
  appliedAmount: number;
  remainingBalance: number;
  finalTotal: number;
}
