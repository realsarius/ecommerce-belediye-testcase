import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import AdminDashboard from './admin/Dashboard';
import SellerDashboard from './seller/Dashboard';

const mockAdminApi = vi.hoisted(() => ({
  useGetAdminDashboardKpiQuery: vi.fn(),
  useGetAdminDashboardRevenueTrendQuery: vi.fn(),
  useGetAdminDashboardCategorySalesQuery: vi.fn(),
  useGetAdminDashboardUserRegistrationsQuery: vi.fn(),
  useGetAdminDashboardOrderStatusDistributionQuery: vi.fn(),
  useGetAdminDashboardLowStockQuery: vi.fn(),
  useGetAdminDashboardRecentOrdersQuery: vi.fn(),
}));

const mockSellerApi = vi.hoisted(() => ({
  useGetSellerProfileQuery: vi.fn(),
  useGetSellerDashboardKpiQuery: vi.fn(),
  useGetSellerDashboardRevenueTrendQuery: vi.fn(),
  useGetSellerDashboardOrderStatusDistributionQuery: vi.fn(),
  useGetSellerDashboardProductPerformanceQuery: vi.fn(),
  useGetSellerDashboardRecentOrdersQuery: vi.fn(),
}));

vi.mock('@/features/admin/adminApi', () => mockAdminApi);
vi.mock('@/features/seller/sellerApi', () => mockSellerApi);
vi.mock('recharts', () => {
  const Chart = ({ children }: { children?: React.ReactNode }) => (
    <svg role="img" aria-hidden="true">
      {children}
    </svg>
  );
  return {
    ResponsiveContainer: ({ children }: { children?: React.ReactNode }) => <div>{children}</div>,
    LineChart: Chart,
    PieChart: Chart,
    BarChart: Chart,
    AreaChart: Chart,
    CartesianGrid: () => null,
    Cell: () => null,
    Legend: () => null,
    Tooltip: () => null,
    XAxis: () => null,
    YAxis: () => null,
    Line: () => null,
    Pie: () => null,
    Bar: () => null,
    Area: () => null,
  };
});

function setAdminSuccessMocks(overrides?: {
  recentOrders?: Array<Record<string, unknown>>;
  lowStock?: Array<Record<string, unknown>>;
}) {
  mockAdminApi.useGetAdminDashboardKpiQuery.mockReturnValue({
    data: {
      todayRevenue: 12500,
      yesterdayRevenue: 10000,
      todayOrders: 8,
      yesterdayOrders: 5,
      todayNewUsers: 3,
      yesterdayNewUsers: 1,
      activeSellers: 4,
      activeProducts: 19,
      categoryCount: 6,
      pendingSellerApplications: 2,
      currency: 'TRY',
    },
    isLoading: false,
  });
  mockAdminApi.useGetAdminDashboardRevenueTrendQuery.mockReturnValue({
    data: [{ label: '01 Mar', revenue: 1000, previousRevenue: 800, orders: 2 }],
    isLoading: false,
  });
  mockAdminApi.useGetAdminDashboardCategorySalesQuery.mockReturnValue({
    data: [{ categoryName: 'Elektronik', salesCount: 12 }],
    isLoading: false,
  });
  mockAdminApi.useGetAdminDashboardUserRegistrationsQuery.mockReturnValue({
    data: [{ label: '01 Mar', date: '2026-03-01', count: 4 }],
    isLoading: false,
  });
  mockAdminApi.useGetAdminDashboardOrderStatusDistributionQuery.mockReturnValue({
    data: [{ status: 'Paid', count: 3 }],
    isLoading: false,
  });
  mockAdminApi.useGetAdminDashboardLowStockQuery.mockReturnValue({
    data: overrides?.lowStock ?? [
      { productId: 77, name: 'Klavye', stock: 2, sellerName: 'Tekno Market' },
    ],
    isLoading: false,
  });
  mockAdminApi.useGetAdminDashboardRecentOrdersQuery.mockReturnValue({
    data: overrides?.recentOrders ?? [
      {
        orderId: 501,
        orderNumber: 'ORD-501',
        customerName: 'Test Müşteri',
        totalAmount: 1499,
        currency: 'TRY',
        status: 'Delivered',
        createdAt: '2026-03-03T10:00:00Z',
      },
    ],
    isLoading: false,
  });
}

