import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { AlertCircle, ArrowRight, ClipboardList, PackageSearch, RotateCcw, ShieldX } from 'lucide-react';
import { toast } from 'sonner';
import { useGetOrdersQuery } from '@/features/orders/ordersApi';
import { useCreateReturnRequestMutation, useGetMyReturnRequestsQuery } from '@/features/returns/returnsApi';
import type { OrderStatus } from '@/features/orders/types';
import type { ReturnRequestStatus, ReturnRequestType } from '@/features/returns/types';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Skeleton } from '@/components/common/skeleton';
import { Textarea } from '@/components/common/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';

const returnStatusLabels: Record<ReturnRequestStatus, string> = {
  Pending: 'İnceleniyor',
  Approved: 'Onaylandı',
  Rejected: 'Reddedildi',
  RefundPending: 'İade İşleniyor',
  Refunded: 'İade Tamamlandı',
};

const returnStatusColors: Record<ReturnRequestStatus, string> = {
  Pending: 'bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-200',
  Approved: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/50 dark:text-emerald-200',
  Rejected: 'bg-rose-100 text-rose-800 dark:bg-rose-900/50 dark:text-rose-200',
  RefundPending: 'bg-sky-100 text-sky-800 dark:bg-sky-900/50 dark:text-sky-200',
  Refunded: 'bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-200',
};

const requestTypeLabels: Record<ReturnRequestType, string> = {
  Return: 'İade',
  Cancellation: 'İptal',
};

const orderStatusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoya Verildi',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal Edildi',
  Refunded: 'İade Edildi',
};

function getEligibleRequestType(orderStatus: OrderStatus): ReturnRequestType | null {
  if (orderStatus === 'Delivered') {
    return 'Return';
  }

  if (orderStatus === 'PendingPayment' || orderStatus === 'Paid' || orderStatus === 'Processing') {
    return 'Cancellation';
  }

  return null;
}

function buildRequestReasonPlaceholder(requestType: ReturnRequestType | ''): string {
  if (requestType === 'Cancellation') {
    return 'İptal talebinizin kısa nedenini yazın';
  }

  if (requestType === 'Return') {
    return 'İade talebinizin kısa nedenini yazın';
  }

  return 'Talep nedeninizi yazın';
}

