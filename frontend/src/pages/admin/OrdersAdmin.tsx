import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import {
  Eye,
  Package as PackageIcon,
  ShoppingBag,
  Store,
  Truck,
  Wallet,
} from 'lucide-react';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
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
import { StatusBadge } from '@/components/admin/StatusBadge';
import { useGetAdminOrdersQuery, useUpdateOrderStatusMutation } from '@/features/admin/adminApi';
import type { OrderStatus } from '@/features/orders/types';
import { useSearchProductsQuery } from '@/features/products/productsApi';

const statusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
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

function getOrderStatusTone(status: OrderStatus) {
  if (status === 'Delivered') return 'success' as const;
  if (status === 'Cancelled' || status === 'Refunded') return 'danger' as const;
  if (status === 'PendingPayment') return 'warning' as const;
  return 'info' as const;
}

export default function OrdersAdmin() {
  const [statusFilter, setStatusFilter] = useState<'all' | OrderStatus>('all');
  const [sellerFilter, setSellerFilter] = useState<'all' | string>('all');
  const [search, setSearch] = useState('');
  const [minAmount, setMinAmount] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [updatingOrderId, setUpdatingOrderId] = useState<number | null>(null);

  const { data: orders = [], isLoading } = useGetAdminOrdersQuery();
  const { data: productsData } = useSearchProductsQuery({ page: 1, pageSize: 500 });
  const [updateStatus] = useUpdateOrderStatusMutation();

  const productSellerMap = useMemo(() => {
    const productMap = new Map<number, string>();
    for (const product of productsData?.items ?? []) {
      productMap.set(
        product.id,
        product.sellerBrandName?.trim() || (product.sellerId ? `Seller #${product.sellerId}` : 'Atanmamış')
      );
    }

    return productMap;
  }, [productsData?.items]);

  const sellerOptions = useMemo(() => {
    return Array.from(new Set(Array.from(productSellerMap.values())))
      .filter(Boolean)
      .sort((a, b) => a.localeCompare(b, 'tr'));
  }, [productSellerMap]);

  const filteredOrders = useMemo(() => {
    return orders.filter((order) => {
      const sellerNames = Array.from(new Set(
        order.items
          .map((item) => productSellerMap.get(item.productId))
          .filter((value): value is string => Boolean(value))
      ));

      if (statusFilter !== 'all' && order.status !== statusFilter) {
        return false;
      }

      if (sellerFilter !== 'all' && !sellerNames.includes(sellerFilter)) {
        return false;
      }

      if (search.trim()) {
        const term = search.trim().toLocaleLowerCase('tr-TR');
        const haystacks = [
          order.orderNumber,
          String(order.id),
          order.customerName,
          sellerNames.join(' '),
          order.userId ? `kullanıcı #${order.userId}` : undefined,
        ]
          .filter(Boolean)
          .join(' ')
          .toLocaleLowerCase('tr-TR');

        if (!haystacks.includes(term)) {
          return false;
        }
      }

      if (minAmount && order.totalAmount < Number(minAmount)) {
        return false;
      }

      const orderDate = new Date(order.createdAt);
      if (fromDate && orderDate < new Date(`${fromDate}T00:00:00`)) {
        return false;
      }

      if (toDate && orderDate > new Date(`${toDate}T23:59:59`)) {
        return false;
      }

      return true;
    });
  }, [fromDate, minAmount, orders, productSellerMap, search, sellerFilter, statusFilter, toDate]);

  const summary = useMemo(() => {
    return {
      total: filteredOrders.length,
      revenue: filteredOrders
        .filter((order) => ['Paid', 'Processing', 'Shipped', 'Delivered'].includes(order.status))
        .reduce((sum, order) => sum + order.totalAmount, 0),
      pending: filteredOrders.filter((order) => order.status === 'PendingPayment' || order.status === 'Paid').length,
      shipped: filteredOrders.filter((order) => order.status === 'Shipped').length,
      delivered: filteredOrders.filter((order) => order.status === 'Delivered').length,
    };
  }, [filteredOrders]);

  const handleStatusChange = async (orderId: number, newStatus: OrderStatus) => {
    try {
      setUpdatingOrderId(orderId);
      await updateStatus({ id: orderId, status: newStatus }).unwrap();
      toast.success('Sipariş durumu güncellendi');
    } catch {
      toast.error('Sipariş durumu güncellenemedi');
    } finally {
      setUpdatingOrderId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-36 rounded-xl" />
          ))}
        </div>
        <Skeleton className="h-[420px] rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Sipariş Yönetimi</h1>
        <p className="max-w-3xl text-muted-foreground">
          Sipariş akışını filtreleyin, operasyonel durumları güncelleyin ve detay sayfasına inerek tüm ödeme/adres bağlamını görün.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Filtrelenen Sipariş"
          value={summary.total.toLocaleString('tr-TR')}
          helperText="Mevcut filtrelere uyan toplam sipariş."
          icon={ShoppingBag}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Gelir Toplamı"
          value={formatCurrency(summary.revenue)}
          helperText="Gelir oluşturan sipariş durumlarından hesaplandı."
          icon={Wallet}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="İşlem Bekleyen"
          value={summary.pending.toLocaleString('tr-TR')}
          helperText="Ödeme bekleyen veya henüz işleme alınan siparişler."
          icon={PackageIcon}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Kargoda / Teslim"
          value={`${summary.shipped} / ${summary.delivered}`}
          helperText="Kargodaki ve tamamlanan siparişlerin görünümü."
          icon={Truck}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Filtreler</CardTitle>
          <CardDescription>Durum, seller, tarih, müşteri ve minimum tutar bazlı hızlı operasyon filtresi.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
          <Input
            placeholder="Sipariş no veya müşteri ara..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />

          <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as 'all' | OrderStatus)}>
            <SelectTrigger>
              <SelectValue placeholder="Durum seçin" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tüm Durumlar</SelectItem>
              {Object.entries(statusLabels).map(([status, label]) => (
                <SelectItem key={status} value={status}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={sellerFilter} onValueChange={setSellerFilter}>
            <SelectTrigger>
              <SelectValue placeholder="Seller seçin" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tüm Seller&apos;lar</SelectItem>
              {sellerOptions.map((seller) => (
                <SelectItem key={seller} value={seller}>
                  {seller}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Input type="number" min="0" placeholder="Minimum tutar" value={minAmount} onChange={(event) => setMinAmount(event.target.value)} />
          <Input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
          <Input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} />
        </CardContent>
      </Card>

      <div className="overflow-hidden rounded-xl border border-border/70 bg-card">
        <Table>
          <TableHeader>
              <TableRow>
                <TableHead>Sipariş</TableHead>
                <TableHead>Müşteri</TableHead>
                <TableHead>Seller</TableHead>
                <TableHead>Ürün</TableHead>
                <TableHead>Tutar</TableHead>
                <TableHead>Durum</TableHead>
              <TableHead>Tarih</TableHead>
              <TableHead className="text-right">Detay</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredOrders.map((order) => (
              <TableRow key={order.id}>
                {(() => {
                  const sellerNames = Array.from(new Set(
                    order.items
                      .map((item) => productSellerMap.get(item.productId))
                      .filter((value): value is string => Boolean(value))
                  ));

                  return (
                    <>
                      <TableCell className="font-medium">#{order.orderNumber || order.id}</TableCell>
                      <TableCell>{order.customerName || `Kullanıcı #${order.userId}`}</TableCell>
                      <TableCell>
                        <div className="flex items-start gap-2 text-sm text-muted-foreground">
                          <Store className="mt-0.5 h-4 w-4" />
                          <div className="space-y-1">
                            {sellerNames.length > 0 ? sellerNames.map((sellerName) => (
                              <p key={`${order.id}-${sellerName}`}>{sellerName}</p>
                            )) : <p>Atanmamış</p>}
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2 text-muted-foreground">
                          <PackageIcon className="h-4 w-4" />
                          <span>{order.items.length} ürün</span>
                        </div>
                      </TableCell>
                      <TableCell>{formatCurrency(order.totalAmount, order.currency || 'TRY')}</TableCell>
                      <TableCell>
                        <Select
                          value={order.status}
                          onValueChange={(value) => handleStatusChange(order.id, value as OrderStatus)}
                          disabled={updatingOrderId === order.id}
                        >
                          <SelectTrigger className="w-[200px]">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {Object.entries(statusLabels).map(([status, label]) => (
                              <SelectItem key={status} value={status}>
                                {label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <StatusBadge
                          label={statusLabels[order.status]}
                          tone={getOrderStatusTone(order.status)}
                          className="mt-2"
                        />
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {new Date(order.createdAt).toLocaleDateString('tr-TR')}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={`/admin/orders/${order.id}`}>
                            <Eye className="mr-2 h-4 w-4" />
                            İncele
                          </Link>
                        </Button>
                      </TableCell>
                    </>
                  );
                })()}
              </TableRow>
            ))}
            {filteredOrders.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} className="py-12 text-center text-muted-foreground">
                  Filtrelere uygun sipariş bulunamadı.
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
