import { baseApi } from '@/app/api';
import type { ContactMessage, CreateContactMessageRequest } from './types';

export const contactApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    createContactMessage: builder.mutation<ContactMessage, CreateContactMessageRequest>({
      query: (body) => ({
        url: '/contact',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: ContactMessage }) => response.data,
    }),
  }),
});

export const { useCreateContactMessageMutation } = contactApi;
