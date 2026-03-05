import { baseApi } from '@/app/api';
import type {
  ActionResult,
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  ResetPasswordRequest,
  RegisterRequest,
  RefreshTokenRequest,
  SocialLoginRequest,
  User,
  VerifyEmailRequest,
} from './types';

export const authApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    login: builder.mutation<AuthResponse, LoginRequest>({
      query: (credentials) => ({
        url: '/auth/login',
        method: 'POST',
        body: credentials,
      }),
      transformResponse: (response: { data: AuthResponse }) => response.data,
    }),
    socialLogin: builder.mutation<AuthResponse, SocialLoginRequest>({
      query: (body) => ({
        url: '/auth/social',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: AuthResponse }) => response.data,
    }),
    register: builder.mutation<AuthResponse, RegisterRequest>({
      query: (data) => ({
        url: '/auth/register',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: AuthResponse }) => response.data,
    }),
    refresh: builder.mutation<AuthResponse, RefreshTokenRequest>({
      query: (data) => ({
        url: '/auth/refresh',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: AuthResponse }) => response.data,
    }),
    verifyEmail: builder.mutation<AuthResponse, VerifyEmailRequest>({
      query: (body) => ({
        url: '/auth/verify-email',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: AuthResponse }) => response.data,
    }),
    resendVerification: builder.mutation<ActionResult, void>({
      query: () => ({
        url: '/auth/resend-verification',
        method: 'POST',
      }),
    }),
    forgotPassword: builder.mutation<ActionResult, ForgotPasswordRequest>({
      query: (body) => ({
        url: '/auth/forgot-password',
        method: 'POST',
        body,
      }),
    }),
    resetPassword: builder.mutation<ActionResult, ResetPasswordRequest>({
      query: (body) => ({
        url: '/auth/reset-password',
        method: 'POST',
        body,
      }),
    }),
    revoke: builder.mutation<void, RefreshTokenRequest>({
      query: (data) => ({
        url: '/auth/revoke',
        method: 'POST',
        body: data,
      }),
    }),
    getMe: builder.query<User, void>({
      query: () => '/auth/me',
      transformResponse: (response: { data: User }) => response.data,
    }),
  }),
});

export const {
  useLoginMutation,
  useSocialLoginMutation,
  useRegisterMutation,
  useRefreshMutation,
  useVerifyEmailMutation,
  useResendVerificationMutation,
  useForgotPasswordMutation,
  useResetPasswordMutation,
  useRevokeMutation,
  useGetMeQuery,
} = authApi;
