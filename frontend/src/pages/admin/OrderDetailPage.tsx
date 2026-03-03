import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
  ArrowLeft,
  Circle,
  CreditCard,
  MapPin,
  Package,
  ReceiptText,
  Truck,
  User,
} from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { useGetAdminOrdersQuery, useUpdateOrderStatusMutation } from '@/features/admin/adminApi';
import type { OrderStatus } from '@/features/orders/types';

const statusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoda',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal',
  Refunded: 'İade',
};

const statusBadgeClasses: Record<OrderStatus, string> = {
  PendingPayment: 'bg-amber-500/10 text-amber-700 dark:text-amber-300',
  Paid: 'bg-sky-500/10 text-sky-700 dark:text-sky-300',
  Processing: 'bg-violet-500/10 text-violet-700 dark:text-violet-300',
  Shipped: 'bg-indigo-500/10 text-indigo-700 dark:text-indigo-300',
  Delivered: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300',
  Cancelled: 'bg-rose-500/10 text-rose-700 dark:text-rose-300',
  Refunded: 'bg-orange-500/10 text-orange-700 dark:text-orange-300',
};

const primaryTimeline: OrderStatus[] = ['PendingPayment', 'Paid', 'Processing', 'Shipped', 'Delivered'];

function formatCurrency(value: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('tr-TR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export default function AdminOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const orderId = Number(id);

  const { data: orders = [], isLoading } = useGetAdminOrdersQuery();
  const [updateStatus, { isLoading: isUpdating }] = useUpdateOrderStatusMutation();
  const [selectedStatus, setSelectedStatus] = useState<OrderStatus>('PendingPayment');

  const order = useMemo(() => orders.find((item) => item.id === orderId), [orderId, orders]);

  useEffect(() => {
    if (order) {
      setSelectedStatus(order.status);
    }
  }, [order]);

  const timeline = useMemo(() => {
    if (!order) {
      return [];
    }

    if (order.status === 'Cancelled' || order.status === 'Refunded') {
      return [...primaryTimeline.slice(0, 3), order.status];
    }

    return primaryTimeline;
  }, [order]);

  const currentTimelineIndex = order ? timeline.indexOf(order.status) : -1;

  const handleStatusUpdate = async () => {
    if (!order || selectedStatus === order.status) {
      return;
    }

    try {
      await updateStatus({ id: order.id, status: selectedStatus }).unwrap();
      toast.success('Sipariş durumu güncellendi');
    } catch {
      toast.error('Sipariş durumu güncellenemedi');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-44" />
        <div className="grid gap-6 lg:grid-cols-[1.3fr_0.7fr]">
          <Skeleton className="h-[520px] rounded-xl" />
          <Skeleton className="h-[520px] rounded-xl" />
        </div>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" asChild>
          <Link to="/admin/orders">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Siparişlere Dön
          </Link>
        </Button>
        <Card className="border-border/70">
          <CardContent className="flex flex-col items-center gap-4 p-12 text-center">
            <Package className="h-12 w-12 text-muted-foreground" />
            <div className="space-y-1">
              <h2 className="text-xl font-semibold">Sipariş bulunamadı</h2>
              <p className="text-muted-foreground">
                Bu sipariş kaydı mevcut değil veya erişilebilir sipariş listesinde yer almıyor.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Button variant="ghost" asChild>
        <Link to="/admin/orders">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Siparişlere Dön
        </Link>
      </Button>

      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight">
              Sipariş #{order.orderNumber || order.id}
            </h1>
            <Badge variant="secondary" className={statusBadgeClasses[order.status]}>
              {statusLabels[order.status]}
            </Badge>
          </div>
          <p className="text-muted-foreground">{formatDateTime(order.createdAt)}</p>
        </div>

        <Card className="w-full max-w-md border-border/70">
          <CardHeader>
            <CardTitle>Durum Güncelle</CardTitle>
            <CardDescription>Durum güncellemesi sonrası müşteri bildirim akışı otomatik tetiklenir.</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-3 sm:flex-row">
            <Select value={selectedStatus} onValueChange={(value) => setSelectedStatus(value as OrderStatus)}>
              <SelectTrigger className="flex-1">
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
            <Button onClick={() => void handleStatusUpdate()} disabled={isUpdating || selectedStatus === order.status}>
              Güncelle
            </Button>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.3fr_0.7fr]">
        <div className="space-y-6">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Sipariş Timeline</CardTitle>
              <CardDescription>Oluşturulma anından mevcut duruma kadar operasyon görünümü.</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {timeline.map((step, index) => {
                  const isCompleted = currentTimelineIndex >= index;
                  const isCurrent = order.status === step;

                  return (
                    <div key={step} className="flex gap-4">
                      <div className="flex flex-col items-center">
                        <div
                          className={`flex h-8 w-8 items-center justify-center rounded-full border ${
                            isCompleted
                              ? 'border-primary bg-primary text-primary-foreground'
                              : 'border-border bg-background text-muted-foreground'
                          }`}
                        >
                          <Circle className={`h-3.5 w-3.5 ${isCurrent ? 'fill-current' : ''}`} />
                        </div>
                        {index < timeline.length - 1 ? (
                          <div className={`mt-2 h-8 w-px ${isCompleted ? 'bg-primary/40' : 'bg-border'}`} />
                        ) : null}
                      </div>
                      <div className="space-y-1 pb-2">
                        <p className="font-medium">{statusLabels[step]}</p>
                        <p className="text-sm text-muted-foreground">
                          {isCurrent ? 'Sipariş şu anda bu aşamada.' : isCompleted ? 'Tamamlanan adım.' : 'Henüz ulaşılmadı.'}
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Ürünler</CardTitle>
              <CardDescription>Siparişteki kalemler ve satır bazlı tutarlar.</CardDescription>
            </CardHeader>
            <CardContent>
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
                  {order.items.map((item) => (
                    <TableRow key={`${order.id}-${item.productId}`}>
                      <TableCell>{item.productName}</TableCell>
                      <TableCell>{item.quantity}</TableCell>
                      <TableCell>{formatCurrency(item.priceSnapshot, order.currency || 'TRY')}</TableCell>
                      <TableCell>{formatCurrency(item.lineTotal, order.currency || 'TRY')}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <User className="h-4 w-4 text-primary" />
                Müşteri
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Ad</span>
                <span className="font-medium">{order.customerName || `Kullanıcı #${order.userId}`}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Kullanıcı ID</span>
                <span className="font-medium">{order.userId}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Sipariş Tarihi</span>
                <span className="font-medium">{formatDateTime(order.createdAt)}</span>
              </div>
            </CardContent>
          </Card>

          <Card className="border-border/70">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-4 w-4 text-primary" />
                Teslimat Adresi
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm leading-6 text-muted-foreground">{order.shippingAddress}</p>
            </CardContent>
          </Card>

          <Card className="border-border/70">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <CreditCard className="h-4 w-4 text-primary" />
                Ödeme Özeti
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Ödeme Yöntemi</span>
                <span className="font-medium">{order.payment?.paymentMethod || 'Kart / sistem kaydı'}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Transaction ID</span>
                <span className="font-medium">{order.payment?.transactionId || 'Yok'}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Kupon</span>
                <span className="font-medium">{order.couponCode || 'Yok'}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Gift Card</span>
                <span className="font-medium">{order.giftCardCode || 'Yok'}</span>
              </div>
              <Separator />
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Kupon İndirimi</span>
                <span className="font-medium">{formatCurrency(order.discountAmount ?? 0, order.currency || 'TRY')}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Gift Card Tutarı</span>
                <span className="font-medium">{formatCurrency(order.giftCardAmount ?? 0, order.currency || 'TRY')}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Loyalty Kullanımı</span>
                <span className="font-medium">{order.loyaltyPointsUsed ?? 0} puan</span>
              </div>
              <div className="flex items-center justify-between gap-4 pt-2 text-base">
                <span className="font-semibold">Toplam</span>
                <span className="font-semibold">{formatCurrency(order.totalAmount, order.currency || 'TRY')}</span>
              </div>
            </CardContent>
          </Card>

          <Card className="border-border/70">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Truck className="h-4 w-4 text-primary" />
                Kargo Notu
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm text-muted-foreground">
              <p>Kargo firmasi ve takip kodu için ayrı backend alanı henüz yok.</p>
              <p>Bu ekran sonraki fazda gerçek kargo entegrasyonu ile genişletilecek.</p>
            </CardContent>
          </Card>

          <Card className="border-border/70">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ReceiptText className="h-4 w-4 text-primary" />
                Teknik Not
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm text-muted-foreground">
              <p>Bu detay sayfası mevcut admin sipariş listesi endpoint’inden besleniyor.</p>
              <p>Ayrı `GET /api/admin/orders/:id` endpoint’i geldiğinde veri kaynağını o yöne taşıyacağız.</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
