import { baseApi } from '@/app/api';
import type {
  Product,
  ProductListRequest,
  CreateProductRequest,
  UpdateProductRequest,
  UpdateStockRequest,
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
  }),
});

export const {
  useGetProductsQuery,
  useGetProductQuery,
  useCreateProductMutation,
  useUpdateProductMutation,
  useDeleteProductMutation,
  useUpdateStockMutation,
} = productsApi;
