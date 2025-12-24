import { baseApi } from '@/app/api';
import type { Order, Payment, ProcessPaymentRequest } from '@/features/orders/types';
import type { CheckoutRequest } from '@/features/cart/types';

export const ordersApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    checkout: builder.mutation<Order, CheckoutRequest>({
      query: (data) => ({
        url: '/orders/checkout',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: Order }) => response.data,
      invalidatesTags: ['Cart', 'Orders'],
    }),
    getOrders: builder.query<Order[], void>({
      query: () => '/orders',
      transformResponse: (response: { data: Order[] }) => response.data,
      providesTags: ['Orders'],
    }),
    getOrder: builder.query<Order, number>({
      query: (id) => `/orders/${id}`,
      transformResponse: (response: { data: Order }) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'Order', id }],
    }),
    cancelOrder: builder.mutation<Order, number>({
      query: (id) => ({
        url: `/orders/${id}/cancel`,
        method: 'POST',
      }),
      transformResponse: (response: { data: Order }) => response.data,
      invalidatesTags: ['Orders'],
    }),
    processPayment: builder.mutation<Payment, ProcessPaymentRequest>({
      query: (data) => ({
        url: '/payments',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: Payment }) => response.data,
      invalidatesTags: ['Orders'],
    }),
  }),
});

export const {
  useCheckoutMutation,
  useGetOrdersQuery,
  useGetOrderQuery,
  useCancelOrderMutation,
  useProcessPaymentMutation,
} = ordersApi;
