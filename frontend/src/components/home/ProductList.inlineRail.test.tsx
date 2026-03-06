import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { createTestStore, renderWithProviders } from '@/test-utils';
import { ProductList } from './ProductList';
import type { Product } from '@/features/products/types';
import type { PaginatedResponse } from '@/types/api';

const mockProductsApi = vi.hoisted(() => ({
  useGetPersonalizedRecommendationsQuery: vi.fn(),
  useSearchProductsQuery: vi.fn(),
}));

const mockWishlistApi = vi.hoisted(() => ({
  useGetWishlistQuery: vi.fn(),
  useAddWishlistItemMutation: vi.fn(),
  useRemoveWishlistItemMutation: vi.fn(),
}));

const mockWishlistFeature = vi.hoisted(() => ({
  getWishlistErrorMessage: vi.fn(() => 'Hata'),
  useGuestWishlist: vi.fn(),
}));

const mockCompare = vi.hoisted(() => ({
  buildCompareUrl: vi.fn(() => '/compare'),
  useProductCompare: vi.fn(),
}));

vi.mock('@/features/products/productsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/products/productsApi')>('@/features/products/productsApi');
  return {
    ...actual,
    ...mockProductsApi,
  };
});

vi.mock('@/features/wishlist/wishlistApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/wishlist/wishlistApi')>('@/features/wishlist/wishlistApi');
  return {
    ...actual,
    ...mockWishlistApi,
  };
});

vi.mock('@/features/wishlist', async () => {
  const actual = await vi.importActual<typeof import('@/features/wishlist')>('@/features/wishlist');
  return {
    ...actual,
    ...mockWishlistFeature,
  };
});

vi.mock('@/features/compare', async () => {
  const actual = await vi.importActual<typeof import('@/features/compare')>('@/features/compare');
  return {
    ...actual,
    ...mockCompare,
  };
});

vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    info: vi.fn(),
    success: vi.fn(),
    warning: vi.fn(),
  },
}));

vi.mock('@/components/products/ProductFeedCard', () => ({
  ProductFeedCard: ({ product }: { product: Product }) => (
    <div data-testid="product-feed-card">{product.name}</div>
  ),
}));

vi.mock('@/components/home/InlineProductRail', () => ({
  InlineProductRail: ({ title }: { title: string }) => (
    <div data-testid="inline-rail">{title}</div>
  ),
}));

const createProduct = (id: number, name = `Grid Product ${id}`, wishlistCount = 3): Product => ({
  id,
  name,
  description: `${name} açıklama`,
  price: 1000 + id,
  originalPrice: 1200 + id,
  currency: 'TRY',
  sku: `SKU-${id}`,
  isActive: true,
  categoryId: 1,
  categoryName: 'Elektronik',
  stockQuantity: 10,
  createdAt: '2026-03-01T10:00:00.000Z',
  averageRating: 4.5,
  reviewCount: 12,
  wishlistCount,
  hasActiveCampaign: false,
  isCampaignFeatured: false,
});

const createProductsResponse = (count = 20): PaginatedResponse<Product> => ({
  items: Array.from({ length: count }, (_, index) => createProduct(index + 1)),
  page: 1,
  pageSize: 12,
  totalCount: count,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
});

const renderProductList = ({
  isAuthenticated = true,
  productState = {},
  productsData = createProductsResponse(20),
}: {
  isAuthenticated?: boolean;
  productState?: Partial<{
    page: number;
    search: string;
    categoryId: string;
    sortBy: string;
    sortDesc: boolean;
  }>;
  productsData?: PaginatedResponse<Product>;
} = {}) => {
  const store = createTestStore({
    auth: {
      user: null,
      token: isAuthenticated ? 'token' : null,
      refreshToken: null,
      isAuthenticated,
    },
    products: {
      page: 1,
      search: '',
      categoryId: '',
      sortBy: 'createdAt',
      sortDesc: true,
      ...productState,
    },
  });

  return renderWithProviders(
    <ProductList
      isLoading={false}
      productsData={productsData}
      isAddingToCart={false}
      handleAddToCart={vi.fn()}
    />,
    { store },
  );
};

