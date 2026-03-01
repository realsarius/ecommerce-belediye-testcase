import { baseApi } from '@/app/api';
import type { Campaign } from './types';

export const campaignsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getActiveCampaigns: builder.query<Campaign[], void>({
      query: () => '/campaigns/active',
      transformResponse: (response: { data: Campaign[] }) => response.data,
      providesTags: ['Products'],
    }),
    trackCampaignInteraction: builder.mutation<void, { campaignId: number; interactionType: 'impression' | 'click'; productId?: number; sessionId?: string | null }>({
      query: ({ campaignId, interactionType, productId, sessionId }) => ({
        url: `/campaigns/${campaignId}/interactions`,
        method: 'POST',
        headers: sessionId ? { 'X-Session-Id': sessionId } : undefined,
        body: {
          interactionType,
          productId,
        },
      }),
    }),
  }),
});

export const { useGetActiveCampaignsQuery, useTrackCampaignInteractionMutation } = campaignsApi;
