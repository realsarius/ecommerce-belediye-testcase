import { useState, type ChangeEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { AlertCircle, ArrowRight, ClipboardList, ImagePlus, PackageSearch, RotateCcw, ShieldX, X } from 'lucide-react';
import { toast } from 'sonner';
import { useGetOrdersQuery } from '@/features/orders/ordersApi';
import { useCreateReturnRequestMutation, useGetMyReturnRequestsQuery, useLazyGetReturnAttachmentAccessUrlQuery, useUploadReturnPhotosMutation } from '@/features/returns/returnsApi';
import type { OrderStatus } from '@/features/orders/types';
import type { ReturnReasonCategory, ReturnRequestStatus, ReturnRequestType, UploadedReturnPhoto } from '@/features/returns/types';
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
import { getOrderStatusLabel } from '@/lib/orderStatus';

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

const reasonCategoryLabels: Record<ReturnReasonCategory, string> = {
  WrongProduct: 'Yanlış ürün',
  DefectiveDamaged: 'Hasarlı / arızalı',
  NotAsDescribed: 'Açıklamaya uymuyor',
  ChangedMind: 'Fikrimi değiştirdim',
  LateDelivery: 'Geç teslimat',
  Other: 'Diğer',
};

const cancellationCategories: ReturnReasonCategory[] = ['ChangedMind', 'WrongProduct', 'Other'];
const returnCategories: ReturnReasonCategory[] = ['WrongProduct', 'DefectiveDamaged', 'NotAsDescribed', 'ChangedMind', 'LateDelivery', 'Other'];

function getReturnDaysRemaining(deliveredAt?: string) {
  if (!deliveredAt) {
    return null;
  }

  const deliveredDate = new Date(deliveredAt);
  const diffInMs = deliveredDate.getTime() + (14 * 24 * 60 * 60 * 1000) - Date.now();
  return Math.max(0, Math.ceil(diffInMs / (24 * 60 * 60 * 1000)));
}

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
  const [uploadReturnPhotos, { isLoading: isUploadingPhotos }] = useUploadReturnPhotosMutation();
  const [getAttachmentAccessUrl] = useLazyGetReturnAttachmentAccessUrlQuery();

  const [reasonCategory, setReasonCategory] = useState<ReturnReasonCategory | ''>('');
  const [selectedOrderItemIds, setSelectedOrderItemIds] = useState<number[]>([]);
  const [reason, setReason] = useState('');
  const [requestNote, setRequestNote] = useState('');
  const [uploadedPhotos, setUploadedPhotos] = useState<UploadedReturnPhoto[]>([]);

  const eligibleOrders = (orders ?? []).filter((order) => {
    const requestType = getEligibleRequestType(order.status);
    if (requestType !== 'Return') {
      return requestType !== null;
    }

    const daysRemaining = getReturnDaysRemaining(order.deliveredAt);
    return daysRemaining === null || daysRemaining > 0;
  });
  const selectedOrderId = selectedOrderParam ?? '';
  const selectedOrder = eligibleOrders.find((order) => order.id.toString() === selectedOrderId);
  const requestType = selectedOrder ? getEligibleRequestType(selectedOrder.status) ?? '' : '';
  const availableCategories = requestType === 'Cancellation' ? cancellationCategories : requestType === 'Return' ? returnCategories : [];
  const daysRemaining = selectedOrder?.status === 'Delivered' ? getReturnDaysRemaining(selectedOrder.deliveredAt) : null;

  const handleOrderChange = (orderId: string) => {
    setSearchParams(orderId ? { orderId } : {});
    setReasonCategory('');
    setSelectedOrderItemIds([]);
    setUploadedPhotos([]);
  };

  const toggleSelectedOrderItem = (orderItemId: number, checked: boolean) => {
    setSelectedOrderItemIds((current) =>
      checked ? [...new Set([...current, orderItemId])] : current.filter((id) => id !== orderItemId)
    );
  };

  const handlePhotoUpload = async (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []);
    if (files.length === 0) {
      return;
    }

    const remainingSlots = Math.max(0, 5 - uploadedPhotos.length);
    if (remainingSlots === 0) {
      toast.error('En fazla 5 fotoğraf ekleyebilirsiniz.');
      event.target.value = '';
      return;
    }

    const filesToUpload = files.slice(0, remainingSlots);

    try {
      const uploaded = await uploadReturnPhotos(filesToUpload).unwrap();
      setUploadedPhotos((current) => [...current, ...uploaded]);
      toast.success(`${uploaded.length} fotoğraf yüklendi.`);
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Fotoğraflar yüklenemedi.');
    } finally {
      event.target.value = '';
    }
  };

  const removeUploadedPhoto = (uploadKey: string) => {
    setUploadedPhotos((current) => current.filter((photo) => photo.uploadKey !== uploadKey));
  };

  const openAttachment = async (returnRequestId: number, attachmentId: number) => {
    try {
      const result = await getAttachmentAccessUrl({ returnRequestId, attachmentId }).unwrap();
      window.open(result.url, '_blank', 'noopener,noreferrer');
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Görsel açılamadı.');
    }
  };

  const handleSubmit = async () => {
    if (!selectedOrder || !requestType || !reasonCategory || !reason.trim()) {
      toast.error('Lütfen sipariş, kategori ve neden alanlarını doldurun.');
      return;
    }

    if (requestType === 'Return' && selectedOrder.items.length > 1 && selectedOrderItemIds.length === 0) {
      toast.error('İade edilecek en az bir ürünü seçin.');
      return;
    }

    const payloadSelectedItemIds =
      requestType === 'Return'
        ? selectedOrder.items.length === 1
          ? [selectedOrder.items[0].id]
          : selectedOrderItemIds
        : undefined;

    try {
      await createReturnRequest({
        orderId: selectedOrder.id,
        type: requestType,
        reasonCategory,
        selectedOrderItemIds: payloadSelectedItemIds,
        uploadedPhotoKeys: uploadedPhotos.map((photo) => photo.uploadKey),
        reason: reason.trim(),
        requestNote: requestNote.trim() || undefined,
      }).unwrap();

      toast.success('Talebiniz oluşturuldu.');
      setReasonCategory('');
      setSelectedOrderItemIds([]);
      setReason('');
      setRequestNote('');
      setUploadedPhotos([]);
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
                          {order.orderNumber ? `${order.orderNumber} · ` : ''}#{order.id} · {getOrderStatusLabel(order.status)}
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
                      Durum: <span className="font-medium text-foreground">{getOrderStatusLabel(selectedOrder.status)}</span>
                    </p>
                    <p className="mt-1 text-muted-foreground">
                      Tutar: <span className="font-medium text-foreground">{selectedOrder.totalAmount.toLocaleString('tr-TR')} {selectedOrder.currency ?? 'TRY'}</span>
                    </p>
                  </div>
                )}

                {selectedOrder && requestType === 'Return' && selectedOrder.items.length > 1 ? (
                  <div className="space-y-3 rounded-2xl border border-border/70 bg-muted/20 p-4">
                    <div>
                      <p className="font-medium">İade Edilecek Ürünler</p>
                      <p className="text-sm text-muted-foreground">
                        Birden fazla ürün içeren siparişlerde iade edilecek kalemleri seçin.
                      </p>
                    </div>
                    <div className="space-y-2">
                      {selectedOrder.items.map((item) => {
                        const checked = selectedOrderItemIds.includes(item.id);

                        return (
                          <label
                            key={item.id}
                            className="flex items-start gap-3 rounded-xl border border-border/70 bg-background/70 p-3"
                          >
                            <input
                              type="checkbox"
                              className="mt-1 h-4 w-4 rounded border-border"
                              checked={checked}
                              onChange={(event) => toggleSelectedOrderItem(item.id, event.target.checked)}
                            />
                            <div className="min-w-0">
                              <p className="font-medium">{item.productName}</p>
                              <p className="text-sm text-muted-foreground">
                                {item.quantity} x {item.priceSnapshot.toLocaleString('tr-TR')} {selectedOrder.currency ?? 'TRY'}
                              </p>
                            </div>
                          </label>
                        );
                      })}
                    </div>
                  </div>
                ) : null}

                <div className="space-y-2">
                  <Label htmlFor="return-category">Kategori</Label>
                  <Select
                    value={reasonCategory}
                    onValueChange={(value) => setReasonCategory(value as ReturnReasonCategory)}
                  >
                    <SelectTrigger id="return-category">
                      <SelectValue placeholder="Talep kategorisini seçin" />
                    </SelectTrigger>
                    <SelectContent>
                      {availableCategories.map((category) => (
                        <SelectItem key={category} value={category}>
                          {reasonCategoryLabels[category]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

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

                {requestType === 'Return' ? (
                  <div className="space-y-3 rounded-2xl border border-border/70 bg-muted/20 p-4">
                    <div className="space-y-1">
                      <Label htmlFor="return-photos">Ürün Fotoğrafları</Label>
                      <p className="text-sm text-muted-foreground">
                        Opsiyonel olarak en fazla 5 görsel yükleyebilirsiniz. Desteklenen tipler: JPG, PNG, WEBP, HEIC.
                      </p>
                    </div>
                    <Input
                      id="return-photos"
                      type="file"
                      accept="image/jpeg,image/png,image/webp,image/heic"
                      multiple
                      onChange={(event) => void handlePhotoUpload(event)}
                      disabled={isUploadingPhotos || uploadedPhotos.length >= 5}
                    />
                    {isUploadingPhotos ? (
                      <p className="text-sm text-muted-foreground">Fotoğraflar yükleniyor...</p>
                    ) : null}
                    {uploadedPhotos.length > 0 ? (
                      <div className="flex flex-wrap gap-2">
                        {uploadedPhotos.map((photo) => (
                          <div
                            key={photo.uploadKey}
                            className="flex items-center gap-2 rounded-full border border-border/70 bg-background/80 px-3 py-2 text-xs"
                          >
                            <ImagePlus className="h-3.5 w-3.5 text-primary" />
                            <span className="max-w-[13rem] truncate">{photo.fileName}</span>
                            <button
                              type="button"
                              className="text-muted-foreground transition hover:text-foreground"
                              onClick={() => removeUploadedPhoto(photo.uploadKey)}
                              aria-label={`${photo.fileName} yüklemesini kaldır`}
                            >
                              <X className="h-3.5 w-3.5" />
                            </button>
                          </div>
                        ))}
                      </div>
                    ) : null}
                  </div>
                ) : null}

                {requestType === 'Return' && daysRemaining !== null ? (
                  <div className="rounded-2xl border border-amber-400/30 bg-amber-500/10 p-4 text-sm text-amber-700 dark:text-amber-300">
                    Yasal cayma hakkı süresi: <strong>{daysRemaining} gün</strong>
                  </div>
                ) : null}

                <Button onClick={handleSubmit} disabled={isSubmitting || !selectedOrder || !requestType || !reasonCategory} className="w-full">
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

                    {request.selectedItems.length > 0 && (
                      <div className="mt-4">
                        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Seçilen Ürünler</p>
                        <div className="mt-2 flex flex-wrap gap-2">
                          {request.selectedItems.map((item) => (
                            <Badge key={item.orderItemId} variant="secondary">
                              {item.productName} x {item.quantity}
                            </Badge>
                          ))}
                        </div>
                      </div>
                    )}

                    {request.attachments.length > 0 && (
                      <div className="mt-4">
                        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Eklenen Fotoğraflar</p>
                        <div className="mt-2 flex flex-wrap gap-2">
                          {request.attachments.map((attachment) => (
                            <button
                              key={attachment.id}
                              type="button"
                              className="inline-flex items-center rounded-full border border-border/70 bg-background/80 px-3 py-1 text-xs transition hover:border-primary hover:text-primary"
                              onClick={() => void openAttachment(request.id, attachment.id)}
                            >
                              {attachment.fileName}
                            </button>
                          ))}
                        </div>
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
