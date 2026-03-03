import type { PaymentProviderType } from '@/features/creditCards/creditCardsApi';
import type { CheckoutInvoiceInfo } from '@/features/cart/types';

// Order Types

export type OrderStatus =
  | 'PendingPayment'
  | 'Paid'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Refunded';

export type ShipmentStatus =
  | 'Pending'
  | 'Preparing'
  | 'HandedToCargo'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Failed'
  | 'Returned';

export interface Order {
  id: number;
  orderNumber?: string;
  userId: number;
  status: OrderStatus;
  totalAmount: number;
  currency?: string;
  items: OrderItem[];
  customerName?: string;
  shippingAddress: string;
  createdAt: string;
  updatedAt: string;
  cargoCompany?: string;
  trackingCode?: string;
  shippedAt?: string;
  estimatedDeliveryDate?: string;
  deliveredAt?: string;
  shipmentStatus?: ShipmentStatus;
  couponCode?: string;
  discountAmount?: number;
  loyaltyPointsUsed?: number;
  loyaltyPointsEarned?: number;
  loyaltyDiscountAmount?: number;
  giftCardCode?: string;
  giftCardAmount?: number;
  payment?: Payment;
  invoiceInfo?: CheckoutInvoiceInfo;
}

export interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  priceSnapshot: number;
  lineTotal: number;
}

// Payment Types
export interface Payment {
  id: number;
  orderId: number;
  amount: number;
  status: string;
  transactionId: string;
  paymentMethod?: string;
  provider?: PaymentProviderType | null;
  errorMessage?: string;
  requiresThreeDS?: boolean;
  threeDSHtmlContent?: string | null;
  createdAt: string;
}

export interface PaymentSettings {
  activeProviders: PaymentProviderType[];
  defaultProvider: PaymentProviderType;
  force3DSecure: boolean;
  force3DSecureAbove: number;
}

export interface ProcessPaymentRequest {
  orderId: number;
  paymentProvider?: PaymentProviderType;
  savedCardId?: number;  // Kayıtlı kart ile ödeme için
  cardHolderName?: string;
  cardNumber?: string;
  expiryDate?: string;
  cvv: string;
  saveCard?: boolean;
  saveCardAlias?: string;
  idempotencyKey?: string;
}

// Shipping Address Types
export interface ShippingAddress {
  id: number;
  title: string;
  fullName: string;
  phone: string;
  city: string;
  district: string;
  addressLine: string;
  postalCode: string;
  isDefault: boolean;
}

export interface CreateShippingAddressRequest {
  title: string;
  fullName: string;
  phone: string;
  city: string;
  district: string;
  addressLine: string;
  postalCode: string;
  isDefault: boolean;
}
