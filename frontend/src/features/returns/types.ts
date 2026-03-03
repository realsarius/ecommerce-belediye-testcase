export type ReturnRequestType = 'Return' | 'Cancellation';

export type ReturnReasonCategory =
  | 'WrongProduct'
  | 'DefectiveDamaged'
  | 'NotAsDescribed'
  | 'ChangedMind'
  | 'LateDelivery'
  | 'Other';

export type ReturnRequestStatus =
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'RefundPending'
  | 'Refunded';

export interface ReturnRequest {
  id: number;
  orderId: number;
  orderNumber: string;
  userId: number;
  customerName: string;
  type: ReturnRequestType;
  reasonCategory: ReturnReasonCategory;
  status: ReturnRequestStatus;
  reason: string;
  requestNote?: string | null;
  selectedItems: ReturnRequestItem[];
  requestedRefundAmount: number;
  paymentStatus?: string | null;
  reviewedByUserId?: number | null;
  reviewerName?: string | null;
  reviewNote?: string | null;
  reviewedAt?: string | null;
  refundRequestId?: number | null;
  refundStatus?: string | null;
  createdAt: string;
}

export interface CreateReturnRequestPayload {
  orderId: number;
  type: ReturnRequestType;
  reasonCategory: ReturnReasonCategory;
  selectedOrderItemIds?: number[];
  reason: string;
  requestNote?: string;
}

export interface ReturnRequestItem {
  orderItemId: number;
  productId: number;
  productName: string;
  quantity: number;
  lineTotal: number;
}
