import { baseApi } from '@/app/api';
import type { PaginatedResponse } from '@/types/api';
import type {
  Category,
  CreateCategoryRequest,
  ProductReviewDto,
  ReviewModerationRequest,
  UpdateCategoryRequest,
} from '@/features/products/types';
import type {
  AdminUserDetail,
  AdminUserListItem,
  AdminUsersQueryParams,
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
import type { SellerProfile } from '@/features/seller/types';

function unwrapApiData<T>(response: T | { data: T }) {
  return (response as { data?: T }).data ?? (response as T);
}

export const adminApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
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
    getAdminReturns: builder.query<ReturnRequest[], void>({
      query: () => '/admin/returns',
      transformResponse: (response: { data: ReturnRequest[] }) => response.data,
      providesTags: ['Returns'],
    }),
    reviewAdminReturn: builder.mutation<
      ReturnRequest,
      { id: number; status: 'Approved' | 'Rejected'; reviewNote?: string }
    >({
      query: ({ id, ...body }) => ({
        url: `/admin/returns/${id}`,
        method: 'PATCH',
        body,
      }),
      transformResponse: (response: { data: ReturnRequest }) => response.data,
      invalidatesTags: ['Returns', 'Orders', 'Loyalty', 'GiftCards', 'Referrals'],
    }),
    getAdminSellerProfile: builder.query<SellerProfile, number>({
      query: (id) => `/admin/sellers/${id}`,
      transformResponse: (response: { data: SellerProfile }) => response.data,
      providesTags: ['SellerProfile'],
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
  }),
});

export const {
  useGetCategoriesQuery,
  useGetAdminCategoriesQuery,
  useCreateCategoryMutation,
  useUpdateCategoryMutation,
  useDeleteCategoryMutation,
  useGetAddressesQuery,
  useCreateAddressMutation,
  useDeleteAddressMutation,
  useUpdateAddressMutation,
  useGetAdminOrdersQuery,
  useUpdateOrderStatusMutation,
  useGetAdminReturnsQuery,
  useReviewAdminReturnMutation,
  useGetAdminSellerProfileQuery,
  useGetAdminUsersQuery,
  useGetAdminUserDetailQuery,
  useUpdateAdminUserRoleMutation,
  useUpdateAdminUserStatusMutation,
  useGetAdminReviewsQuery,
  useApproveAdminReviewMutation,
  useRejectAdminReviewMutation,
  useBulkApproveAdminReviewsMutation,
  useGetAdminNotificationTemplatesQuery,
  useUpdateAdminNotificationTemplateMutation,
} = adminApi;
