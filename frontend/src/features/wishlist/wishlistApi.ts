import { baseApi } from '@/app/api';
import type { Wishlist, AddWishlistItemRequest } from './types';
import type { ApiResponse } from '@/types/api';

export const wishlistApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getWishlist: builder.query<Wishlist, void>({
            query: () => '/wishlists',
            transformResponse: (response: ApiResponse<Wishlist>) => response.data!,
            providesTags: ['Wishlists'],
        }),
        addWishlistItem: builder.mutation<void, AddWishlistItemRequest>({
            query: (data) => ({
                url: '/wishlists/items',
                method: 'POST',
                body: data,
            }),
            invalidatesTags: ['Wishlists'],
        }),
        removeWishlistItem: builder.mutation<void, number>({
            query: (productId) => ({
                url: `/wishlists/items/${productId}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['Wishlists'],
        }),
        clearWishlist: builder.mutation<void, void>({
            query: () => ({
                url: '/wishlists',
                method: 'DELETE',
            }),
            invalidatesTags: ['Wishlists'],
        }),
    }),
});

export const {
    useGetWishlistQuery,
    useAddWishlistItemMutation,
    useRemoveWishlistItemMutation,
    useClearWishlistMutation,
} = wishlistApi;
