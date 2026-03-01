export type NotificationType = 'Wishlist' | 'Order' | 'Campaign' | 'Support' | 'Refund';

export interface NotificationItem {
  id: number;
  userId: number;
  type: NotificationType;
  title: string;
  body: string;
  deepLink?: string | null;
  isRead: boolean;
  readAt?: string | null;
  createdAt: string;
}

export interface NotificationCount {
  unreadCount: number;
}
