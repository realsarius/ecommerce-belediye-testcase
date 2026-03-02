// Seller types
export interface SellerProfile {
  id: number;
  userId: number;
  brandName: string;
  brandDescription?: string;
  logoUrl?: string;
  bannerImageUrl?: string;
  contactEmail?: string;
  contactPhone?: string;
  websiteUrl?: string;
  instagramUrl?: string;
  facebookUrl?: string;
  xUrl?: string;
  isVerified: boolean;
  createdAt: string;
  sellerFirstName: string;
  sellerLastName: string;
}

export interface CreateSellerProfileRequest {
  brandName: string;
  brandDescription?: string;
  logoUrl?: string;
  bannerImageUrl?: string;
  contactEmail?: string;
  contactPhone?: string;
  websiteUrl?: string;
  instagramUrl?: string;
  facebookUrl?: string;
  xUrl?: string;
}

export interface UpdateSellerProfileRequest {
  brandName?: string;
  brandDescription?: string;
  logoUrl?: string;
  bannerImageUrl?: string;
  contactEmail?: string;
  contactPhone?: string;
  websiteUrl?: string;
  instagramUrl?: string;
  facebookUrl?: string;
  xUrl?: string;
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

export interface SellerFinanceTrendPoint {
  date: string;
  orders: number;
  grossSales: number;
  netSales: number;
  commissionAmount: number;
  netEarnings: number;
}

export interface SellerFinanceMonthlySummary {
  monthKey: string;
  monthLabel: string;
  orders: number;
  grossSales: number;
  netSales: number;
  commissionAmount: number;
  netEarnings: number;
}

export interface SellerFinanceSummary {
  periodDays: number;
  totalOrders: number;
  grossSales: number;
  refundedAmount: number;
  netSales: number;
  commissionRate: number;
  commissionAmount: number;
  netEarnings: number;
  averageOrderValue: number;
  averageDailyRevenue: number;
  lifetimeGrossRevenue: number;
  currency: string;
  dailyTrend: SellerFinanceTrendPoint[];
  monthlySummaries: SellerFinanceMonthlySummary[];
}
