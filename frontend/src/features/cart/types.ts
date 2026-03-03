// Cart Types

export interface Cart {
  id: number;
  items: CartItem[];
  totalAmount: number;
  totalItems: number;
}

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  productSKU: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  availableStock: number;
}

export interface AddToCartRequest {
  productId: number;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

export interface ReorderCartRequest {
  orderId: number;
}

export interface ReorderCartSkippedProduct {
  productId: number;
  name: string;
  reason: string;
}

export interface ReorderCartResult {
  requestedCount: number;
  addedCount: number;
  skippedCount: number;
  skippedProducts: ReorderCartSkippedProduct[];
}

export type CheckoutInvoiceType = 'Individual' | 'Corporate';

export interface CheckoutInvoiceInfo {
  type: CheckoutInvoiceType;
  fullName?: string;
  tcKimlikNo?: string;
  companyName?: string;
  taxOffice?: string;
  taxNumber?: string;
  invoiceAddress: string;
}

// Checkout Request
export interface CheckoutRequest {
  shippingAddress: string;
  paymentMethod: string;
  notes?: string;
  idempotencyKey: string;
  couponCode?: string;
  loyaltyPointsToUse?: number;
  giftCardCode?: string;
  preliminaryInfoAccepted: boolean;
  distanceSalesContractAccepted: boolean;
  invoiceInfo: CheckoutInvoiceInfo;
}