function setSellerSuccessMocks(overrides?: {
  profile?: Record<string, unknown> | null;
  productPerformance?: Array<Record<string, unknown>>;
  recentOrders?: Array<Record<string, unknown>>;
}) {
  mockSellerApi.useGetSellerProfileQuery.mockReturnValue({
    data: overrides?.profile === undefined ? { brandName: 'Test Mağaza' } : overrides.profile,
    isLoading: false,
  });
  mockSellerApi.useGetSellerDashboardKpiQuery.mockReturnValue({
    data: {
      periodDays: 30,
      revenue: 8200,
      revenueDelta: 12.5,
      totalOrders: 14,
      completedOrdersInPeriod: 9,
      averageRating: 4.4,
      reviewCount: 11,
      netEarnings: 7380,
      commissionRate: 10,
      currency: 'TRY',
    },
    isLoading: false,
  });
  mockSellerApi.useGetSellerDashboardRevenueTrendQuery.mockReturnValue({
    data: [{ label: '01 Mar', revenue: 600, orders: 2 }],
    isLoading: false,
  });
  mockSellerApi.useGetSellerDashboardOrderStatusDistributionQuery.mockReturnValue({
    data: [{ status: 'Delivered', count: 4 }],
    isLoading: false,
  });
  mockSellerApi.useGetSellerDashboardProductPerformanceQuery.mockReturnValue({
    data: overrides?.productPerformance ?? [
      {
        productId: 91,
        productName: 'Test Ürün',
        categoryName: 'Elektronik',
        revenue: 3200,
        unitsSold: 6,
        averageRating: 4.8,
        stockQuantity: 12,
        currency: 'TRY',
      },
    ],
    isLoading: false,
  });
  mockSellerApi.useGetSellerDashboardRecentOrdersQuery.mockReturnValue({
    data: overrides?.recentOrders ?? [
      {
        orderId: 88,
        orderNumber: 'ORD-88',
        customerName: 'Ali Veli',
        totalAmount: 899,
        currency: 'TRY',
        status: 'Paid',
        createdAt: '2026-03-03T10:00:00Z',
      },
    ],
    isLoading: false,
  });
}

describe('Dashboard durum testleri', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('Admin dashboard loading durumunda skeleton göstermeli', () => {
    mockAdminApi.useGetAdminDashboardKpiQuery.mockReturnValue({ data: undefined, isLoading: true });
    mockAdminApi.useGetAdminDashboardRevenueTrendQuery.mockReturnValue({ data: [], isLoading: true });
    mockAdminApi.useGetAdminDashboardCategorySalesQuery.mockReturnValue({ data: [], isLoading: true });
    mockAdminApi.useGetAdminDashboardUserRegistrationsQuery.mockReturnValue({ data: [], isLoading: true });
    mockAdminApi.useGetAdminDashboardOrderStatusDistributionQuery.mockReturnValue({ data: [], isLoading: true });
    mockAdminApi.useGetAdminDashboardLowStockQuery.mockReturnValue({ data: [], isLoading: true });
    mockAdminApi.useGetAdminDashboardRecentOrdersQuery.mockReturnValue({ data: [], isLoading: true });

    const { container } = renderWithProviders(<AdminDashboard />);

    expect(screen.queryByText('Admin Dashboard')).not.toBeInTheDocument();
    expect(container.querySelectorAll('[data-slot="skeleton"]').length).toBeGreaterThan(0);
  });

  it('Admin dashboard empty durumda boş state mesajlarını göstermeli', () => {
    setAdminSuccessMocks({
      recentOrders: [],
      lowStock: [],
    });

    renderWithProviders(<AdminDashboard />);

    expect(screen.getByText('Henüz sipariş verisi yok')).toBeInTheDocument();
    expect(screen.getByText('Kritik stok uyarısı yok')).toBeInTheDocument();
  });

  it('Admin dashboard success durumda KPI ve tablo verilerini göstermeli', () => {
    setAdminSuccessMocks();

    renderWithProviders(<AdminDashboard />);

    expect(screen.getByRole('heading', { name: 'Admin Dashboard' })).toBeInTheDocument();
    expect(screen.getByText('Bugünkü Gelir')).toBeInTheDocument();
    expect(screen.getByText('Test Müşteri')).toBeInTheDocument();
    expect(screen.getByText('Klavye')).toBeInTheDocument();
  });

  it('Seller dashboard loading durumunda skeleton göstermeli', () => {
    mockSellerApi.useGetSellerProfileQuery.mockReturnValue({ data: undefined, isLoading: true });
    mockSellerApi.useGetSellerDashboardKpiQuery.mockReturnValue({ data: undefined, isLoading: true });
    mockSellerApi.useGetSellerDashboardRevenueTrendQuery.mockReturnValue({ data: [], isLoading: true });
    mockSellerApi.useGetSellerDashboardOrderStatusDistributionQuery.mockReturnValue({ data: [], isLoading: true });
    mockSellerApi.useGetSellerDashboardProductPerformanceQuery.mockReturnValue({ data: [], isLoading: true });
    mockSellerApi.useGetSellerDashboardRecentOrdersQuery.mockReturnValue({ data: [], isLoading: true });

    const { container } = renderWithProviders(<SellerDashboard />);

    expect(screen.queryByText('Seller Dashboard')).not.toBeInTheDocument();
    expect(container.querySelectorAll('[data-slot="skeleton"]').length).toBeGreaterThan(0);
  });

  it('Seller dashboard empty durumda mağaza profili uyarısını göstermeli', () => {
    setSellerSuccessMocks({
      profile: null,
      productPerformance: [],
      recentOrders: [],
    });

    renderWithProviders(<SellerDashboard />);

    expect(screen.getByText('Mağaza profilinizi tamamlayın')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Profil Oluştur' })).toBeInTheDocument();
  });

  it('Seller dashboard success durumda KPI ve seller verilerini göstermeli', () => {
    setSellerSuccessMocks();

    renderWithProviders(<SellerDashboard />);

    expect(screen.getByRole('heading', { name: 'Seller Dashboard' })).toBeInTheDocument();
    expect(screen.getByText('Test Ürün')).toBeInTheDocument();
    expect(screen.getByText('A** V**')).toBeInTheDocument();
    expect(screen.getByText('Net Kazanç')).toBeInTheDocument();
  });
});
