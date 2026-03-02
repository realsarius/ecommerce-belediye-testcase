import { useMemo, useState } from 'react';
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
  HeartPulse,
  Info,
  ReceiptText,
  Store,
  Wallet,
} from 'lucide-react';
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
import { useGetAdminOrdersQuery } from '@/features/admin/adminApi';
import { useSearchProductsQuery } from '@/features/products/productsApi';
import type { Order, OrderStatus } from '@/features/orders/types';

const DEFAULT_COMMISSION_RATE = 10;

type SellerFinanceRow = {
  sellerName: string;
  grossSales: number;
  refundedAmount: number;
  netSales: number;
  successfulOrders: number;
  commissionRate: number;
  commissionAmount: number;
  netEarnings: number;
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

function isRevenueStatus(status: OrderStatus) {
  return status === 'Paid' || status === 'Processing' || status === 'Shipped' || status === 'Delivered';
}

function isRefundStatus(status: OrderStatus) {
  return status === 'Refunded';
}

function toDateInputValue(value: Date) {
  return value.toISOString().slice(0, 10);
}

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

function buildSellerRows(
  orders: Order[],
  productSellerMap: Map<number, string>
) {
  const sellerMap = new Map<string, SellerFinanceRow>();

  for (const order of orders) {
    for (const item of order.items) {
      const sellerName = productSellerMap.get(item.productId) ?? 'Atanmamış';
      const current = sellerMap.get(sellerName) ?? {
        sellerName,
        grossSales: 0,
        refundedAmount: 0,
        netSales: 0,
        successfulOrders: 0,
        commissionRate: DEFAULT_COMMISSION_RATE,
        commissionAmount: 0,
        netEarnings: 0,
      };

      if (isRevenueStatus(order.status)) {
        current.grossSales += item.lineTotal;
      }

      if (isRefundStatus(order.status)) {
        current.refundedAmount += item.lineTotal;
      }

      sellerMap.set(sellerName, current);
    }

    if (isRevenueStatus(order.status)) {
      const sellerNames = Array.from(new Set(
        order.items.map((item) => productSellerMap.get(item.productId) ?? 'Atanmamış')
      ));

      for (const sellerName of sellerNames) {
        const current = sellerMap.get(sellerName);
        if (current) {
          current.successfulOrders += 1;
        }
      }
    }
  }

  return Array.from(sellerMap.values())
    .map((row) => {
      const netSales = Math.max(0, row.grossSales - row.refundedAmount);
      const commissionAmount = netSales * (row.commissionRate / 100);
      const netEarnings = Math.max(0, netSales - commissionAmount);

      return {
        ...row,
        netSales,
        commissionAmount,
        netEarnings,
      };
    })
    .sort((a, b) => b.netSales - a.netSales);
}

export default function AdminFinancePage() {
  const defaultToDate = new Date();
  const defaultFromDate = new Date();
  defaultFromDate.setDate(defaultToDate.getDate() - 29);

  const [fromDate, setFromDate] = useState(toDateInputValue(defaultFromDate));
  const [toDate, setToDate] = useState(toDateInputValue(defaultToDate));

  const { data: orders = [], isLoading: ordersLoading } = useGetAdminOrdersQuery();
  const { data: productsData, isLoading: productsLoading } = useSearchProductsQuery({ page: 1, pageSize: 500 });

  const isLoading = ordersLoading || productsLoading;

  const financeData = useMemo(() => {
    const productSellerMap = new Map<number, string>();
    for (const product of productsData?.items ?? []) {
      productSellerMap.set(
        product.id,
        product.sellerBrandName?.trim() || (product.sellerId ? `Seller #${product.sellerId}` : 'Atanmamış')
      );
    }

    const filteredOrders = orders.filter((order) => {
      const orderDate = new Date(order.createdAt);
      if (fromDate && orderDate < new Date(`${fromDate}T00:00:00`)) {
        return false;
      }

      if (toDate && orderDate > new Date(`${toDate}T23:59:59`)) {
        return false;
      }

      return true;
    });

    const currency = filteredOrders[0]?.currency || 'TRY';
    const sellerRows = buildSellerRows(filteredOrders, productSellerMap);
    const totalRevenue = sellerRows.reduce((sum, row) => sum + row.grossSales, 0);
    const totalRefund = sellerRows.reduce((sum, row) => sum + row.refundedAmount, 0);
    const totalCommission = sellerRows.reduce((sum, row) => sum + row.commissionAmount, 0);
    const successfulOrderCount = filteredOrders.filter((order) => isRevenueStatus(order.status)).length;
    const averageOrderValue = successfulOrderCount > 0 ? totalRevenue / successfulOrderCount : 0;

    const sellerChartData = sellerRows.slice(0, 8).map((row) => ({
      sellerName: row.sellerName.length > 16 ? `${row.sellerName.slice(0, 16)}...` : row.sellerName,
      grossSales: row.grossSales,
      netEarnings: row.netEarnings,
    }));

    return {
      currency,
      filteredOrders,
      sellerRows,
      totalRevenue,
      totalRefund,
      totalCommission,
      averageOrderValue,
      sellerChartData,
    };
  }, [fromDate, orders, productsData?.items, toDate]);

  const handleExportCsv = () => {
    const rows: string[][] = [
      ['Rapor', 'Deger'],
      ['Baslangic Tarihi', fromDate],
      ['Bitis Tarihi', toDate],
      ['Toplam Gelir', formatCurrency(financeData.totalRevenue, financeData.currency)],
      ['Toplam Komisyon', formatCurrency(financeData.totalCommission, financeData.currency)],
      ['Ortalama Siparis Degeri', formatCurrency(financeData.averageOrderValue, financeData.currency)],
      ['Toplam Iade Tutari', formatCurrency(financeData.totalRefund, financeData.currency)],
      [''],
      ['Seller', 'Basarili Siparis', 'Brut Satis', 'Iade', 'Net Satis', 'Komisyon Orani', 'Komisyon Tutari', 'Net Kazanc'],
      ...financeData.sellerRows.map((row) => [
        row.sellerName,
        row.successfulOrders.toLocaleString('tr-TR'),
        formatCurrency(row.grossSales, financeData.currency),
        formatCurrency(row.refundedAmount, financeData.currency),
        formatCurrency(row.netSales, financeData.currency),
        `%${row.commissionRate}`,
        formatCurrency(row.commissionAmount, financeData.currency),
        formatCurrency(row.netEarnings, financeData.currency),
      ]),
    ];

    downloadCsv(`admin-finance-${fromDate}-${toDate}.csv`, rows);
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
            Sipariş ve katalog verilerinden üretilen seller bazlı finans görünümü. Gerçek komisyon override ve export endpoint’leri geldiğinde bu ekran doğrudan onlara bağlanacak.
          </p>
        </div>

        <div className="flex w-full flex-col gap-3 sm:flex-row sm:justify-end xl:max-w-2xl">
          <Input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
          <Input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} />
          <Button variant="outline" onClick={handleExportCsv} disabled={financeData.sellerRows.length === 0}>
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
          helperText={`Varsayılan %${DEFAULT_COMMISSION_RATE} komisyon kabulü ile tahmini.`}
          icon={HeartPulse}
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
          value={formatCurrency(financeData.totalRefund, financeData.currency)}
          helperText="İade statüsündeki sipariş kalemlerinden türetildi."
          icon={CircleDollarSign}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Card className="border-border/70 bg-amber-50/70 dark:bg-amber-950/20">
        <CardContent className="flex gap-3 p-5">
          <Info className="mt-0.5 h-5 w-5 shrink-0 text-amber-600 dark:text-amber-300" />
          <div className="space-y-1 text-sm text-muted-foreground">
            <p className="font-medium text-foreground">Komisyon değerleri şu an tahmini olarak hesaplanıyor.</p>
            <p>
              Ayrı admin finance endpoint’i ve seller bazlı komisyon override yapısı henüz olmadığı için bu ekran, varsayılan %{DEFAULT_COMMISSION_RATE} oranı ile sipariş kalemlerinden türetilen bir görünüm sunuyor.
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Seller Bazlı Net Kazanç</CardTitle>
            <CardDescription>En yüksek net satış üreten seller hesaplarının özeti.</CardDescription>
          </CardHeader>
          <CardContent className="h-[340px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={financeData.sellerChartData} layout="vertical" margin={{ left: 12, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} strokeOpacity={0.25} />
                <XAxis type="number" tickLine={false} axisLine={false} tickFormatter={formatCompactTick} />
                <YAxis
                  type="category"
                  dataKey="sellerName"
                  tickLine={false}
                  axisLine={false}
                  width={110}
                />
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
                {financeData.sellerRows.map((row) => (
                  <TableRow key={row.sellerName}>
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
                    <TableCell>{row.successfulOrders.toLocaleString('tr-TR')}</TableCell>
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
                {financeData.sellerRows.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="py-12 text-center text-muted-foreground">
                      Seçili tarih aralığı için finans verisi bulunamadı.
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
              {financeData.sellerRows.length > 0 ? (
                <TableFooter>
                  <TableRow>
                    <TableCell className="font-semibold">Toplam</TableCell>
                    <TableCell>{financeData.filteredOrders.filter((order) => isRevenueStatus(order.status)).length.toLocaleString('tr-TR')}</TableCell>
                    <TableCell>{formatCurrency(financeData.totalRevenue, financeData.currency)}</TableCell>
                    <TableCell>{formatCurrency(financeData.totalCommission, financeData.currency)}</TableCell>
                    <TableCell>{formatCurrency(Math.max(0, financeData.totalRevenue - financeData.totalRefund - financeData.totalCommission), financeData.currency)}</TableCell>
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
