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
