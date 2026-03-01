import { baseApi } from '@/app/api';
import type { CreateReturnRequestPayload, ReturnRequest } from '@/features/returns/types';

export const returnsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getMyReturnRequests: builder.query<ReturnRequest[], void>({
      query: () => '/returns/mine',
      transformResponse: (response: { data: ReturnRequest[] }) => response.data,
      providesTags: ['Returns'],
    }),
    createReturnRequest: builder.mutation<ReturnRequest, CreateReturnRequestPayload>({
      query: ({ orderId, ...body }) => ({
        url: `/orders/${orderId}/returns`,
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: ReturnRequest }) => response.data,
      invalidatesTags: ['Returns', 'Orders'],
    }),
  }),
});

export const {
  useGetMyReturnRequestsQuery,
  useCreateReturnRequestMutation,
} = returnsApi;
