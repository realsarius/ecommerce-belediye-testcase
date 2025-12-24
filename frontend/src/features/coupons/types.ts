// Coupon Types

export const CouponType = {
  Percentage: 0,
  FixedAmount: 1,
} as const;

export type CouponType = (typeof CouponType)[keyof typeof CouponType];

export interface Coupon {
  id: number;
  code: string;
  type: CouponType;
  value: number;
  minOrderAmount?: number;
  usageLimit: number;
  usedCount: number;
  expiresAt: string;
  isActive: boolean;
  description?: string;
  createdAt: string;
}

export interface CreateCouponRequest {
  code: string;
  type: CouponType;
  value: number;
  minOrderAmount?: number;
  usageLimit: number;
  validDays: number;
  description?: string;
}

export interface UpdateCouponRequest {
  code?: string;
  type?: CouponType;
  value?: number;
  minOrderAmount?: number;
  usageLimit?: number;
  expiresAt?: string;
  isActive?: boolean;
  description?: string;
}

export interface ValidateCouponRequest {
  code: string;
  orderTotal: number;
}

export interface CouponValidationResult {
  isValid: boolean;
  errorMessage?: string;
  coupon?: Coupon;
  discountAmount: number;
  finalTotal: number;
}
