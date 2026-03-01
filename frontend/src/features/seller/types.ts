// Seller types
export interface SellerProfile {
  id: number;
  userId: number;
  brandName: string;
  brandDescription?: string;
  logoUrl?: string;
  isVerified: boolean;
  createdAt: string;
  sellerFirstName: string;
  sellerLastName: string;
}

export interface CreateSellerProfileRequest {
  brandName: string;
  brandDescription?: string;
  logoUrl?: string;
}

export interface UpdateSellerProfileRequest {
  brandName?: string;
  brandDescription?: string;
  logoUrl?: string;
}

export interface HasProfileResponse {
  hasProfile: boolean;
}

export interface SellerAnalyticsSummary {
  totalProducts: number;
  activeProducts: number;
  totalViews: number;
  totalWishlistCount: number;
  favoriteRate: number;
  conversionRate: number;
  averageRating: number;
  reviewCount: number;
  returnRate: number;
  successfulOrderCount: number;
  returnedRequestCount: number;
  grossRevenue: number;
  currency: string;
}

export interface SellerAnalyticsTrendPoint {
  date: string;
  views: number;
  favorites: number;
  orders: number;
  revenue: number;
  averageRating: number;
}
