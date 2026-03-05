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
  primaryImageUrl?: string | null;
  images?: ProductImage[];
  variants?: ProductVariant[];
}

export interface ProductImage {
  id?: number;
  imageUrl: string;
  objectKey?: string;
  sortOrder: number;
  isPrimary: boolean;
}

export interface ProductVariant {
  id?: number;
  name: string;
  value: string;
  sortOrder: number;
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
  images?: ProductImage[];
  variants?: ProductVariant[];
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
  images?: ProductImage[];
  variants?: ProductVariant[];
}

export interface BulkUpdateProductsRequest {
  ids: number[];
  action: 'activate' | 'deactivate' | 'delete';
}

export interface UpdateStockRequest {
  quantityChange: number;
  reason: string;
}

// Category Types (related to products)
export interface Category {
  id: number;
  name: string;
  description: string;
  imageUrl?: string | null;
  isActive: boolean;
  parentCategoryId?: number | null;
  sortOrder: number;
  productCount: number;
  childCount: number;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
  isActive: boolean;
  parentCategoryId?: number | null;
}

export interface UpdateCategoryRequest {
  name?: string;
  description?: string;
  isActive?: boolean;
  parentCategoryId?: number | null;
  sortOrder?: number;
}

export interface ReorderCategoryItemRequest {
  id: number;
  parentCategoryId?: number | null;
  sortOrder: number;
}

export interface ReorderCategoriesRequest {
  items: ReorderCategoryItemRequest[];
}
