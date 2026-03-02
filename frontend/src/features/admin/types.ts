import type { Order, ShippingAddress } from '@/features/orders/types';

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
