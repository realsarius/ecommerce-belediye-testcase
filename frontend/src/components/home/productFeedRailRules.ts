import type { Product } from '@/features/products/types';

export const FIRST_RAIL_INSERT_INDEX = 8;
export const SECOND_RAIL_INSERT_INDEX = 16;
export const EARLY_FIRST_RAIL_INSERT_INDEX = 4;
export const EARLY_SECOND_RAIL_INSERT_INDEX = 12;
export const PERSONALIZED_RAIL_LIMIT = 6;
export const TOP_WISHLISTED_RAIL_LIMIT = 8;

export interface RailInsertionConfig {
  firstRailInsertIndex: number;
  secondRailInsertIndex: number;
}

export const DEFAULT_RAIL_INSERTION_CONFIG: RailInsertionConfig = {
  firstRailInsertIndex: FIRST_RAIL_INSERT_INDEX,
  secondRailInsertIndex: SECOND_RAIL_INSERT_INDEX,
};

export const EARLY_RAIL_INSERTION_CONFIG: RailInsertionConfig = {
  firstRailInsertIndex: EARLY_FIRST_RAIL_INSERT_INDEX,
  secondRailInsertIndex: EARLY_SECOND_RAIL_INSERT_INDEX,
};

export function getRailInsertionConfig(mode: string | null): RailInsertionConfig {
  return mode === 'early'
    ? EARLY_RAIL_INSERTION_CONFIG
    : DEFAULT_RAIL_INSERTION_CONFIG;
}

interface RailFetchFlagsArgs {
  hasDiscoveryFeedContext: boolean;
  isAuthenticated: boolean;
  totalProducts: number;
  insertionConfig?: RailInsertionConfig;
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

export function splitProductsForInlineRails(
  products: Product[],
  insertionConfig: RailInsertionConfig = DEFAULT_RAIL_INSERTION_CONFIG,
) {
  const { firstRailInsertIndex, secondRailInsertIndex } = insertionConfig;

  return {
    firstSegment: products.slice(0, firstRailInsertIndex),
    secondSegment: products.slice(firstRailInsertIndex, secondRailInsertIndex),
    remainingSegment: products.slice(secondRailInsertIndex),
  };
}

export function getRailFetchFlags({
  hasDiscoveryFeedContext,
  isAuthenticated,
  totalProducts,
  insertionConfig = DEFAULT_RAIL_INSERTION_CONFIG,
}: RailFetchFlagsArgs) {
  const canInsertFirstRail = totalProducts > insertionConfig.firstRailInsertIndex;
  const canInsertSecondRail = totalProducts > insertionConfig.secondRailInsertIndex;

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
