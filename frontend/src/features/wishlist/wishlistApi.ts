import { baseApi } from '@/app/api';
import type { Wishlist, AddWishlistItemRequest, GetWishlistRequest } from './types';
import type { ApiResponse } from '@/types/api';

export const wishlistApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        getWishlist: builder.query<Wishlist, GetWishlistRequest | void>({
            query: (arg) => {
                const params = new URLSearchParams();

                if (arg?.cursor) {
                    params.set('cursor', arg.cursor);
                }

                if (typeof arg?.limit === 'number') {
                    params.set('limit', String(arg.limit));
                }

                const query = params.toString();
                return query ? `/wishlists?${query}` : '/wishlists';
            },
            transformResponse: (response: ApiResponse<Wishlist>) => response.data!,
            providesTags: (result) => result
                ? [
                    'Wishlists',
                    ...result.items.map((item) => ({ type: 'WishlistItem' as const, id: item.productId })),
                ]
                : ['Wishlists'],
        }),
        addWishlistItem: builder.mutation<void, AddWishlistItemRequest>({
            query: (data) => ({
                url: '/wishlists/items',
                method: 'POST',
                body: data,
            }),
            invalidatesTags: (_result, _error, arg) => [
                'Wishlists',
                { type: 'WishlistItem' as const, id: arg.productId },
                { type: 'Product' as const, id: arg.productId },
            ],
        }),
        removeWishlistItem: builder.mutation<void, number>({
            query: (productId) => ({
                url: `/wishlists/items/${productId}`,
                method: 'DELETE',
            }),
            invalidatesTags: (_result, _error, productId) => [
                'Wishlists',
                { type: 'WishlistItem' as const, id: productId },
                { type: 'Product' as const, id: productId },
            ],
        }),
        clearWishlist: builder.mutation<void, void>({
            query: () => ({
                url: '/wishlists',
                method: 'DELETE',
            }),
            invalidatesTags: ['Wishlists', 'Products'],
        }),
    }),
});

export const {
    useGetWishlistQuery,
    useLazyGetWishlistQuery,
    useAddWishlistItemMutation,
    useRemoveWishlistItemMutation,
    useClearWishlistMutation,
} = wishlistApi;
