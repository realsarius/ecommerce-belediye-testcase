import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import { BadgeCheck, Clock3, RotateCcw, ShieldX } from 'lucide-react';
import { Badge } from '@/components/common/badge';
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
import { Label } from '@/components/common/label';
import { Skeleton } from '@/components/common/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/common/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import { Textarea } from '@/components/common/textarea';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { TableLoadingState } from '@/components/admin/TableLoadingState';
import { useLazyGetReturnAttachmentAccessUrlQuery } from '@/features/returns/returnsApi';
import { useGetFrontendFeaturesQuery } from '@/features/settings/settingsApi';
import type { ReturnReasonCategory, ReturnRequest, ReturnRequestStatus, ReturnRequestType } from '@/features/returns/types';
import {
  useApproveSellerReturnMutation,
  useGetSellerReturnsQuery,
  useRejectSellerReturnMutation,
} from '@/features/seller/sellerApi';
import { formatCurrency, formatDateTime, formatNumber } from '@/lib/format';

const returnStatusLabels: Record<ReturnRequestStatus, string> = {
  Pending: 'İnceleniyor',
  Approved: 'Onaylandı',
  Rejected: 'Reddedildi',
  RefundPending: 'İade İşleniyor',
  Refunded: 'İade Tamamlandı',
};

const returnTypeLabels: Record<ReturnRequestType, string> = {
  Return: 'İade',
  Cancellation: 'İptal',
};

const reasonCategoryLabels: Record<ReturnReasonCategory, string> = {
  WrongProduct: 'Yanlış ürün',
  DefectiveDamaged: 'Hasarlı / arızalı',
  NotAsDescribed: 'Açıklamaya uymuyor',
  ChangedMind: 'Fikrini değiştirdi',
  LateDelivery: 'Geç teslimat',
  Other: 'Diğer',
};

const paymentProviderLabels = {
  Iyzico: 'Iyzico',
  Stripe: 'Stripe',
  PayTR: 'PayTR',
} as const;

function getReturnStatusTone(status: ReturnRequestStatus) {
  switch (status) {
    case 'Approved':
      return 'success' as const;
    case 'Rejected':
      return 'danger' as const;
    case 'Pending':
      return 'warning' as const;
    case 'RefundPending':
      return 'info' as const;
    default:
      return 'neutral' as const;
  }
}

type ReturnsTab = 'Pending' | 'Processed';

