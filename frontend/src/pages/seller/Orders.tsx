import { useEffect, useMemo, useState } from 'react';
import { Package, Truck } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import { Input } from '@/components/common/input';
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
import { Label } from '@/components/common/label';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { useUpdateOrderStatusMutation } from '@/features/admin/adminApi';
import { useGetSellerOrdersQuery } from '@/features/seller/sellerApi';
import type { Order, OrderStatus } from '@/features/orders/types';

const SHIPPING_STORAGE_KEY = 'seller-order-shipping-records';

type ShippingRecord = {
  trackingCode: string;
  cargoCompany: string;
  updatedAt: string;
};

const orderStatusLabels: Record<OrderStatus, string> = {
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

function canShipOrder(status: OrderStatus) {
  return status === 'Paid' || status === 'Processing';
}

function buildTimeline(order: Order, shippingRecord?: ShippingRecord) {
  const currentIndex = ({
    PendingPayment: 0,
    Paid: 1,
    Processing: 2,
    Shipped: 3,
    Delivered: 4,
    Cancelled: 4,
    Refunded: 4,
  } as const)[order.status];

  return [
    { label: 'Oluşturuldu', active: currentIndex >= 0, meta: new Date(order.createdAt).toLocaleString('tr-TR') },
    { label: 'Onaylandı', active: currentIndex >= 1, meta: currentIndex >= 1 ? 'Ödeme doğrulandı' : 'Bekleniyor' },
    { label: 'Hazırlanıyor', active: currentIndex >= 2, meta: currentIndex >= 2 ? 'Seller operasyonunda' : 'Bekleniyor' },
    {
      label: 'Kargoda',
      active: currentIndex >= 3,
      meta: shippingRecord
        ? `${shippingRecord.cargoCompany} • ${shippingRecord.trackingCode}`
        : (currentIndex >= 3 ? 'Kargo bilgisi bekleniyor' : 'Bekleniyor'),
    },
    { label: 'Teslim Edildi', active: currentIndex >= 4 && order.status !== 'Cancelled' && order.status !== 'Refunded', meta: currentIndex >= 4 ? 'Tamamlandı' : 'Bekleniyor' },
  ];
}

export default function SellerOrders() {
  const [statusFilter, setStatusFilter] = useState<'all' | OrderStatus>('all');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [shippingDialogOrder, setShippingDialogOrder] = useState<Order | null>(null);
  const [trackingCode, setTrackingCode] = useState('');
  const [cargoCompany, setCargoCompany] = useState('');
  const [shippingRecords, setShippingRecords] = useState<Record<number, ShippingRecord>>(() => {
    if (typeof window === 'undefined') {
      return {};
    }

    try {
      const saved = window.localStorage.getItem(SHIPPING_STORAGE_KEY);
      return saved ? JSON.parse(saved) as Record<number, ShippingRecord> : {};
    } catch {
      return {};
    }
  });

  const { data: orders = [], isLoading } = useGetSellerOrdersQuery();
  const [updateOrderStatus, { isLoading: isUpdating }] = useUpdateOrderStatusMutation();

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    window.localStorage.setItem(SHIPPING_STORAGE_KEY, JSON.stringify(shippingRecords));
  }, [shippingRecords]);

  const filteredOrders = useMemo(() => {
    return orders.filter((order) => {
      if (statusFilter !== 'all' && order.status !== statusFilter) {
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
  }, [fromDate, orders, statusFilter, toDate]);

  const summary = useMemo(() => {
    return {
      total: filteredOrders.length,
      active: filteredOrders.filter((order) => order.status === 'Processing' || order.status === 'Shipped').length,
      delivered: filteredOrders.filter((order) => order.status === 'Delivered').length,
      pending: filteredOrders.filter((order) => order.status === 'PendingPayment' || order.status === 'Paid').length,
    };
  }, [filteredOrders]);

  const openShippingDialog = (order: Order) => {
    const savedRecord = shippingRecords[order.id];
    setShippingDialogOrder(order);
    setTrackingCode(savedRecord?.trackingCode ?? '');
    setCargoCompany(savedRecord?.cargoCompany ?? '');
  };

  const handleShipOrder = async () => {
    if (!shippingDialogOrder) {
      return;
    }

    if (!trackingCode.trim() || !cargoCompany.trim()) {
      toast.error('Kargo firması ve takip kodu zorunludur');
      return;
    }

    try {
      await updateOrderStatus({ id: shippingDialogOrder.id, status: 'Shipped' }).unwrap();
      setShippingRecords((current) => ({
        ...current,
        [shippingDialogOrder.id]: {
          trackingCode: trackingCode.trim(),
          cargoCompany: cargoCompany.trim(),
          updatedAt: new Date().toISOString(),
        },
      }));
      toast.success('Sipariş kargoya verildi olarak işaretlendi');
      setShippingDialogOrder(null);
      setTrackingCode('');
      setCargoCompany('');
    } catch {
      toast.error('Sipariş kargoya verilemedi');
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
            Mağazanıza ait siparişleri filtreleyin, detaylarını inceleyin ve hazır olanları takip kodu ile kargoya verin.
          </p>
          <p className="text-sm text-muted-foreground">
            Ayrı seller ship endpoint’i gelene kadar takip kodu ve kargo firması bilgisi bu panelde yerel olarak saklanır; sipariş durumu ise mevcut backend akışıyla güncellenir.
          </p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Filtrelenen Sipariş"
          value={summary.total.toLocaleString('tr-TR')}
          helperText="Seçili durum ve tarih aralığına uyan siparişler."
          icon={Package}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Operasyon"
          value={summary.active.toLocaleString('tr-TR')}
          helperText="Hazırlanan veya kargoda olan siparişler."
          icon={Truck}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Teslim Edilen"
          value={summary.delivered.toLocaleString('tr-TR')}
          helperText="Tamamlanan siparişler."
          icon={Package}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="İşlem Bekleyen"
          value={summary.pending.toLocaleString('tr-TR')}
          helperText="Henüz kargoya verilmeyen ödemeli siparişler."
          icon={Package}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Filtreler</CardTitle>
          <CardDescription>Durum ve tarih aralığı ile operasyon görünümünü daraltın.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-3">
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

          <Input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
          <Input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} />
        </CardContent>
      </Card>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Truck className="h-5 w-5 text-amber-600" />
            Sipariş Listesi
          </CardTitle>
          <CardDescription>
            Kargoya hazır siparişlerde takip kodu tanımlayıp durumu doğrudan bu ekrandan güncelleyebilirsiniz.
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
                <TableHead>Kargo</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead>Tarih</TableHead>
                <TableHead className="text-right">İşlemler</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredOrders.map((order) => {
                const shippingRecord = shippingRecords[order.id];

                return (
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
                    <TableCell className="text-sm text-muted-foreground">
                      {shippingRecord ? (
                        <div className="space-y-1">
                          <p className="font-medium text-foreground">{shippingRecord.cargoCompany}</p>
                          <p>{shippingRecord.trackingCode}</p>
                        </div>
                      ) : (
                        'Henüz tanımlanmadı'
                      )}
                    </TableCell>
                    <TableCell>
                      <StatusBadge label={orderStatusLabels[order.status]} tone={getOrderStatusTone(order.status)} />
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(order.createdAt).toLocaleDateString('tr-TR')}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        {canShipOrder(order.status) ? (
                          <Button variant="outline" size="sm" onClick={() => openShippingDialog(order)}>
                            Kargoya Ver
                          </Button>
                        ) : null}
                        <Button variant="ghost" size="sm" onClick={() => setSelectedOrder(order)}>
                          İncele
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
              {filteredOrders.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} className="py-12 text-center text-muted-foreground">
                    Seçili filtre için sipariş bulunamadı.
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Dialog open={!!selectedOrder} onOpenChange={(open) => (!open ? setSelectedOrder(null) : undefined)}>
        <DialogContent className="max-w-4xl">
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
                    <CardTitle className="text-lg">
                      <StatusBadge
                        label={orderStatusLabels[selectedOrder.status]}
                        tone={getOrderStatusTone(selectedOrder.status)}
                      />
                    </CardTitle>
                  </CardHeader>
                </Card>
              </div>

              <div className="grid gap-4 xl:grid-cols-[1.3fr_0.7fr]">
                <Card>
                  <CardHeader>
                    <CardTitle className="text-base">Ürünler</CardTitle>
                  </CardHeader>
                  <CardContent>
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
                  </CardContent>
                </Card>

                <div className="space-y-4">
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">Teslimat Adresi</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <p className="rounded-xl border bg-muted/30 p-4 text-sm text-muted-foreground">
                        {selectedOrder.shippingAddress}
                      </p>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">Kargo Bilgisi</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2 text-sm">
                      {shippingRecords[selectedOrder.id] ? (
                        <>
                          <p><span className="font-medium">Firma:</span> {shippingRecords[selectedOrder.id].cargoCompany}</p>
                          <p><span className="font-medium">Takip Kodu:</span> {shippingRecords[selectedOrder.id].trackingCode}</p>
                          <p className="text-muted-foreground">
                            Son güncelleme: {new Date(shippingRecords[selectedOrder.id].updatedAt).toLocaleString('tr-TR')}
                          </p>
                        </>
                      ) : (
                        <p className="text-muted-foreground">Henüz takip bilgisi girilmedi.</p>
                      )}
                    </CardContent>
                  </Card>
                </div>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Sipariş Akışı</CardTitle>
                  <CardDescription>Mevcut sipariş durumuna göre operasyon adımları.</CardDescription>
                </CardHeader>
                <CardContent className="grid gap-3 md:grid-cols-5">
                  {buildTimeline(selectedOrder, shippingRecords[selectedOrder.id]).map((step) => (
                    <div
                      key={`${selectedOrder.id}-${step.label}`}
                      className={`rounded-xl border p-4 ${step.active ? 'border-primary/40 bg-primary/5' : 'border-border/70 bg-muted/20'}`}
                    >
                      <p className="font-medium">{step.label}</p>
                      <p className="mt-1 text-sm text-muted-foreground">{step.meta}</p>
                    </div>
                  ))}
                </CardContent>
              </Card>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>

      <Dialog open={!!shippingDialogOrder} onOpenChange={(open) => (!open ? setShippingDialogOrder(null) : undefined)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Siparişi Kargoya Ver</DialogTitle>
            <DialogDescription>
              {shippingDialogOrder ? `#${shippingDialogOrder.orderNumber || shippingDialogOrder.id}` : 'Sipariş'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="cargo-company">Kargo Firması</Label>
              <Input
                id="cargo-company"
                placeholder="Yurtiçi Kargo, MNG, Aras..."
                value={cargoCompany}
                onChange={(event) => setCargoCompany(event.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="tracking-code">Takip Kodu</Label>
              <Input
                id="tracking-code"
                placeholder="ABC123456789"
                value={trackingCode}
                onChange={(event) => setTrackingCode(event.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShippingDialogOrder(null)} disabled={isUpdating}>
              İptal
            </Button>
            <Button onClick={() => void handleShipOrder()} disabled={isUpdating}>
              Kargoya Ver
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
