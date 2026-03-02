// Product Types

export type ReviewModerationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  originalPrice: number;
  currency: string;
  sku: string;
  isActive: boolean;
  categoryId: number;
  categoryName: string;
  stockQuantity: number;
  sellerId?: number | null;
  sellerBrandName?: string | null;
  createdAt: string;
  averageRating: number;
  reviewCount: number;
  wishlistCount: number;
  hasActiveCampaign: boolean;
  campaignPrice?: number | null;
  campaignName?: string | null;
  campaignBadgeText?: string | null;
  campaignEndsAt?: string | null;
  isCampaignFeatured: boolean;
}

export interface ProductReviewDto {
  id: number;
  productId: number;
  productName: string;
  userId: number;
  userFullName: string;
  rating: number;
  comment: string;
  sellerReply?: string | null;
  sellerRepliedByUserId?: number | null;
  sellerRepliedAt?: string | null;
  moderationStatus: ReviewModerationStatus;
  moderationNote?: string | null;
  moderatedByUserId?: number | null;
  moderatedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ReviewSummaryDto {
  averageRating: number;
  totalReviews: number;
  ratingDistribution: Record<number, number>;
}

export interface CreateReviewRequest {
  rating: number;
  comment: string;
}

export interface ReviewModerationRequest {
  moderationNote?: string;
}

export interface SellerReviewReplyRequest {
  replyText: string;
}

export interface ProductListRequest {
  page?: number;
  pageSize?: number;
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  search?: string;
  inStock?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  price: number;
  currency: string;
  sku: string;
  isActive: boolean;
  categoryId: number;
  initialStock: number;
}

export interface UpdateProductRequest {
  name?: string;
  description?: string;
  price?: number;
  currency?: string;
  sku?: string;
  isActive?: boolean;
  categoryId?: number;
  stockQuantity?: number;
}

export interface UpdateStockRequest {
  quantityChange: number;
  reason: string;
}

// Category Types (related to products)
export interface Category {
  id: number;
  name: string;
  isActive: boolean;
  productCount: number;
}

export interface CreateCategoryRequest {
  name: string;
  isActive: boolean;
}

export interface UpdateCategoryRequest {
  name?: string;
  isActive?: boolean;
}
