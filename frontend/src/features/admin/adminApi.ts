import { baseApi } from '@/app/api';
import type { PaginatedResponse } from '@/types/api';
import type { DashboardPeriod } from '@/types/chart';
import type {
  Category,
  Product,
  ProductListRequest,
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
  AdminOrdersQueryParams,
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

type AdminGranularTag =
  | 'Categories'
  | 'Addresses'
  | 'Orders'
  | 'Returns'
  | 'Products'
  | 'SellerProfile'
  | 'Users'
  | 'Reviews'
  | 'Announcements';

function createListTags<T extends { id: number | string }, TTag extends AdminGranularTag>(type: TTag, items?: T[]) {
  if (!items?.length) {
    return [{ type, id: 'LIST' }] as const;
  }

  return [
    { type, id: 'LIST' },
    ...items.map((item) => ({ type, id: item.id })),
  ] as const;
}

function createPaginatedListTags<T extends { id: number | string }, TTag extends AdminGranularTag>(
  type: TTag,
  result?: PaginatedResponse<T>
) {
  return createListTags(type, result?.items);
}

export const adminApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getAdminDashboardKpi: builder.query<AdminDashboardKpi, void>({
      query: () => '/admin/dashboard/kpi',
      transformResponse: (response: { data: AdminDashboardKpi }) => response.data,
      providesTags: ['Orders', 'Users', 'Products', 'Categories'],
    }),
    getAdminDashboardRevenueTrend: builder.query<AdminDashboardRevenueTrendPoint[], { period?: DashboardPeriod } | void>({
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
      providesTags: (result) => createListTags('Categories', result),
    }),
    getAdminCategories: builder.query<Category[], void>({
      query: () => '/admin/categories',
      transformResponse: (response: { data: Category[] }) => response.data,
      providesTags: (result) => createListTags('Categories', result),
    }),
    getAdminProducts: builder.query<PaginatedResponse<Product>, ProductListRequest | void>({
      query: (params) => ({
        url: '/admin/products',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: PaginatedResponse<Product> }) => response.data,
      providesTags: (result) => createPaginatedListTags('Products', result),
    }),
    createCategory: builder.mutation<Category, CreateCategoryRequest>({
      query: (data) => ({
        url: '/admin/categories',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: Category }) => response.data,
      invalidatesTags: (result) => result
        ? [{ type: 'Categories', id: result.id }, { type: 'Categories', id: 'LIST' }]
        : [{ type: 'Categories', id: 'LIST' }],
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
      invalidatesTags: (_result, _error, id) => [{ type: 'Categories', id }, { type: 'Categories', id: 'LIST' }],
    }),
    getAddresses: builder.query<ShippingAddress[], void>({
      query: () => '/shippingaddress',
      providesTags: (result) => createListTags('Addresses', result),
    }),
    createAddress: builder.mutation<ShippingAddress, CreateShippingAddressRequest>({
      query: (data) => ({
        url: '/shippingaddress',
        method: 'POST',
        body: data,
      }),
      invalidatesTags: [{ type: 'Addresses', id: 'LIST' }],
    }),
    deleteAddress: builder.mutation<void, number>({
      query: (id) => ({
        url: `/shippingaddress/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, id) => [{ type: 'Addresses', id }, { type: 'Addresses', id: 'LIST' }],
    }),
    updateAddress: builder.mutation<ShippingAddress, { id: number; data: CreateShippingAddressRequest }>({
      query: ({ id, data }) => ({
        url: `/shippingaddress/${id}`,
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: (_result, _error, { id }) => [{ type: 'Addresses', id }, { type: 'Addresses', id: 'LIST' }],
    }),
    getAdminOrders: builder.query<Order[], AdminOrdersQueryParams | void>({
      query: (params) => ({
        url: '/admin/orders',
        params: {
          status: params?.status || undefined,
          minAmount: params?.minAmount,
          from: params?.from || undefined,
          to: params?.to || undefined,
        },
      }),
      transformResponse: (response: Order[] | { data: Order[] }) => unwrapApiData(response),
      providesTags: (result) => createListTags('Orders', result),
    }),
    getAdminOrderDetail: builder.query<Order, number>({
      query: (id) => `/admin/orders/${id}`,
      transformResponse: (response: Order | { data: Order }) => unwrapApiData(response),
      providesTags: (_result, _error, id) => [{ type: 'Orders', id }],
    }),
    updateOrderStatus: builder.mutation<Order, { id: number; status: string }>({
      query: ({ id, status }) => ({
        url: `/admin/orders/${id}/status`,
        method: 'PATCH',
        body: { status },
      }),
      transformResponse: (response: Order | { data: Order }) => unwrapApiData(response),
      invalidatesTags: (_result, _error, { id }) => [{ type: 'Orders', id }, { type: 'Orders', id: 'LIST' }],
    }),
    getAdminReturns: builder.query<ReturnRequest[], { status?: string } | void>({
      query: (params) => ({
        url: '/admin/returns',
        params: params?.status ? { status: params.status } : undefined,
      }),
      transformResponse: (response: { data: ReturnRequest[] }) => response.data,
      providesTags: (result) => createListTags('Returns', result),
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
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Returns', id },
        { type: 'Returns', id: 'LIST' },
        { type: 'Orders', id: 'LIST' },
        'Loyalty',
        'GiftCards',
        'Referrals',
      ],
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
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Returns', id },
        { type: 'Returns', id: 'LIST' },
        { type: 'Orders', id: 'LIST' },
        'Loyalty',
        'GiftCards',
        'Referrals',
      ],
    }),
    getAdminSellers: builder.query<AdminSellerListItem[], { status?: string } | void>({
      query: (params) => ({
        url: '/admin/sellers',
        params: params?.status ? { status: params.status } : undefined,
      }),
      transformResponse: (response: { data: AdminSellerListItem[] }) => response.data,
      providesTags: (result) => createListTags('SellerProfile', result),
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
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'SellerProfile', id },
        { type: 'SellerProfile', id: 'LIST' },
        { type: 'Users', id: 'LIST' },
      ],
    }),
    updateAdminSellerCommission: builder.mutation<AdminSellerDetail, { id: number; rate?: number | null }>({
      query: ({ id, rate }) => ({
        url: `/admin/sellers/${id}/commission`,
        method: 'PUT',
        body: { rate },
      }),
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      invalidatesTags: (_result, _error, { id }) => [{ type: 'SellerProfile', id }, { type: 'SellerProfile', id: 'LIST' }],
    }),
    approveAdminSellerApplication: builder.mutation<AdminSellerDetail, { id: number; reviewNote?: string }>({
      query: ({ id, reviewNote }) => ({
        url: `/admin/sellers/applications/${id}/approve`,
        method: 'PUT',
        body: { reviewNote },
      }),
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'SellerProfile', id },
        { type: 'SellerProfile', id: 'LIST' },
        { type: 'Users', id: 'LIST' },
      ],
    }),
    rejectAdminSellerApplication: builder.mutation<AdminSellerDetail, { id: number; reviewNote?: string }>({
      query: ({ id, reviewNote }) => ({
        url: `/admin/sellers/applications/${id}/reject`,
        method: 'PUT',
        body: { reviewNote },
      }),
      transformResponse: (response: { data: AdminSellerDetail }) => response.data,
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'SellerProfile', id },
        { type: 'SellerProfile', id: 'LIST' },
        { type: 'Users', id: 'LIST' },
      ],
    }),
    getAdminUsers: builder.query<PaginatedResponse<AdminUserListItem>, AdminUsersQueryParams | void>({
      query: (params) => ({
        url: '/admin/users',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: PaginatedResponse<AdminUserListItem> }) => response.data,
      providesTags: (result) => createPaginatedListTags('Users', result),
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
      invalidatesTags: (_result, _error, { id }) => [{ type: 'Users', id }, { type: 'Users', id: 'LIST' }],
    }),
    updateAdminUserStatus: builder.mutation<AdminUserDetail, { id: number; status: string }>({
      query: ({ id, status }) => ({
        url: `/admin/users/${id}/status`,
        method: 'PUT',
        body: { status },
      }),
      transformResponse: (response: { data: AdminUserDetail }) => response.data,
      invalidatesTags: (_result, _error, { id }) => [{ type: 'Users', id }, { type: 'Users', id: 'LIST' }],
    }),
    getAdminReviews: builder.query<ProductReviewDto[], { status?: string } | void>({
      query: (params) => ({
        url: '/admin/reviews',
        params: params?.status ? { status: params.status } : undefined,
      }),
      transformResponse: (response: { data: ProductReviewDto[] }) => response.data,
      providesTags: (result) => createListTags('Reviews', result),
    }),
    approveAdminReview: builder.mutation<ProductReviewDto, number>({
      query: (id) => ({
        url: `/admin/reviews/${id}/approve`,
        method: 'PUT',
      }),
      transformResponse: (response: { data: ProductReviewDto }) => response.data,
      invalidatesTags: (_result, _error, id) => [{ type: 'Reviews', id }, { type: 'Reviews', id: 'LIST' }, 'Product'],
    }),
    rejectAdminReview: builder.mutation<ProductReviewDto, { id: number; data: ReviewModerationRequest }>({
      query: ({ id, data }) => ({
        url: `/admin/reviews/${id}/reject`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: ProductReviewDto }) => response.data,
      invalidatesTags: (_result, _error, { id }) => [{ type: 'Reviews', id }, { type: 'Reviews', id: 'LIST' }, 'Product'],
    }),
    bulkApproveAdminReviews: builder.mutation<void, number[]>({
      query: (ids) => ({
        url: '/admin/reviews/bulk-approve',
        method: 'PUT',
        body: { ids },
      }),
      invalidatesTags: [{ type: 'Reviews', id: 'LIST' }, 'Product'],
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
      providesTags: (result) => createListTags('Announcements', result),
    }),
    createAdminAnnouncement: builder.mutation<AdminAnnouncement, CreateAdminAnnouncementRequest>({
      query: (data) => ({
        url: '/admin/announcements',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: AdminAnnouncement }) => response.data,
      invalidatesTags: [{ type: 'Announcements', id: 'LIST' }, { type: 'Notifications', id: 'LIST' }],
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
  useGetAdminProductsQuery,
  useCreateCategoryMutation,
  useUpdateCategoryMutation,
  useReorderCategoriesMutation,
  useDeleteCategoryMutation,
  useGetAddressesQuery,
  useCreateAddressMutation,
  useDeleteAddressMutation,
  useUpdateAddressMutation,
  useGetAdminOrdersQuery,
  useGetAdminOrderDetailQuery,
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
