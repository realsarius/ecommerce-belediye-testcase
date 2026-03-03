import { baseApi } from '@/app/api';
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

export const sellerApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    // Seller Profile endpoints
    getSellerProfile: builder.query<SellerProfile, void>({
      query: () => '/seller/profile',
      transformResponse: (response: { data: SellerProfile }) => response.data,
      providesTags: ['SellerProfile'],
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
      invalidatesTags: ['SellerProfile'],
    }),

    updateSellerProfile: builder.mutation<SellerProfile, UpdateSellerProfileRequest>({
      query: (data) => ({
        url: '/seller/profile',
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: SellerProfile }) => response.data,
      invalidatesTags: ['SellerProfile'],
    }),

    getSellerAnalyticsSummary: builder.query<SellerAnalyticsSummary, void>({
      query: () => '/seller/analytics/summary',
      transformResponse: (response: { data: SellerAnalyticsSummary }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerDashboardKpi: builder.query<SellerDashboardKpi, number | void>({
      query: (days = 30) => ({
        url: '/seller/dashboard/kpi',
        params: { days },
      }),
      transformResponse: (response: { data: SellerDashboardKpi }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerDashboardRevenueTrend: builder.query<SellerDashboardRevenueTrendPoint[], { period?: 'daily' | 'weekly' | 'monthly' } | void>({
      query: (params) => ({
        url: '/seller/dashboard/revenue-trend',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: SellerDashboardRevenueTrendPoint[] }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerDashboardOrderStatusDistribution: builder.query<SellerDashboardOrderStatusDistributionItem[], void>({
      query: () => '/seller/dashboard/order-status-distribution',
      transformResponse: (response: { data: SellerDashboardOrderStatusDistributionItem[] }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerDashboardProductPerformance: builder.query<SellerDashboardProductPerformanceItem[], number | void>({
      query: (take = 5) => ({
        url: '/seller/dashboard/product-performance',
        params: { take },
      }),
      transformResponse: (response: { data: SellerDashboardProductPerformanceItem[] }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerDashboardRecentOrders: builder.query<SellerDashboardRecentOrder[], number | void>({
      query: (take = 5) => ({
        url: '/seller/dashboard/recent-orders',
        params: { take },
      }),
      transformResponse: (response: { data: SellerDashboardRecentOrder[] }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerAnalyticsTrends: builder.query<SellerAnalyticsTrendPoint[], number | void>({
      query: (days = 30) => ({
        url: '/seller/analytics/trends',
        params: { days },
      }),
      transformResponse: (response: { data: SellerAnalyticsTrendPoint[] }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerFinanceSummary: builder.query<SellerFinanceSummary, number | void>({
      query: (days = 30) => ({
        url: '/seller/analytics/finance',
        params: { days },
      }),
      transformResponse: (response: { data: SellerFinanceSummary }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerOrders: builder.query<Order[], void>({
      query: () => '/seller/orders',
      transformResponse: (response: Order[] | { data: Order[] }) => unwrapApiData(response),
      providesTags: ['Orders'],
    }),

    shipSellerOrder: builder.mutation<Order, { id: number; trackingCode: string; cargoCompany: string }>({
      query: ({ id, trackingCode, cargoCompany }) => ({
        url: `/seller/orders/${id}/ship`,
        method: 'PUT',
        body: { trackingCode, cargoCompany },
      }),
      transformResponse: (response: Order | { data: Order }) => unwrapApiData(response),
      invalidatesTags: ['Orders'],
    }),

    // Seller Products endpoints (uses admin/products but filtered for seller)
    getSellerProducts: builder.query<PaginatedResponse<Product>, ProductListRequest>({
      query: (params) => ({
        url: '/seller/products',
        params,
      }),
      transformResponse: (response: { data: PaginatedResponse<Product> }) => response.data,
      providesTags: ['SellerProducts'],
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
      invalidatesTags: ['SellerProducts', 'Products'],
    }),

    updateSellerProduct: builder.mutation<Product, { id: number; data: UpdateProductRequest }>({
      query: ({ id, data }) => ({
        url: `/seller/products/${id}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: Product }) => response.data,
      invalidatesTags: ['SellerProducts', 'Products'],
    }),

    deleteSellerProduct: builder.mutation<void, number>({
      query: (id) => ({
        url: `/seller/products/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['SellerProducts', 'Products'],
    }),

    getSellerReviews: builder.query<ProductReviewDto[], { productId?: number; rating?: number; replied?: boolean } | void>({
      query: (params) => ({
        url: '/seller/reviews',
        params: params ?? undefined,
      }),
      transformResponse: (response: { data: ProductReviewDto[] }) => response.data,
      providesTags: ['Reviews'],
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
