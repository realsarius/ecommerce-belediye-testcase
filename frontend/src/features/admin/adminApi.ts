import { baseApi } from '@/app/api';
import type { PaginatedResponse } from '@/types/api';
import type {
  Category,
  CreateCategoryRequest,
  ProductReviewDto,
  ReorderCategoriesRequest,
  ReviewModerationRequest,
  UpdateCategoryRequest,
} from '@/features/products/types';
import type {
  AdminErrorLog,
  AdminFinanceSummary,
  AdminAnnouncement,
  AdminDashboardCategorySalesItem,
  AdminDashboardKpi,
  AdminDashboardLowStockItem,
  AdminDashboardOrderStatusDistributionItem,
  AdminDashboardRecentOrder,
  AdminDashboardRevenueTrendPoint,
  AdminDashboardUserRegistrationPoint,
  AdminSellerDetail,
  AdminSellerListItem,
  AdminUserDetail,
  AdminUserListItem,
  CreateAdminAnnouncementRequest,
  AdminUsersQueryParams,
  AdminSystemHealth,
} from '@/features/admin/types';
import type {
  ShippingAddress,
  CreateShippingAddressRequest,
  Order,
} from '@/features/orders/types';
import type { ReturnRequest } from '@/features/returns/types';
import type {
  NotificationTemplate,
  UpdateNotificationTemplateRequest,
} from '@/features/notifications/types';

function unwrapApiData<T>(response: T | { data: T }) {
  return (response as { data?: T }).data ?? (response as T);
}

