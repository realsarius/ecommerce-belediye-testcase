import type { Product } from '@/features/products/types';

export const FIRST_RAIL_INSERT_INDEX = 8;
export const SECOND_RAIL_INSERT_INDEX = 16;
export const PERSONALIZED_RAIL_LIMIT = 6;
export const TOP_WISHLISTED_RAIL_LIMIT = 8;

interface RailFetchFlagsArgs {
  hasDiscoveryFeedContext: boolean;
  isAuthenticated: boolean;
  totalProducts: number;
}

interface RailRenderFlagsArgs {
  shouldFetchPersonalizedRail: boolean;
  shouldFetchTopWishlistedRail: boolean;
  isPersonalizedLoading: boolean;
  isTopWishlistedLoading: boolean;
  personalizedCount: number;
  topWishlistedCount: number;
}

interface DedupedRailItemsArgs {
  feedProducts: Product[];
  personalizedCandidates: Product[];
  topWishlistedCandidates: Product[];
}

export function splitProductsForInlineRails(products: Product[]) {
  return {
    firstSegment: products.slice(0, FIRST_RAIL_INSERT_INDEX),
    secondSegment: products.slice(FIRST_RAIL_INSERT_INDEX, SECOND_RAIL_INSERT_INDEX),
    remainingSegment: products.slice(SECOND_RAIL_INSERT_INDEX),
  };
}

export function getRailFetchFlags({
  hasDiscoveryFeedContext,
  isAuthenticated,
  totalProducts,
}: RailFetchFlagsArgs) {
  const canInsertFirstRail = totalProducts > FIRST_RAIL_INSERT_INDEX;
  const canInsertSecondRail = totalProducts > SECOND_RAIL_INSERT_INDEX;

  return {
    shouldFetchPersonalizedRail: hasDiscoveryFeedContext && isAuthenticated && canInsertFirstRail,
    shouldFetchTopWishlistedRail: hasDiscoveryFeedContext && canInsertSecondRail,
  };
}

export function buildDedupedRailItems({
  feedProducts,
  personalizedCandidates,
  topWishlistedCandidates,
}: DedupedRailItemsArgs) {
  const productIdsInFeed = new Set(feedProducts.map((product) => product.id));

  const personalizedItems = personalizedCandidates
    .filter((item) => !productIdsInFeed.has(item.id))
    .slice(0, PERSONALIZED_RAIL_LIMIT);
  const personalizedIds = new Set(personalizedItems.map((item) => item.id));

  const topWishlistedItems = topWishlistedCandidates
    .filter((item) => item.wishlistCount > 0)
    .filter((item) => !productIdsInFeed.has(item.id))
    .filter((item) => !personalizedIds.has(item.id))
    .slice(0, TOP_WISHLISTED_RAIL_LIMIT);

  return {
    personalizedItems,
    topWishlistedItems,
  };
}

export function getRailRenderFlags({
  shouldFetchPersonalizedRail,
  shouldFetchTopWishlistedRail,
  isPersonalizedLoading,
  isTopWishlistedLoading,
  personalizedCount,
  topWishlistedCount,
}: RailRenderFlagsArgs) {
  return {
    shouldRenderPersonalizedRail:
      shouldFetchPersonalizedRail && (isPersonalizedLoading || personalizedCount > 0),
    shouldRenderTopWishlistedRail:
      shouldFetchTopWishlistedRail && (isTopWishlistedLoading || topWishlistedCount > 0),
  };
}
