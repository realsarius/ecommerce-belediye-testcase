import { baseApi } from '@/app/api';
import type { DashboardPeriod } from '@/types/chart';
import type {
  Product,
  ProductListRequest,
  CreateProductRequest,
  UpdateProductRequest,
  ProductReviewDto,
  SellerReviewReplyRequest,
} from '@/features/products/types';
import type { PaginatedResponse } from '@/types/api';
import type { Order } from '@/features/orders/types';
import type {
  SellerProfile,
  CreateSellerProfileRequest,
  UpdateSellerProfileRequest,
  HasProfileResponse,
  SellerDashboardKpi,
  SellerDashboardOrderStatusDistributionItem,
  SellerDashboardProductPerformanceItem,
  SellerDashboardRecentOrder,
  SellerDashboardRevenueTrendPoint,
  SellerAnalyticsSummary,
  SellerAnalyticsTrendPoint,
  SellerFinanceSummary,
} from './types';

function unwrapApiData<T>(response: T | { data: T }) {
  return (response as { data?: T }).data ?? (response as T);
}

type SellerGranularTag = 'Orders' | 'SellerProducts' | 'Reviews';

function createListTags<T extends { id: number | string }, TTag extends SellerGranularTag>(type: TTag, items?: T[]) {
  if (!items?.length) {
    return [{ type, id: 'LIST' }] as const;
  }

  return [
    { type, id: 'LIST' },
    ...items.map((item) => ({ type, id: item.id })),
  ] as const;
}

function createPaginatedListTags<T extends { id: number | string }, TTag extends SellerGranularTag>(
  type: TTag,
  result?: PaginatedResponse<T>
) {
  return createListTags(type, result?.items);
}

export interface SellerFinanceSummaryQuery {
  days?: number;
  from?: string;
  to?: string;
}