export const adminApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getAdminDashboardKpi: builder.query<AdminDashboardKpi, void>({
      query: () => '/admin/dashboard/kpi',
      transformResponse: (response: { data: AdminDashboardKpi }) => response.data,
      providesTags: ['Orders', 'Users', 'Products', 'Categories'],
    }),
    getAdminDashboardRevenueTrend: builder.query<AdminDashboardRevenueTrendPoint[], { period?: 'daily' | 'weekly' | 'monthly' } | void>({
      query: (params) => ({
        url: '/admin/dashboard/revenue-trend',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: AdminDashboardRevenueTrendPoint[] }) => response.data,
      providesTags: ['Orders'],
    }),
    getAdminDashboardCategorySales: builder.query<AdminDashboardCategorySalesItem[], void>({
      query: () => '/admin/dashboard/category-sales',
      transformResponse: (response: { data: AdminDashboardCategorySalesItem[] }) => response.data,
      providesTags: ['Orders', 'Categories'],
    }),
    getAdminDashboardUserRegistrations: builder.query<AdminDashboardUserRegistrationPoint[], number | void>({
      query: (days = 30) => ({
        url: '/admin/dashboard/user-registrations',
        params: { days },
      }),
      transformResponse: (response: { data: AdminDashboardUserRegistrationPoint[] }) => response.data,
      providesTags: ['Users'],
    }),
    getAdminDashboardOrderStatusDistribution: builder.query<AdminDashboardOrderStatusDistributionItem[], void>({
      query: () => '/admin/dashboard/order-status-distribution',
      transformResponse: (response: { data: AdminDashboardOrderStatusDistributionItem[] }) => response.data,
      providesTags: ['Orders'],
    }),
    getAdminDashboardLowStock: builder.query<AdminDashboardLowStockItem[], number | void>({
      query: (threshold = 5) => ({
        url: '/admin/dashboard/low-stock',
        params: { threshold },
      }),
      transformResponse: (response: { data: AdminDashboardLowStockItem[] }) => response.data,
      providesTags: ['Products'],
    }),
    getAdminDashboardRecentOrders: builder.query<AdminDashboardRecentOrder[], number | void>({
      query: (limit = 5) => ({
        url: '/admin/dashboard/recent-orders',
        params: { limit },
      }),
      transformResponse: (response: { data: AdminDashboardRecentOrder[] }) => response.data,
      providesTags: ['Orders'],
    }),
    getCategories: builder.query<Category[], void>({
      query: () => '/categories',
      transformResponse: (response: { data: Category[] }) => response.data,
      providesTags: ['Categories'],
    }),
    getAdminCategories: builder.query<Category[], void>({
      query: () => '/admin/categories',
      transformResponse: (response: { data: Category[] }) => response.data,
      providesTags: ['Categories'],
    }),
    createCategory: builder.mutation<Category, CreateCategoryRequest>({
      query: (data) => ({
        url: '/admin/categories',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: Category }) => response.data,
      invalidatesTags: ['Categories'],
    }),
    updateCategory: builder.mutation<Category, { id: number; data: UpdateCategoryRequest }>({
      query: ({ id, data }) => ({
        url: `/admin/categories/${id}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: Category }) => response.data,
      invalidatesTags: ['Categories'],
    }),
    reorderCategories: builder.mutation<void, ReorderCategoriesRequest>({
      query: (data) => ({
        url: '/admin/categories/reorder',
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: ['Categories'],
    }),
    deleteCategory: builder.mutation<void, number>({
      query: (id) => ({
        url: `/admin/categories/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Categories'],
    }),
    getAddresses: builder.query<ShippingAddress[], void>({
      query: () => '/shippingaddress',
      providesTags: ['Addresses'],
    }),
    createAddress: builder.mutation<ShippingAddress, CreateShippingAddressRequest>({
      query: (data) => ({
        url: '/shippingaddress',
        method: 'POST',
        body: data,
      }),
      invalidatesTags: ['Addresses'],
    }),
    deleteAddress: builder.mutation<void, number>({
      query: (id) => ({
        url: `/shippingaddress/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Addresses'],
    }),
    updateAddress: builder.mutation<ShippingAddress, { id: number; data: CreateShippingAddressRequest }>({
      query: ({ id, data }) => ({
        url: `/shippingaddress/${id}`,
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: ['Addresses'],
    }),
    getAdminOrders: builder.query<Order[], void>({
      query: () => '/admin/orders',
      transformResponse: (response: Order[] | { data: Order[] }) => unwrapApiData(response),
      providesTags: ['Orders'],
    }),
    updateOrderStatus: builder.mutation<Order, { id: number; status: string }>({
      query: ({ id, status }) => ({
        url: `/admin/orders/${id}/status`,
        method: 'PATCH',
        body: { status },
      }),
      transformResponse: (response: Order | { data: Order }) => unwrapApiData(response),
      invalidatesTags: ['Orders'],
    }),
    getAdminReturns: builder.query<ReturnRequest[], { status?: string } | void>({
      query: (params) => ({
        url: '/admin/returns',
        params: params?.status ? { status: params.status } : undefined,
      }),
      transformResponse: (response: { data: ReturnRequest[] }) => response.data,
      providesTags: ['Returns'],
    }),
    approveAdminReturn: builder.mutation<
      ReturnRequest,
      { id: number; reviewNote?: string }
    >({
      query: ({ id, reviewNote }) => ({
        url: `/admin/returns/${id}/approve`,
        method: 'PUT',
        body: { reviewNote },
      }),
      transformResponse: (response: { data: ReturnRequest }) => response.data,
      invalidatesTags: ['Returns', 'Orders', 'Loyalty', 'GiftCards', 'Referrals'],
    }),
    rejectAdminReturn: builder.mutation<
      ReturnRequest,
      { id: number; reviewNote?: string }
    >({
      query: ({ id, reviewNote }) => ({
        url: `/admin/returns/${id}/reject`,
        method: 'PUT',
        body: { reviewNote },
      }),
      transformResponse: (response: { data: ReturnRequest }) => response.data,
      invalidatesTags: ['Returns', 'Orders', 'Loyalty', 'GiftCards', 'Referrals'],
    }),
    getAdminSellers: builder.query<AdminSellerListItem[], { status?: string } | void>({
      query: (params) => ({
        url: '/admin/sellers',
        params: params?.status ? { status: params.status } : undefined,
      }),
      transformResponse: (response: { data: AdminSellerListItem[] }) => response.data,
      providesTags: ['SellerProfile'],
    }),
    getAdminSellerDetail: builder.query<AdminSellerDetail, number>({
      query: (id) => `/admin/sellers/${id}`,
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'SellerProfile', id }],
    }),
    getAdminFinanceSummary: builder.query<AdminFinanceSummary, { from?: string; to?: string } | void>({
      query: (params) => ({
        url: '/admin/finance',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: AdminFinanceSummary }) => response.data,
      providesTags: ['Orders', 'SellerProfile'],
    }),
    updateAdminSellerStatus: builder.mutation<AdminSellerDetail, { id: number; status: string; reviewNote?: string }>({
      query: ({ id, status, reviewNote }) => ({
        url: `/admin/sellers/${id}/status`,
        method: 'PUT',
        body: { status, reviewNote },
      }),
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      invalidatesTags: ['SellerProfile', 'Users'],
    }),
    updateAdminSellerCommission: builder.mutation<AdminSellerDetail, { id: number; rate?: number | null }>({
      query: ({ id, rate }) => ({
        url: `/admin/sellers/${id}/commission`,
        method: 'PUT',
        body: { rate },
      }),
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      invalidatesTags: ['SellerProfile'],
    }),
    approveAdminSellerApplication: builder.mutation<AdminSellerDetail, { id: number; reviewNote?: string }>({
      query: ({ id, reviewNote }) => ({
        url: `/admin/sellers/applications/${id}/approve`,
        method: 'PUT',
        body: { reviewNote },
      }),
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      invalidatesTags: ['SellerProfile', 'Users'],
    }),
    rejectAdminSellerApplication: builder.mutation<AdminSellerDetail, { id: number; reviewNote?: string }>({
      query: ({ id, reviewNote }) => ({
        url: `/admin/sellers/applications/${id}/reject`,
        method: 'PUT',
        body: { reviewNote },
      }),
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      invalidatesTags: ['SellerProfile', 'Users'],
    }),
    getAdminUsers: builder.query<PaginatedResponse<AdminUserListItem>, AdminUsersQueryParams | void>({
      query: (params) => ({
        url: '/admin/users',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: PaginatedResponse<AdminUserListItem> }) => response.data,
      providesTags: ['Users'],
    }),
    getAdminUserDetail: builder.query<AdminUserDetail, number>({
      query: (id) => `/admin/users/${id}`,
      transformResponse: (response: { data: AdminUserDetail }) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'Users', id }],
    }),
    getAdminSystemHealth: builder.query<AdminSystemHealth, void>({
      query: () => '/admin/health',
      transformResponse: (response: { data: AdminSystemHealth }) => response.data,
    }),
    getAdminErrorLogs: builder.query<AdminErrorLog[], number | void>({
      query: (limit = 20) => ({
        url: '/admin/logs/errors',
        params: { limit },
      }),
      transformResponse: (response: { data: AdminErrorLog[] }) => response.data,
    }),
    updateAdminUserRole: builder.mutation<AdminUserDetail, { id: number; role: string }>({
      query: ({ id, role }) => ({
        url: `/admin/users/${id}/role`,
        method: 'PUT',
        body: { role },
      }),
      transformResponse: (response: { data: AdminUserDetail }) => response.data,
      invalidatesTags: ['Users'],
    }),
    updateAdminUserStatus: builder.mutation<AdminUserDetail, { id: number; status: string }>({
      query: ({ id, status }) => ({
        url: `/admin/users/${id}/status`,
        method: 'PUT',
        body: { status },
      }),
      transformResponse: (response: { data: AdminUserDetail }) => response.data,
      invalidatesTags: ['Users'],
    }),
    getAdminReviews: builder.query<ProductReviewDto[], { status?: string } | void>({
      query: (params) => ({
        url: '/admin/reviews',
        params: params?.status ? { status: params.status } : undefined,
      }),
      transformResponse: (response: { data: ProductReviewDto[] }) => response.data,
      providesTags: ['Reviews'],
    }),
    approveAdminReview: builder.mutation<ProductReviewDto, number>({
      query: (id) => ({
        url: `/admin/reviews/${id}/approve`,
        method: 'PUT',
      }),
      transformResponse: (response: { data: ProductReviewDto }) => response.data,
      invalidatesTags: ['Reviews', 'Product'],
    }),
    rejectAdminReview: builder.mutation<ProductReviewDto, { id: number; data: ReviewModerationRequest }>({
      query: ({ id, data }) => ({
        url: `/admin/reviews/${id}/reject`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: ProductReviewDto }) => response.data,
      invalidatesTags: ['Reviews', 'Product'],
    }),
    bulkApproveAdminReviews: builder.mutation<void, number[]>({
      query: (ids) => ({
        url: '/admin/reviews/bulk-approve',
        method: 'PUT',
        body: { ids },
      }),
      invalidatesTags: ['Reviews', 'Product'],
    }),
    getAdminNotificationTemplates: builder.query<NotificationTemplate[], void>({
      query: () => '/admin/notifications/templates',
      transformResponse: (response: { data: NotificationTemplate[] }) => response.data,
      providesTags: ['Notifications'],
    }),
    updateAdminNotificationTemplate: builder.mutation<
      NotificationTemplate,
      { type: string; data: UpdateNotificationTemplateRequest }
    >({
      query: ({ type, data }) => ({
        url: `/admin/notifications/templates/${type}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: NotificationTemplate }) => response.data,
      invalidatesTags: ['Notifications'],
    }),
    getAdminAnnouncements: builder.query<AdminAnnouncement[], number | void>({
      query: (take = 20) => ({
        url: '/admin/announcements',
        params: { take },
      }),
      transformResponse: (response: { data: AdminAnnouncement[] }) => response.data,
      providesTags: ['Announcements'],
    }),
    createAdminAnnouncement: builder.mutation<AdminAnnouncement, CreateAdminAnnouncementRequest>({
      query: (data) => ({
        url: '/admin/announcements',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: AdminAnnouncement }) => response.data,
      invalidatesTags: ['Announcements', 'Notifications'],
    }),
  }),
});

