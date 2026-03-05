import { baseApi } from '@/app/api';
import type {
  ActionResult,
  AuthResponse,
  ChangeEmailRequest,
  ConfirmEmailChangeRequest,
  ForgotPasswordRequest,
  LoginRequest,
  ResetPasswordRequest,
  RegisterRequest,
  RefreshTokenRequest,
  ResendVerificationCodeRequest,
  SocialLoginRequest,
  User,
  VerifyEmailCodeRequest,
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
    verifyEmailCode: builder.mutation<AuthResponse, VerifyEmailCodeRequest>({
      query: (body) => ({
        url: '/auth/verify-email-code',
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
    resendVerificationCode: builder.mutation<ActionResult, ResendVerificationCodeRequest>({
      query: (body) => ({
        url: '/auth/resend-verification-code',
        method: 'POST',
        body,
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
    changeEmail: builder.mutation<ActionResult, ChangeEmailRequest>({
      query: (body) => ({
        url: '/account/change-email',
        method: 'POST',
        body,
      }),
    }),
    confirmEmailChange: builder.mutation<AuthResponse, ConfirmEmailChangeRequest>({
      query: (body) => ({
        url: '/auth/confirm-email-change',
        method: 'POST',
        body,
      }),
      transformResponse: (response: { data: AuthResponse }) => response.data,
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
  useVerifyEmailCodeMutation,
  useResendVerificationMutation,
  useResendVerificationCodeMutation,
  useForgotPasswordMutation,
  useResetPasswordMutation,
  useChangeEmailMutation,
  useConfirmEmailChangeMutation,
  useRevokeMutation,
  useGetMeQuery,
} = authApi;
