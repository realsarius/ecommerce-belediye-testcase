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
