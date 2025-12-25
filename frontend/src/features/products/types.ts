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
