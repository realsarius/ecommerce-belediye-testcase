import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import OrdersAdmin from './admin/OrdersAdmin';
import SellerOrders from './seller/Orders';

const mockAdminApi = vi.hoisted(() => ({
  useGetAdminOrdersQuery: vi.fn(),
  useUpdateOrderStatusMutation: vi.fn(),
}));

const mockProductsApi = vi.hoisted(() => ({
  useSearchProductsQuery: vi.fn(),
}));

const mockSellerApi = vi.hoisted(() => ({
  useGetSellerOrdersQuery: vi.fn(),
  useShipSellerOrderMutation: vi.fn(),
}));

vi.mock('@/features/admin/adminApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/admin/adminApi')>('@/features/admin/adminApi');
  return {
    ...actual,
    ...mockAdminApi,
  };
});

vi.mock('@/features/products/productsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/products/productsApi')>('@/features/products/productsApi');
  return {
    ...actual,
    ...mockProductsApi,
  };
});

vi.mock('@/features/seller/sellerApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/seller/sellerApi')>('@/features/seller/sellerApi');
  return {
    ...actual,
    ...mockSellerApi,
  };
});

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

function setupAdminOrderMocks() {
  mockAdminApi.useGetAdminOrdersQuery.mockReturnValue({
    data: [
      {
        id: 1,
        orderNumber: 'ORD-1',
        customerName: 'Ali Yılmaz',
        totalAmount: 500,
        currency: 'TRY',
        status: 'Paid',
        createdAt: '2026-03-01T10:00:00Z',
        items: [{ productId: 101 }],
        userId: 11,
      },
      {
        id: 2,
        orderNumber: 'ORD-2',
        customerName: 'Ayşe Demir',
        totalAmount: 1500,
        currency: 'TRY',
        status: 'Delivered',
        createdAt: '2026-03-02T10:00:00Z',
        items: [{ productId: 202 }],
        userId: 12,
      },
    ],
    isLoading: false,
  });

  mockProductsApi.useSearchProductsQuery.mockReturnValue({
    data: {
      items: [
        { id: 101, sellerBrandName: 'Tekno Market', sellerId: 1 },
        { id: 202, sellerBrandName: 'Moda Store', sellerId: 2 },
      ],
    },
  });

  mockAdminApi.useUpdateOrderStatusMutation.mockReturnValue([
    vi.fn(() => ({
      unwrap: async () => undefined,
    })),
  ]);
}

function setupSellerOrderMocks() {
  mockSellerApi.useGetSellerOrdersQuery.mockReturnValue({
    data: [
      {
        id: 11,
        orderNumber: 'SELL-1',
        customerName: 'Ali Veli',
        totalAmount: 700,
        currency: 'TRY',
        status: 'Paid',
        createdAt: '2026-03-01T10:00:00Z',
        items: [{ productId: 1 }],
      },
      {
        id: 12,
        orderNumber: 'SELL-2',
        customerName: 'Zeynep Kaya',
        totalAmount: 900,
        currency: 'TRY',
        status: 'Delivered',
        createdAt: '2026-03-03T10:00:00Z',
        items: [{ productId: 2 }],
      },
    ],
    isLoading: false,
  });

  mockSellerApi.useShipSellerOrderMutation.mockReturnValue([
    vi.fn(() => ({
      unwrap: async () => undefined,
    })),
    { isLoading: false },
  ]);
}

describe('Kritik tablo filtreleme testleri', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('Admin sipariş tablosu arama ile seller adına göre filtrelenmeli', async () => {
    setupAdminOrderMocks();
    const user = userEvent.setup();

    renderWithProviders(<OrdersAdmin />);

    await user.type(screen.getByPlaceholderText('Sipariş no veya müşteri ara...'), 'Moda');

    expect(screen.getByText('#ORD-2')).toBeInTheDocument();
    expect(screen.queryByText('#ORD-1')).not.toBeInTheDocument();
  });

  it('Admin sipariş tablosu minimum tutara göre filtrelenmeli', async () => {
    setupAdminOrderMocks();
    const user = userEvent.setup();

    renderWithProviders(<OrdersAdmin />);

    await user.type(screen.getByPlaceholderText('Minimum tutar'), '1000');

    expect(screen.getByText('#ORD-2')).toBeInTheDocument();
    expect(screen.queryByText('#ORD-1')).not.toBeInTheDocument();
  });

  it('Seller sipariş tablosu tarih aralığına göre filtrelenmeli', async () => {
    setupSellerOrderMocks();
    const user = userEvent.setup();

    renderWithProviders(<SellerOrders />);

    const dateInputs = screen.getAllByDisplayValue('');
    await user.type(dateInputs[0], '2026-03-02');
    await user.type(dateInputs[1], '2026-03-03');

    expect(screen.getByText('#SELL-2')).toBeInTheDocument();
    expect(screen.queryByText('#SELL-1')).not.toBeInTheDocument();
  });
});
