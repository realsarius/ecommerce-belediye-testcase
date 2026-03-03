import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import {
  CircleDollarSign,
  Download,
  ReceiptText,
  Store,
  Wallet,
} from 'lucide-react';
import { EmptyState } from '@/components/admin/EmptyState';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { KpiCard } from '@/components/admin/KpiCard';
import { useGetAdminFinanceSummaryQuery } from '@/features/admin/adminApi';
import { formatCompactNumber, formatCurrency, formatDateInput, formatNumber } from '@/lib/format';

function buildApiUrl(path: string) {
  const configuredBase = import.meta.env.VITE_API_URL || '/api/v1';

  if (configuredBase.startsWith('http://') || configuredBase.startsWith('https://')) {
    return `${configuredBase}${path}`;
  }

  return `${window.location.origin}${configuredBase}${path}`;
}

export default function AdminFinancePage() {
  const defaultToDate = new Date();
  const defaultFromDate = new Date();
  defaultFromDate.setDate(defaultToDate.getDate() - 29);

  const [fromDate, setFromDate] = useState(formatDateInput(defaultFromDate));
  const [toDate, setToDate] = useState(formatDateInput(defaultToDate));
  const [isExporting, setIsExporting] = useState(false);

  const { data: financeData, isLoading } = useGetAdminFinanceSummaryQuery({
    from: fromDate,
    to: toDate,
  });

  const chartData = useMemo(() => (
    financeData?.sellers.slice(0, 8).map((row) => ({
      sellerName: row.sellerName.length > 16 ? `${row.sellerName.slice(0, 16)}...` : row.sellerName,
      grossSales: row.grossSales,
      netEarnings: row.netEarnings,
    })) ?? []
  ), [financeData?.sellers]);

  const handleExportCsv = async () => {
    const token = localStorage.getItem('token');
    if (!token) {
      toast.error('Oturum bulunamadı. Lütfen yeniden giriş yapın.');
      return;
    }

    setIsExporting(true);

    try {
      const query = new URLSearchParams({ from: fromDate, to: toDate }).toString();
      const response = await fetch(buildApiUrl(`/admin/finance/export?${query}`), {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('CSV dışa aktarma başarısız');
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `admin-finance-${fromDate}-${toDate}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      toast.success('Finans raporu indirildi');
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'CSV dışa aktarma başarısız');
    } finally {
      setIsExporting(false);
    }
  };

  if (isLoading || !financeData) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-72" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-40 rounded-xl" />
          ))}
        </div>
        <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
          <Skeleton className="h-[340px] rounded-xl" />
          <Skeleton className="h-[340px] rounded-xl" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Gelir Raporu</h1>
          <p className="max-w-3xl text-muted-foreground">
            Seller bazlı komisyon ve net kazanç görünümü artık gerçek admin finance endpoint’inden geliyor.
          </p>
        </div>

        <div className="flex w-full flex-col gap-3 sm:flex-row sm:justify-end xl:max-w-2xl">
          <Input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
          <Input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} />
          <Button variant="outline" onClick={handleExportCsv} disabled={financeData.sellers.length === 0 || isExporting}>
            <Download className="mr-2 h-4 w-4" />
            CSV Dışa Aktar
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Toplam Gelir"
          value={formatCurrency(financeData.totalRevenue, financeData.currency)}
          helperText="Başarılı sipariş satırlarından hesaplandı."
          icon={Wallet}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Toplam Komisyon"
          value={formatCurrency(financeData.totalCommission, financeData.currency)}
          helperText="Seller override oranları da bu tutara dahil."
          icon={Store}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Ort. Sipariş Değeri"
          value={formatCurrency(financeData.averageOrderValue, financeData.currency)}
          helperText="Gelir oluşturan siparişler üzerinden hesaplandı."
          icon={ReceiptText}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Toplam İade Tutarı"
          value={formatCurrency(financeData.totalRefundAmount, financeData.currency)}
          helperText="İade statüsündeki sipariş kalemlerinden hesaplandı."
          icon={CircleDollarSign}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Seller Bazlı Net Kazanç</CardTitle>
            <CardDescription>En yüksek net satış üreten seller hesaplarının özeti.</CardDescription>
          </CardHeader>
          <CardContent className="h-[340px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} layout="vertical" margin={{ left: 12, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} strokeOpacity={0.25} />
                <XAxis type="number" tickLine={false} axisLine={false} tickFormatter={formatCompactNumber} />
                <YAxis type="category" dataKey="sellerName" tickLine={false} axisLine={false} width={110} />
                <Tooltip
                  formatter={(value, name) => [
                    formatCurrency(typeof value === 'number' ? value : 0, financeData.currency),
                    name === 'grossSales' ? 'Brüt Satış' : 'Net Kazanç',
                  ]}
                />
                <Bar dataKey="grossSales" fill="#cbd5e1" radius={[0, 8, 8, 0]} />
                <Bar dataKey="netEarnings" fill="#10b981" radius={[0, 8, 8, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Seller Bazlı Komisyon Tablosu</CardTitle>
            <CardDescription>
              Sipariş kalemleri seller bazında gruplanarak komisyon ve net kazanç görünümü üretildi.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Seller</TableHead>
                  <TableHead>Sipariş</TableHead>
                  <TableHead>Brüt Satış</TableHead>
                  <TableHead>Komisyon</TableHead>
                  <TableHead>Net Kazanç</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {financeData.sellers.map((row) => (
                  <TableRow key={row.sellerId ?? row.sellerName}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Store className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <p className="font-medium">{row.sellerName}</p>
                          <p className="text-xs text-muted-foreground">
                            İade: {formatCurrency(row.refundedAmount, financeData.currency)}
                          </p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>{formatNumber(row.successfulOrders)}</TableCell>
                    <TableCell>{formatCurrency(row.grossSales, financeData.currency)}</TableCell>
                    <TableCell>
                      <div>
                        <p>{formatCurrency(row.commissionAmount, financeData.currency)}</p>
                        <p className="text-xs text-muted-foreground">%{row.commissionRate}</p>
                      </div>
                    </TableCell>
                    <TableCell>{formatCurrency(row.netEarnings, financeData.currency)}</TableCell>
                  </TableRow>
                ))}
                {financeData.sellers.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="p-4">
                      <EmptyState
                        icon={Wallet}
                        title="Finans verisi bulunamadı"
                        description="Seçili tarih aralığında seller bazlı komisyon veya net kazanç verisi oluşmadı."
                        className="border-none bg-transparent shadow-none"
                      />
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
              {financeData.sellers.length > 0 ? (
                <TableFooter>
                  <TableRow>
                    <TableCell className="font-semibold">Toplam</TableCell>
                    <TableCell>{formatNumber(financeData.successfulOrderCount)}</TableCell>
                    <TableCell>{formatCurrency(financeData.totalRevenue, financeData.currency)}</TableCell>
                    <TableCell>{formatCurrency(financeData.totalCommission, financeData.currency)}</TableCell>
                    <TableCell>
                      {formatCurrency(
                        Math.max(0, financeData.totalRevenue - financeData.totalRefundAmount - financeData.totalCommission),
                        financeData.currency
                      )}
                    </TableCell>
                  </TableRow>
                </TableFooter>
              ) : null}
            </Table>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
