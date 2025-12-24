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
