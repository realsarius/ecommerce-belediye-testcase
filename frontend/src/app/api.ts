import {
  createApi,
  fetchBaseQuery,
  type BaseQueryFn,
  type FetchArgs,
  type FetchBaseQueryError,
} from '@reduxjs/toolkit/query/react';
import { logout, setCredentials } from '@/features/auth/authSlice';

interface AuthState {
  user: {
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
  } | null;
  token: string | null;
  refreshToken: string | null;
}

const rawBaseQuery = fetchBaseQuery({
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

type RefreshResponse = {
  data?: {
    success: boolean;
    token?: string;
    refreshToken?: string;
    user?: AuthState['user'];
  };
};

let refreshPromise: Promise<boolean> | null = null;

const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions
) => {
  let result = await rawBaseQuery(args, api, extraOptions);

  const isRefreshRequest =
    typeof args === 'string'
      ? args.includes('/auth/refresh')
      : args.url.includes('/auth/refresh');

  if (result.error?.status !== 401 || isRefreshRequest) {
    return result;
  }

  const state = api.getState() as { auth: AuthState };
  const refreshToken = state.auth.refreshToken;
  const currentUser = state.auth.user;

  if (!refreshToken || !currentUser) {
    api.dispatch(logout());
    return result;
  }

  if (!refreshPromise) {
    refreshPromise = (async () => {
      const refreshResult = await rawBaseQuery(
        {
          url: '/auth/refresh',
          method: 'POST',
          body: { refreshToken },
        },
        api,
        extraOptions
      );

      if ('data' in refreshResult) {
        const payload = (refreshResult as RefreshResponse).data;
        if (payload?.success && payload.token && payload.refreshToken && payload.user) {
          api.dispatch(
            setCredentials({
              user: payload.user,
              token: payload.token,
              refreshToken: payload.refreshToken,
            })
          );
          return true;
        }
      }

      api.dispatch(logout());
      return false;
    })();
  }

  const refreshSucceeded = await refreshPromise;
  refreshPromise = null;

  if (!refreshSucceeded) {
    return result;
  }

  result = await rawBaseQuery(args, api, extraOptions);
  return result;
};

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
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
    'WishlistCollections',
    'WishlistItem',
    'WishlistPriceAlerts',
    'WishlistShare',
    'Returns',
    'Notifications',
    'Loyalty',
  ],
  endpoints: () => ({}),
});
