import { baseApi } from '@/app/api';
import type {
  Coupon,
  CreateCouponRequest,
  UpdateCouponRequest,
  ValidateCouponRequest,
  CouponValidationResult,
} from './types';

export const couponsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    // Admin endpoints
    getCoupons: builder.query<Coupon[], void>({
      query: () => '/coupons',
      transformResponse: (response: { data: Coupon[] }) => response.data,
      providesTags: ['Coupons'],
    }),
    getActiveCoupons: builder.query<Coupon[], void>({
      query: () => '/coupons/active',
      transformResponse: (response: { data: Coupon[] }) => response.data,
      providesTags: ['Coupons'],
    }),
    getCoupon: builder.query<Coupon, number>({
      query: (id) => `/coupons/${id}`,
      transformResponse: (response: { data: Coupon }) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'Coupon', id }],
    }),
    createCoupon: builder.mutation<Coupon, CreateCouponRequest>({
      query: (data) => ({
        url: '/coupons',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: Coupon }) => response.data,
      invalidatesTags: ['Coupons'],
    }),
    updateCoupon: builder.mutation<Coupon, { id: number; data: UpdateCouponRequest }>({
      query: ({ id, data }) => ({
        url: `/coupons/${id}`,
        method: 'PUT',
        body: data,
      }),
      transformResponse: (response: { data: Coupon }) => response.data,
      invalidatesTags: ['Coupons'],
    }),
    deleteCoupon: builder.mutation<void, number>({
      query: (id) => ({
        url: `/coupons/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Coupons'],
    }),
    // User endpoint
    validateCoupon: builder.mutation<CouponValidationResult, ValidateCouponRequest>({
      query: (data) => ({
        url: '/coupons/validate',
        method: 'POST',
        body: data,
      }),
      transformResponse: (response: { data: CouponValidationResult }) => response.data,
    }),
  }),
});

export const {
  useGetCouponsQuery,
  useGetActiveCouponsQuery,
  useGetCouponQuery,
  useCreateCouponMutation,
  useUpdateCouponMutation,
  useDeleteCouponMutation,
  useValidateCouponMutation,
} = couponsApi;
