import type { FetchBaseQueryError } from '@reduxjs/toolkit/query';
import { baseApi } from '@/app/api';
import { getRuntimeApiBaseUrl } from '@/lib/runtimeApi';
import type { CreateReturnRequestPayload, ReturnAttachmentAccessUrl, ReturnRequest, UploadedReturnPhoto } from '@/features/returns/types';

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
    uploadReturnPhotos: builder.mutation<UploadedReturnPhoto[], File[]>({
      async queryFn(files, api) {
        const state = api.getState() as { auth: { token: string | null } };
        const formData = new FormData();

        files.forEach((file) => {
          formData.append('files', file);
        });

        try {
          const response = await fetch(`${getRuntimeApiBaseUrl()}/uploads/return-photos`, {
            method: 'POST',
            headers: state.auth.token ? { Authorization: `Bearer ${state.auth.token}` } : undefined,
            body: formData,
          });

          const payload = await response.json().catch(() => null) as { data?: UploadedReturnPhoto[]; message?: string } | null;
          if (!response.ok) {
            return {
              error: {
                status: response.status,
                data: payload ?? { message: 'Fotoğraflar yüklenemedi.' },
              } as FetchBaseQueryError,
            };
          }

          return { data: payload?.data ?? [] };
        } catch (error) {
          return {
            error: {
              status: 'FETCH_ERROR',
              error: error instanceof Error ? error.message : 'Fotoğraflar yüklenemedi.',
            } as FetchBaseQueryError,
          };
        }
      },
    }),
    getReturnAttachmentAccessUrl: builder.query<ReturnAttachmentAccessUrl, { returnRequestId: number; attachmentId: number }>({
      query: ({ returnRequestId, attachmentId }) => `/returns/${returnRequestId}/attachments/${attachmentId}/access-url`,
      transformResponse: (response: { data: ReturnAttachmentAccessUrl }) => response.data,
    }),
  }),
});

export const {
  useGetMyReturnRequestsQuery,
  useCreateReturnRequestMutation,
  useUploadReturnPhotosMutation,
  useLazyGetReturnAttachmentAccessUrlQuery,
} = returnsApi;
