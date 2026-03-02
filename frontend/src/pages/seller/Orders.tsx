import { useMemo, useState } from 'react';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { useUpdateOrderStatusMutation } from '@/features/admin/adminApi';
import { useGetSellerOrdersQuery } from '@/features/seller/sellerApi';
import type { Order, OrderStatus } from '@/features/orders/types';
import { Package, Truck } from 'lucide-react';
import { toast } from 'sonner';

const orderStatusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
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

export default function SellerOrders() {
  const [statusFilter, setStatusFilter] = useState<'all' | OrderStatus>('all');
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const { data: orders = [], isLoading } = useGetSellerOrdersQuery();
  const [updateOrderStatus, { isLoading: isUpdating }] = useUpdateOrderStatusMutation();

  const filteredOrders = useMemo(() => {
    if (statusFilter === 'all') {
      return orders;
    }

    return orders.filter((order) => order.status === statusFilter);
  }, [orders, statusFilter]);

  const summary = useMemo(() => {
    return {
      total: orders.length,
      active: orders.filter((order) => order.status === 'Processing' || order.status === 'Shipped').length,
      delivered: orders.filter((order) => order.status === 'Delivered').length,
      pending: orders.filter((order) => order.status === 'PendingPayment' || order.status === 'Paid').length,
    };
  }, [orders]);

  const handleStatusChange = async (orderId: number, status: OrderStatus) => {
    try {
      await updateOrderStatus({ id: orderId, status }).unwrap();
      toast.success('Sipariş durumu güncellendi');
    } catch {
      toast.error('Sipariş durumu güncellenemedi');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-72" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-32 rounded-xl" />
          ))}
        </div>
        <Skeleton className="h-[420px] rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Siparişlerim</h1>
          <p className="max-w-3xl text-muted-foreground">
            Mağazanıza ait siparişleri takip edin, duruma göre filtreleyin ve operasyonel ilerlemeyi tek ekrandan yönetin.
          </p>
        </div>

        <div className="w-full max-w-xs">
          <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as 'all' | OrderStatus)}>
            <SelectTrigger>
              <SelectValue placeholder="Duruma göre filtrele" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tüm Durumlar</SelectItem>
              {Object.entries(orderStatusLabels).map(([status, label]) => (
                <SelectItem key={status} value={status}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Toplam Sipariş</CardDescription>
            <CardTitle className="text-3xl">{summary.total.toLocaleString('tr-TR')}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Aktif Operasyon</CardDescription>
            <CardTitle className="text-3xl">{summary.active.toLocaleString('tr-TR')}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Teslim Edilen</CardDescription>
            <CardTitle className="text-3xl">{summary.delivered.toLocaleString('tr-TR')}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>İşlem Bekleyen</CardDescription>
            <CardTitle className="text-3xl">{summary.pending.toLocaleString('tr-TR')}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Truck className="h-5 w-5 text-amber-600" />
            Sipariş Listesi
          </CardTitle>
          <CardDescription>
            Bu liste seller yetkisiyle erişilebilen mevcut sipariş endpoint’i üzerinden mağazanıza göre filtrelenir.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Sipariş</TableHead>
                <TableHead>Müşteri</TableHead>
                <TableHead>Ürünler</TableHead>
                <TableHead>Tutar</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead>Tarih</TableHead>
                <TableHead className="text-right">Detay</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredOrders.map((order) => (
                <TableRow key={order.id}>
                  <TableCell className="font-medium">#{order.orderNumber || order.id}</TableCell>
                  <TableCell>{maskCustomerName(order.customerName)}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2 text-muted-foreground">
                      <Package className="h-4 w-4" />
                      <span>{order.items.length} ürün</span>
                    </div>
                  </TableCell>
                  <TableCell>{formatCurrency(order.totalAmount, order.currency || 'TRY')}</TableCell>
                  <TableCell>
                    <Select
                      value={order.status}
                      onValueChange={(value) => handleStatusChange(order.id, value as OrderStatus)}
                      disabled={isUpdating}
                    >
                      <SelectTrigger className="h-9 w-[190px]">
                        <Badge className={orderStatusClasses[order.status]} variant="secondary">
                          {orderStatusLabels[order.status]}
                        </Badge>
                      </SelectTrigger>
                      <SelectContent>
                        {Object.entries(orderStatusLabels).map(([status, label]) => (
                          <SelectItem key={status} value={status}>
                            {label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {new Date(order.createdAt).toLocaleDateString('tr-TR')}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button variant="ghost" size="sm" onClick={() => setSelectedOrder(order)}>
                      İncele
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {filteredOrders.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="py-12 text-center text-muted-foreground">
                    Seçili filtre için sipariş bulunamadı.
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Dialog open={!!selectedOrder} onOpenChange={(open) => (!open ? setSelectedOrder(null) : undefined)}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Sipariş Detayı</DialogTitle>
            <DialogDescription>
              {selectedOrder ? `#${selectedOrder.orderNumber || selectedOrder.id}` : 'Sipariş detayları'}
            </DialogDescription>
          </DialogHeader>

          {selectedOrder ? (
            <div className="space-y-6">
              <div className="grid gap-4 md:grid-cols-3">
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Müşteri</CardDescription>
                    <CardTitle className="text-lg">{maskCustomerName(selectedOrder.customerName)}</CardTitle>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Toplam Tutar</CardDescription>
                    <CardTitle className="text-lg">
                      {formatCurrency(selectedOrder.totalAmount, selectedOrder.currency || 'TRY')}
                    </CardTitle>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Durum</CardDescription>
                    <CardTitle className="text-lg">{orderStatusLabels[selectedOrder.status]}</CardTitle>
                  </CardHeader>
                </Card>
              </div>

              <div className="space-y-3">
                <h3 className="font-semibold">Ürünler</h3>
                <div className="rounded-xl border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Ürün</TableHead>
                        <TableHead>Adet</TableHead>
                        <TableHead>Birim Fiyat</TableHead>
                        <TableHead>Ara Toplam</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {selectedOrder.items.map((item) => (
                        <TableRow key={`${selectedOrder.id}-${item.productId}`}>
                          <TableCell>{item.productName}</TableCell>
                          <TableCell>{item.quantity}</TableCell>
                          <TableCell>{formatCurrency(item.priceSnapshot, selectedOrder.currency || 'TRY')}</TableCell>
                          <TableCell>{formatCurrency(item.lineTotal, selectedOrder.currency || 'TRY')}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </div>

              <div className="space-y-2">
                <h3 className="font-semibold">Teslimat Adresi</h3>
                <p className="rounded-xl border bg-muted/30 p-4 text-sm text-muted-foreground">
                  {selectedOrder.shippingAddress}
                </p>
              </div>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  );
}
