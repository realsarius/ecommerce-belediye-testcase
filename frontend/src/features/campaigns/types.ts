export const CampaignType = {
  FlashSale: 1,
  Seasonal: 2,
  Highlight: 3,
} as const;

export type CampaignType = (typeof CampaignType)[keyof typeof CampaignType];

export const CampaignStatus = {
  Draft: 1,
  Scheduled: 2,
  Active: 3,
  Ended: 4,
} as const;

export type CampaignStatus = (typeof CampaignStatus)[keyof typeof CampaignStatus];

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
  type: CampaignType | string;
  status: CampaignStatus | string;
  isEnabled: boolean;
  startsAt: string;
  endsAt: string;
  products: CampaignProduct[];
}

export interface CreateCampaignProductRequest {
  productId: number;
  campaignPrice: number;
  isFeatured: boolean;
}

export interface CreateCampaignRequest {
  name: string;
  description?: string;
  badgeText?: string;
  type: CampaignType;
  isEnabled: boolean;
  startsAt: string;
  endsAt: string;
  products: CreateCampaignProductRequest[];
}

export interface UpdateCampaignRequest {
  name?: string;
  description?: string;
  badgeText?: string;
  type?: CampaignType;
  isEnabled?: boolean;
  startsAt?: string;
  endsAt?: string;
  products?: CreateCampaignProductRequest[];
}
