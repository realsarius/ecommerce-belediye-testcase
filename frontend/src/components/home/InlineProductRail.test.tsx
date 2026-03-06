import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { InlineProductRail } from './InlineProductRail';
import type { Product } from '@/features/products/types';

const mockProductFeedCard = vi.hoisted(() => vi.fn());

vi.mock('@/components/products/ProductFeedCard', () => ({
  ProductFeedCard: (props: unknown) => {
    mockProductFeedCard(props);
    const { product } = props as { product: Product };
    return <div data-testid="rail-product-card">{product.name}</div>;
  },
}));

const createProduct = (id: number): Product => ({
  id,
  name: `Rail Ürün ${id}`,
  description: 'Ürün açıklaması',
  price: 500 + id,
  originalPrice: 700 + id,
  currency: 'TRY',
  sku: `SKU-${id}`,
  isActive: true,
  categoryId: 1,
  categoryName: 'Elektronik',
  stockQuantity: 9,
  createdAt: '2026-03-01T10:00:00.000Z',
  averageRating: 4.4,
  reviewCount: 12,
  wishlistCount: 6,
  hasActiveCampaign: false,
  isCampaignFeatured: false,
});

describe('InlineProductRail', () => {
  beforeEach(() => {
    mockProductFeedCard.mockClear();
  });

  it('loading durumunda skeleton kartları render eder', () => {
    const { container } = render(
      <InlineProductRail
        title="Senin İçin Öneriler"
        badgeText="Sana özel seçkiler"
        tone="personalized"
        products={[]}
        isLoading
        isAddingToCart={false}
        isInWishlist={() => false}
        onAddToCart={vi.fn()}
        onWishlistToggle={vi.fn()}
      />,
    );

    expect(screen.queryByTestId('rail-product-card')).not.toBeInTheDocument();
    expect(container.querySelectorAll('[data-slot="card"]').length).toBe(4);
  });

  it('ürünleri compact varyant ile ProductFeedCard üzerinden render eder', () => {
    const onAddToCart = vi.fn();
    const onWishlistToggle = vi.fn();

    render(
      <InlineProductRail
        title="En Çok Favorilenenler"
        badgeText="Bu hafta öne çıkanlar"
        tone="wishlisted"
        products={[createProduct(1), createProduct(2)]}
        isLoading={false}
        isAddingToCart={false}
        isInWishlist={(id) => id === 1}
        onAddToCart={onAddToCart}
        onWishlistToggle={onWishlistToggle}
      />,
    );

    expect(screen.getAllByTestId('rail-product-card')).toHaveLength(2);
    expect(mockProductFeedCard).toHaveBeenCalledTimes(2);
    expect(mockProductFeedCard.mock.calls[0]?.[0]).toEqual(
      expect.objectContaining({
        variant: 'compact',
        isInWishlist: true,
        isAddingToCart: false,
      }),
    );
    expect(mockProductFeedCard.mock.calls[1]?.[0]).toEqual(
      expect.objectContaining({
        variant: 'compact',
        isInWishlist: false,
      }),
    );
  });

  it('rail containerı banner yerine kompakt feed satırı sınıflarını uygular', () => {
    const { container } = render(
      <InlineProductRail
        title="Senin İçin Öneriler"
        badgeText="Sana özel"
        helperText="Kişiselleştirilmiş öneri motoru"
        description="Kısa açıklama"
        tone="personalized"
        products={[createProduct(7)]}
        isLoading={false}
        isAddingToCart={false}
        isInWishlist={() => false}
        onAddToCart={vi.fn()}
        onWishlistToggle={vi.fn()}
      />,
    );

    const section = container.querySelector('section');
    expect(section).toHaveClass('rounded-xl');
    expect(section).not.toHaveClass('rounded-2xl');
    expect(section?.className).toContain('shadow-[0_6px_16px');
    expect(screen.getByText('Sana özel')).toBeInTheDocument();
    expect(screen.getByText('Kişiselleştirilmiş öneri motoru')).toBeInTheDocument();
  });
});
