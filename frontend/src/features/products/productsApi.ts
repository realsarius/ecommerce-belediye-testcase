import { baseApi } from '@/app/api';
import type {
  Product,
  ProductListRequest,
  CreateProductRequest,
  UpdateProductRequest,
  UpdateStockRequest,
  ProductReviewDto,
  ReviewSummaryDto,
  CreateReviewRequest,
} from './types';
import type { PaginatedResponse } from '@/types/api';

export const productsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getProducts: builder.query<PaginatedResponse<Product>, ProductListRequest>({
      query: (params) => ({
        url: '/products',
        params,
      }),
      transformResponse: (response: { data: PaginatedResponse<Product> }) => response.data,
      providesTags: ['Products'],
    }),
    getProduct: builder.query<Product, number>({
      query: (id) => `/products/${id}`,
      transformResponse: (response: { data: Product }) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'Product', id }],
    }),
    createProduct: builder.mutation<Product, CreateProductRequest>({
      query: (data) => ({
        url: '/admin/products',
        method: 'POST',
        body: data,
      }),
      invalidatesTags: ['Products'],
    }),
    updateProduct: builder.mutation<Product, { id: number; data: UpdateProductRequest }>({
      query: ({ id, data }) => ({
        url: `/admin/products/${id}`,
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: (_result, _error, { id }) => [
        'Products',
        { type: 'Product', id },
      ],
    }),
    deleteProduct: builder.mutation<void, number>({
      query: (id) => ({
        url: `/admin/products/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Products'],
    }),
    updateStock: builder.mutation<void, { id: number; data: UpdateStockRequest }>({
      query: ({ id, data }) => ({
        url: `/admin/products/${id}/stock`,
        method: 'PATCH',
        body: data,
      }),
      invalidatesTags: (_result, _error, { id }) => [
        'Products',
        { type: 'Product', id },
      ],
    }),
    searchProducts: builder.query<PaginatedResponse<Product>, ProductListRequest>({
      query: ({ search, ...params }) => ({
        url: '/search/products',
        params: {
          ...params,
          q: search || undefined,
        },
      }),
      transformResponse: (response: { data: PaginatedResponse<Product> }) => response.data,
      providesTags: ['Products'],
    }),
    searchSuggestions: builder.query<Product[], { q: string; limit?: number }>({
      query: ({ q, limit = 8 }) => ({
        url: '/search/suggestions',
        params: { q, limit },
      }),
      transformResponse: (response: { data: Product[] }) => response.data,
      providesTags: ['Products'],
    }),

    // --- Product Reviews ---
    getProductReviews: builder.query<ProductReviewDto[], number>({
      query: (productId) => `/products/${productId}/reviews`,
      transformResponse: (response: { data: ProductReviewDto[] }) => response.data,
      providesTags: (_result, _error, productId) => [{ type: 'Product', id: `reviews-${productId}` }],
    }),
    getReviewSummary: builder.query<ReviewSummaryDto, number>({
      query: (productId) => `/products/${productId}/reviews/summary`,
      transformResponse: (response: { data: ReviewSummaryDto }) => response.data,
      providesTags: (_result, _error, productId) => [{ type: 'Product', id: `summary-${productId}` }],
    }),
    createReview: builder.mutation<ProductReviewDto, { productId: number; data: CreateReviewRequest }>({
      query: ({ productId, data }) => ({
        url: `/products/${productId}/reviews`,
        method: 'POST',
        body: data,
      }),
      invalidatesTags: (_result, _error, { productId }) => [
        { type: 'Product', id: `reviews-${productId}` },
        { type: 'Product', id: `summary-${productId}` },
      ],
    }),
    updateReview: builder.mutation<ProductReviewDto, { productId: number; reviewId: number; data: { rating: number; comment: string } }>({
      query: ({ productId, reviewId, data }) => ({
        url: `/products/${productId}/reviews/${reviewId}`,
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: (_result, _error, { productId }) => [
        { type: 'Product', id: `reviews-${productId}` },
        { type: 'Product', id: `summary-${productId}` },
      ],
    }),
    deleteReview: builder.mutation<void, { productId: number; reviewId: number }>({
      query: ({ productId, reviewId }) => ({
        url: `/products/${productId}/reviews/${reviewId}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { productId }) => [
        { type: 'Product', id: `reviews-${productId}` },
        { type: 'Product', id: `summary-${productId}` },
      ],
    }),
    canUserReview: builder.query<boolean, number>({
      query: (productId) => `/products/${productId}/reviews/can-review`,
      transformResponse: (response: { data: boolean }) => response.data,
      providesTags: (_result, _error, productId) => [{ type: 'Product', id: `canreview-${productId}` }],
    }),
  }),
});

export const {
  useGetProductsQuery,
  useGetProductQuery,
  useCreateProductMutation,
  useUpdateProductMutation,
  useDeleteProductMutation,
  useUpdateStockMutation,
  useSearchProductsQuery,
  useSearchSuggestionsQuery,
  useGetProductReviewsQuery,
  useGetReviewSummaryQuery,
  useCreateReviewMutation,
  useUpdateReviewMutation,
  useDeleteReviewMutation,
  useCanUserReviewQuery,
} = productsApi;
