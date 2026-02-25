import { baseApi } from '@/app/api';
import type { PaginatedResponse } from '@/types/api';
import type {
    AssignSupportConversationRequest,
    SendSupportMessageRequest,
    StartSupportConversationRequest,
    SupportConversation,
    SupportMessagesRequest,
    SupportMessage,
    SupportQueueRequest,
} from './types';

export const supportApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        startSupportConversation: builder.mutation<SupportConversation, StartSupportConversationRequest>({
            query: (body) => ({
                url: '/support/conversations',
                method: 'POST',
                body,
            }),
            transformResponse: (response: { data: SupportConversation }) => response.data,
            invalidatesTags: [{ type: 'SupportConversations', id: 'LIST' }],
        }),

        getMySupportConversations: builder.query<SupportConversation[], void>({
            query: () => '/support/conversations/my',
            transformResponse: (response: { data: SupportConversation[] }) => response.data,
            providesTags: [{ type: 'SupportConversations', id: 'LIST' }],
        }),

        getSupportQueue: builder.query<PaginatedResponse<SupportConversation>, SupportQueueRequest | void>({
            query: (params) => ({
                url: '/support/conversations/queue',
                params: params ?? undefined,
            }),
            transformResponse: (response: { data: PaginatedResponse<SupportConversation> }) => response.data,
            providesTags: [{ type: 'SupportConversations', id: 'LIST' }],
        }),


        getSupportMessages: builder.query<PaginatedResponse<SupportMessage>, SupportMessagesRequest>({
            query: ({ conversationId, ...params }) => ({
                url: `/support/conversations/${conversationId}/messages`,
                params,
            }),
            transformResponse: (response: { data: PaginatedResponse<SupportMessage> }) => response.data,
            providesTags: (_result, _error, arg) => [{ type: 'SupportMessages', id: arg.conversationId }],
        }),

        sendSupportMessage: builder.mutation<
            SupportMessage,
            { conversationId: number; body: SendSupportMessageRequest }
        >({
            query: ({ conversationId, body }) => ({
                url: `/support/conversations/${conversationId}/messages`,
                method: 'POST',
                body,
            }),
            transformResponse: (response: { data: SupportMessage }) => response.data,
            invalidatesTags: (_result, _error, arg) => [
                { type: 'SupportMessages', id: arg.conversationId },
                { type: 'SupportConversations', id: 'LIST' },
            ],
        }),

        assignSupportConversation: builder.mutation<
            SupportConversation,
            { conversationId: number; body: AssignSupportConversationRequest }
        >({
            query: ({ conversationId, body }) => ({
                url: `/support/conversations/${conversationId}/assign`,
                method: 'POST',
                body,
            }),
            transformResponse: (response: { data: SupportConversation }) => response.data,
            invalidatesTags: (_result, _error, arg) => [
                { type: 'SupportConversations', id: 'LIST' },
                { type: 'SupportMessages', id: arg.conversationId },
            ],
        }),

        closeSupportConversation: builder.mutation<SupportConversation, { conversationId: number }>({
            query: ({ conversationId }) => ({
                url: `/support/conversations/${conversationId}/close`,
                method: 'POST',
            }),
            transformResponse: (response: { data: SupportConversation }) => response.data,
            invalidatesTags: (_result, _error, arg) => [
                { type: 'SupportConversations', id: 'LIST' },
                { type: 'SupportMessages', id: arg.conversationId },
            ],
        }),
    }),
});

export const {
    useStartSupportConversationMutation,
    useGetMySupportConversationsQuery,
    useGetSupportQueueQuery,
    useGetSupportMessagesQuery,
    useSendSupportMessageMutation,
    useAssignSupportConversationMutation,
    useCloseSupportConversationMutation,
} = supportApi;
