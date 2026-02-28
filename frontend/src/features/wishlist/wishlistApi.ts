import { baseApi } from '@/app/api';
import type { Wishlist, AddWishlistItemRequest, GetWishlistRequest, WishlistPriceAlert, UpsertWishlistPriceAlertRequest } from './types';
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
        getWishlistPriceAlerts: builder.query<WishlistPriceAlert[], void>({
            query: () => '/wishlists/price-alerts',
            transformResponse: (response: ApiResponse<WishlistPriceAlert[]>) => response.data ?? [],
            providesTags: (result) => result
                ? [
                    'WishlistPriceAlerts',
                    ...result.map((alert) => ({ type: 'WishlistItem' as const, id: alert.productId })),
                    ...result.map((alert) => ({ type: 'Product' as const, id: alert.productId })),
                ]
                : ['WishlistPriceAlerts'],
        }),
        upsertWishlistPriceAlert: builder.mutation<WishlistPriceAlert, UpsertWishlistPriceAlertRequest>({
            query: (body) => ({
                url: '/wishlists/price-alerts',
                method: 'PUT',
                body,
            }),
            transformResponse: (response: ApiResponse<WishlistPriceAlert>) => response.data!,
            invalidatesTags: (_result, _error, arg) => [
                'WishlistPriceAlerts',
                { type: 'WishlistItem' as const, id: arg.productId },
                { type: 'Product' as const, id: arg.productId },
            ],
        }),
        removeWishlistPriceAlert: builder.mutation<void, number>({
            query: (productId) => ({
                url: `/wishlists/price-alerts/${productId}`,
                method: 'DELETE',
            }),
            invalidatesTags: (_result, _error, productId) => [
                'WishlistPriceAlerts',
                { type: 'WishlistItem' as const, id: productId },
                { type: 'Product' as const, id: productId },
            ],
        }),
    }),
});

export const {
    useGetWishlistQuery,
    useLazyGetWishlistQuery,
    useAddWishlistItemMutation,
    useRemoveWishlistItemMutation,
    useClearWishlistMutation,
    useGetWishlistPriceAlertsQuery,
    useUpsertWishlistPriceAlertMutation,
    useRemoveWishlistPriceAlertMutation,
} = wishlistApi;
