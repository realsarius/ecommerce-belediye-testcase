import { useParams, Link } from 'react-router-dom';
import { useGetOrderQuery, useCancelOrderMutation } from '@/features/orders/ordersApi';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import { ArrowLeft, Package, MapPin, CreditCard, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import type { OrderStatus } from '@/features/orders/types';

const statusColors: Record<OrderStatus, string> = {
  PendingPayment: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
  Paid: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
  Processing: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
  Shipped: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200',
  Delivered: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
  Cancelled: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
  Refunded: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
};

const statusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoya Verildi',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal Edildi',
  Refunded: 'İade Edildi',
};

export default function OrderDetail() {
  const { id } = useParams<{ id: string }>();
  const orderId = parseInt(id || '0');

  const { data: order, isLoading, error } = useGetOrderQuery(orderId);
  const [cancelOrder, { isLoading: isCancelling }] = useCancelOrderMutation();

  const handleCancel = async () => {
    if (!confirm('Siparişi iptal etmek istediğinize emin misiniz?')) return;
    try {
      await cancelOrder(orderId).unwrap();
      toast.success('Sipariş iptal edildi');
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Sipariş iptal edilemedi');
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-32 mb-8" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-48" />
            <Skeleton className="h-64" />
          </div>
          <Skeleton className="h-48" />
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <Package className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
        <h2 className="text-2xl font-semibold mb-2">Sipariş Bulunamadı</h2>
        <p className="text-muted-foreground mb-6">
          Aradığınız sipariş mevcut değil.
        </p>
        <Button asChild>
          <Link to="/orders">Siparişlere Dön</Link>
        </Button>
      </div>
    );
  }

  const canCancel = ['PendingPayment', 'Paid', 'Processing'].includes(order.status);

  return (
    <div className="container mx-auto px-4 py-8">
      <Button variant="ghost" asChild className="mb-8">
        <Link to="/orders">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Siparişlere Dön
        </Link>
      </Button>

      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold">Sipariş #{order.id}</h1>
          <p className="text-muted-foreground">
            {new Date(order.createdAt).toLocaleDateString('tr-TR', {
              year: 'numeric',
              month: 'long',
              day: 'numeric',
              hour: '2-digit',
              minute: '2-digit',
            })}
          </p>
        </div>
        <Badge className={statusColors[order.status]} variant="secondary">
          {statusLabels[order.status]}
        </Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-6">
          {/* Order Items */}
          <Card>
            <CardHeader>
              <CardTitle>Ürünler</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {order.items.map((item, index) => (
                <div key={index} className="flex items-center gap-4">
                  <div className="h-16 w-16 bg-muted rounded-lg flex items-center justify-center flex-shrink-0">
                    <Package className="h-8 w-8 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-semibold truncate">{item.productName}</p>
                    <p className="text-sm text-muted-foreground">
                      {item.quantity} x {item.priceSnapshot.toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                  <p className="font-bold">
                    {item.lineTotal.toLocaleString('tr-TR')} ₺
                  </p>
                </div>
              ))}
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>Toplam</span>
                <span>{order.totalAmount.toLocaleString('tr-TR')} ₺</span>
              </div>
            </CardContent>
          </Card>

          {/* Cancel Button */}
          {canCancel && (
            <Button
              variant="destructive"
              onClick={handleCancel}
              disabled={isCancelling}
            >
              <XCircle className="mr-2 h-4 w-4" />
              Siparişi İptal Et
            </Button>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Shipping Address */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                <CardTitle className="text-base">Teslimat Adresi</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground text-sm">
                {order.shippingAddress}
              </p>
            </CardContent>
          </Card>

          {/* Payment Info */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                <CardTitle className="text-base">Ödeme Bilgisi</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground text-sm">
                Durum: {order.status === 'Paid' ? 'Ödeme alındı' : statusLabels[order.status]}
              </p>
              {order.payment?.status === 'Failed' && (
                <p className="text-sm text-destructive mt-2">
                  Hata: {order.payment.errorMessage || 'Ödeme başarısız'}
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