export default function SellerReturnsPage() {
  const [activeTab, setActiveTab] = useState<ReturnsTab>('Pending');
  const [selectedRequest, setSelectedRequest] = useState<ReturnRequest | null>(null);
  const [reviewNote, setReviewNote] = useState('');
  const { data: requests = [], isLoading } = useGetSellerReturnsQuery();
  const { data: frontendFeatures } = useGetFrontendFeaturesQuery();
  const [approveReturn, { isLoading: isApproving }] = useApproveSellerReturnMutation();
  const [rejectReturn, { isLoading: isRejecting }] = useRejectSellerReturnMutation();
  const [getAttachmentAccessUrl] = useLazyGetReturnAttachmentAccessUrlQuery();
  const effectiveFrontendFeatures = frontendFeatures ?? {
    enableCheckoutLegalConsents: true,
    enableCheckoutInvoiceInfo: true,
    enableShipmentTimeline: true,
    enableReturnAttachments: true,
  };
  const isReviewing = isApproving || isRejecting;

  const summary = useMemo(() => ({
    pendingCount: requests.filter((request) => request.status === 'Pending').length,
    returnCount: requests.filter((request) => request.type === 'Return').length,
    cancellationCount: requests.filter((request) => request.type === 'Cancellation').length,
    pendingRefundAmount: requests
      .filter((request) => request.status === 'Pending')
      .reduce((sum, request) => sum + request.requestedRefundAmount, 0),
  }), [requests]);

  const tabData = useMemo(() => {
    if (activeTab === 'Pending') {
      return requests.filter((request) => request.status === 'Pending');
    }

    return requests.filter((request) => request.status !== 'Pending');
  }, [activeTab, requests]);

  const handleOpenRequest = (request: ReturnRequest) => {
    setSelectedRequest(request);
    setReviewNote(request.reviewNote ?? '');
  };

  const handleReview = async (status: 'Approved' | 'Rejected') => {
    if (!selectedRequest) {
      return;
    }

    if (status === 'Rejected' && !reviewNote.trim()) {
      toast.error('Red işlemi için inceleme notu girin.');
      return;
    }

    try {
      if (status === 'Approved') {
        await approveReturn({ id: selectedRequest.id, reviewNote: reviewNote.trim() || undefined }).unwrap();
      } else {
        await rejectReturn({ id: selectedRequest.id, reviewNote: reviewNote.trim() || undefined }).unwrap();
      }

      toast.success(status === 'Approved' ? 'Talep onaylandı.' : 'Talep reddedildi.');
      setSelectedRequest(null);
      setReviewNote('');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Talep değerlendirilemedi.');
    }
  };

  const openAttachment = async (returnRequestId: number, attachmentId: number) => {
    try {
      const result = await getAttachmentAccessUrl({ returnRequestId, attachmentId }).unwrap();
      window.open(result.url, '_blank', 'noopener,noreferrer');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Görsel açılamadı.');
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
        <TableLoadingState rowCount={6} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">İade Talepleri</h1>
        <p className="max-w-3xl text-muted-foreground">
          Mağazanıza ait iade ve iptal taleplerini inceleyin, not ekleyerek onaylayın veya reddedin.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard title="Bekleyen Talep" value={formatNumber(summary.pendingCount)} helperText="İnceleme bekleyen talepler." icon={Clock3} accentClass="text-amber-600 dark:text-amber-300" surfaceClass="bg-amber-500/10" />
        <KpiCard title="İade Talebi" value={formatNumber(summary.returnCount)} helperText="Teslim edilen siparişlerden gelen talepler." icon={RotateCcw} accentClass="text-sky-600 dark:text-sky-300" surfaceClass="bg-sky-500/10" />
        <KpiCard title="İptal Talebi" value={formatNumber(summary.cancellationCount)} helperText="Hazırlık aşamasındaki siparişler." icon={ShieldX} accentClass="text-rose-600 dark:text-rose-300" surfaceClass="bg-rose-500/10" />
        <KpiCard title="Bekleyen Tutar" value={formatCurrency(summary.pendingRefundAmount)} helperText="İnceleme kuyruğundaki toplam iade tutarı." icon={BadgeCheck} accentClass="text-emerald-600 dark:text-emerald-300" surfaceClass="bg-emerald-500/10" />
      </div>

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as ReturnsTab)} className="space-y-4">
        <TabsList>
          <TabsTrigger value="Pending">Bekleyen</TabsTrigger>
          <TabsTrigger value="Processed">Sonuçlanan</TabsTrigger>
        </TabsList>

        <TabsContent value={activeTab} className="space-y-4">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>{activeTab === 'Pending' ? 'Bekleyen Talepler' : 'Sonuçlanan Talepler'}</CardTitle>
              <CardDescription>
                {activeTab === 'Pending'
                  ? 'Talep detayını açarak onay veya red kararı verebilirsiniz.'
                  : 'Daha önce değerlendirdiğiniz talepler burada listelenir.'}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Sipariş</TableHead>
                    <TableHead>Müşteri</TableHead>
                    <TableHead>Tip</TableHead>
                    <TableHead>Durum</TableHead>
                    <TableHead>Kategori</TableHead>
                    <TableHead>Tutar</TableHead>
                    <TableHead className="text-right">İşlem</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tabData.map((request) => (
                    <TableRow key={request.id}>
                      <TableCell className="font-medium">{request.orderNumber || `Sipariş #${request.orderId}`}</TableCell>
                      <TableCell>{request.customerName || `Kullanıcı #${request.userId}`}</TableCell>
                      <TableCell><Badge variant="outline">{returnTypeLabels[request.type]}</Badge></TableCell>
                      <TableCell><StatusBadge label={returnStatusLabels[request.status]} tone={getReturnStatusTone(request.status)} /></TableCell>
                      <TableCell>{reasonCategoryLabels[request.reasonCategory]}</TableCell>
                      <TableCell>{formatCurrency(request.requestedRefundAmount)}</TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="sm" onClick={() => handleOpenRequest(request)}>Detay</Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {tabData.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} className="p-0">
                        <EmptyState
                          icon={RotateCcw}
                          title="Talep bulunmuyor"
                          description="Duruma uyan iade veya iptal talepleri burada listelenecek."
                          className="border-0 shadow-none"
                        />
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <Dialog open={!!selectedRequest} onOpenChange={(open) => (!open ? setSelectedRequest(null) : undefined)}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Talep Detayı</DialogTitle>
            <DialogDescription>
              {selectedRequest
                ? `${selectedRequest.orderNumber || `Sipariş #${selectedRequest.orderId}`} için ${returnTypeLabels[selectedRequest.type]} talebi`
                : 'Talep detayları'}
            </DialogDescription>
          </DialogHeader>

          {selectedRequest ? (
            <div className="space-y-6">
              <div className="grid gap-4 md:grid-cols-3">
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Durum</CardDescription>
                    <CardTitle className="text-lg">
                      <StatusBadge label={returnStatusLabels[selectedRequest.status]} tone={getReturnStatusTone(selectedRequest.status)} />
                    </CardTitle>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>Müşteri</CardDescription>
                    <CardTitle className="text-lg">{selectedRequest.customerName}</CardTitle>
                  </CardHeader>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardDescription>İade Tutarı</CardDescription>
                    <CardTitle className="text-lg">{formatCurrency(selectedRequest.requestedRefundAmount)}</CardTitle>
                  </CardHeader>
                </Card>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label>Kategori</Label>
                  <Input value={reasonCategoryLabels[selectedRequest.reasonCategory]} readOnly />
                </div>
                <div className="space-y-2">
                  <Label>Talep Nedeni</Label>
                  <Input value={selectedRequest.reason} readOnly />
                </div>
                <div className="space-y-2">
                  <Label>Talep Tarihi</Label>
                  <Input value={formatDateTime(selectedRequest.createdAt)} readOnly />
                </div>
                <div className="space-y-2">
                  <Label>Refund Sağlayıcısı</Label>
                  <Input
                    value={selectedRequest.refundProvider ? paymentProviderLabels[selectedRequest.refundProvider] : 'Henüz oluşmadı'}
                    readOnly
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label>Müşteri Notu</Label>
                <Textarea value={selectedRequest.requestNote || 'Ek not bırakılmamış'} readOnly rows={4} />
              </div>

              {selectedRequest.selectedItems.length > 0 ? (
                <div className="space-y-2">
                  <Label>Seçilen Ürünler</Label>
                  <div className="rounded-xl border border-border/70 bg-muted/20 p-4">
                    <div className="flex flex-wrap gap-2">
                      {selectedRequest.selectedItems.map((item) => (
                        <Badge key={item.orderItemId} variant="outline">
                          {item.productName} x {item.quantity}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </div>
              ) : null}

              {effectiveFrontendFeatures.enableReturnAttachments && selectedRequest.attachments.length > 0 ? (
                <div className="space-y-2">
                  <Label>Eklenen Fotoğraflar</Label>
                  <div className="rounded-xl border border-border/70 bg-muted/20 p-4">
                    <div className="flex flex-wrap gap-2">
                      {selectedRequest.attachments.map((attachment) => (
                        <button
                          key={attachment.id}
                          type="button"
                          className="inline-flex items-center rounded-full border border-border/70 bg-background/80 px-3 py-1 text-xs transition hover:border-primary hover:text-primary"
                          onClick={() => void openAttachment(selectedRequest.id, attachment.id)}
                        >
                          {attachment.fileName}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              ) : null}

              <div className="space-y-2">
                <Label htmlFor="seller-review-note">İnceleme Notu</Label>
                <Textarea
                  id="seller-review-note"
                  value={reviewNote}
                  onChange={(event) => setReviewNote(event.target.value)}
                  placeholder="Red gerekçesi veya karar notu girin"
                  rows={4}
                />
              </div>
            </div>
          ) : null}

          <DialogFooter>
            <Button variant="outline" onClick={() => setSelectedRequest(null)} disabled={isReviewing}>Kapat</Button>
            {selectedRequest?.status === 'Pending' ? (
              <>
                <Button variant="destructive" onClick={() => void handleReview('Rejected')} disabled={isReviewing}>Reddet</Button>
                <Button onClick={() => void handleReview('Approved')} disabled={isReviewing}>Onayla</Button>
              </>
            ) : null}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
