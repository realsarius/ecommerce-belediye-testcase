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
  userId: number;
  status: OrderStatus;
  totalAmount: number;
  items: OrderItem[];
  customerName?: string;
  shippingAddress: string;
  createdAt: string;
  updatedAt: string;
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
  errorMessage?: string;
  createdAt: string;
}

export interface ProcessPaymentRequest {
  orderId: number;
  cardHolderName: string;
  cardNumber: string;
  expiryDate: string;
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
