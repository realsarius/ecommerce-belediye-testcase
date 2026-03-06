import { describe, expect, it } from 'vitest';
import {
  buildDedupedRailItems,
  FIRST_RAIL_INSERT_INDEX,
  getRailFetchFlags,
  getRailRenderFlags,
  SECOND_RAIL_INSERT_INDEX,
  splitProductsForInlineRails,
} from './productFeedRailRules';
import type { Product } from '@/features/products/types';

const createProduct = (id: number, wishlistCount = 2): Product => ({
  id,
  name: `Ürün ${id}`,
  description: 'Açıklama',
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
  reviewCount: 10,
  wishlistCount,
  hasActiveCampaign: false,
  isCampaignFeatured: false,
});

describe('productFeedRailRules', () => {
  it('ürünleri 8/16 eşiklerine göre segmentlere ayırır', () => {
    const products = Array.from({ length: 22 }, (_, index) => createProduct(index + 1));
    const { firstSegment, secondSegment, remainingSegment } = splitProductsForInlineRails(products);

    expect(firstSegment).toHaveLength(FIRST_RAIL_INSERT_INDEX);
    expect(secondSegment).toHaveLength(SECOND_RAIL_INSERT_INDEX - FIRST_RAIL_INSERT_INDEX);
    expect(remainingSegment).toHaveLength(22 - SECOND_RAIL_INSERT_INDEX);
  });

  it('fetch flaglerini discovery/auth ve ürün sayısına göre üretir', () => {
    const withAllConditions = getRailFetchFlags({
      hasDiscoveryFeedContext: true,
      isAuthenticated: true,
      totalProducts: 20,
    });
    const withoutEnoughProducts = getRailFetchFlags({
      hasDiscoveryFeedContext: true,
      isAuthenticated: true,
      totalProducts: 8,
    });

    expect(withAllConditions.shouldFetchPersonalizedRail).toBe(true);
    expect(withAllConditions.shouldFetchTopWishlistedRail).toBe(true);
    expect(withoutEnoughProducts.shouldFetchPersonalizedRail).toBe(false);
    expect(withoutEnoughProducts.shouldFetchTopWishlistedRail).toBe(false);
  });

  it('feeddeki ve cross-rail tekrarları dedupe eder', () => {
    const feedProducts = [createProduct(1), createProduct(2), createProduct(3)];
    const personalizedCandidates = [createProduct(1), createProduct(101), createProduct(102)];
    const topCandidates = [createProduct(2, 99), createProduct(101, 88), createProduct(201, 77)];

    const { personalizedItems, topWishlistedItems } = buildDedupedRailItems({
      feedProducts,
      personalizedCandidates,
      topWishlistedCandidates: topCandidates,
    });

    expect(personalizedItems.map((item) => item.id)).toEqual([101, 102]);
    expect(topWishlistedItems.map((item) => item.id)).toEqual([201]);
  });

  it('wishlistCount 0 olan top wishlisted ürünleri dışlar', () => {
    const { topWishlistedItems } = buildDedupedRailItems({
      feedProducts: [],
      personalizedCandidates: [],
      topWishlistedCandidates: [createProduct(301, 0), createProduct(302, 11)],
    });

    expect(topWishlistedItems.map((item) => item.id)).toEqual([302]);
  });

  it('render flaglerini loading veya içerik varlığına göre döner', () => {
    const loadingState = getRailRenderFlags({
      shouldFetchPersonalizedRail: true,
      shouldFetchTopWishlistedRail: true,
      isPersonalizedLoading: true,
      isTopWishlistedLoading: true,
      personalizedCount: 0,
      topWishlistedCount: 0,
    });
    const dataState = getRailRenderFlags({
      shouldFetchPersonalizedRail: true,
      shouldFetchTopWishlistedRail: true,
      isPersonalizedLoading: false,
      isTopWishlistedLoading: false,
      personalizedCount: 2,
      topWishlistedCount: 1,
    });
    const hiddenState = getRailRenderFlags({
      shouldFetchPersonalizedRail: false,
      shouldFetchTopWishlistedRail: false,
      isPersonalizedLoading: false,
      isTopWishlistedLoading: false,
      personalizedCount: 4,
      topWishlistedCount: 4,
    });

    expect(loadingState.shouldRenderPersonalizedRail).toBe(true);
    expect(loadingState.shouldRenderTopWishlistedRail).toBe(true);
    expect(dataState.shouldRenderPersonalizedRail).toBe(true);
    expect(dataState.shouldRenderTopWishlistedRail).toBe(true);
    expect(hiddenState.shouldRenderPersonalizedRail).toBe(false);
    expect(hiddenState.shouldRenderTopWishlistedRail).toBe(false);
  });
});
