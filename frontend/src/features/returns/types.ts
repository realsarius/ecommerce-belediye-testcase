export type ReturnRequestType = 'Return' | 'Cancellation';

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
  status: ReturnRequestStatus;
  reason: string;
  requestNote?: string | null;
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
  reason: string;
  requestNote?: string;
}
