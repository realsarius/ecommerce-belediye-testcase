import { useEffect, useMemo } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { BadgeCheck, ClipboardList, ShoppingBag } from 'lucide-react';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';

const PENDING_THREE_DS_ORDER_ID_KEY = 'pending_three_ds_order_id';
const RECENT_CHECKOUT_ORDER_ID_KEY = 'recent_checkout_order_id';

export default function CheckoutSuccess() {
  const [searchParams] = useSearchParams();

  const orderId = useMemo(() => {
    const queryOrderId = searchParams.get('orderId');
    const pendingOrderId = window.localStorage.getItem(PENDING_THREE_DS_ORDER_ID_KEY);
    const recentOrderId = window.localStorage.getItem(RECENT_CHECKOUT_ORDER_ID_KEY);

    return queryOrderId ?? pendingOrderId ?? recentOrderId ?? null;
  }, [searchParams]);

  useEffect(() => {
    if (orderId) {
      window.localStorage.setItem(RECENT_CHECKOUT_ORDER_ID_KEY, orderId);
    }

    window.localStorage.removeItem(PENDING_THREE_DS_ORDER_ID_KEY);
  }, [orderId]);

  return (
    <div className="container mx-auto flex min-h-[70vh] max-w-3xl items-center px-4 py-10">
      <Card className="w-full border-emerald-500/20 bg-gradient-to-br from-emerald-500/10 via-background to-background shadow-xl">
        <CardHeader className="space-y-6 text-center">
          <div className="mx-auto flex h-20 w-20 items-center justify-center rounded-full border border-emerald-500/30 bg-emerald-500/15">
            <BadgeCheck className="h-10 w-10 text-emerald-400" />
          </div>
          <div className="space-y-3">
            <CardTitle className="text-3xl font-semibold tracking-tight sm:text-4xl">
              Siparisiniz Alindi
            </CardTitle>
            <CardDescription className="mx-auto max-w-xl text-base leading-7 text-muted-foreground sm:text-lg">
              Odemeniz basariyla alindi. Siparisiniz hazirlaniyor; kargo ve iade surecini siparis detay ekranindan takip edebilirsiniz.
            </CardDescription>
          </div>
        </CardHeader>
        <CardContent className="space-y-8">
          <div className="rounded-2xl border border-border/60 bg-background/70 p-5 text-center">
            <p className="text-sm uppercase tracking-[0.2em] text-muted-foreground">
              Siparis Bilgisi
            </p>
            <p className="mt-2 text-lg font-medium">
              {orderId ? `Siparis #${orderId}` : 'Siparis numarasi hazirlaniyor'}
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <Button asChild size="lg" className="h-12 text-base">
              <Link to={orderId ? `/orders/${orderId}` : '/orders'}>
                <ClipboardList className="mr-2 h-4 w-4" />
                Siparis Detayina Git
              </Link>
            </Button>
            <Button asChild size="lg" variant="outline" className="h-12 text-base">
              <Link to="/">
                <ShoppingBag className="mr-2 h-4 w-4" />
                Alisverise Devam Et
              </Link>
            </Button>
          </div>

          <div className="rounded-2xl border border-dashed border-border/70 bg-background/40 p-4 text-sm text-muted-foreground">
            Size e-posta ve bildirim olarak durum guncellemesi gonderecegiz. Kargoya verildiginde takip numarasini siparis detayinda gorebilirsiniz.
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