export default function Returns() {
  const [searchParams, setSearchParams] = useSearchParams();
  const selectedOrderParam = searchParams.get('orderId');

  const { data: orders, isLoading: isOrdersLoading } = useGetOrdersQuery();
  const { data: returnRequests, isLoading: isRequestsLoading } = useGetMyReturnRequestsQuery();
  const [createReturnRequest, { isLoading: isSubmitting }] = useCreateReturnRequestMutation();

  const [reason, setReason] = useState('');
  const [requestNote, setRequestNote] = useState('');

  const eligibleOrders = (orders ?? []).filter((order) => getEligibleRequestType(order.status) !== null);
  const selectedOrderId = selectedOrderParam ?? '';
  const selectedOrder = eligibleOrders.find((order) => order.id.toString() === selectedOrderId);
  const requestType = selectedOrder ? getEligibleRequestType(selectedOrder.status) ?? '' : '';

  const handleOrderChange = (orderId: string) => {
    setSearchParams(orderId ? { orderId } : {});
  };

  const handleSubmit = async () => {
    if (!selectedOrder || !requestType || !reason.trim()) {
      toast.error('Lütfen sipariş, talep tipi ve neden alanlarını doldurun.');
      return;
    }

    try {
      await createReturnRequest({
        orderId: selectedOrder.id,
        type: requestType,
        reason: reason.trim(),
        requestNote: requestNote.trim() || undefined,
      }).unwrap();

      toast.success('Talebiniz oluşturuldu.');
      setReason('');
      setRequestNote('');
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Talep oluşturulamadı.');
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <Badge className="mb-3 bg-orange-100 text-orange-800 dark:bg-orange-900/50 dark:text-orange-200">
            Sipariş Sonrası Süreç
          </Badge>
          <h1 className="text-3xl font-bold tracking-tight">İade ve İptal Taleplerim</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Teslim edilen siparişler için iade, hazırlanmakta olan siparişler için iptal talebi oluşturabilirsiniz.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link to="/orders">
            Siparişlerime Dön
            <ArrowRight className="ml-2 h-4 w-4" />
          </Link>
        </Button>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_1.6fr]">
        <Card className="border-white/10 bg-card/70 backdrop-blur">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-xl">
              <RotateCcw className="h-5 w-5 text-primary" />
              Yeni Talep Oluştur
            </CardTitle>
            <CardDescription>
              Uygun siparişinizi seçin, sistem izin verdiği talep tipini otomatik belirlesin.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            {isOrdersLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-24 w-full" />
              </div>
            ) : eligibleOrders.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-border/70 bg-muted/20 p-6 text-center">
                <ShieldX className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
                <p className="font-medium">Şu anda talep açılabilir siparişiniz yok.</p>
                <p className="mt-2 text-sm text-muted-foreground">
                  İade için teslim edilmiş, iptal için ise henüz sevk edilmemiş bir sipariş gerekir.
                </p>
              </div>
            ) : (
              <>
                <div className="space-y-2">
                  <Label htmlFor="return-order">Sipariş</Label>
                  <Select value={selectedOrderId} onValueChange={handleOrderChange}>
                    <SelectTrigger id="return-order">
                      <SelectValue placeholder="Talep açmak istediğiniz siparişi seçin" />
                    </SelectTrigger>
                    <SelectContent>
                      {eligibleOrders.map((order) => (
                        <SelectItem key={order.id} value={order.id.toString()}>
                          {order.orderNumber ? `${order.orderNumber} · ` : ''}#{order.id} · {orderStatusLabels[order.status]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="return-type">Talep Tipi</Label>
                  <Input
                    id="return-type"
                    value={requestType ? requestTypeLabels[requestType] : ''}
                    placeholder="Sipariş seçildiğinde otomatik belirlenir"
                    disabled
                  />
                </div>

                {selectedOrder && (
                  <div className="rounded-2xl border border-border/70 bg-muted/20 p-4 text-sm">
                    <p className="font-medium">Seçilen Sipariş</p>
                    <p className="mt-1 text-muted-foreground">
                      {selectedOrder.orderNumber ? `${selectedOrder.orderNumber} · ` : ''}#{selectedOrder.id}
                    </p>
                    <p className="mt-2 text-muted-foreground">
                      Durum: <span className="font-medium text-foreground">{orderStatusLabels[selectedOrder.status]}</span>
                    </p>
                    <p className="mt-1 text-muted-foreground">
                      Tutar: <span className="font-medium text-foreground">{selectedOrder.totalAmount.toLocaleString('tr-TR')} {selectedOrder.currency ?? 'TRY'}</span>
                    </p>
                  </div>
                )}

                <div className="space-y-2">
                  <Label htmlFor="return-reason">Talep Nedeni</Label>
                  <Input
                    id="return-reason"
                    value={reason}
                    onChange={(event) => setReason(event.target.value)}
                    placeholder={buildRequestReasonPlaceholder(requestType)}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="return-note">Ek Not</Label>
                  <Textarea
                    id="return-note"
                    value={requestNote}
                    onChange={(event) => setRequestNote(event.target.value)}
                    placeholder="İsterseniz satıcıya veya destek ekibine ek bilgi bırakın"
                    rows={4}
                  />
                </div>

                <Button onClick={handleSubmit} disabled={isSubmitting || !selectedOrder || !requestType} className="w-full">
                  {isSubmitting ? 'Gönderiliyor...' : 'Talep Oluştur'}
                </Button>
              </>
            )}
          </CardContent>
        </Card>

        <Card className="border-white/10 bg-card/70 backdrop-blur">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-xl">
              <ClipboardList className="h-5 w-5 text-primary" />
              Mevcut Taleplerim
            </CardTitle>
            <CardDescription>
              Satıcı veya yönetici incelemesindeki tüm iade ve iptal taleplerinizi buradan takip edebilirsiniz.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {isRequestsLoading ? (
              <div className="space-y-4">
                <Skeleton className="h-28 w-full" />
                <Skeleton className="h-28 w-full" />
              </div>
            ) : !returnRequests || returnRequests.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-border/70 bg-muted/20 p-8 text-center">
                <PackageSearch className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
                <p className="font-medium">Henüz oluşturulmuş talebiniz yok.</p>
                <p className="mt-2 text-sm text-muted-foreground">
                  Uygun siparişleriniz için bu sayfadan iade veya iptal sürecini başlatabilirsiniz.
                </p>
              </div>
            ) : (
              <div className="space-y-4">
                {returnRequests.map((request) => (
                  <div
                    key={request.id}
                    className="rounded-2xl border border-border/70 bg-muted/10 p-5 transition-colors hover:bg-muted/20"
                  >
                    <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                      <div>
                        <div className="flex flex-wrap items-center gap-2">
                          <p className="text-base font-semibold">
                            {request.orderNumber ? request.orderNumber : `Sipariş #${request.orderId}`}
                          </p>
                          <Badge className={returnStatusColors[request.status]}>
                            {returnStatusLabels[request.status]}
                          </Badge>
                          <Badge variant="outline">
                            {requestTypeLabels[request.type]}
                          </Badge>
                        </div>
                        <p className="mt-2 text-sm text-muted-foreground">
                          {new Date(request.createdAt).toLocaleDateString('tr-TR', {
                            year: 'numeric',
                            month: 'long',
                            day: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit',
                          })}
                        </p>
                      </div>
                      <div className="text-sm text-muted-foreground md:text-right">
                        <p className="font-medium text-foreground">
                          {request.requestedRefundAmount.toLocaleString('tr-TR')} TRY
                        </p>
                        {request.refundStatus && (
                          <p className="mt-1">Refund: {request.refundStatus}</p>
                        )}
                      </div>
                    </div>

                    <div className="mt-4 grid gap-4 md:grid-cols-2">
                      <div>
                        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Neden</p>
                        <p className="mt-1 text-sm">{request.reason}</p>
                      </div>
                      <div>
                        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">İnceleme</p>
                        {request.reviewNote ? (
                          <p className="mt-1 text-sm">{request.reviewNote}</p>
                        ) : (
                          <p className="mt-1 text-sm text-muted-foreground">Henüz değerlendirme notu yok.</p>
                        )}
                      </div>
                    </div>

                    {request.requestNote && (
                      <div className="mt-4 rounded-xl border border-border/60 bg-background/60 p-4">
                        <p className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                          <AlertCircle className="h-3.5 w-3.5" />
                          Ek Not
                        </p>
                        <p className="mt-2 text-sm">{request.requestNote}</p>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
