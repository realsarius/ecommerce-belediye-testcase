import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

interface AuthState {
  token: string | null;
}

const baseQuery = fetchBaseQuery({
  baseUrl: import.meta.env.VITE_API_URL || '/api/v1',
  prepareHeaders: (headers, { getState }) => {
    const state = getState() as { auth: AuthState };
    const token = state.auth.token;
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    headers.set('Content-Type', 'application/json');
    return headers;
  },
});

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery,
  tagTypes: [
    'Products',
    'Product',
    'Cart',
    'Orders',
    'Order',
    'Categories',
    'Addresses',
    'CreditCards',
    'Coupons',
    'Coupon',
    'SellerProfile',
    'SellerProducts',
    'SupportConversations',
    'SupportMessages',
    'Wishlists',
  ],
  endpoints: () => ({}),
});
