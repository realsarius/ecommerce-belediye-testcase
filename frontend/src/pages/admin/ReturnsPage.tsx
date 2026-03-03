import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import {
  BadgeCheck,
  Clock3,
  RotateCcw,
  ShieldX,
} from 'lucide-react';
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import { Textarea } from '@/components/common/textarea';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { TableLoadingState } from '@/components/admin/TableLoadingState';
import {
  useApproveAdminReturnMutation,
  useGetAdminReturnsQuery,
  useRejectAdminReturnMutation,
} from '@/features/admin/adminApi';
import type { ReturnReasonCategory, ReturnRequest, ReturnRequestStatus, ReturnRequestType } from '@/features/returns/types';
import { formatCurrency, formatDate, formatDateTime, formatNumber } from '@/lib/format';

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

type ReturnsTab = 'Pending' | 'Approved' | 'Rejected';

export default function ReturnsPage() {
  const [activeTab, setActiveTab] = useState<ReturnsTab>('Pending');
  const [selectedRequest, setSelectedRequest] = useState<ReturnRequest | null>(null);
  const [reviewNote, setReviewNote] = useState('');
  const { data: requests = [], isLoading } = useGetAdminReturnsQuery();
  const [approveReturn, { isLoading: isApproving }] = useApproveAdminReturnMutation();
  const [rejectReturn, { isLoading: isRejecting }] = useRejectAdminReturnMutation();
  const isReviewing = isApproving || isRejecting;

  const summary = useMemo(() => {
    return {
      pendingCount: requests.filter((request) => request.status === 'Pending').length,
      cancellationCount: requests.filter((request) => request.type === 'Cancellation').length,
      returnCount: requests.filter((request) => request.type === 'Return').length,
      pendingRefundAmount: requests
        .filter((request) => request.status === 'Pending')
        .reduce((sum, request) => sum + request.requestedRefundAmount, 0),
    };
  }, [requests]);

  const tabData = useMemo(() => {
    if (activeTab === 'Pending') {
      return requests.filter((request) => request.status === 'Pending');
    }

    if (activeTab === 'Approved') {
      return requests.filter((request) =>
        request.status === 'Approved' || request.status === 'RefundPending' || request.status === 'Refunded');
    }

    return requests.filter((request) => request.status === 'Rejected');
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
        await approveReturn({
          id: selectedRequest.id,
          reviewNote: reviewNote.trim() || undefined,
        }).unwrap();
      } else {
        await rejectReturn({
          id: selectedRequest.id,
          reviewNote: reviewNote.trim() || undefined,
        }).unwrap();
      }

      toast.success(status === 'Approved' ? 'Talep onaylandı.' : 'Talep reddedildi.');
      setSelectedRequest(null);
      setReviewNote('');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Talep değerlendirilemedi.');
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
        <TableLoadingState rowCount={7} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">İade Talepleri</h1>
        <p className="max-w-3xl text-muted-foreground">
          Bekleyen iade ve iptal taleplerini inceleyin, onaylayın veya gerekçeyle reddedin.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Bekleyen Talep"
          value={formatNumber(summary.pendingCount)}
          helperText="İnceleme bekleyen toplam talep."
          icon={Clock3}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="İptal Talebi"
          value={formatNumber(summary.cancellationCount)}
          helperText="Ödeme veya hazırlık aşamasındaki siparişler."
          icon={ShieldX}
          accentClass="text-rose-600 dark:text-rose-300"
          surfaceClass="bg-rose-500/10"
        />
        <KpiCard
          title="İade Talebi"
          value={formatNumber(summary.returnCount)}
          helperText="Teslim edilmiş siparişler için açılan talepler."
          icon={RotateCcw}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Bekleyen Tutar"
          value={formatCurrency(summary.pendingRefundAmount)}
          helperText="İnceleme kuyruğundaki toplam iade tutarı."
          icon={BadgeCheck}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
      </div>

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as ReturnsTab)} className="space-y-4">
        <TabsList>
          <TabsTrigger value="Pending">Bekleyen</TabsTrigger>
          <TabsTrigger value="Approved">Onaylanan</TabsTrigger>
          <TabsTrigger value="Rejected">Reddedilen</TabsTrigger>
        </TabsList>

        <TabsContent value="Pending" className="space-y-4">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Bekleyen Talepler</CardTitle>
              <CardDescription>
                Her satırdan talebi açıp iade onayı veya red kararı verebilirsiniz.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Sipariş</TableHead>
                    <TableHead>Müşteri</TableHead>
                    <TableHead>Tip</TableHead>
                    <TableHead>Kategori</TableHead>
                    <TableHead>Neden</TableHead>
                    <TableHead>Tutar</TableHead>
                    <TableHead>Tarih</TableHead>
                    <TableHead className="text-right">İşlem</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tabData.map((request) => (
                    <TableRow key={request.id}>
                      <TableCell className="font-medium">
                        {request.orderNumber || `Sipariş #${request.orderId}`}
                      </TableCell>
                      <TableCell>{request.customerName || `Kullanıcı #${request.userId}`}</TableCell>
                      <TableCell>
                        <Badge variant="outline">{returnTypeLabels[request.type]}</Badge>
                      </TableCell>
                      <TableCell>{reasonCategoryLabels[request.reasonCategory]}</TableCell>
                      <TableCell className="max-w-[22rem] truncate">{request.reason}</TableCell>
                      <TableCell>{formatCurrency(request.requestedRefundAmount)}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {formatDate(request.createdAt)}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="sm" onClick={() => handleOpenRequest(request)}>
                          İncele
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {tabData.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={8} className="p-0">
                        <EmptyState
                          icon={RotateCcw}
                          title="Bekleyen iade talebi bulunmuyor"
                          description="Yeni iade veya iptal talepleri geldiğinde inceleme kuyruğu burada listelenecek."
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

        <TabsContent value="Approved">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Onaylanan Talepler</CardTitle>
              <CardDescription>
                Onaylanan, iade sürecine alınan ve tamamlanan talepler burada görünür.
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
                    <TableHead>Tutar</TableHead>
                    <TableHead>İnceleyen</TableHead>
                    <TableHead className="text-right">İşlem</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tabData.map((request) => (
                    <TableRow key={request.id}>
                      <TableCell className="font-medium">
                        {request.orderNumber || `Sipariş #${request.orderId}`}
                      </TableCell>
                      <TableCell>{request.customerName || `Kullanıcı #${request.userId}`}</TableCell>
                      <TableCell>
                        <Badge variant="outline">{returnTypeLabels[request.type]}</Badge>
                      </TableCell>
                      <TableCell>
                        <StatusBadge
                          label={returnStatusLabels[request.status]}
                          tone={getReturnStatusTone(request.status)}
                        />
                      </TableCell>
                      <TableCell>{formatCurrency(request.requestedRefundAmount)}</TableCell>
                      <TableCell>{request.reviewerName || 'Sistem'}</TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="sm" onClick={() => handleOpenRequest(request)}>
                          Detay
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {tabData.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} className="p-0">
                        <EmptyState
                          icon={BadgeCheck}
                          title="Onaylanan talep yok"
                          description="İşleme alınan veya tamamlanan talepler burada listelenecek."
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

        <TabsContent value="Rejected">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Reddedilen Talepler</CardTitle>
              <CardDescription>
                Red nedeni girilen ve işleme alınmayan taleplerin geçmişi.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Sipariş</TableHead>
                    <TableHead>Müşteri</TableHead>
                    <TableHead>Tip</TableHead>
                    <TableHead>Neden</TableHead>
                    <TableHead>İnceleyen</TableHead>
                    <TableHead className="text-right">İşlem</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tabData.map((request) => (
                    <TableRow key={request.id}>
                      <TableCell className="font-medium">
                        {request.orderNumber || `Sipariş #${request.orderId}`}
                      </TableCell>
                      <TableCell>{request.customerName || `Kullanıcı #${request.userId}`}</TableCell>
                      <TableCell>
                        <Badge variant="outline">{returnTypeLabels[request.type]}</Badge>
                      </TableCell>
                      <TableCell className="max-w-[20rem] truncate">
                        {request.reviewNote || request.reason}
                      </TableCell>
                      <TableCell>{request.reviewerName || 'Sistem'}</TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="sm" onClick={() => handleOpenRequest(request)}>
                          Detay
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {tabData.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="p-0">
                        <EmptyState
                          icon={ShieldX}
                          title="Reddedilen talep yok"
                          description="Red kararı verilen talepler burada listelenecek."
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
                    <StatusBadge
                      label={returnStatusLabels[selectedRequest.status]}
                      tone={getReturnStatusTone(selectedRequest.status)}
                    />
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
              </div>

              <div className="space-y-2">
                <Label>Müşteri Notu</Label>
                <Textarea
                  value={selectedRequest.requestNote || 'Ek not bırakılmamış'}
                  readOnly
                  rows={4}
                />
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

              {selectedRequest.attachments.length > 0 ? (
                <div className="space-y-2">
                  <Label>Eklenen Fotoğraflar</Label>
                  <div className="rounded-xl border border-border/70 bg-muted/20 p-4">
                    <div className="flex flex-wrap gap-2">
                      {selectedRequest.attachments.map((attachment) => (
                        <Badge key={attachment.id} variant="secondary">
                          {attachment.fileName}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </div>
              ) : null}

              <div className="space-y-2">
                <Label htmlFor="review-note">İnceleme Notu</Label>
                <Textarea
                  id="review-note"
                  value={reviewNote}
                  onChange={(event) => setReviewNote(event.target.value)}
                  placeholder="Red gerekçesi veya karar notu girin"
                  rows={4}
                />
              </div>
            </div>
          ) : null}

          <DialogFooter>
            <Button variant="outline" onClick={() => setSelectedRequest(null)} disabled={isReviewing}>
              Kapat
            </Button>
            <Button
              variant="destructive"
              onClick={() => void handleReview('Rejected')}
              disabled={isReviewing}
            >
              Reddet
            </Button>
            <Button onClick={() => void handleReview('Approved')} disabled={isReviewing}>
              Onayla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
