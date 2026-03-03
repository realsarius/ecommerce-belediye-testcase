import { baseApi } from '@/app/api';
import type { Campaign, CreateCampaignRequest, UpdateCampaignRequest } from './types';

export const campaignsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getActiveCampaigns: builder.query<Campaign[], void>({
      query: () => '/campaigns/active',
      transformResponse: (response: { data: Campaign[] }) => response.data,
      providesTags: ['Products', 'Campaigns'],
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
    getAdminCampaigns: builder.query<Campaign[], void>({
      query: () => '/admin/campaigns',
      transformResponse: (response: { data: Campaign[] }) => response.data,
      providesTags: ['Campaigns'],
    }),
    createAdminCampaign: builder.mutation<Campaign, CreateCampaignRequest>({
      query: (body) => ({
        url: '/admin/campaigns',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: Campaign }) => response.data,
      invalidatesTags: ['Campaigns', 'Products'],
    }),
    updateAdminCampaign: builder.mutation<Campaign, { id: number; data: UpdateCampaignRequest }>({
      query: ({ id, data }) => ({
        url: `/admin/campaigns/${id}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: Campaign }) => response.data,
      invalidatesTags: ['Campaigns', 'Products'],
    }),
    deleteAdminCampaign: builder.mutation<void, number>({
      query: (id) => ({
        url: `/admin/campaigns/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Campaigns', 'Products'],
    }),
  }),
});

export const {
  useGetActiveCampaignsQuery,
  useTrackCampaignInteractionMutation,
  useGetAdminCampaignsQuery,
  useCreateAdminCampaignMutation,
  useUpdateAdminCampaignMutation,
  useDeleteAdminCampaignMutation,
} = campaignsApi;
