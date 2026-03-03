import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import OrderDetail from './OrderDetail';

const mockNavigate = vi.fn();

const mockOrdersApi = vi.hoisted(() => ({
  useGetOrderQuery: vi.fn(),
  useCancelOrderMutation: vi.fn(),
  useProcessPaymentMutation: vi.fn(),
  useUpdateOrderItemsMutation: vi.fn(),
}));

const mockReturnsApi = vi.hoisted(() => ({
  useGetMyReturnRequestsQuery: vi.fn(),
}));

const mockProductsApi = vi.hoisted(() => ({
  useSearchProductsQuery: vi.fn(),
}));

const mockCartApi = vi.hoisted(() => ({
  useReorderCartMutation: vi.fn(),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useParams: () => ({ id: '42' }),
    useNavigate: () => mockNavigate,
  };
});

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

vi.mock('@/features/products/productsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/products/productsApi')>('@/features/products/productsApi');
  return {
    ...actual,
    ...mockProductsApi,
  };
});

vi.mock('@/features/cart/cartApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/cart/cartApi')>('@/features/cart/cartApi');
  return {
    ...actual,
    ...mockCartApi,
  };
});

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}));

function createOrder(overrides: Record<string, unknown> = {}) {
  return {
    id: 42,
    orderNumber: 'ORD-42',
    userId: 7,
    status: 'Delivered',
    totalAmount: 3200,
    currency: 'TRY',
    items: [
      {
        id: 501,
        productId: 101,
        productName: 'Kablosuz Klavye',
        quantity: 1,
        priceSnapshot: 1200,
        lineTotal: 1200,
      },
      {
        id: 502,
        productId: 202,
        productName: 'Mouse',
        quantity: 2,
        priceSnapshot: 1000,
        lineTotal: 2000,
      },
    ],
    shippingAddress: 'Kadıköy / İstanbul',
    createdAt: '2026-03-03T10:00:00Z',
    updatedAt: '2026-03-03T10:30:00Z',
    cargoCompany: 'YurticiKargo',
    trackingCode: 'TRK123',
    shippedAt: '2026-03-03T12:00:00Z',
    estimatedDeliveryDate: '2026-03-05T12:00:00Z',
    deliveredAt: '2026-03-04T12:00:00Z',
    shipmentStatus: 'Delivered',
    payment: {
      id: 11,
      orderId: 42,
      amount: 3200,
      status: 'Success',
      transactionId: 'txn_123',
      paymentMethod: 'CreditCard',
      provider: 'Iyzico',
      last4Digits: '4242',
      createdAt: '2026-03-03T10:00:00Z',
    },
    invoiceInfo: {
      type: 'Individual',
      fullName: 'Berkan Sozer',
      invoiceAddress: 'Kadıköy / İstanbul',
    },
    ...overrides,
  };
}

function createReturnRequest(overrides: Record<string, unknown> = {}) {
  return {
    id: 900,
    orderId: 42,
    orderNumber: 'ORD-42',
    userId: 7,
    customerName: 'Berkan Sozer',
    type: 'Return',
    reasonCategory: 'ChangedMind',
    status: 'Pending',
    reason: 'Ürünü iade etmek istiyorum.',
    attachments: [],
    selectedItems: [
      {
        orderItemId: 501,
        productId: 101,
        productName: 'Kablosuz Klavye',
        quantity: 1,
        lineTotal: 1200,
      },
    ],
    requestedRefundAmount: 1200,
    createdAt: '2026-03-04T14:00:00Z',
    ...overrides,
  };
}

describe('OrderDetail sayfası', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockOrdersApi.useCancelOrderMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockOrdersApi.useProcessPaymentMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockOrdersApi.useUpdateOrderItemsMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
    mockReturnsApi.useGetMyReturnRequestsQuery.mockReturnValue({ data: [], isLoading: false });
    mockProductsApi.useSearchProductsQuery.mockReturnValue({ data: undefined });
    mockCartApi.useReorderCartMutation.mockReturnValue([vi.fn(), { isLoading: false }]);
  });

  it('aktif iade talebi varsa timeline gösterip yeni iade CTAsını gizlemeli', () => {
    mockOrdersApi.useGetOrderQuery.mockReturnValue({
      data: createOrder(),
      isLoading: false,
      error: undefined,
    });
    mockReturnsApi.useGetMyReturnRequestsQuery.mockReturnValue({
      data: [createReturnRequest()],
      isLoading: false,
    });

    renderWithProviders(<OrderDetail />);

    expect(screen.getByText('Kargo Takibi')).toBeInTheDocument();
    expect(screen.getByText('İade Süreci')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /tekrar satın al/i })).toBeInTheDocument();
    expect(screen.getByText('•••• 4242')).toBeInTheDocument();
    expect(screen.getByText('Ödeme Alındı')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /iade talebi oluştur/i })).not.toBeInTheDocument();
  });

  it('ödeme bekleyen siparişte iptal ve tekrar ödeme aksiyonlarını göstermeli', () => {
    mockOrdersApi.useGetOrderQuery.mockReturnValue({
      data: createOrder({
        status: 'PendingPayment',
        deliveredAt: undefined,
        shipmentStatus: 'Pending',
        payment: {
          id: 11,
          orderId: 42,
          amount: 3200,
          status: 'Pending',
          transactionId: 'txn_123',
          paymentMethod: 'CreditCard',
          provider: 'Iyzico',
          last4Digits: '4242',
          createdAt: '2026-03-03T10:00:00Z',
        },
      }),
      isLoading: false,
      error: undefined,
    });

    renderWithProviders(<OrderDetail />);

    expect(screen.getByRole('button', { name: 'Siparişi İptal Et' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Tekrar Öde' })).toBeInTheDocument();
    expect(screen.getAllByText('Ödeme Bekleniyor')).toHaveLength(2);
    expect(screen.queryByText('İade Süreci')).not.toBeInTheDocument();
  });

  it('teslim edilmiş ve aktif talebi olmayan siparişte iade talebi CTA göstermeli', () => {
    mockOrdersApi.useGetOrderQuery.mockReturnValue({
      data: createOrder(),
      isLoading: false,
      error: undefined,
    });

    renderWithProviders(<OrderDetail />);

    expect(screen.getByRole('link', { name: 'İade Talebi Oluştur' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'İptal Talebi Oluştur' })).not.toBeInTheDocument();
  });

  it('hazırlanan siparişte iptal talebi CTA göstermeli', () => {
    mockOrdersApi.useGetOrderQuery.mockReturnValue({
      data: createOrder({
        status: 'Processing',
        deliveredAt: undefined,
        shipmentStatus: 'Preparing',
      }),
      isLoading: false,
      error: undefined,
    });

    renderWithProviders(<OrderDetail />);

    expect(screen.getByRole('link', { name: 'İptal Talebi Oluştur' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'İade Talebi Oluştur' })).not.toBeInTheDocument();
  });
});
