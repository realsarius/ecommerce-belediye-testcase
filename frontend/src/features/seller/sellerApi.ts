import { baseApi } from '@/app/api';
import type { Product, ProductListRequest, CreateProductRequest, UpdateProductRequest } from '@/features/products/types';
import type { PaginatedResponse } from '@/types/api';
import type { Order } from '@/features/orders/types';
import type {
  SellerProfile,
  CreateSellerProfileRequest,
  UpdateSellerProfileRequest,
  HasProfileResponse,
  SellerAnalyticsSummary,
  SellerAnalyticsTrendPoint,
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

    getSellerAnalyticsTrends: builder.query<SellerAnalyticsTrendPoint[], number | void>({
      query: (days = 30) => ({
        url: '/seller/analytics/trends',
        params: { days },
      }),
      transformResponse: (response: { data: SellerAnalyticsTrendPoint[] }) => response.data,
      providesTags: ['SellerAnalytics'],
    }),

    getSellerOrders: builder.query<Order[], void>({
      query: () => '/admin/orders',
      transformResponse: (response: Order[] | { data: Order[] }) => unwrapApiData(response),
      providesTags: ['Orders'],
    }),

    // Seller Products endpoints (uses admin/products but filtered for seller)
    getSellerProducts: builder.query<PaginatedResponse<Product>, ProductListRequest>({
      query: (params) => ({
        url: '/admin/products',
        params,
      }),
      transformResponse: (response: { data: PaginatedResponse<Product> }) => response.data,
      providesTags: ['SellerProducts'],
    }),

    createSellerProduct: builder.mutation<Product, CreateProductRequest>({
      query: (data) => ({
        url: '/admin/products',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: Product }) => response.data,
      invalidatesTags: ['SellerProducts', 'Products'],
    }),

    updateSellerProduct: builder.mutation<Product, { id: number; data: UpdateProductRequest }>({
      query: ({ id, data }) => ({
        url: `/admin/products/${id}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: Product }) => response.data,
      invalidatesTags: ['SellerProducts', 'Products'],
    }),

    deleteSellerProduct: builder.mutation<void, number>({
      query: (id) => ({
        url: `/admin/products/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['SellerProducts', 'Products'],
    }),
  }),
});

export const {
  useGetSellerProfileQuery,
  useCheckSellerProfileQuery,
  useCreateSellerProfileMutation,
  useUpdateSellerProfileMutation,
  useGetSellerAnalyticsSummaryQuery,
  useGetSellerAnalyticsTrendsQuery,
  useGetSellerOrdersQuery,
  useGetSellerProductsQuery,
  useCreateSellerProductMutation,
  useUpdateSellerProductMutation,
  useDeleteSellerProductMutation,
} = sellerApi;
