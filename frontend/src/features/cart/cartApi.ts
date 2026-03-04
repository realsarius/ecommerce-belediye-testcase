import { baseApi } from '@/app/api';
import type { Cart, AddToCartRequest, ReorderCartRequest, ReorderCartResult, UpdateCartItemRequest } from './types';

export const cartApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getCart: builder.query<Cart, void>({
      query: () => '/cart',
      transformResponse: (response: { data: Cart }) => response.data,
      providesTags: ['Cart'],
    }),
    addToCart: builder.mutation<Cart, AddToCartRequest>({
      query: (data) => ({
        url: '/cart/items',
        method: 'POST',
        body: data,
      }),
      invalidatesTags: ['Cart'],
    }),
    reorderCart: builder.mutation<ReorderCartResult, ReorderCartRequest>({
      query: (data) => ({
        url: '/cart/reorder',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: ReorderCartResult }) => response.data,
      invalidatesTags: ['Cart'],
    }),
    updateCartItem: builder.mutation<Cart, { productId: number; data: UpdateCartItemRequest }>({
      query: ({ productId, data }) => ({
        url: `/cart/items/${productId}`,
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: ['Cart'],
    }),
    removeFromCart: builder.mutation<Cart, number>({
      query: (productId) => ({
        url: `/cart/items/${productId}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Cart'],
    }),
    clearCart: builder.mutation<void, void>({
      query: () => ({
        url: '/cart',
        method: 'DELETE',
      }),
      invalidatesTags: ['Cart'],
    }),
  }),
});

export const {
  useGetCartQuery,
  useAddToCartMutation,
  useReorderCartMutation,
  useUpdateCartItemMutation,
  useRemoveFromCartMutation,
  useClearCartMutation,
} = cartApi;
