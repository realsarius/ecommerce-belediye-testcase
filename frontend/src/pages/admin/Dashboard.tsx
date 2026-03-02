import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import {
  Activity,
  ArrowRight,
  Boxes,
  CircleDollarSign,
  Package,
  ShoppingBag,
  TriangleAlert,
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
import { Badge } from '@/components/common/badge';
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
import { KpiCard } from '@/components/admin/KpiCard';
import { useGetAdminCategoriesQuery, useGetAdminOrdersQuery } from '@/features/admin/adminApi';
import { useSearchProductsQuery } from '@/features/products/productsApi';
import type { Order, OrderStatus } from '@/features/orders/types';
import { formatShortDate } from '@/lib/dashboardLayout';

const chartColors = ['#6366f1', '#8b5cf6', '#06b6d4', '#10b981', '#f59e0b', '#ef4444'];

const orderStatusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Beklemede',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoda',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal',
  Refunded: 'İade',
};

const orderStatusClasses: Record<OrderStatus, string> = {
  PendingPayment: 'bg-amber-500/10 text-amber-700 dark:text-amber-300',
  Paid: 'bg-sky-500/10 text-sky-700 dark:text-sky-300',
  Processing: 'bg-violet-500/10 text-violet-700 dark:text-violet-300',
  Shipped: 'bg-indigo-500/10 text-indigo-700 dark:text-indigo-300',
  Delivered: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300',
  Cancelled: 'bg-rose-500/10 text-rose-700 dark:text-rose-300',
  Refunded: 'bg-orange-500/10 text-orange-700 dark:text-orange-300',
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

function formatCountTooltip(value?: number, suffix?: string) {
  if (typeof value !== 'number') {
    return '';
  }

  return suffix ? `${value.toLocaleString('tr-TR')} ${suffix}` : value.toLocaleString('tr-TR');
}

function formatDelta(current: number, previous: number) {
  if (previous === 0) {
    return current === 0 ? 0 : 100;
  }

  return ((current - previous) / previous) * 100;
}

function isRevenueOrder(status: OrderStatus) {
  return status === 'Paid' || status === 'Processing' || status === 'Shipped' || status === 'Delivered';
}

function buildRevenueTrend(orders: Order[]) {
  const today = new Date();
  const buckets = Array.from({ length: 7 }).map((_, index) => {
    const currentDate = new Date(today);
    currentDate.setDate(today.getDate() - (6 - index));

    const previousDate = new Date(currentDate);
    previousDate.setDate(currentDate.getDate() - 7);

    const currentKey = currentDate.toISOString().slice(0, 10);
    const previousKey = previousDate.toISOString().slice(0, 10);

    return {
      label: formatShortDate(currentDate.toISOString()),
      currentKey,
      previousKey,
      revenue: 0,
      previousRevenue: 0,
      orders: 0,
    };
  });

  for (const bucket of buckets) {
    for (const order of orders) {
      const orderKey = new Date(order.createdAt).toISOString().slice(0, 10);
      if (orderKey === bucket.currentKey) {
        bucket.orders += 1;
        if (isRevenueOrder(order.status)) {
          bucket.revenue += order.totalAmount;
        }
      }

      if (orderKey === bucket.previousKey && isRevenueOrder(order.status)) {
        bucket.previousRevenue += order.totalAmount;
      }
    }
  }

  return buckets;
}

export default function AdminDashboard() {
  const { data: orders = [], isLoading: ordersLoading } = useGetAdminOrdersQuery();
  const { data: categories = [], isLoading: categoriesLoading } = useGetAdminCategoriesQuery();
  const { data: products, isLoading: productsLoading } = useSearchProductsQuery({ page: 1, pageSize: 500 });

  const isLoading = ordersLoading || categoriesLoading || productsLoading;

  const dashboardData = useMemo(() => {
    const now = new Date();
    const todayKey = now.toISOString().slice(0, 10);
    const yesterday = new Date(now);
    yesterday.setDate(now.getDate() - 1);
    const yesterdayKey = yesterday.toISOString().slice(0, 10);

    const todayOrders = orders.filter((order) => new Date(order.createdAt).toISOString().slice(0, 10) === todayKey);
    const yesterdayOrders = orders.filter((order) => new Date(order.createdAt).toISOString().slice(0, 10) === yesterdayKey);

    const todayRevenue = todayOrders
      .filter((order) => isRevenueOrder(order.status))
      .reduce((sum, order) => sum + order.totalAmount, 0);
    const yesterdayRevenue = yesterdayOrders
      .filter((order) => isRevenueOrder(order.status))
      .reduce((sum, order) => sum + order.totalAmount, 0);

    const productItems = products?.items ?? [];
    const activeProducts = productItems.filter((product) => product.isActive).length;
    const lowStockProducts = productItems
      .filter((product) => product.stockQuantity <= 5)
      .sort((a, b) => a.stockQuantity - b.stockQuantity)
      .slice(0, 5);

    const recentOrders = [...orders]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);

    const revenueTrend = buildRevenueTrend(orders);
    const statusDistribution = Object.entries(orderStatusLabels).map(([status, label], index) => ({
      name: label,
      value: orders.filter((order) => order.status === status).length,
      fill: chartColors[index % chartColors.length],
    }));

    const categoryProductCounts = [...categories]
      .sort((a, b) => b.productCount - a.productCount)
      .slice(0, 6)
      .map((category) => ({
        name: category.name,
        count: category.productCount,
      }));

    return {
      activeProducts,
      categoryCount: categories.length,
      lowStockProducts,
      lowStockCount: lowStockProducts.length,
      orderTrend: revenueTrend,
      recentOrders,
      statusDistribution,
      todayOrdersCount: todayOrders.length,
      todayOrdersDelta: formatDelta(todayOrders.length, yesterdayOrders.length),
      todayRevenue,
      todayRevenueDelta: formatDelta(todayRevenue, yesterdayRevenue),
      categoryProductCounts,
      totalOrders: orders.length,
    };
  }, [categories, orders, products]);

  if (isLoading) {
    return (
      <div className="space-y-8">
        <div className="space-y-3">
          <Skeleton className="h-10 w-72" />
          <Skeleton className="h-5 w-[32rem]" />
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
            Bu ilk sürüm, mevcut admin sipariş, katalog ve arama endpoint’lerinden türetilen operasyonel metriklerle
            hazırlanmıştır. Kullanıcı ve seller bazlı gelişmiş metrikler sonraki fazda eklenecek.
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
          value={formatCurrency(dashboardData.todayRevenue)}
          delta={dashboardData.todayRevenueDelta}
          deltaLabel="düne göre"
          helperText="Bugün gelir oluşturan siparişlerden hesaplandı."
          icon={CircleDollarSign}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Bugünkü Sipariş"
          value={dashboardData.todayOrdersCount.toLocaleString('tr-TR')}
          delta={dashboardData.todayOrdersDelta}
          deltaLabel="düne göre"
          helperText="Yeni oluşturulan sipariş adedi."
          icon={ShoppingBag}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Ürün"
          value={dashboardData.activeProducts.toLocaleString('tr-TR')}
          helperText={`${dashboardData.categoryCount.toLocaleString('tr-TR')} kategori içinde satışta olan ürünler.`}
          icon={Package}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Düşük Stok Uyarısı"
          value={dashboardData.lowStockCount.toLocaleString('tr-TR')}
          helperText="Stok seviyesi 5 ve altındaki ürünler."
          icon={TriangleAlert}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.35fr_0.65fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Activity className="h-5 w-5 text-primary" />
              Gelir Trendi
            </CardTitle>
            <CardDescription>Son 7 gün ile bir önceki 7 günlük dönemin gelir karşılaştırması.</CardDescription>
          </CardHeader>
          <CardContent className="h-[320px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={dashboardData.orderTrend}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} />
                <YAxis
                  tickLine={false}
                  axisLine={false}
                  tickFormatter={formatCompactTick}
                />
                <Tooltip
                  formatter={(value, name) => [
                    formatCurrency(typeof value === 'number' ? value : 0),
                    name === 'revenue' ? 'Bu hafta' : 'Geçen hafta',
                  ]}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="revenue"
                  name="Bu hafta"
                  stroke="#6366f1"
                  strokeWidth={3}
                  dot={{ r: 3 }}
                />
                <Line
                  type="monotone"
                  dataKey="previousRevenue"
                  name="Geçen hafta"
                  stroke="#94a3b8"
                  strokeWidth={2}
                  strokeDasharray="6 6"
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Sipariş Durum Dağılımı</CardTitle>
            <CardDescription>Toplam {dashboardData.totalOrders.toLocaleString('tr-TR')} siparişin güncel dağılımı.</CardDescription>
          </CardHeader>
          <CardContent className="h-[320px]">
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
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Kategori Bazlı Katalog Yoğunluğu</CardTitle>
            <CardDescription>MVP kapsamında kategori bazlı ürün sayısı gösterilmektedir.</CardDescription>
          </CardHeader>
          <CardContent className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={dashboardData.categoryProductCounts}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="name" tickLine={false} axisLine={false} />
                <YAxis tickLine={false} axisLine={false} />
                <Tooltip formatter={(value) => formatCountTooltip(typeof value === 'number' ? value : undefined, 'ürün')} />
                <Bar dataKey="count" fill="#06b6d4" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Sipariş Hacmi</CardTitle>
            <CardDescription>Son 7 günde oluşturulan sipariş adedi.</CardDescription>
          </CardHeader>
          <CardContent className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={dashboardData.orderTrend}>
                <defs>
                  <linearGradient id="ordersAreaGradient" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.45} />
                    <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0.05} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} />
                <YAxis tickLine={false} axisLine={false} />
                <Tooltip formatter={(value) => formatCountTooltip(typeof value === 'number' ? value : undefined, 'sipariş')} />
                <Area
                  type="monotone"
                  dataKey="orders"
                  stroke="#8b5cf6"
                  fill="url(#ordersAreaGradient)"
                  strokeWidth={3}
                />
              </AreaChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <Card className="border-border/70">
          <CardHeader className="flex flex-row items-center justify-between gap-4">
            <div>
              <CardTitle>Son Siparişler</CardTitle>
              <CardDescription>En son gelen 5 sipariş hızlı inceleme için listeleniyor.</CardDescription>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/admin/orders">
                Tümünü Gör
                <ArrowRight className="ml-1 h-4 w-4" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Sipariş</TableHead>
                  <TableHead>Müşteri</TableHead>
                  <TableHead>Tutar</TableHead>
                  <TableHead>Durum</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {dashboardData.recentOrders.map((order) => (
                  <TableRow key={order.id}>
                    <TableCell className="font-medium">#{order.orderNumber || order.id}</TableCell>
                    <TableCell>{order.customerName || `Kullanıcı #${order.userId}`}</TableCell>
                    <TableCell>{formatCurrency(order.totalAmount, order.currency || 'TRY')}</TableCell>
                    <TableCell>
                      <Badge className={orderStatusClasses[order.status]} variant="secondary">
                        {orderStatusLabels[order.status]}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
                {dashboardData.recentOrders.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} className="py-10 text-center text-muted-foreground">
                      Henüz sipariş kaydı bulunmuyor.
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader className="flex flex-row items-center justify-between gap-4">
            <div>
              <CardTitle>Düşük Stok Uyarıları</CardTitle>
              <CardDescription>En kritik stok seviyesine sahip ürünler listeleniyor.</CardDescription>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link to="/admin/products">
                Ürünlere Git
                <ArrowRight className="ml-1 h-4 w-4" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Stok</TableHead>
                  <TableHead>Kategori</TableHead>
                  <TableHead>Durum</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {dashboardData.lowStockProducts.map((product) => (
                  <TableRow key={product.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Boxes className="h-4 w-4 text-muted-foreground" />
                        <span className="max-w-[16rem] truncate font-medium">{product.name}</span>
                      </div>
                    </TableCell>
                    <TableCell className={product.stockQuantity <= 0 ? 'font-semibold text-rose-600' : 'font-semibold text-amber-600'}>
                      {product.stockQuantity}
                    </TableCell>
                    <TableCell>{product.categoryName}</TableCell>
                    <TableCell>
                      <Badge
                        variant="secondary"
                        className={product.stockQuantity <= 0 ? 'bg-rose-500/10 text-rose-700 dark:text-rose-300' : 'bg-amber-500/10 text-amber-700 dark:text-amber-300'}
                      >
                        {product.stockQuantity <= 0 ? 'Tükendi' : 'Kritik'}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
                {dashboardData.lowStockProducts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} className="py-10 text-center text-muted-foreground">
                      Kritik stok uyarısı bulunmuyor.
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
