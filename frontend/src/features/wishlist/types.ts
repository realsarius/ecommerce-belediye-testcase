export interface WishlistItem {
    id: number;
    productId: number;
    productName: string;
    productPrice: number;
    productCurrency: string;
    productImageUrl?: string;
    isAvailable: boolean;
    unavailableReason?: string;
    addedAt: string;
    addedAtPrice: number;
    priceChange?: number;
    priceChangePercentage?: number;
}

export interface Wishlist {
    id: number;
    userId: number;
    limit?: number;
    hasMore?: boolean;
    nextCursor?: string | null;
    items: WishlistItem[];
}

export interface AddWishlistItemRequest {
    productId: number;
}

export interface GetWishlistRequest {
    cursor?: string;
    limit?: number;
}

export interface WishlistPriceAlert {
    id: number;
    productId: number;
    productName: string;
    currency: string;
    currentPrice: number;
    targetPrice: number;
    isActive: boolean;
    lastTriggeredPrice?: number | null;
    lastNotifiedAt?: string | null;
    createdAt: string;
}

export interface UpsertWishlistPriceAlertRequest {
    productId: number;
    targetPrice: number;
}

export interface WishlistPriceAlertNotification {
    productId: number;
    productName: string;
    targetPrice: number;
    oldPrice: number;
    newPrice: number;
    currency: string;
    occurredAt: string;
}
