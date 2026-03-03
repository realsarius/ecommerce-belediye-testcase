// Order Types

export type OrderStatus =
  | 'PendingPayment'
  | 'Paid'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Refunded';

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
  couponCode?: string;
  discountAmount?: number;
  loyaltyPointsUsed?: number;
  loyaltyPointsEarned?: number;
  loyaltyDiscountAmount?: number;
  giftCardCode?: string;
  giftCardAmount?: number;
  payment?: Payment;
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
  errorMessage?: string;
  createdAt: string;
}

export interface ProcessPaymentRequest {
  orderId: number;
  savedCardId?: number;  // Kayıtlı kart ile ödeme için
  cardHolderName?: string;
  cardNumber?: string;
  expiryDate?: string;
  cvv: string;
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
