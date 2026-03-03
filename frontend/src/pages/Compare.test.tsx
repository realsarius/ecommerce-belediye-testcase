import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import Compare from './Compare';

const mockProductsApi = vi.hoisted(() => ({
  useGetProductQuery: vi.fn(),
}));

vi.mock('@/features/products/productsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/products/productsApi')>('@/features/products/productsApi');
  return {
    ...actual,
    ...mockProductsApi,
  };
});

function createProduct(id: number, name: string) {
  return {
    id,
    name,
    description: `${name} açıklaması`,
    price: 100,
    originalPrice: 120,
    currency: 'TRY',
    sku: `SKU-${id}`,
    isActive: true,
    categoryId: 1,
    categoryName: 'Gıda',
    stockQuantity: 15,
    sellerId: 1,
    sellerBrandName: 'Demo Store',
    createdAt: '2026-03-03T10:00:00Z',
    averageRating: 4.2,
    reviewCount: 3,
    wishlistCount: 5,
    hasActiveCampaign: false,
    isCampaignFeatured: false,
    primaryImageUrl: null,
    images: [],
    variants: [],
  };
}

describe('Compare sayfası', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
  });

  it('Listeyi temizle butonu URL ve local storage listesini birlikte temizlemeli', async () => {
    const user = userEvent.setup();

    mockProductsApi.useGetProductQuery.mockImplementation((arg: number | symbol) => {
      if (typeof arg !== 'number') {
        return { data: undefined, isLoading: false };
      }

      return {
        data: createProduct(arg, `Ürün ${arg}`),
        isLoading: false,
      };
    });

    renderWithProviders(<Compare />, { route: '/compare?ids=1,2' });

    expect(await screen.findByText('Ürün 1')).toBeInTheDocument();
    expect(screen.getByText('Ürün 2')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Listeyi temizle' }));

    await waitFor(() => {
      expect(screen.getByText('Karşılaştırma listesi boş')).toBeInTheDocument();
    });

    expect(window.localStorage.getItem('product_compare_ids')).toBe('[]');
  });
});
