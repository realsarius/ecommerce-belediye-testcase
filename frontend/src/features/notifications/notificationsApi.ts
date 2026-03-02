import { baseApi } from '@/app/api';
import type {
  NotificationCount,
  NotificationItem,
  NotificationPreference,
  NotificationPreferencesResponse,
  UpdateNotificationPreferencesRequest,
} from './types';

export const notificationsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getNotifications: builder.query<NotificationItem[], { take?: number } | void>({
      query: (args) => ({
        url: '/notifications',
        params: args?.take ? { take: args.take } : undefined,
      }),
      transformResponse: (response: { data: NotificationItem[] }) => response.data,
      providesTags: ['Notifications'],
    }),
    getUnreadNotificationCount: builder.query<NotificationCount, void>({
      query: () => '/notifications/unread-count',
      transformResponse: (response: { data: NotificationCount }) => response.data,
      providesTags: ['Notifications'],
    }),
    markNotificationAsRead: builder.mutation<NotificationItem, number>({
      query: (notificationId) => ({
        url: `/notifications/${notificationId}/read`,
        method: 'POST',
      }),
      transformResponse: (response: { data: NotificationItem }) => response.data,
      invalidatesTags: ['Notifications'],
    }),
    markAllNotificationsAsRead: builder.mutation<void, void>({
      query: () => ({
        url: '/notifications/read-all',
        method: 'POST',
      }),
      invalidatesTags: ['Notifications'],
    }),
    getNotificationPreferences: builder.query<NotificationPreferencesResponse, void>({
      query: () => '/notifications/preferences',
      transformResponse: (response: { data: NotificationPreferencesResponse }) => response.data,
      providesTags: ['Notifications'],
    }),
    updateNotificationPreferences: builder.mutation<NotificationPreference[], UpdateNotificationPreferencesRequest>({
      query: (body) => ({
        url: '/notifications/preferences',
        method: 'PUT',
        body,
      }),
      transformResponse: (response: { data: NotificationPreference[] }) => response.data,
      invalidatesTags: ['Notifications'],
    }),
  }),
});

export const {
  useGetNotificationsQuery,
  useGetUnreadNotificationCountQuery,
  useMarkNotificationAsReadMutation,
  useMarkAllNotificationsAsReadMutation,
  useGetNotificationPreferencesQuery,
  useUpdateNotificationPreferencesMutation,
} = notificationsApi;
