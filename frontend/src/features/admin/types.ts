import type { Order, OrderStatus, ShippingAddress } from '@/features/orders/types';

export type AdminUserStatus = 'Active' | 'Suspended' | 'Banned';

export interface AdminUserListItem {
  id: number;
  fullName: string;
  email: string;
  role: string;
  status: AdminUserStatus;
  createdAt: string;
  lastLoginAt?: string | null;
  totalSpent: number;
  orderCount: number;
  isEmailVerified: boolean;
}

export interface AdminUserDetail {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  status: AdminUserStatus;
  isEmailVerified: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
  totalSpent: number;
  averageOrderValue: number;
  orderCount: number;
  orders: Order[];
  addresses: ShippingAddress[];
}

export interface AdminUsersQueryParams {
  search?: string;
  role?: string;
  status?: AdminUserStatus | '';
  registeredFrom?: string;
  registeredTo?: string;
  page?: number;
  pageSize?: number;
}

export type AdminServiceStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface AdminServiceHealth {
  name: string;
  status: AdminServiceStatus;
  description: string;
  checkedAt: string;
  responseTimeMs?: number | null;
}

export interface AdminHangfireFailedJob {
  id: string;
  reason?: string | null;
  exceptionType?: string | null;
  exceptionMessage?: string | null;
  failedAt?: string | null;
}

export interface AdminHangfireSummary {
  enabled: boolean;
  processingCount: number;
  failedCount: number;
  scheduledCount: number;
  enqueuedCount: number;
  succeededCount: number;
  failedJobs: AdminHangfireFailedJob[];
}

export interface AdminSystemHealth {
  overallStatus: AdminServiceStatus;
  generatedAt: string;
  services: AdminServiceHealth[];
  hangfire: AdminHangfireSummary;
}

export interface AdminErrorLog {
  timestamp?: string | null;
  level: string;
  message: string;
  exception?: string | null;
  correlationId?: string | null;
}

export type AdminAnnouncementAudienceType = 'AllUsers' | 'AllSellers' | 'Role' | 'SpecificUsers';
export type AdminAnnouncementStatus = 'Scheduled' | 'Processing' | 'Sent' | 'PartiallySent' | 'Failed';
export type AdminAnnouncementChannel = 'InApp' | 'Email';

export interface AdminAnnouncement {
  id: number;
  title: string;
  message: string;
  audienceType: AdminAnnouncementAudienceType;
  targetRole?: string | null;
  targetUserIds: number[];
  channels: AdminAnnouncementChannel[];
  status: AdminAnnouncementStatus;
  recipientCount: number;
  deliveredCount: number;
  failedCount: number;
  scheduledAt?: string | null;
  sentAt?: string | null;
  createdAt: string;
  createdByName: string;
}

export interface CreateAdminAnnouncementRequest {
  title: string;
  message: string;
  audienceType: AdminAnnouncementAudienceType;
  targetRole?: string;
  targetUserIds: number[];
  channels: AdminAnnouncementChannel[];
  scheduledAt?: string | null;
}

export interface AdminDashboardKpi {
  todayRevenue: number;
  yesterdayRevenue: number;
  todayOrders: number;
  yesterdayOrders: number;
  todayNewUsers: number;
  yesterdayNewUsers: number;
  activeSellers: number;
  activeProducts: number;
  categoryCount: number;
  pendingSellerApplications: number;
  currency: string;
}

export interface AdminDashboardRevenueTrendPoint {
  label: string;
  date: string;
  revenue: number;
  previousRevenue: number;
  orders: number;
}

export interface AdminDashboardCategorySalesItem {
  categoryName: string;
  salesCount: number;
}

export interface AdminDashboardUserRegistrationPoint {
  label: string;
  date: string;
  count: number;
}

export interface AdminDashboardOrderStatusDistributionItem {
  status: OrderStatus;
  count: number;
}

export interface AdminDashboardLowStockItem {
  productId: number;
  name: string;
  stock: number;
  sellerName: string;
}

export interface AdminDashboardRecentOrder {
  orderId: number;
  orderNumber: string;
  customerName: string;
  totalAmount: number;
  currency: string;
  status: OrderStatus;
  createdAt: string;
}
