import { Link } from 'react-router-dom';
import { useGetOrdersQuery } from '@/features/orders/ordersApi';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { ShoppingBag, Package, ArrowRight, RotateCcw } from 'lucide-react';
import type { OrderStatus } from '@/features/orders/types';
import { getOrderStatusLabel, getOrderStatusTone } from '@/lib/orderStatus';

const getEligibleReturnAction = (orderStatus: OrderStatus) => {
  if (orderStatus === 'Delivered') {
    return 'İade Talebi Oluştur';
  }

  if (orderStatus === 'PendingPayment' || orderStatus === 'Paid' || orderStatus === 'Processing') {
    return 'İptal Talebi Oluştur';
  }

  return null;
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
        {orders.map((order) => {
          const returnActionLabel = getEligibleReturnAction(order.status);

          return (
          <Card key={order.id}>
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg">Sipariş #{order.id}</CardTitle>
                <StatusBadge
                  label={getOrderStatusLabel(order.status)}
                  tone={getOrderStatusTone(order.status)}
                />
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
                <div className="flex items-center gap-2">
                  {returnActionLabel && (
                    <Button variant="outline" asChild>
                      <Link to={`/returns?orderId=${order.id}`}>
                        <RotateCcw className="mr-2 h-4 w-4" />
                        {returnActionLabel}
                      </Link>
                    </Button>
                  )}
                  <Button variant="ghost" asChild>
                    <Link to={`/orders/${order.id}`}>
                      Detaylar
                      <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        )})}
      </div>
    </div>
  );
}
