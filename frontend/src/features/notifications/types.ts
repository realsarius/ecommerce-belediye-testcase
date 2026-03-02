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

export interface NotificationPreference {
  type: NotificationType;
  displayName: string;
  description: string;
  inAppEnabled: boolean;
  emailEnabled: boolean;
  pushEnabled: boolean;
  supportsInApp: boolean;
  supportsEmail: boolean;
  supportsPush: boolean;
}

export interface NotificationTemplate {
  type: NotificationType;
  displayName: string;
  description: string;
  titleExample: string;
  bodyExample: string;
  supportsInApp: boolean;
  supportsEmail: boolean;
  supportsPush: boolean;
}

export interface NotificationPreferencesResponse {
  preferences: NotificationPreference[];
  templates: NotificationTemplate[];
}

export interface UpdateNotificationPreferencesRequest {
  preferences: Array<{
    type: NotificationType;
    inAppEnabled: boolean;
    emailEnabled: boolean;
    pushEnabled: boolean;
  }>;
}

export interface UpdateNotificationTemplateRequest {
  displayName: string;
  description: string;
  titleExample: string;
  bodyExample: string;
}