describe('ProductList inline rail akışı', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockWishlistApi.useGetWishlistQuery.mockReturnValue({ data: { items: [] } });
    mockWishlistApi.useAddWishlistItemMutation.mockReturnValue([vi.fn()]);
    mockWishlistApi.useRemoveWishlistItemMutation.mockReturnValue([vi.fn()]);

    mockWishlistFeature.useGuestWishlist.mockReturnValue({
      addProduct: vi.fn(() => ({ limitReached: false })),
      isPending: vi.fn(() => false),
      pendingCount: 0,
      pendingProductIds: [],
      removeProduct: vi.fn(),
    });

    mockCompare.useProductCompare.mockReturnValue({
      addProduct: vi.fn(() => ({ alreadyExists: false, limitReached: false })),
      compareIds: [],
      containsProduct: vi.fn(() => false),
      removeProduct: vi.fn(),
    });

    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [] },
    });
  });

  it('keşif akışında rail satırlarını ürünlerin arasına gömer', () => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [createProduct(101, 'Kişisel Öneri 1')],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(201, 'Favori Ürün 1', 77)] },
    });

    renderProductList();

    const firstRail = screen.getByText('Senin İçin Öneriler');
    const secondRail = screen.getByText('En Çok Favorilenenler');
    const eighthProduct = screen.getByText('Grid Product 8');
    const sixteenthProduct = screen.getByText('Grid Product 16');

    expect(eighthProduct.compareDocumentPosition(firstRail) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0);
    expect(sixteenthProduct.compareDocumentPosition(secondRail) & Node.DOCUMENT_POSITION_FOLLOWING).not.toBe(0);
  });

  it('giriş yoksa personalized raili göstermez', () => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [createProduct(102, 'Kişisel Öneri 2')],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(202, 'Favori Ürün 2', 18)] },
    });

    renderProductList({ isAuthenticated: false });

    expect(screen.queryByText('Senin İçin Öneriler')).not.toBeInTheDocument();
    expect(screen.getByText('En Çok Favorilenenler')).toBeInTheDocument();
  });

  it('filtreli akışta railleri gizler', () => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [createProduct(103, 'Kişisel Öneri 3')],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(203, 'Favori Ürün 3', 12)] },
    });

    renderProductList({
      productState: {
        search: 'kulaklık',
      },
    });

    expect(screen.queryByText('Senin İçin Öneriler')).not.toBeInTheDocument();
    expect(screen.queryByText('En Çok Favorilenenler')).not.toBeInTheDocument();
  });

  it.each([
    {
      name: 'kategori filtresi aktifken',
      productState: { categoryId: '3' },
    },
    {
      name: 'varsayılan dışı sıralama seçiliyken',
      productState: { sortBy: 'price' },
    },
    {
      name: 'artan sıralama seçiliyken',
      productState: { sortDesc: false },
    },
  ])('discovery bağlamı $name railleri gizler', ({ productState }) => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [createProduct(130, 'Kişisel Öneri Parametre')],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(230, 'Favori Ürün Parametre', 14)] },
    });

    renderProductList({ productState });

    expect(screen.queryByText('Senin İçin Öneriler')).not.toBeInTheDocument();
    expect(screen.queryByText('En Çok Favorilenenler')).not.toBeInTheDocument();
  });

  it('varsayılan keşif koşulu bozulduğunda railleri gizler', () => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [createProduct(104, 'Kişisel Öneri 4')],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(204, 'Favori Ürün 4', 50)] },
    });

    renderProductList({
      productState: {
        page: 2,
      },
    });

    expect(screen.queryByText('Senin İçin Öneriler')).not.toBeInTheDocument();
    expect(screen.queryByText('En Çok Favorilenenler')).not.toBeInTheDocument();
    expect(mockProductsApi.useGetPersonalizedRecommendationsQuery).toHaveBeenCalledWith(
      { take: 6 },
      { skip: true },
    );
    expect(mockProductsApi.useSearchProductsQuery).toHaveBeenCalledWith(
      {
        page: 1,
        pageSize: 8,
        sortBy: 'wishlistCount',
        sortDescending: true,
      },
      { skip: true },
    );
  });

  it('rail datası boşsa render etmez', () => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(205, 'Wish 0', 0)] },
    });

    renderProductList();

    expect(screen.queryByText('Senin İçin Öneriler')).not.toBeInTheDocument();
    expect(screen.queryByText('En Çok Favorilenenler')).not.toBeInTheDocument();
  });

  it('ana feedde zaten olan ürünleri rail listelerinden düşer', () => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [createProduct(1, 'Feed İçindeki Öneri')],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(2, 'Feed İçindeki Favori', 33)] },
    });

    renderProductList();

    expect(screen.queryByText('Senin İçin Öneriler')).not.toBeInTheDocument();
    expect(screen.queryByText('En Çok Favorilenenler')).not.toBeInTheDocument();
  });

  it('top wishlisted railde personalized ile çakışan ürünleri tekrar göstermez', () => {
    mockProductsApi.useGetPersonalizedRecommendationsQuery.mockReturnValue({
      data: [createProduct(101, 'Tekrar Eden Ürün')],
    });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({
      data: { items: [createProduct(101, 'Tekrar Eden Ürün', 87)] },
    });

    renderProductList();

    expect(screen.getByText('Senin İçin Öneriler')).toBeInTheDocument();
    expect(screen.queryByText('En Çok Favorilenenler')).not.toBeInTheDocument();
  });
});
