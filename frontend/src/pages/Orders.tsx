import { Link } from 'react-router-dom';
import { useGetOrdersQuery } from '@/features/orders/ordersApi';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { ShoppingBag, Package, ArrowRight } from 'lucide-react';
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

export default function Orders() {
  const { data: orders, isLoading } = useGetOrdersQuery();

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-48 mb-8" />
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-32" />
          ))}
        </div>
      </div>
    );
  }

  if (!orders || orders.length === 0) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <ShoppingBag className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
        <h2 className="text-2xl font-semibold mb-2">Henüz Siparişiniz Yok</h2>
        <p className="text-muted-foreground mb-6">
          İlk siparişinizi verin!
        </p>
        <Button asChild>
          <Link to="/">Alışverişe Başla</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Siparişlerim</h1>

      <div className="space-y-4">
        {orders.map((order) => (
          <Card key={order.id}>
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg">Sipariş #{order.id}</CardTitle>
                <Badge className={statusColors[order.status]}>
                  {statusLabels[order.status]}
                </Badge>
              </div>
              <p className="text-sm text-muted-foreground">
                {new Date(order.createdAt).toLocaleDateString('tr-TR', {
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </p>
            </CardHeader>
            <CardContent>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="flex -space-x-2">
                    {order.items.slice(0, 3).map((_, i) => (
                      <div
                        key={i}
                        className="h-10 w-10 rounded-full bg-muted border-2 border-background flex items-center justify-center"
                      >
                        <Package className="h-5 w-5 text-muted-foreground" />
                      </div>
                    ))}
                    {order.items.length > 3 && (
                      <div className="h-10 w-10 rounded-full bg-primary text-primary-foreground border-2 border-background flex items-center justify-center text-sm font-medium">
                        +{order.items.length - 3}
                      </div>
                    )}
                  </div>
                  <div>
                    <p className="font-medium">
                      {order.items.length} ürün
                    </p>
                    <p className="text-lg font-bold">
                      {order.totalAmount.toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                </div>
                <Button variant="ghost" asChild>
                  <Link to={`/orders/${order.id}`}>
                    Detaylar
                    <ArrowRight className="ml-2 h-4 w-4" />
                  </Link>
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
