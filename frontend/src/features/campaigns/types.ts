export interface CampaignProduct {
  productId: number;
  productName: string;
  productSku: string;
  originalPrice: number;
  campaignPrice: number;
  isFeatured: boolean;
}

export interface Campaign {
  id: number;
  name: string;
  description?: string | null;
  badgeText?: string | null;
  type: string;
  status: string;
  isEnabled: boolean;
  startsAt: string;
  endsAt: string;
  products: CampaignProduct[];
}
