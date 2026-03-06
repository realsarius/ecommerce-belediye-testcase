import { describe, expect, it, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test-utils';
import { ActiveCampaignSpotlight } from './ActiveCampaignSpotlight';
import type { Campaign } from '@/features/campaigns/types';

const mockCampaignsApi = vi.hoisted(() => ({
  useGetActiveCampaignsQuery: vi.fn(),
  useTrackCampaignInteractionMutation: vi.fn(),
}));

vi.mock('@/features/campaigns/campaignsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/campaigns/campaignsApi')>('@/features/campaigns/campaignsApi');
  return {
    ...actual,
    ...mockCampaignsApi,
  };
});

vi.mock('@/features/campaigns/campaignSession', () => ({
  getCampaignSessionId: () => 'session-test',
}));

vi.mock('@/components/campaigns/CampaignCountdown', () => ({
  CampaignCountdown: ({ className }: { className?: string }) => (
    <span className={className}>Kalan süre</span>
  ),
}));

const createCampaign = (id: number): Campaign => ({
  id,
  name: `Kampanya ${id}`,
  description: `Kampanya ${id} açıklaması`,
  badgeText: `Badge ${id}`,
  type: 'FlashSale',
  status: 'Active',
  isEnabled: true,
  startsAt: '2026-03-01T10:00:00.000Z',
  endsAt: '2026-03-30T10:00:00.000Z',
  products: [
    {
      productId: id * 10 + 1,
      productName: `Ürün ${id}-1`,
      productSku: `SKU-${id}-1`,
      originalPrice: 1200,
      campaignPrice: 999,
      isFeatured: true,
    },
  ],
});

describe('ActiveCampaignSpotlight', () => {
  const trackInteractionMock = vi.fn(() => ({
    unwrap: () => Promise.resolve(),
  }));

  beforeEach(() => {
    vi.clearAllMocks();
    mockCampaignsApi.useTrackCampaignInteractionMutation.mockReturnValue([trackInteractionMock]);
    mockCampaignsApi.useGetActiveCampaignsQuery.mockReturnValue({
      data: [],
      isLoading: false,
    });
  });

  it('aktif kampanya yoksa render etmez', () => {
    renderWithProviders(<ActiveCampaignSpotlight />);
    expect(screen.queryByRole('heading', { name: 'Canlı Kampanyalar' })).not.toBeInTheDocument();
  });

  it('campaign kartlarında responsive görünürlük sınıfları uygular', () => {
    mockCampaignsApi.useGetActiveCampaignsQuery.mockReturnValue({
      data: [createCampaign(1), createCampaign(2), createCampaign(3)],
      isLoading: false,
    });

    renderWithProviders(<ActiveCampaignSpotlight />);

    const firstCard = screen.getByText('Kampanya 1').closest('[data-slot="card"]');
    const secondCard = screen.getByText('Kampanya 2').closest('[data-slot="card"]');
    const thirdCard = screen.getByText('Kampanya 3').closest('[data-slot="card"]');

    expect(firstCard).not.toBeNull();
    expect(secondCard).not.toBeNull();
    expect(thirdCard).not.toBeNull();
    expect(firstCard).not.toHaveClass('hidden');
    expect(secondCard).toHaveClass('hidden', 'md:block');
    expect(thirdCard).toHaveClass('hidden', 'lg:block');
  });

  it('kampanya impression etkileşimlerini campaign başına tetikler', async () => {
    mockCampaignsApi.useGetActiveCampaignsQuery.mockReturnValue({
      data: [createCampaign(11), createCampaign(22), createCampaign(33)],
      isLoading: false,
    });

    renderWithProviders(<ActiveCampaignSpotlight />);

    await waitFor(() => {
      expect(trackInteractionMock).toHaveBeenCalledTimes(3);
    });

    expect(trackInteractionMock).toHaveBeenCalledWith({
      campaignId: 11,
      interactionType: 'impression',
      sessionId: 'session-test',
    });
    expect(trackInteractionMock).toHaveBeenCalledWith({
      campaignId: 22,
      interactionType: 'impression',
      sessionId: 'session-test',
    });
    expect(trackInteractionMock).toHaveBeenCalledWith({
      campaignId: 33,
      interactionType: 'impression',
      sessionId: 'session-test',
    });
  });
});
