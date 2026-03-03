import { baseApi } from '@/app/api';
import type { FrontendFeatureSettings } from '@/features/settings/types';
import type { ApiResponse } from '@/types/api';

export const settingsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getFrontendFeatures: builder.query<FrontendFeatureSettings, void>({
      query: () => '/frontend-settings/features',
      transformResponse: (response: ApiResponse<FrontendFeatureSettings>) => response.data!,
    }),
  }),
});

export const {
  useGetFrontendFeaturesQuery,
} = settingsApi;
