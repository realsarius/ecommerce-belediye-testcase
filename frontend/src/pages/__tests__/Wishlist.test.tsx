import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders, createTestStore } from '@/test-utils';
import Wishlist from '../Wishlist';
import { vi } from 'vitest';
import * as wishlistApi from '@/features/wishlist/wishlistApi';

vi.mock('@/features/wishlist/wishlistApi', async (importOriginal) => {
    const actual = await importOriginal<typeof import('@/features/wishlist/wishlistApi')>();
    return {
        ...actual,
        useLazyGetWishlistQuery: vi.fn(),
        useGetWishlistPriceAlertsQuery: vi.fn(),
        useUpsertWishlistPriceAlertMutation: () => [vi.fn()],
        useRemoveWishlistPriceAlertMutation: () => [vi.fn()],
        useRemoveWishlistItemMutation: () => [vi.fn()],
        useClearWishlistMutation: () => [vi.fn()],
    };
});

describe('Wishlist Component', () => {
    it('renders login prompt when user is not authenticated', () => {
        vi.mocked(wishlistApi.useLazyGetWishlistQuery).mockReturnValue([
            vi.fn(),
            { data: undefined, isLoading: false },
        ] as any);
        vi.mocked(wishlistApi.useGetWishlistPriceAlertsQuery).mockReturnValue({
            data: [],
            isLoading: false,
        } as any);

        renderWithProviders(<Wishlist />, {
            store: createTestStore({
                auth: {
                    isAuthenticated: false,
                    user: null,
                    token: null,
                    refreshToken: null
                }
            })
        });

        expect(screen.getByText('Giriş Yapmanız Gerekiyor')).toBeInTheDocument();
        expect(screen.getByText('Favorilerinizi hesabinizla senkronize etmek ve tum cihazlarinizda gormek icin lutfen giris yapin.')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Giris Yap' })).toBeInTheDocument();
    });

    it('renders empty state when authenticated but wishlist is empty', async () => {
        const trigger = vi.fn().mockReturnValue({
            unwrap: () => Promise.resolve({ id: 1, userId: 1, items: [], hasMore: false, nextCursor: null }),
        });
        vi.mocked(wishlistApi.useLazyGetWishlistQuery).mockReturnValue([
            trigger,
            { data: undefined, isLoading: false },
        ] as any);
        vi.mocked(wishlistApi.useGetWishlistPriceAlertsQuery).mockReturnValue({
            data: [],
            isLoading: false,
        } as any);

        renderWithProviders(<Wishlist />, {
            store: createTestStore({
                auth: {
                    isAuthenticated: true,
                    user: { id: 1, email: 'test@example.com', roles: [], firstName: 'Test', lastName: 'User' },
                    token: 'fake-token',
                    refreshToken: 'fake-refresh'
                }
            })
        });

        expect(await screen.findByText('Favorileriniz Boş')).toBeInTheDocument();
        expect(await screen.findByText('Henüz favorilerinize hiçbir ürün eklemediniz.')).toBeInTheDocument();
        expect(await screen.findByRole('link', { name: 'Ürünlere Göz At' })).toBeInTheDocument();
    });
});
