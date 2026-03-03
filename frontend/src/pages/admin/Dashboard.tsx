import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Activity,
  ArrowRight,
  Boxes,
  CircleDollarSign,
  ShoppingBag,
  Store,
  UserPlus,
} from 'lucide-react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
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
import { Tabs, TabsList, TabsTrigger } from '@/components/common/tabs';
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
  useGetAdminDashboardCategorySalesQuery,
  useGetAdminDashboardKpiQuery,
  useGetAdminDashboardLowStockQuery,
  useGetAdminDashboardOrderStatusDistributionQuery,
  useGetAdminDashboardRecentOrdersQuery,
  useGetAdminDashboardRevenueTrendQuery,
  useGetAdminDashboardUserRegistrationsQuery,
} from '@/features/admin/adminApi';
import type { OrderStatus } from '@/features/orders/types';
import type { DashboardPeriod } from '@/types/chart';
import { formatCompactNumber, formatCurrency, formatNumber } from '@/lib/format';
import { getOrderStatusLabel, getOrderStatusTone } from '@/lib/orderStatus';

const chartColors = ['#6366f1', '#8b5cf6', '#06b6d4', '#10b981', '#f59e0b', '#ef4444'];

function formatCountTooltip(value?: number, suffix?: string) {
  if (typeof value !== 'number') {
    return '';
  }

  return suffix ? `${formatNumber(value)} ${suffix}` : formatNumber(value);
}

function formatDelta(current: number, previous: number) {
  if (previous === 0) {
    return current === 0 ? 0 : 100;
  }

  return ((current - previous) / previous) * 100;
}

