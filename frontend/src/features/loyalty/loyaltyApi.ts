import { baseApi } from '@/app/api';
import type { LoyaltySummary, LoyaltyTransaction } from './types';

export const loyaltyApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getLoyaltySummary: builder.query<LoyaltySummary, void>({
      query: () => '/loyalty/summary',
      transformResponse: (response: { data: LoyaltySummary }) => response.data,
      providesTags: ['Loyalty'],
    }),
    getLoyaltyHistory: builder.query<LoyaltyTransaction[], number | void>({
      query: (limit = 50) => `/loyalty/history?limit=${limit}`,
      transformResponse: (response: { data: LoyaltyTransaction[] }) => response.data,
      providesTags: ['Loyalty'],
    }),
  }),
});

export const {
  useGetLoyaltySummaryQuery,
  useGetLoyaltyHistoryQuery,
} = loyaltyApi;