export const sellerApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    // Seller Profile endpoints
    getSellerProfile: builder.query<SellerProfile, void>({
      query: () => '/seller/profile',
      transformResponse: (response: { data: SellerProfile }) => response.data,
      providesTags: (result) => result ? [{ type: 'SellerProfile', id: result.id }] : [{ type: 'SellerProfile', id: 'LIST' }],
    }),
    
    checkSellerProfile: builder.query<HasProfileResponse, void>({
      query: () => '/seller/profile/exists',
    }),

    createSellerProfile: builder.mutation<SellerProfile, CreateSellerProfileRequest>({
      query: (data) => ({
        url: '/seller/profile',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: SellerProfile }) => response.data,
      invalidatesTags: [{ type: 'SellerProfile', id: 'LIST' }],
    }),

    updateSellerProfile: builder.mutation<SellerProfile, UpdateSellerProfileRequest>({
      query: (data) => ({
        url: '/seller/profile',
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: SellerProfile }) => response.data,
      invalidatesTags: (result) => result ? [{ type: 'SellerProfile', id: result.id }, { type: 'SellerProfile', id: 'LIST' }] : [{ type: 'SellerProfile', id: 'LIST' }],
    }),

    getSellerAnalyticsSummary: builder.query<SellerAnalyticsSummary, void>({
      query: () => '/seller/analytics/summary',
      transformResponse: (response: { data: SellerAnalyticsSummary }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'SUMMARY' }],
    }),

    getSellerDashboardKpi: builder.query<SellerDashboardKpi, number | void>({
      query: (days = 30) => ({
        url: '/seller/dashboard/kpi',
        params: { days },
      }),
      transformResponse: (response: { data: SellerDashboardKpi }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'DASHBOARD_KPI' }],
    }),

    getSellerDashboardRevenueTrend: builder.query<SellerDashboardRevenueTrendPoint[], { period?: DashboardPeriod } | void>({
      query: (params) => ({
        url: '/seller/dashboard/revenue-trend',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: SellerDashboardRevenueTrendPoint[] }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'DASHBOARD_REVENUE' }],
    }),

    getSellerDashboardOrderStatusDistribution: builder.query<SellerDashboardOrderStatusDistributionItem[], void>({
      query: () => '/seller/dashboard/order-status-distribution',
      transformResponse: (response: { data: SellerDashboardOrderStatusDistributionItem[] }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'DASHBOARD_STATUS' }],
    }),

    getSellerDashboardProductPerformance: builder.query<SellerDashboardProductPerformanceItem[], number | void>({
      query: (take = 5) => ({
        url: '/seller/dashboard/product-performance',
        params: { take },
      }),
      transformResponse: (response: { data: SellerDashboardProductPerformanceItem[] }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'DASHBOARD_PRODUCTS' }],
    }),

    getSellerDashboardRecentOrders: builder.query<SellerDashboardRecentOrder[], number | void>({
      query: (take = 5) => ({
        url: '/seller/dashboard/recent-orders',
        params: { take },
      }),
      transformResponse: (response: { data: SellerDashboardRecentOrder[] }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'DASHBOARD_ORDERS' }],
    }),

    getSellerAnalyticsTrends: builder.query<SellerAnalyticsTrendPoint[], number | void>({
      query: (days = 30) => ({
        url: '/seller/analytics/trends',
        params: { days },
      }),
      transformResponse: (response: { data: SellerAnalyticsTrendPoint[] }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'TRENDS' }],
    }),

    getSellerFinanceSummary: builder.query<SellerFinanceSummary, SellerFinanceSummaryQuery | void>({
      query: (params) => ({
        url: '/seller/analytics/finance',
        params: params ?? { days: 30 },
      }),
      transformResponse: (response: { data: SellerFinanceSummary }) => response.data,
      providesTags: [{ type: 'SellerAnalytics', id: 'FINANCE' }],
    }),

    getSellerOrders: builder.query<Order[], void>({
      query: () => '/seller/orders',
      transformResponse: (response: Order[] | { data: Order[] }) => unwrapApiData(response),
      providesTags: (result) => createListTags('Orders', result),
    }),

    shipSellerOrder: builder.mutation<Order, { id: number; trackingCode: string; cargoCompany: string; estimatedDeliveryDate?: string }>({
      query: ({ id, trackingCode, cargoCompany, estimatedDeliveryDate }) => ({
        url: `/seller/orders/${id}/ship`,
        method: 'PUT',
        body: { trackingCode, cargoCompany, estimatedDeliveryDate },
      }),
      transformResponse: (response: Order | { data: Order }) => unwrapApiData(response),
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Orders', id },
        { type: 'Orders', id: 'LIST' },
        { type: 'SellerAnalytics', id: 'DASHBOARD_KPI' },
        { type: 'SellerAnalytics', id: 'DASHBOARD_STATUS' },
        { type: 'SellerAnalytics', id: 'DASHBOARD_ORDERS' },
      ],
    }),

    // Seller Products endpoints (uses admin/products but filtered for seller)
    getSellerProducts: builder.query<PaginatedResponse<Product>, ProductListRequest>({
      query: (params) => ({
        url: '/seller/products',
        params,
      }),
      transformResponse: (response: { data: PaginatedResponse<Product> }) => response.data,
      providesTags: (result) => createPaginatedListTags('SellerProducts', result),
    }),

    getSellerProduct: builder.query<Product, number>({
      query: (id) => `/seller/products/${id}`,
      transformResponse: (response: { data: Product }) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'SellerProducts', id }],
    }),

    createSellerProduct: builder.mutation<Product, CreateProductRequest>({
      query: (data) => ({
        url: '/seller/products',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: Product }) => response.data,
      invalidatesTags: [{ type: 'SellerProducts', id: 'LIST' }, 'Products'],
    }),

    updateSellerProduct: builder.mutation<Product, { id: number; data: UpdateProductRequest }>({
      query: ({ id, data }) => ({
        url: `/seller/products/${id}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: Product }) => response.data,
      invalidatesTags: (_result, _error, { id }) => [{ type: 'SellerProducts', id }, { type: 'SellerProducts', id: 'LIST' }, 'Products'],
    }),

    deleteSellerProduct: builder.mutation<void, number>({
      query: (id) => ({
        url: `/seller/products/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, id) => [{ type: 'SellerProducts', id }, { type: 'SellerProducts', id: 'LIST' }, 'Products'],
    }),

    getSellerReviews: builder.query<ProductReviewDto[], { productId?: number; rating?: number; replied?: boolean } | void>({
      query: (params) => ({
        url: '/seller/reviews',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: ProductReviewDto[] }) => response.data,
      providesTags: (result) => createListTags('Reviews', result),
    }),

    replySellerReview: builder.mutation<ProductReviewDto, { reviewId: number; data: SellerReviewReplyRequest; productId: number }>({
      query: ({ reviewId, data }) => ({
        url: `/seller/reviews/${reviewId}/reply`,
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: ProductReviewDto }) => response.data,
      invalidatesTags: (_result, _error, { productId }) => [
        'Reviews',
        { type: 'Product', id: `reviews-${productId}` },
        { type: 'Product', id: `summary-${productId}` },
      ],
    }),
  }),
});

export const {
  useGetSellerProfileQuery,
  useCheckSellerProfileQuery,
  useCreateSellerProfileMutation,
  useUpdateSellerProfileMutation,
  useGetSellerAnalyticsSummaryQuery,
  useGetSellerDashboardKpiQuery,
  useGetSellerDashboardRevenueTrendQuery,
  useGetSellerDashboardOrderStatusDistributionQuery,
  useGetSellerDashboardProductPerformanceQuery,
  useGetSellerDashboardRecentOrdersQuery,
  useGetSellerAnalyticsTrendsQuery,
  useGetSellerFinanceSummaryQuery,
  useGetSellerOrdersQuery,
  useShipSellerOrderMutation,
  useGetSellerProductsQuery,
  useGetSellerProductQuery,
  useCreateSellerProductMutation,
  useUpdateSellerProductMutation,
  useDeleteSellerProductMutation,
  useGetSellerReviewsQuery,
  useReplySellerReviewMutation,
} = sellerApi;