export default function AdminDashboard() {
  const [selectedPeriod, setSelectedPeriod] = useState<DashboardPeriod>('daily');
  const pollingOptions = { pollingInterval: 60000 } as const;

  const { data: kpi, isLoading: kpiLoading } = useGetAdminDashboardKpiQuery(undefined, pollingOptions);
  const { data: revenueTrend = [], isLoading: revenueTrendLoading } =
    useGetAdminDashboardRevenueTrendQuery({ period: selectedPeriod }, pollingOptions);
  const { data: categorySales = [], isLoading: categorySalesLoading } =
    useGetAdminDashboardCategorySalesQuery(undefined, pollingOptions);
  const { data: userRegistrations = [], isLoading: registrationsLoading } =
    useGetAdminDashboardUserRegistrationsQuery(30, pollingOptions);
  const { data: statusDistribution = [], isLoading: statusLoading } =
    useGetAdminDashboardOrderStatusDistributionQuery(undefined, pollingOptions);
  const { data: lowStockProducts = [], isLoading: lowStockLoading } =
    useGetAdminDashboardLowStockQuery(5, pollingOptions);
  const { data: recentOrders = [], isLoading: recentOrdersLoading } =
    useGetAdminDashboardRecentOrdersQuery(5, pollingOptions);

  const isLoading =
    kpiLoading ||
    revenueTrendLoading ||
    categorySalesLoading ||
    registrationsLoading ||
    statusLoading ||
    lowStockLoading ||
    recentOrdersLoading;

  const dashboardData = useMemo(() => {
    const mappedStatusDistribution = (['PendingPayment', 'Paid', 'Processing', 'Shipped', 'Delivered', 'Cancelled', 'Refunded'] as OrderStatus[]).map((status, index) => ({
      name: getOrderStatusLabel(status, { compact: true }),
      value: statusDistribution.find((item) => item.status === status)?.count ?? 0,
      fill: chartColors[index % chartColors.length],
    }));

    return {
      lowStockProducts,
      recentOrders,
      statusDistribution: mappedStatusDistribution,
      totalOrders: mappedStatusDistribution.reduce((sum, item) => sum + item.value, 0),
    };
  }, [lowStockProducts, recentOrders, statusDistribution]);

  const revenueDescriptions: Record<DashboardPeriod, string> = {
    daily: 'Son 7 gün ile bir önceki 7 günlük dönemin günlük gelir karşılaştırması.',
    weekly: 'Son 8 hafta ile önceki 8 haftanın haftalık gelir karşılaştırması.',
    monthly: 'Son 6 ay ile önceki 6 ayın aylık gelir karşılaştırması.',
  };

  if (isLoading) {
    return (
      <div className="space-y-8">
        <div className="space-y-3">
          <Skeleton className="h-10 w-72" />
          <Skeleton className="h-5 w-full max-w-[32rem]" />
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-44 rounded-xl" />
          ))}
        </div>

        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.3fr_0.7fr]">
          <Skeleton className="h-[360px] rounded-xl" />
          <Skeleton className="h-[360px] rounded-xl" />
        </div>

        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.15fr_0.85fr]">
          <Skeleton className="h-[320px] rounded-xl" />
          <Skeleton className="h-[320px] rounded-xl" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Admin Dashboard</h1>
          <p className="max-w-3xl text-muted-foreground">
            Sipariş, kullanıcı ve katalog metriklerini gerçek admin dashboard endpoint&apos;leri üzerinden tek ekranda
            izleyin. Seller başvuruları için ayrı backend hazır olduğunda bu yüzey daha da derinleşecek.
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <Button variant="outline" asChild>
            <Link to="/admin/orders">Siparişleri Yönet</Link>
          </Button>
          <Button asChild>
            <Link to="/admin/products">Kataloğa Git</Link>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Bugünkü Gelir"
          value={formatCurrency(kpi?.todayRevenue ?? 0, kpi?.currency)}
          delta={formatDelta(kpi?.todayRevenue ?? 0, kpi?.yesterdayRevenue ?? 0)}
          deltaLabel="düne göre"
          helperText="Bugün gelir oluşturan siparişlerden hesaplandı."
          icon={CircleDollarSign}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Bugünkü Sipariş"
          value={formatNumber(kpi?.todayOrders ?? 0)}
          delta={formatDelta(kpi?.todayOrders ?? 0, kpi?.yesterdayOrders ?? 0)}
          deltaLabel="düne göre"
          helperText="Yeni oluşturulan sipariş adedi."
          icon={ShoppingBag}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Yeni Üye"
          value={formatNumber(kpi?.todayNewUsers ?? 0)}
          delta={formatDelta(kpi?.todayNewUsers ?? 0, kpi?.yesterdayNewUsers ?? 0)}
          deltaLabel="düne göre"
          helperText="Bugün kayıt olan kullanıcı sayısı."
          icon={UserPlus}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Aktif Seller"
          value={formatNumber(kpi?.activeSellers ?? 0)}
          helperText={`${formatNumber(kpi?.activeProducts ?? 0)} aktif ürün ve ${formatNumber(kpi?.categoryCount ?? 0)} kategori içinde hesaplandı.`}
          badge={`${kpi?.pendingSellerApplications ?? 0} bekleyen`}
          icon={Store}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.35fr_0.65fr]">
        <Card className="min-w-0 border-border/70">
          <CardHeader>
            <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
              <div>
                <CardTitle className="flex items-center gap-2">
                  <Activity className="h-5 w-5 text-primary" />
                  Gelir Trendi
                </CardTitle>
                <CardDescription>{revenueDescriptions[selectedPeriod]}</CardDescription>
              </div>
              <Tabs value={selectedPeriod} onValueChange={(value) => setSelectedPeriod(value as DashboardPeriod)}>
                <TabsList>
                  <TabsTrigger value="daily">Günlük</TabsTrigger>
                  <TabsTrigger value="weekly">Haftalık</TabsTrigger>
                  <TabsTrigger value="monthly">Aylık</TabsTrigger>
                </TabsList>
              </Tabs>
            </div>
          </CardHeader>
          <CardContent className="min-w-0 h-[320px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={revenueTrend}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} />
                <YAxis tickLine={false} axisLine={false} tickFormatter={formatCompactNumber} />
                <Tooltip
                  formatter={(value, name) => [
                    formatCurrency(typeof value === 'number' ? value : 0, kpi?.currency),
                    name === 'revenue' ? 'Seçili dönem' : 'Karşılaştırma',
                  ]}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="revenue"
                  name="Seçili dönem"
                  stroke="#6366f1"
                  strokeWidth={3}
                  dot={{ r: 3 }}
                />
                <Line
                  type="monotone"
                  dataKey="previousRevenue"
                  name="Karşılaştırma"
                  stroke="#94a3b8"
                  strokeWidth={2}
                  strokeDasharray="6 6"
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="min-w-0 border-border/70">
          <CardHeader>
            <CardTitle>Sipariş Durum Dağılımı</CardTitle>
            <CardDescription>Toplam {formatNumber(dashboardData.totalOrders)} siparişin güncel dağılımı.</CardDescription>
          </CardHeader>
          <CardContent className="min-w-0 h-[320px]">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={dashboardData.statusDistribution}
                  dataKey="value"
                  nameKey="name"
                  innerRadius={72}
                  outerRadius={102}
                  paddingAngle={3}
                >
                  {dashboardData.statusDistribution.map((entry) => (
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

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.15fr_0.85fr]">
        <Card className="min-w-0 border-border/70">
          <CardHeader>
            <CardTitle>Kategori Bazlı Satış</CardTitle>
            <CardDescription>Kategori bazlı satış hacmi başarılı sipariş kalemlerinden hesaplanır.</CardDescription>
          </CardHeader>
          <CardContent className="min-w-0 h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={categorySales}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="categoryName" tickLine={false} axisLine={false} />
                <YAxis tickLine={false} axisLine={false} />
                <Tooltip formatter={(value) => formatCountTooltip(typeof value === 'number' ? value : undefined, 'adet')} />
                <Bar dataKey="salesCount" fill="#06b6d4" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="min-w-0 border-border/70">
          <CardHeader>
            <CardTitle>Kullanıcı Kayıt Trendi</CardTitle>
            <CardDescription>Son 30 günde sisteme katılan kullanıcı sayısı.</CardDescription>
          </CardHeader>
          <CardContent className="min-w-0 h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={userRegistrations}>
                <defs>
                  <linearGradient id="userRegistrationGradient" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.45} />
                    <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0.05} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} />
                <YAxis tickLine={false} axisLine={false} />
                <Tooltip formatter={(value) => formatCountTooltip(typeof value === 'number' ? value : undefined, 'kayıt')} />
                <Area
                  type="monotone"
                  dataKey="count"
                  stroke="#8b5cf6"
                  strokeWidth={3}
                  fill="url(#userRegistrationGradient)"
                />
              </AreaChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <DataTable
          title="Son Siparişler"
          description="En son oluşturulan siparişlerin hızlı görünümü."
          actions={(
            <Button variant="ghost" size="sm" asChild>
              <Link to="/admin/orders">
                Siparişleri Gör
                <ArrowRight className="ml-1 h-4 w-4" />
              </Link>
            </Button>
          )}
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
                  <TableCell>{order.customerName || 'Misafir kullanıcı'}</TableCell>
                  <TableCell>{formatCurrency(order.totalAmount, order.currency || kpi?.currency)}</TableCell>
                  <TableCell>
                    <StatusBadge
                      label={getOrderStatusLabel(order.status, { compact: true })}
                      tone={getOrderStatusTone(order.status)}
                    />
                  </TableCell>
                  <TableCell className="text-right">
                    <Button variant="ghost" size="sm" asChild>
                      <Link to={`/admin/orders/${order.orderId}`}>Detay</Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {dashboardData.recentOrders.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="p-0">
                    <EmptyState
                      icon={ShoppingBag}
                      title="Henüz sipariş verisi yok"
                      description="Yeni siparişler oluştukça hızlı özet burada görünecek."
                      className="border-0 shadow-none"
                    />
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </DataTable>

        <DataTable
          title="Düşük Stok Uyarıları"
          description="Stok seviyesi kritik eşiğin altında kalan aktif ürünler."
          actions={<StatusBadge label={`${dashboardData.lowStockProducts.length} kayıt`} tone="warning" />}
        >
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ürün</TableHead>
                <TableHead>Stok</TableHead>
                <TableHead>Seller</TableHead>
                <TableHead className="text-right">Aksiyon</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {dashboardData.lowStockProducts.map((product) => (
                <TableRow key={product.productId}>
                  <TableCell className="font-medium">{product.name}</TableCell>
                  <TableCell>
                    <StatusBadge
                      label={String(product.stock)}
                      tone={product.stock <= 0 ? 'danger' : 'warning'}
                    />
                  </TableCell>
                  <TableCell>{product.sellerName}</TableCell>
                  <TableCell className="text-right">
                    <Button variant="ghost" size="sm" asChild>
                      <Link to={`/admin/products/${product.productId}`}>Ürüne Git</Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {dashboardData.lowStockProducts.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="p-0">
                    <EmptyState
                      icon={Boxes}
                      title="Kritik stok uyarısı yok"
                      description="Seçilen eşikte stok seviyesi düşük olan aktif ürün bulunmuyor."
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
