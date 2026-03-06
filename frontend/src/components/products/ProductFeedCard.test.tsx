import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test-utils';
import { ProductFeedCard } from './ProductFeedCard';
import type { Product } from '@/features/products/types';

vi.mock('@/components/products/ProductCardMediaPreview', () => ({
  ProductCardMediaPreview: ({ product }: { product: Product }) => (
    <div data-testid="media-preview">{product.name}</div>
  ),
}));

vi.mock('@/components/campaigns/CampaignCountdown', () => ({
  CampaignCountdown: () => <span>Kalan süre</span>,
}));

const createProduct = (overrides: Partial<Product> = {}): Product => ({
  id: 10,
  name: 'Örnek Ürün',
  description: 'Ürün açıklaması',
  price: 999,
  originalPrice: 1299,
  currency: 'TRY',
  sku: 'SKU-10',
  isActive: true,
  categoryId: 1,
  categoryName: 'Elektronik',
  stockQuantity: 7,
  createdAt: '2026-03-01T10:00:00.000Z',
  averageRating: 4.6,
  reviewCount: 23,
  wishlistCount: 18,
  hasActiveCampaign: true,
  campaignBadgeText: 'Fırsat',
  campaignEndsAt: '2026-03-30T10:00:00.000Z',
  isCampaignFeatured: false,
  ...overrides,
});

describe('ProductFeedCard', () => {
  it('default varyantta karşılaştırma aksiyonunu gösterir', () => {
    const product = createProduct();
    renderWithProviders(
      <ProductFeedCard
        product={product}
        variant="default"
        isAddingToCart={false}
        isInWishlist={false}
        isInCompare={false}
        onAddToCart={vi.fn()}
        onWishlistToggle={vi.fn()}
        onCompareToggle={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: 'Sepete Ekle' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Karşılaştırmaya Ekle' })).toBeInTheDocument();
    expect(screen.getByText(/kişi favoriledi/i)).toBeInTheDocument();
    expect(screen.getByText('Kalan süre')).toBeInTheDocument();
  });

  it('compact varyantta compare ve ağır meta alanlarını gizler', () => {
    const product = createProduct();
    renderWithProviders(
      <ProductFeedCard
        product={product}
        variant="compact"
        isAddingToCart={false}
        isInWishlist={true}
        onAddToCart={vi.fn()}
        onWishlistToggle={vi.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: 'Sepete' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Karşılaştırmaya Ekle' })).not.toBeInTheDocument();
    expect(screen.queryByText(/kişi favoriledi/i)).not.toBeInTheDocument();
    expect(screen.queryByText('Kalan süre')).not.toBeInTheDocument();
  });
});
