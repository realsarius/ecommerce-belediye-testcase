import type { OrderStatus } from '@/features/orders/types';
import type { StatusBadgeTone } from '@/components/admin/StatusBadge';

const defaultOrderStatusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoya Verildi',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal Edildi',
  Refunded: 'İade Edildi',
};

const compactOrderStatusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Beklemede',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoda',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal',
  Refunded: 'İade',
};

export function getOrderStatusLabel(status: OrderStatus, options?: { compact?: boolean }) {
  return options?.compact ? compactOrderStatusLabels[status] : defaultOrderStatusLabels[status];
}

export function getOrderStatusTone(status: OrderStatus): StatusBadgeTone {
  if (status === 'Delivered') {
    return 'success';
  }

  if (status === 'Cancelled' || status === 'Refunded') {
    return 'danger';
  }

  if (status === 'PendingPayment') {
    return 'warning';
  }

  return 'info';
}
