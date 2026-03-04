import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import Returns from './Returns';

const mockOrdersApi = vi.hoisted(() => ({
  useGetOrdersQuery: vi.fn(),
}));

const mockReturnsApi = vi.hoisted(() => ({
  useCreateReturnRequestMutation: vi.fn(),
  useGetMyReturnRequestsQuery: vi.fn(),
  useLazyGetReturnAttachmentAccessUrlQuery: vi.fn(),
  useUploadReturnPhotosMutation: vi.fn(),
}));

const mockSettingsApi = vi.hoisted(() => ({
  useGetFrontendFeaturesQuery: vi.fn(),
}));

vi.mock('@/features/orders/ordersApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/orders/ordersApi')>('@/features/orders/ordersApi');
  return {
    ...actual,
    ...mockOrdersApi,
  };
});

vi.mock('@/features/returns/returnsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/returns/returnsApi')>('@/features/returns/returnsApi');
  return {
    ...actual,
    ...mockReturnsApi,
  };
});

vi.mock('@/features/settings/settingsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/settings/settingsApi')>('@/features/settings/settingsApi');
  return {
    ...actual,
    ...mockSettingsApi,
  };
});

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}));

describe('Returns migration-safe render', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockOrdersApi.useGetOrdersQuery.mockReturnValue({
      data: [
        {
          id: 42,
          orderNumber: 'ORD-42',
          status: 'Delivered',
          totalAmount: 229.8,
          currency: 'TRY',
          deliveredAt: '2026-03-04T07:22:38.548421Z',
          items: [
            {
              id: 501,
              productId: 207,
              productName: 'Smoke Seller Product 19E7',
              quantity: 1,
              priceSnapshot: 199.9,
              lineTotal: 199.9,
            },
          ],
        },
      ],
      isLoading: false,
    });

    mockReturnsApi.useGetMyReturnRequestsQuery.mockReturnValue({
      data: [
        {
          id: 4,
          orderId: 42,
          orderNumber: 'ORD-42',
          userId: 2,
          customerName: 'Test Customer',
          type: 'Return',
          reasonCategory: 'ChangedMind',
          status: 'Pending',
          reason: 'Numara beklentimi karsilamadi',
          requestNote: null,
          requestWindowEndsAt: null,
          selectedItems: null,
          attachments: null,
          requestedRefundAmount: 229.8,
          paymentStatus: 'Success',
          reviewedByUserId: null,
          reviewerName: null,
          reviewNote: null,
          reviewedAt: null,
          refundRequestId: null,
          refundProvider: null,
          refundStatus: null,
          createdAt: '2026-03-04T07:22:38.548421Z',
        },
      ],
      isLoading: false,
    });

    mockReturnsApi.useCreateReturnRequestMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockReturnsApi.useUploadReturnPhotosMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockReturnsApi.useLazyGetReturnAttachmentAccessUrlQuery.mockReturnValue([vi.fn()]);

    mockSettingsApi.useGetFrontendFeaturesQuery.mockReturnValue({
      data: {
        enableCheckoutLegalConsents: true,
        enableCheckoutInvoiceInfo: true,
        enableShipmentTimeline: true,
        enableReturnAttachments: true,
      },
    });
  });

  it('selectedItems ve attachments null gelse bile sayfayi cokturtmemeli', () => {
    renderWithProviders(<Returns />, { route: '/returns?orderId=42' });

    expect(screen.getByRole('heading', { name: 'İade ve İptal Taleplerim' })).toBeVisible();
    expect(screen.getByText('Numara beklentimi karsilamadi')).toBeVisible();
    expect(screen.getByText('İnceleniyor')).toBeVisible();
  });
});
