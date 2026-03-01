import { baseApi } from '@/app/api';
import type { Campaign } from './types';

export const campaignsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getActiveCampaigns: builder.query<Campaign[], void>({
      query: () => '/campaigns/active',
      transformResponse: (response: { data: Campaign[] }) => response.data,
      providesTags: ['Products'],
    }),
  }),
});

export const { useGetActiveCampaignsQuery } = campaignsApi;
