import type { PaymentProviderType } from '@/features/creditCards/creditCardsApi';

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
  requestWindowEndsAt?: string | null;
  selectedItems: ReturnRequestItem[];
  attachments: ReturnRequestAttachment[];
  requestedRefundAmount: number;
  paymentStatus?: string | null;
  reviewedByUserId?: number | null;
  reviewerName?: string | null;
  reviewNote?: string | null;
  reviewedAt?: string | null;
  refundRequestId?: number | null;
  refundProvider?: PaymentProviderType | null;
  refundStatus?: string | null;
  createdAt: string;
}

export interface CreateReturnRequestPayload {
  orderId: number;
  type: ReturnRequestType;
  reasonCategory: ReturnReasonCategory;
  selectedOrderItemIds?: number[];
  uploadedPhotoKeys?: string[];
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

export interface ReturnRequestAttachment {
  id: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

export interface ReturnAttachmentAccessUrl {
  url: string;
  expiresAt: string;
}

export interface UploadedReturnPhoto {
  uploadKey: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export function normalizeReturnRequest(request: Partial<ReturnRequest>): ReturnRequest {
  return {
    id: request.id ?? 0,
    orderId: request.orderId ?? 0,
    orderNumber: request.orderNumber ?? '',
    userId: request.userId ?? 0,
    customerName: request.customerName ?? '',
    type: request.type ?? 'Return',
    reasonCategory: request.reasonCategory ?? 'Other',
    status: request.status ?? 'Pending',
    reason: request.reason ?? '',
    requestNote: request.requestNote ?? null,
    requestWindowEndsAt: request.requestWindowEndsAt ?? null,
    selectedItems: request.selectedItems ?? [],
    attachments: request.attachments ?? [],
    requestedRefundAmount: request.requestedRefundAmount ?? 0,
    paymentStatus: request.paymentStatus ?? null,
    reviewedByUserId: request.reviewedByUserId ?? null,
    reviewerName: request.reviewerName ?? null,
    reviewNote: request.reviewNote ?? null,
    reviewedAt: request.reviewedAt ?? null,
    refundRequestId: request.refundRequestId ?? null,
    refundProvider: request.refundProvider ?? null,
    refundStatus: request.refundStatus ?? null,
    createdAt: request.createdAt ?? new Date(0).toISOString(),
  };
}
