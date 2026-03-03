import { baseApi } from '@/app/api';

export type PaymentProviderType = 'Iyzico' | 'Stripe' | 'PayTR';

export interface CreditCard {
  id: number;
  cardAlias: string;
  cardHolderName: string;
  last4Digits: string;
  expireMonth: string;
  expireYear: string;
  isTokenized: boolean;
  tokenProvider?: PaymentProviderType | null;
  isDefault: boolean;
}

export const creditCardsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getCreditCards: builder.query<CreditCard[], void>({
      query: () => '/creditcards',
      providesTags: ['CreditCards'],
    }),
    deleteCreditCard: builder.mutation<void, number>({
      query: (id) => ({
        url: `/creditcards/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['CreditCards'],
    }),
    setDefaultCard: builder.mutation<void, number>({
      query: (id) => ({
        url: `/creditcards/${id}`,
        method: 'PATCH',
        body: { isDefault: true },
      }),
      invalidatesTags: ['CreditCards'],
    }),
  }),
});

export const {
  useGetCreditCardsQuery,
  useDeleteCreditCardMutation,
  useSetDefaultCardMutation,
} = creditCardsApi;
