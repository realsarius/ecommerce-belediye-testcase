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
  CalendarDays,
  CircleDollarSign,
  Download,
  Info,
  ShoppingBag,
  Store,
  Wallet,
} from 'lucide-react';
import { EmptyState } from '@/components/admin/EmptyState';
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
  useGetSellerFinanceSummaryQuery,
  useGetSellerProfileQuery,
} from '@/features/seller/sellerApi';
import { formatShortDate } from '@/lib/dashboardLayout';
import {
  formatCompactNumber,
  formatCurrency,
  formatDateInput,
  formatNumber,
  formatPercent,
} from '@/lib/format';

function downloadCsv(filename: string, rows: string[][]) {
  const csvContent = rows
    .map((row) => row.map((cell) => `"${String(cell).replaceAll('"', '""')}"`).join(','))
    .join('\n');

  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

const periodOptions = [
  { value: 7, label: 'Son 7 Gün' },
  { value: 30, label: 'Son 30 Gün' },
  { value: 90, label: 'Son 90 Gün' },
] as const;

function buildPresetDateRange(days: number) {
  const end = new Date();
  const start = new Date(end);
  start.setDate(end.getDate() - (days - 1));

  return {
    from: formatDateInput(start),
    to: formatDateInput(end),
  };
}

function getInclusiveDayCount(from: string, to: string) {
  if (!from || !to) {
    return 0;
  }

  const fromDate = new Date(`${from}T00:00:00`);
  const toDate = new Date(`${to}T00:00:00`);
  const diff = toDate.getTime() - fromDate.getTime();
  return diff >= 0 ? Math.floor(diff / (1000 * 60 * 60 * 24)) + 1 : 0;
}

export default function SellerFinancePage() {
  const [selectedPreset, setSelectedPreset] = useState<string>('30');
  const [dateRange, setDateRange] = useState(() => buildPresetDateRange(30));
  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const shouldSkipProtectedQueries = profileLoading || !profile;
  const selectedDays = useMemo(
    () => getInclusiveDayCount(dateRange.from, dateRange.to),
    [dateRange.from, dateRange.to]
  );
  const hasInvalidDateRange = selectedDays === 0;
  const { data: financeSummary, isLoading: financeLoading } = useGetSellerFinanceSummaryQuery({
    from: dateRange.from,
    to: dateRange.to,
    days: selectedDays || 30,
  }, {
    skip: shouldSkipProtectedQueries || hasInvalidDateRange,
  });

  const isLoading = profileLoading || financeLoading;

  const financeData = useMemo(() => {
    const currency = financeSummary?.currency || 'TRY';
    const trendSeries = (financeSummary?.dailyTrend ?? []).map((point) => ({
      label: formatShortDate(point.date),
      grossSales: point.grossSales,
      netSales: point.netSales,
      commissionAmount: point.commissionAmount,
      netEarnings: point.netEarnings,
      orders: point.orders,
    }));

    return {
      currency,
      grossSales: financeSummary?.grossSales ?? 0,
      refundedAmount: financeSummary?.refundedAmount ?? 0,
      netSales: financeSummary?.netSales ?? 0,
      orders: financeSummary?.totalOrders ?? 0,
      commissionRate: financeSummary?.commissionRate ?? 0,
      commissionAmount: financeSummary?.commissionAmount ?? 0,
      netEarnings: financeSummary?.netEarnings ?? 0,
      avgOrderValue: financeSummary?.averageOrderValue ?? 0,
      avgDailyRevenue: financeSummary?.averageDailyRevenue ?? 0,
      trendSeries,
      monthlyRows: financeSummary?.monthlySummaries ?? [],
      lifetimeGrossRevenue: financeSummary?.lifetimeGrossRevenue ?? 0,
    };
  }, [financeSummary]);

  const handleExportCsv = () => {
    const filename = `seller-finance-${dateRange.from || 'baslangic'}-${dateRange.to || 'bitis'}.csv`;
    const rows: string[][] = [
      ['Rapor', 'Deger'],
      ['Secili Donem', `${dateRange.from} - ${dateRange.to}`],
      ['Brüt Satış', formatCurrency(financeData.grossSales, financeData.currency)],
      ['İade Tutarı', formatCurrency(financeData.refundedAmount, financeData.currency)],
      ['Net Satış', formatCurrency(financeData.netSales, financeData.currency)],
      ['Siparis Sayisi', formatNumber(financeData.orders)],
      ['Komisyon Orani', formatPercent(financeData.commissionRate)],
      ['Komisyon Tutari', formatCurrency(financeData.commissionAmount, financeData.currency)],
      ['Net Kazanc', formatCurrency(financeData.netEarnings, financeData.currency)],
      ['Ortalama Siparis Degeri', formatCurrency(financeData.avgOrderValue, financeData.currency)],
      ['Ortalama Gunluk Gelir', formatCurrency(financeData.avgDailyRevenue, financeData.currency)],
      ['Toplam Ciro', formatCurrency(financeData.lifetimeGrossRevenue, financeData.currency)],
      [''],
      ['Gun', 'Siparis', 'Brut Satis', 'Net Satis', 'Komisyon', 'Net Kazanc'],
      ...financeData.trendSeries.map((point) => [
        point.label,
        formatNumber(point.orders),
        formatCurrency(point.grossSales, financeData.currency),
        formatCurrency(point.netSales, financeData.currency),
        formatCurrency(point.commissionAmount, financeData.currency),
        formatCurrency(point.netEarnings, financeData.currency),
      ]),
      [''],
      ['Ay', 'Siparis', 'Brut Satis', 'Net Satis', 'Komisyon', 'Net Kazanc'],
      ...financeData.monthlyRows.map((row) => [
        row.monthLabel,
        formatNumber(row.orders),
        formatCurrency(row.grossSales, financeData.currency),
        formatCurrency(row.netSales, financeData.currency),
        formatCurrency(row.commissionAmount, financeData.currency),
        formatCurrency(row.netEarnings, financeData.currency),
      ]),
    ];

    downloadCsv(filename, rows);
  };

  const handlePresetChange = (value: string) => {
    setSelectedPreset(value);
    const days = Number(value);
    if (!Number.isNaN(days) && days > 0) {
      setDateRange(buildPresetDateRange(days));
    }
  };

  const handleDateChange = (field: 'from' | 'to', value: string) => {
    setSelectedPreset('custom');
    setDateRange((current) => ({
      ...current,
      [field]: value,
    }));
  };

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
            Seçili dönem için brüt satış, komisyon ve net kazanç görünümünü mağazanızın gerçek finans endpoint’i üzerinden izleyin.
          </p>
        </div>

        <div className="flex w-full flex-col gap-3 sm:justify-end xl:max-w-xl">
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="sm:col-span-3">
              <Select value={selectedPreset} onValueChange={handlePresetChange}>
                <SelectTrigger>
                  <SelectValue placeholder="Dönem seçin" />
                </SelectTrigger>
                <SelectContent>
                  {periodOptions.map((option) => (
                    <SelectItem key={option.value} value={String(option.value)}>
                      {option.label}
                    </SelectItem>
                  ))}
                  <SelectItem value="custom">Özel Aralık</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <input
              type="date"
              value={dateRange.from}
              onChange={(event) => handleDateChange('from', event.target.value)}
              className="border-input bg-background rounded-md border px-3 py-2 text-sm"
            />
            <input
              type="date"
              value={dateRange.to}
              onChange={(event) => handleDateChange('to', event.target.value)}
              className="border-input bg-background rounded-md border px-3 py-2 text-sm"
            />
            <div className="flex items-center gap-2 rounded-md border border-dashed border-border/70 px-3 py-2 text-xs text-muted-foreground">
              <CalendarDays className="h-4 w-4" />
              {hasInvalidDateRange ? 'Geçersiz tarih aralığı' : `${selectedDays} gün`}
            </div>
          </div>
          <Button variant="outline" onClick={handleExportCsv} disabled={financeData.trendSeries.length === 0 || hasInvalidDateRange}>
            <Download className="mr-2 h-4 w-4" />
            CSV Dışa Aktar
          </Button>
        </div>
      </div>

      {hasInvalidDateRange ? (
        <Card className="border-destructive/30 bg-destructive/5">
          <CardContent className="flex items-center gap-3 p-4 text-sm text-destructive">
            <Info className="h-4 w-4" />
            Başlangıç tarihi bitiş tarihinden büyük olamaz.
          </CardContent>
        </Card>
      ) : null}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Brüt Satış"
          value={formatCurrency(financeData.grossSales, financeData.currency)}
          helperText={`${dateRange.from} - ${dateRange.to} aralığı için toplam satış.`}
          icon={Wallet}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Platform Komisyonu"
          value={formatPercent(financeData.commissionRate)}
          helperText="Varsayılan platform oranı gerçek finans özetinden gösterilir."
          icon={ShoppingBag}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Komisyon Tutarı"
          value={formatCurrency(financeData.commissionAmount, financeData.currency)}
          helperText={`İade sonrası net satıştan düşülen toplam komisyon.`}
          icon={BarChart3}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Net Kazanç"
          value={formatCurrency(financeData.netEarnings, financeData.currency)}
          helperText={`${formatNumber(financeData.orders)} siparişten sonra satıcıya kalan tutar.`}
          icon={CircleDollarSign}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Card className="border-border/70 bg-amber-50/70 dark:bg-amber-950/20">
        <CardContent className="flex gap-3 p-5">
          <Info className="mt-0.5 h-5 w-5 shrink-0 text-amber-600 dark:text-amber-300" />
          <div className="space-y-1 text-sm text-muted-foreground">
            <p className="font-medium text-foreground">Finans özeti şu an varsayılan platform komisyon oranı ile hesaplanıyor.</p>
            <p>
              Seller bazlı özel komisyon oranı tanımlandığında bu ekran aynı endpoint üzerinden doğrudan onu gösterecek.
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card className="min-w-0 border-border/70">
          <CardHeader>
            <CardTitle>Net Kazanç Trendi</CardTitle>
            <CardDescription>Seçili dönemde gün bazlı net kazanç akışı.</CardDescription>
          </CardHeader>
          <CardContent className="min-w-0 h-[320px]">
            <ResponsiveContainer width="100%" height="100%" minWidth={0}>
              <LineChart data={financeData.trendSeries}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} minTickGap={18} />
                <YAxis tickLine={false} axisLine={false} tickFormatter={formatCompactNumber} />
                <Tooltip
                  formatter={(value) => [
                    formatCurrency(typeof value === 'number' ? value : 0, financeData.currency),
                    'Net kazanç',
                  ]}
                />
                <Line type="monotone" dataKey="netEarnings" stroke="#10b981" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="min-w-0 border-border/70">
          <CardHeader>
            <CardTitle>Brüt ve Net Satış</CardTitle>
            <CardDescription>Seçili dönemde günlük satış kırılımı.</CardDescription>
          </CardHeader>
          <CardContent className="min-w-0 h-[320px]">
            <ResponsiveContainer width="100%" height="100%" minWidth={0}>
              <BarChart data={financeData.trendSeries}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} strokeOpacity={0.25} />
                <XAxis dataKey="label" tickLine={false} axisLine={false} minTickGap={18} />
                <YAxis tickLine={false} axisLine={false} tickFormatter={formatCompactNumber} />
                <Tooltip
                  formatter={(value) => [
                    formatCurrency(typeof value === 'number' ? value : 0, financeData.currency),
                    'Tutar',
                  ]}
                />
                <Bar dataKey="grossSales" name="Brüt satış" fill="#6366f1" radius={[8, 8, 0, 0]} />
                <Bar dataKey="netSales" name="Net satış" fill="#f59e0b" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Aylık Özet</CardTitle>
          <CardDescription>
            Ortalama günlük ciro {formatCurrency(financeData.avgDailyRevenue, financeData.currency)}, toplam ciro ise{' '}
            {formatCurrency(financeData.lifetimeGrossRevenue, financeData.currency)}.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ay</TableHead>
                <TableHead>Sipariş</TableHead>
                <TableHead>Brüt Satış</TableHead>
                <TableHead>Net Satış</TableHead>
                <TableHead>Komisyon</TableHead>
                <TableHead>Net Kazanç</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {financeData.monthlyRows.map((row) => (
                <TableRow key={row.monthKey}>
                  <TableCell className="font-medium">{row.monthLabel}</TableCell>
                  <TableCell>{formatNumber(row.orders)}</TableCell>
                  <TableCell>{formatCurrency(row.grossSales, financeData.currency)}</TableCell>
                  <TableCell>{formatCurrency(row.netSales, financeData.currency)}</TableCell>
                  <TableCell>{formatCurrency(row.commissionAmount, financeData.currency)}</TableCell>
                  <TableCell>{formatCurrency(row.netEarnings, financeData.currency)}</TableCell>
                </TableRow>
              ))}
              {financeData.monthlyRows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="p-4">
                    <EmptyState
                      icon={Wallet}
                      title="Finans özeti bulunamadı"
                      description="Seçili dönemde aylık finans özeti oluşturacak sipariş verisi bulunamadı."
                      className="border-none bg-transparent shadow-none"
                    />
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
