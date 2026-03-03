import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import {
  ArrowRight,
  CircleDollarSign,
  Package,
  ShoppingBag,
  Star,
  Store,
  Wallet,
} from 'lucide-react';
import {
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { DataTable } from '@/components/admin/DataTable';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import {
  useGetSellerDashboardKpiQuery,
  useGetSellerDashboardOrderStatusDistributionQuery,
  useGetSellerDashboardProductPerformanceQuery,
  useGetSellerDashboardRecentOrdersQuery,
  useGetSellerDashboardRevenueTrendQuery,
  useGetSellerProfileQuery,
} from '@/features/seller/sellerApi';
import type { OrderStatus } from '@/features/orders/types';

const chartColors = ['#f59e0b', '#10b981', '#6366f1', '#8b5cf6', '#ef4444', '#06b6d4'];

const orderStatusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Beklemede',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoda',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal',
  Refunded: 'İade',
};

function formatCurrency(value: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

function formatCompactTick(value?: number) {
  if (typeof value !== 'number') {
    return '';
  }

  return `${Math.round(value / 1000)}k`;
}

function formatCountTooltip(value?: number) {
  if (typeof value !== 'number') {
    return '';
  }

  return value.toLocaleString('tr-TR');
}

function maskCustomerName(value?: string) {
  if (!value) {
    return 'Müşteri';
  }

  return value
    .split(' ')
    .filter(Boolean)
    .map((segment) => `${segment.charAt(0)}**`)
    .join(' ');
}

function getOrderStatusTone(status: OrderStatus) {
  if (status === 'Delivered') return 'success' as const;
  if (status === 'Cancelled' || status === 'Refunded') return 'danger' as const;
  if (status === 'PendingPayment') return 'warning' as const;
  return 'info' as const;
}

export default function SellerDashboard() {
  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const shouldSkipProtectedQueries = profileLoading || !profile;
  const pollingOptions = {
    skip: shouldSkipProtectedQueries,
    pollingInterval: 60000,
  } as const;
  const { data: dashboardKpi, isLoading: kpiLoading } = useGetSellerDashboardKpiQuery(30, pollingOptions);
  const { data: revenueTrend = [], isLoading: revenueTrendLoading } = useGetSellerDashboardRevenueTrendQuery(
    { period: 'daily' },
    pollingOptions
  );
  const { data: orderStatusDistribution = [], isLoading: statusLoading } =
    useGetSellerDashboardOrderStatusDistributionQuery(undefined, pollingOptions);
  const { data: productPerformance = [], isLoading: productPerformanceLoading } =
    useGetSellerDashboardProductPerformanceQuery(5, pollingOptions);
  const { data: recentOrders = [], isLoading: recentOrdersLoading } =
    useGetSellerDashboardRecentOrdersQuery(5, pollingOptions);

  const isLoading =
    profileLoading ||
    kpiLoading ||
    revenueTrendLoading ||
    statusLoading ||
    productPerformanceLoading ||
    recentOrdersLoading;

  const dashboardData = useMemo(() => {
    const currency = dashboardKpi?.currency || 'TRY';

    const distribution = Object.entries(orderStatusLabels).map(([status, label], index) => ({
      name: label,
      value: orderStatusDistribution.find((item) => item.status === status)?.count ?? 0,
      fill: chartColors[index % chartColors.length],
    }));

    const trendSeries = revenueTrend.map((point) => ({
      label: point.label,
      revenue: point.revenue,
      orders: point.orders,
    }));

    return {
      currency,
      commissionRate: dashboardKpi?.commissionRate ?? 0,
      monthlyRevenue: dashboardKpi?.revenue ?? 0,
      netRevenue: dashboardKpi?.netEarnings ?? 0,
      revenueDelta: dashboardKpi?.revenueDelta ?? 0,
      recentOrders,
      productPerformance,
      orderStatusDistribution: distribution,
      totalOrders: dashboardKpi?.totalOrders ?? 0,
      completedOrders: dashboardKpi?.completedOrdersInPeriod ?? 0,
      averageRating: dashboardKpi?.averageRating ?? 0,
      reviewCount: dashboardKpi?.reviewCount ?? 0,
      trendSeries,
    };
  }, [dashboardKpi, orderStatusDistribution, productPerformance, recentOrders, revenueTrend]);

  if (isLoading) {
    return (
      <div className="space-y-8">
        <div className="space-y-3">
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-5 w-[30rem]" />
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-44 rounded-xl" />
          ))}
        </div>

        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.35fr_0.65fr]">
          <Skeleton className="h-[340px] rounded-xl" />
          <Skeleton className="h-[340px] rounded-xl" />
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <Card className="border-amber-500/30 bg-amber-50 dark:bg-amber-950/20">
        <CardContent className="flex flex-col gap-4 p-6 md:flex-row md:items-center md:justify-between">
          <div className="flex items-start gap-3">
            <Store className="mt-0.5 h-5 w-5 text-amber-600 dark:text-amber-300" />
            <div className="space-y-1">
              <p className="font-medium">Mağaza profilinizi tamamlayın</p>
              <p className="text-sm text-muted-foreground">
                Dashboard metrikleri, ürün ekleme ve sipariş operasyonları için önce mağaza profiliniz oluşturulmalı.
              </p>
            </div>
          </div>
          <Button asChild>
            <Link to="/seller/profile">Profil Oluştur</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Seller Dashboard</h1>
          <p className="max-w-3xl text-muted-foreground">
            Mağazanızın son 30 günlük gelirini, sipariş durumlarını ve öne çıkan ürünlerinizi tek ekranda izleyin.
            Sipariş ve finans detaylarının ileri seviyeleri sonraki fazlarda derinleşecek.
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <Button variant="outline" asChild>
            <Link to="/seller/products">Ürünlerim</Link>
          </Button>
          <Button asChild>
            <Link to="/seller/products/new">Yeni Ürün Ekle</Link>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Son 30 Gün Ciro"
          value={formatCurrency(dashboardData.monthlyRevenue, dashboardData.currency)}
          delta={dashboardData.revenueDelta}
          deltaLabel="önceki döneme göre"
          helperText="Son 30 gün seller trend verilerinden hesaplandı."
          icon={Wallet}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Toplam Sipariş"
          value={dashboardData.totalOrders.toLocaleString('tr-TR')}
          helperText={`Son ${dashboardKpi?.periodDays ?? 30} günde ${dashboardData.completedOrders.toLocaleString('tr-TR')} sipariş teslim edildi.`}
          icon={ShoppingBag}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Ortalama Ürün Puanı"
          value={`${dashboardData.averageRating.toFixed(1)} / 5`}
          helperText={`${dashboardData.reviewCount.toLocaleString('tr-TR')} değerlendirme üzerinden hesaplandı.`}
          icon={Star}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Net Kazanç"
          value={formatCurrency(dashboardData.netRevenue, dashboardData.currency)}
          helperText={`Son 30 günde %${dashboardData.commissionRate.toLocaleString('tr-TR', { maximumFractionDigits: 1 })} platform komisyonu düşülerek hesaplandı.`}
          icon={CircleDollarSign}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.35fr_0.65fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Gelir Trendi</CardTitle>
            <CardDescription>Son 30 gün boyunca günlük ciro değişimi.</CardDescription>
          </CardHeader>
          <CardContent className="h-[320px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={dashboardData.trendSeries}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} minTickGap={18} />
                <YAxis tickLine={false} axisLine={false} tickFormatter={formatCompactTick} />
                <Tooltip
                  formatter={(value) => [
                    formatCurrency(typeof value === 'number' ? value : 0, dashboardData.currency),
                    'Gelir',
                  ]}
                />
                <Line
                  type="monotone"
                  dataKey="revenue"
                  name="Gelir"
                  stroke="#f59e0b"
                  strokeWidth={3}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Sipariş Durumları</CardTitle>
            <CardDescription>Mağazanıza ait siparişlerin dağılımı.</CardDescription>
          </CardHeader>
          <CardContent className="h-[320px]">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={dashboardData.orderStatusDistribution}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={72}
                  outerRadius={102}
                  paddingAngle={3}
                >
                  {dashboardData.orderStatusDistribution.map((entry) => (
                    <Cell key={entry.name} fill={entry.fill} />
                  ))}
                </Pie>
                <Tooltip formatter={(value) => formatCountTooltip(typeof value === 'number' ? value : undefined)} />
                <Legend verticalAlign="bottom" height={42} />
                <text x="50%" y="47%" textAnchor="middle" className="fill-foreground text-2xl font-semibold">
                  {dashboardData.totalOrders}
                </text>
                <text x="50%" y="55%" textAnchor="middle" className="fill-muted-foreground text-xs">
                  toplam sipariş
                </text>
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <DataTable
          title="Ürün Performans Görünümü"
          description="Satış adedi, ciro ve stok görünümüne göre öne çıkan ürünler."
          actions={(
            <Button variant="ghost" size="sm" asChild>
              <Link to="/seller/products">
                Ürünleri Gör
                <ArrowRight className="ml-1 h-4 w-4" />
              </Link>
            </Button>
          )}
        >
          <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Ciro</TableHead>
                  <TableHead>Satış</TableHead>
                  <TableHead>Puan</TableHead>
                  <TableHead>Stok</TableHead>
                  <TableHead className="text-right">Aksiyon</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {dashboardData.productPerformance.map((product) => (
                  <TableRow key={product.productId}>
                    <TableCell>
                      <div className="space-y-1">
                        <p className="font-medium">{product.productName}</p>
                        <p className="text-xs text-muted-foreground">{product.categoryName || 'Kategori bilgisi bekleniyor'}</p>
                      </div>
                    </TableCell>
                    <TableCell>{formatCurrency(product.revenue, product.currency)}</TableCell>
                    <TableCell>{product.unitsSold.toLocaleString('tr-TR')}</TableCell>
                    <TableCell>{product.averageRating > 0 ? `${product.averageRating.toFixed(1)} / 5` : 'Henüz yok'}</TableCell>
                    <TableCell>
                      <StatusBadge
                        label={String(product.stockQuantity)}
                        tone={
                          product.stockQuantity <= 0
                            ? 'danger'
                            : product.stockQuantity <= 5
                              ? 'warning'
                              : 'success'
                        }
                      />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="ghost" size="sm" asChild>
                        <Link to={`/seller/products/${product.productId}`}>Düzenle</Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {dashboardData.productPerformance.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="p-0">
                      <EmptyState
                        icon={Package}
                        title="Ürün performansı için veri bekleniyor"
                        description="Favori, değerlendirme ve stok verisi oluştukça en güçlü ürünler burada görünecek."
                        className="border-0 shadow-none"
                      />
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
          </Table>
        </DataTable>

        <DataTable
          title="Son Siparişler"
          description="Mağazanıza gelen son siparişlerin hızlı özeti."
          actions={<StatusBadge label="Seller filtreli" tone="warning" />}
        >
          <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Sipariş</TableHead>
                  <TableHead>Müşteri</TableHead>
                  <TableHead>Tutar</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="text-right">Aksiyon</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {dashboardData.recentOrders.map((order) => (
                  <TableRow key={order.orderId}>
                    <TableCell className="font-medium">#{order.orderNumber || order.orderId}</TableCell>
                    <TableCell>{maskCustomerName(order.customerName)}</TableCell>
                    <TableCell>{formatCurrency(order.totalAmount, order.currency || dashboardData.currency)}</TableCell>
                    <TableCell>
                      <StatusBadge
                        label={orderStatusLabels[order.status as OrderStatus]}
                        tone={getOrderStatusTone(order.status as OrderStatus)}
                      />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="ghost" size="sm" asChild>
                        <Link to="/seller/orders">Git</Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {dashboardData.recentOrders.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="p-0">
                      <EmptyState
                        icon={ShoppingBag}
                        title="Henüz sipariş kaydı yok"
                        description="Mağazanıza ait yeni siparişler geldikçe hızlı özet burada görünecek."
                        className="border-0 shadow-none"
                      />
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
          </Table>
        </DataTable>
      </div>
    </div>
  );
}
