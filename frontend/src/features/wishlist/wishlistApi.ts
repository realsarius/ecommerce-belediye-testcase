import { baseApi } from '@/app/api';
import type {
    Wishlist,
    AddWishlistItemRequest,
    GetWishlistRequest,
    WishlistPriceAlert,
    UpsertWishlistPriceAlertRequest,
    WishlistBulkAddToCartResult,
    WishlistShareSettings,
    SharedWishlist,
} from './types';
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
        getWishlistShareSettings: builder.query<WishlistShareSettings, void>({
            query: () => '/wishlists/share',
            transformResponse: (response: ApiResponse<WishlistShareSettings>) => response.data ?? { isPublic: false },
            providesTags: ['WishlistShare'],
        }),
        enableWishlistSharing: builder.mutation<WishlistShareSettings, void>({
            query: () => ({
                url: '/wishlists/share',
                method: 'POST',
            }),
            transformResponse: (response: ApiResponse<WishlistShareSettings>) => response.data!,
            invalidatesTags: ['WishlistShare'],
        }),
        disableWishlistSharing: builder.mutation<void, void>({
            query: () => ({
                url: '/wishlists/share',
                method: 'DELETE',
            }),
            invalidatesTags: ['WishlistShare'],
        }),
        getSharedWishlist: builder.query<SharedWishlist, GetWishlistRequest & { shareToken: string }>({
            query: ({ shareToken, cursor, limit }) => {
                const params = new URLSearchParams();

                if (cursor) {
                    params.set('cursor', cursor);
                }

                if (typeof limit === 'number') {
                    params.set('limit', String(limit));
                }

                const query = params.toString();
                return query
                    ? `/wishlists/share/${shareToken}?${query}`
                    : `/wishlists/share/${shareToken}`;
            },
            transformResponse: (response: ApiResponse<SharedWishlist>) => response.data!,
            providesTags: (_result, _error, arg) => [{ type: 'WishlistShare' as const, id: arg.shareToken }],
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
        addAllWishlistItemsToCart: builder.mutation<WishlistBulkAddToCartResult, void>({
            query: () => ({
                url: '/wishlists/add-all-to-cart',
                method: 'POST',
            }),
            transformResponse: (response: ApiResponse<WishlistBulkAddToCartResult>) => response.data!,
            invalidatesTags: ['Cart'],
        }),
    }),
});

export const {
    useGetWishlistQuery,
    useLazyGetWishlistQuery,
    useGetWishlistShareSettingsQuery,
    useEnableWishlistSharingMutation,
    useDisableWishlistSharingMutation,
    useLazyGetSharedWishlistQuery,
    useAddWishlistItemMutation,
    useRemoveWishlistItemMutation,
    useClearWishlistMutation,
    useGetWishlistPriceAlertsQuery,
    useUpsertWishlistPriceAlertMutation,
    useRemoveWishlistPriceAlertMutation,
    useAddAllWishlistItemsToCartMutation,
} = wishlistApi;
