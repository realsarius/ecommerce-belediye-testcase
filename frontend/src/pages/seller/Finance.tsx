import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import {
  BarChart3,
  CircleDollarSign,
  Info,
  ShoppingBag,
  Store,
  Wallet,
} from 'lucide-react';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { KpiCard } from '@/components/admin/KpiCard';
import {
  useGetSellerAnalyticsSummaryQuery,
  useGetSellerAnalyticsTrendsQuery,
  useGetSellerProfileQuery,
} from '@/features/seller/sellerApi';
import { formatShortDate } from '@/lib/dashboardLayout';

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

const periodOptions = [
  { value: 7, label: 'Son 7 Gün' },
  { value: 30, label: 'Son 30 Gün' },
  { value: 90, label: 'Son 90 Gün' },
] as const;

export default function SellerFinancePage() {
  const [selectedDays, setSelectedDays] = useState<number>(30);
  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const shouldSkipProtectedQueries = profileLoading || !profile;
  const { data: summary, isLoading: summaryLoading } = useGetSellerAnalyticsSummaryQuery(undefined, {
    skip: shouldSkipProtectedQueries,
  });
  const { data: trends = [], isLoading: trendsLoading } = useGetSellerAnalyticsTrendsQuery(selectedDays, {
    skip: shouldSkipProtectedQueries,
  });

  const isLoading = profileLoading || summaryLoading || trendsLoading;

  const financeData = useMemo(() => {
    const currency = summary?.currency || 'TRY';
    const revenue = trends.reduce((sum, point) => sum + point.revenue, 0);
    const orders = trends.reduce((sum, point) => sum + point.orders, 0);
    const avgOrderValue = orders > 0 ? revenue / orders : 0;
    const avgDailyRevenue = trends.length > 0 ? revenue / trends.length : 0;

    const trendSeries = trends.map((point) => ({
      label: formatShortDate(point.date),
      revenue: point.revenue,
      orders: point.orders,
    }));

    const monthlyRows = Object.values(
      trends.reduce<Record<string, { month: string; revenue: number; orders: number }>>((acc, point) => {
        const date = new Date(point.date);
        const monthKey = `${date.getFullYear()}-${date.getMonth() + 1}`;
        const monthLabel = date.toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' });

        if (!acc[monthKey]) {
          acc[monthKey] = {
            month: monthLabel,
            revenue: 0,
            orders: 0,
          };
        }

        acc[monthKey].revenue += point.revenue;
        acc[monthKey].orders += point.orders;
        return acc;
      }, {})
    ).reverse();

    return {
      currency,
      revenue,
      orders,
      avgOrderValue,
      avgDailyRevenue,
      trendSeries,
      monthlyRows,
      lifetimeGrossRevenue: summary?.grossRevenue ?? 0,
    };
  }, [summary?.currency, summary?.grossRevenue, trends]);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-72" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-40 rounded-xl" />
          ))}
        </div>
        <div className="grid gap-6 xl:grid-cols-2">
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
              <p className="font-medium">Önce mağaza profilinizi tamamlayın</p>
              <p className="text-sm text-muted-foreground">
                Finans özetini görebilmek için aktif bir seller profiliniz olmalı.
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
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Kazancım ve Finans</h1>
          <p className="max-w-3xl text-muted-foreground">
            Bu ekran mevcut seller analytics verilerinden üretilen gelir görünümünü gösterir.
            Gerçek komisyon ve net kazanç endpoint’leri geldiğinde aynı alanı onların üstüne taşıyacağız.
          </p>
        </div>

        <div className="w-full max-w-xs">
          <Select value={String(selectedDays)} onValueChange={(value) => setSelectedDays(Number(value))}>
            <SelectTrigger>
              <SelectValue placeholder="Dönem seçin" />
            </SelectTrigger>
            <SelectContent>
              {periodOptions.map((option) => (
                <SelectItem key={option.value} value={String(option.value)}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Brüt Satış"
          value={formatCurrency(financeData.revenue, financeData.currency)}
          helperText={`Seçili ${selectedDays} günlük dönem için toplam satış.`}
          icon={Wallet}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Sipariş Sayısı"
          value={financeData.orders.toLocaleString('tr-TR')}
          helperText="Trend verisinden türetilen toplam sipariş."
          icon={ShoppingBag}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Ort. Sipariş Değeri"
          value={formatCurrency(financeData.avgOrderValue, financeData.currency)}
          helperText="Brüt satış / sipariş sayısı."
          icon={BarChart3}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Toplam Ciro"
          value={formatCurrency(financeData.lifetimeGrossRevenue, financeData.currency)}
          helperText="Mevcut summary endpoint’inden gelen toplam ciro."
          icon={CircleDollarSign}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Card className="border-border/70 bg-amber-50/70 dark:bg-amber-950/20">
        <CardContent className="flex gap-3 p-5">
          <Info className="mt-0.5 h-5 w-5 shrink-0 text-amber-600 dark:text-amber-300" />
          <div className="space-y-1 text-sm text-muted-foreground">
            <p className="font-medium text-foreground">Komisyon ve net kazanç burada henüz tahmin edilmedi.</p>
            <p>
              Projede ayrı seller finance endpoint’i olmadığı için bu ekran şimdilik yalnızca gerçek gelir ve sipariş trendini gösteriyor.
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Gelir Trendi</CardTitle>
            <CardDescription>Seçili dönemde gün bazlı gelir akışı.</CardDescription>
          </CardHeader>
          <CardContent className="h-[320px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={financeData.trendSeries}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} minTickGap={18} />
                <YAxis tickLine={false} axisLine={false} tickFormatter={formatCompactTick} />
                <Tooltip
                  formatter={(value) => [
                    formatCurrency(typeof value === 'number' ? value : 0, financeData.currency),
                    'Gelir',
                  ]}
                />
                <Line type="monotone" dataKey="revenue" stroke="#f59e0b" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Sipariş Hacmi</CardTitle>
            <CardDescription>Seçili dönemde gün bazlı sipariş adedi.</CardDescription>
          </CardHeader>
          <CardContent className="h-[320px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={financeData.trendSeries}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} minTickGap={18} />
                <YAxis tickLine={false} axisLine={false} />
                <Tooltip
                  formatter={(value) => [
                    typeof value === 'number' ? value.toLocaleString('tr-TR') : '0',
                    'Sipariş',
                  ]}
                />
                <Bar dataKey="orders" fill="#6366f1" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Aylık Özet</CardTitle>
          <CardDescription>
            Seçili dönem içinde oluşan aylık kırılım. Ortalama günlük ciro: {formatCurrency(financeData.avgDailyRevenue, financeData.currency)}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ay</TableHead>
                <TableHead>Sipariş</TableHead>
                <TableHead>Brüt Satış</TableHead>
                <TableHead>Ort. Sipariş Değeri</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {financeData.monthlyRows.map((row) => (
                <TableRow key={row.month}>
                  <TableCell className="font-medium">{row.month}</TableCell>
                  <TableCell>{row.orders.toLocaleString('tr-TR')}</TableCell>
                  <TableCell>{formatCurrency(row.revenue, financeData.currency)}</TableCell>
                  <TableCell>
                    {formatCurrency(row.orders > 0 ? row.revenue / row.orders : 0, financeData.currency)}
                  </TableCell>
                </TableRow>
              ))}
              {financeData.monthlyRows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="py-12 text-center text-muted-foreground">
                    Seçili dönem için finans verisi bulunamadı.
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
