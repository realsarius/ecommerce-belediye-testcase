import { baseApi } from '@/app/api';
import type {
  ConfirmMediaUploadRequest,
  ConfirmMediaUploadResult,
  PresignMediaUploadRequest,
  PresignedMediaUpload,
  ReorderProductImagesRequest,
} from './types';

export const mediaApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    presignMediaUpload: builder.mutation<PresignedMediaUpload, PresignMediaUploadRequest>({
      query: (body) => ({
        url: '/media/presign',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: PresignedMediaUpload }) => response.data,
    }),

    confirmMediaUpload: builder.mutation<ConfirmMediaUploadResult, ConfirmMediaUploadRequest>({
      query: (body) => ({
        url: '/media/confirm',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: ConfirmMediaUploadResult }) => response.data,
      invalidatesTags: (_result, _error, arg) => {
        if (arg.context === 'category') {
          return ['Categories'];
        }

        if (arg.context === 'seller-logo' || arg.context === 'seller-banner') {
          return ['SellerProfile'];
        }

        return ['Products', 'SellerProducts'];
      },
    }),

    deleteMediaImage: builder.mutation<void, number>({
      query: (imageId) => ({
        url: `/media/${imageId}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Products', 'SellerProducts'],
    }),

    reorderProductImages: builder.mutation<void, { productId: number; data: ReorderProductImagesRequest }>({
      query: ({ productId, data }) => ({
        url: `/media/products/${productId}/images/reorder`,
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: (_result, _error, { productId }) => [
        'Products',
        { type: 'Product', id: productId },
        { type: 'SellerProducts', id: productId },
      ],
    }),
  }),
});

export const {
  usePresignMediaUploadMutation,
  useConfirmMediaUploadMutation,
  useDeleteMediaImageMutation,
  useReorderProductImagesMutation,
} = mediaApi;
