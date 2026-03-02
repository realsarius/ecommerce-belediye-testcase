import { baseApi } from '@/app/api';
import type {
  CreateGiftCardRequest,
  GiftCard,
  GiftCardSummary,
  GiftCardTransaction,
  GiftCardValidationResult,
  UpdateGiftCardRequest,
  ValidateGiftCardRequest,
} from './types';

export const giftCardsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getGiftCards: builder.query<GiftCard[], void>({
      query: () => '/gift-cards',
      transformResponse: (response: { data: GiftCard[] }) => response.data,
      providesTags: ['GiftCards'],
    }),
    getGiftCard: builder.query<GiftCard, number>({
      query: (id) => `/gift-cards/${id}`,
      transformResponse: (response: { data: GiftCard }) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'GiftCard', id }],
    }),
    createGiftCard: builder.mutation<GiftCard, CreateGiftCardRequest>({
      query: (data) => ({
        url: '/gift-cards',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: GiftCard }) => response.data,
      invalidatesTags: ['GiftCards'],
    }),
    updateGiftCard: builder.mutation<GiftCard, { id: number; data: UpdateGiftCardRequest }>({
      query: ({ id, data }) => ({
        url: `/gift-cards/${id}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: GiftCard }) => response.data,
      invalidatesTags: ['GiftCards'],
    }),
    getMyGiftCards: builder.query<GiftCard[], void>({
      query: () => '/gift-cards/my',
      transformResponse: (response: { data: GiftCard[] }) => response.data,
      providesTags: ['GiftCards'],
    }),
    getGiftCardSummary: builder.query<GiftCardSummary, void>({
      query: () => '/gift-cards/my/summary',
      transformResponse: (response: { data: GiftCardSummary }) => response.data,
      providesTags: ['GiftCards'],
    }),
    getGiftCardHistory: builder.query<GiftCardTransaction[], number | void>({
      query: (limit = 50) => `/gift-cards/my/history?limit=${limit}`,
      transformResponse: (response: { data: GiftCardTransaction[] }) => response.data,
      providesTags: ['GiftCards'],
    }),
    validateGiftCard: builder.mutation<GiftCardValidationResult, ValidateGiftCardRequest>({
      query: (data) => ({
        url: '/gift-cards/validate',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: GiftCardValidationResult }) => response.data,
    }),
  }),
});

export const {
  useGetGiftCardsQuery,
  useGetGiftCardQuery,
  useCreateGiftCardMutation,
  useUpdateGiftCardMutation,
  useGetMyGiftCardsQuery,
  useGetGiftCardSummaryQuery,
  useGetGiftCardHistoryQuery,
  useValidateGiftCardMutation,
} = giftCardsApi;
