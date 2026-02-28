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
        useGetWishlistQuery: vi.fn(),
        useRemoveWishlistItemMutation: () => [vi.fn()],
        useClearWishlistMutation: () => [vi.fn()],
    };
});

describe('Wishlist Component', () => {
    it('renders login prompt when user is not authenticated', () => {
        vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
            data: undefined,
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
        expect(screen.getByText('Favorilerinizi görmek için lütfen giriş yapın.')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Giriş Yap' })).toBeInTheDocument();
    });

    it('renders empty state when authenticated but wishlist is empty', () => {
        vi.mocked(wishlistApi.useGetWishlistQuery).mockReturnValue({
            data: { id: 1, userId: 1, items: [] },
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

        // The exact empty text from Wishlist.tsx
        expect(screen.getByText('Favorileriniz Boş')).toBeInTheDocument();
        expect(screen.getByText('Henüz favorilerinize hiçbir ürün eklemediniz.')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: 'Ürünlere Göz At' })).toBeInTheDocument();
    });
});