export const {
  useGetAdminDashboardKpiQuery,
  useGetAdminDashboardRevenueTrendQuery,
  useGetAdminDashboardCategorySalesQuery,
  useGetAdminDashboardUserRegistrationsQuery,
  useGetAdminDashboardOrderStatusDistributionQuery,
  useGetAdminDashboardLowStockQuery,
  useGetAdminDashboardRecentOrdersQuery,
  useGetCategoriesQuery,
  useGetAdminCategoriesQuery,
  useCreateCategoryMutation,
  useUpdateCategoryMutation,
  useReorderCategoriesMutation,
  useDeleteCategoryMutation,
  useGetAddressesQuery,
  useCreateAddressMutation,
  useDeleteAddressMutation,
  useUpdateAddressMutation,
  useGetAdminOrdersQuery,
  useUpdateOrderStatusMutation,
  useGetAdminReturnsQuery,
  useApproveAdminReturnMutation,
  useRejectAdminReturnMutation,
  useGetAdminSellersQuery,
  useGetAdminSellerDetailQuery,
  useGetAdminFinanceSummaryQuery,
  useUpdateAdminSellerStatusMutation,
  useUpdateAdminSellerCommissionMutation,
  useApproveAdminSellerApplicationMutation,
  useRejectAdminSellerApplicationMutation,
  useGetAdminUsersQuery,
  useGetAdminUserDetailQuery,
  useGetAdminSystemHealthQuery,
  useGetAdminErrorLogsQuery,
  useUpdateAdminUserRoleMutation,
  useUpdateAdminUserStatusMutation,
  useGetAdminReviewsQuery,
  useApproveAdminReviewMutation,
  useRejectAdminReviewMutation,
  useBulkApproveAdminReviewsMutation,
  useGetAdminNotificationTemplatesQuery,
  useUpdateAdminNotificationTemplateMutation,
  useGetAdminAnnouncementsQuery,
  useCreateAdminAnnouncementMutation,
} = adminApi;
