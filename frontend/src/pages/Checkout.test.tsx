import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import Checkout from './Checkout';

const mockNavigate = vi.fn();

const mockCartApi = vi.hoisted(() => ({
  useGetCartQuery: vi.fn(),
  useClearCartMutation: vi.fn(),
}));

const mockAdminApi = vi.hoisted(() => ({
  useGetAddressesQuery: vi.fn(),
  useCreateAddressMutation: vi.fn(),
}));

const mockOrdersApi = vi.hoisted(() => ({
  useCheckoutMutation: vi.fn(),
  useGetPaymentSettingsQuery: vi.fn(),
  useProcessPaymentMutation: vi.fn(),
}));

const mockCreditCardsApi = vi.hoisted(() => ({
  useGetCreditCardsQuery: vi.fn(),
}));

const mockSettingsApi = vi.hoisted(() => ({
  useGetFrontendFeaturesQuery: vi.fn(),
}));

const mockCouponsApi = vi.hoisted(() => ({
  useValidateCouponMutation: vi.fn(),
}));

const mockLoyaltyApi = vi.hoisted(() => ({
  useGetLoyaltySummaryQuery: vi.fn(),
}));

const mockGiftCardsApi = vi.hoisted(() => ({
  useGetGiftCardSummaryQuery: vi.fn(),
  useValidateGiftCardMutation: vi.fn(),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ pathname: '/checkout', search: '', state: null }),
  };
});

vi.mock('@/features/cart/cartApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/cart/cartApi')>('@/features/cart/cartApi');
  return {
    ...actual,
    ...mockCartApi,
  };
});

vi.mock('@/features/admin/adminApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/admin/adminApi')>('@/features/admin/adminApi');
  return {
    ...actual,
    ...mockAdminApi,
  };
});

vi.mock('@/features/orders/ordersApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/orders/ordersApi')>('@/features/orders/ordersApi');
  return {
    ...actual,
    ...mockOrdersApi,
  };
});

vi.mock('@/features/creditCards/creditCardsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/creditCards/creditCardsApi')>('@/features/creditCards/creditCardsApi');
  return {
    ...actual,
    ...mockCreditCardsApi,
  };
});

vi.mock('@/features/settings/settingsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/settings/settingsApi')>('@/features/settings/settingsApi');
  return {
    ...actual,
    ...mockSettingsApi,
  };
});

vi.mock('@/features/coupons/couponsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/coupons/couponsApi')>('@/features/coupons/couponsApi');
  return {
    ...actual,
    ...mockCouponsApi,
  };
});

vi.mock('@/features/loyalty/loyaltyApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/loyalty/loyaltyApi')>('@/features/loyalty/loyaltyApi');
  return {
    ...actual,
    ...mockLoyaltyApi,
  };
});

vi.mock('@/features/giftCards/giftCardsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/giftCards/giftCardsApi')>('@/features/giftCards/giftCardsApi');
  return {
    ...actual,
    ...mockGiftCardsApi,
  };
});

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}));

describe('Checkout legal consent davranışı', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockCartApi.useGetCartQuery.mockReturnValue({
      data: {
        items: [
          {
            id: 1,
            productId: 101,
            productName: 'Kablosuz Klavye',
            quantity: 1,
            unitPrice: 1499,
            totalPrice: 1499,
            imageUrl: null,
            stockQuantity: 12,
          },
        ],
        totalItems: 1,
        totalAmount: 1499,
      },
      isLoading: false,
    });
    mockCartApi.useClearCartMutation.mockReturnValue([vi.fn(), { isLoading: false }]);

    mockAdminApi.useGetAddressesQuery.mockReturnValue({
      data: [
        {
          id: 11,
          title: 'Ev',
          fullName: 'Berkan Sözer',
          phone: '05551234567',
          city: 'İstanbul',
          district: 'Kadıköy',
          addressLine: 'Moda Caddesi No: 10',
          postalCode: '34710',
          isDefault: true,
        },
      ],
      isLoading: false,
    });
    mockAdminApi.useCreateAddressMutation.mockReturnValue([vi.fn(), { isLoading: false }]);

    mockOrdersApi.useCheckoutMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockOrdersApi.useProcessPaymentMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockOrdersApi.useGetPaymentSettingsQuery.mockReturnValue({
      data: {
        activeProviders: ['Iyzico'],
        defaultProvider: 'Iyzico',
        enableMultiProviderSelection: false,
        enableTokenizedCardSave: false,
        allowLegacyEncryptedSavedCardPayments: true,
        force3DSecure: false,
        force3DSecureAbove: 5000,
      },
    });

    mockCreditCardsApi.useGetCreditCardsQuery.mockReturnValue({ data: [] });
    mockSettingsApi.useGetFrontendFeaturesQuery.mockReturnValue({
      data: {
        enableCheckoutLegalConsents: true,
        enableCheckoutInvoiceInfo: true,
        enableShipmentTimeline: true,
        enableReturnAttachments: true,
      },
    });

    mockCouponsApi.useValidateCouponMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockLoyaltyApi.useGetLoyaltySummaryQuery.mockReturnValue({ data: { availablePoints: 0 } });
    mockGiftCardsApi.useGetGiftCardSummaryQuery.mockReturnValue({ data: { availableBalance: 0 } });
    mockGiftCardsApi.useValidateGiftCardMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
  });

  it('iki yasal onay tamamlanmadan siparisi tamamla butonunu aktif etmemeli', async () => {
    const user = userEvent.setup();

    renderWithProviders(<Checkout />, { route: '/checkout' });

    const completeOrderButton = screen.getByRole('button', { name: /siparişi tamamla/i });
    expect(completeOrderButton).toBeDisabled();

    await user.click(screen.getByRole('button', { name: 'Ön Bilgilendirme Formu' }));
    await user.click(screen.getByRole('button', { name: 'Kabul Ediyorum' }));

    expect(completeOrderButton).toBeDisabled();

    await user.click(screen.getByRole('button', { name: 'Mesafeli Satış Sözleşmesi' }));
    await user.click(screen.getByRole('button', { name: 'Kabul Ediyorum' }));

    expect(completeOrderButton).toBeEnabled();
  });
});
