// Product Types

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  currency: string;
  sku: string;
  isActive: boolean;
  categoryId: number;
  categoryName: string;
  stockQuantity: number;
  createdAt: string;
  averageRating: number;
  reviewCount: number;
}

export interface ProductReviewDto {
  id: number;
  productId: number;
  userId: number;
  userFullName: string;
  rating: number;
  comment: string;
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
