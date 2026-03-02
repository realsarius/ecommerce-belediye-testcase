import { baseApi } from '@/app/api';
import type { ReferralSummary, ReferralTransaction } from './types';

export const referralsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getReferralSummary: builder.query<ReferralSummary, void>({
      query: () => '/referrals/summary',
      transformResponse: (response: { data: ReferralSummary }) => response.data,
      providesTags: ['Referrals'],
    }),
    getReferralHistory: builder.query<ReferralTransaction[], number | void>({
      query: (limit = 50) => `/referrals/history?limit=${limit}`,
      transformResponse: (response: { data: ReferralTransaction[] }) => response.data,
      providesTags: ['Referrals'],
    }),
  }),
});

export const {
  useGetReferralSummaryQuery,
  useGetReferralHistoryQuery,
} = referralsApi;
