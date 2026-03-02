import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import {
  AlertCircle,
  BadgeCheck,
  Clock3,
  PackageSearch,
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
import { KpiCard } from '@/components/admin/KpiCard';
import { useGetAdminReturnsQuery, useReviewAdminReturnMutation } from '@/features/admin/adminApi';
import type { ReturnRequest, ReturnRequestStatus, ReturnRequestType } from '@/features/returns/types';

const returnStatusLabels: Record<ReturnRequestStatus, string> = {
  Pending: 'İnceleniyor',
  Approved: 'Onaylandı',
  Rejected: 'Reddedildi',
  RefundPending: 'İade İşleniyor',
  Refunded: 'İade Tamamlandı',
};

const returnStatusClasses: Record<ReturnRequestStatus, string> = {
  Pending: 'bg-amber-500/10 text-amber-700 dark:text-amber-300',
  Approved: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300',
  Rejected: 'bg-rose-500/10 text-rose-700 dark:text-rose-300',
  RefundPending: 'bg-sky-500/10 text-sky-700 dark:text-sky-300',
  Refunded: 'bg-slate-500/10 text-slate-700 dark:text-slate-300',
};

const returnTypeLabels: Record<ReturnRequestType, string> = {
  Return: 'İade',
  Cancellation: 'İptal',
};

type ReturnsTab = 'Pending' | 'Approved' | 'Rejected';

function formatCurrency(value: number) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
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

export default function ReturnsPage() {
  const [activeTab, setActiveTab] = useState<ReturnsTab>('Pending');
  const [selectedRequest, setSelectedRequest] = useState<ReturnRequest | null>(null);
  const [reviewNote, setReviewNote] = useState('');
  const { data: requests = [], isLoading } = useGetAdminReturnsQuery();
  const [reviewReturn, { isLoading: isReviewing }] = useReviewAdminReturnMutation();

  const summary = useMemo(() => {
    return {
      pendingCount: requests.filter((request) => request.status === 'Pending').length,
      cancellationCount: requests.filter((request) => request.type === 'Cancellation').length,
      returnCount: requests.filter((request) => request.type === 'Return').length,
      pendingRefundAmount: requests.reduce((sum, request) => sum + request.requestedRefundAmount, 0),
    };
  }, [requests]);

  const tabData = useMemo(() => {
    if (activeTab === 'Pending') {
      return requests.filter((request) => request.status === 'Pending');
    }

    return [];
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
      toast.error('Red işlemi için inceleme notu girin');
      return;
    }

    try {
      await reviewReturn({
        id: selectedRequest.id,
        status,
        reviewNote: reviewNote.trim() || undefined,
      }).unwrap();

      toast.success(status === 'Approved' ? 'Talep onaylandı' : 'Talep reddedildi');
      setSelectedRequest(null);
      setReviewNote('');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Talep değerlendirilemedi');
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
        <Skeleton className="h-[460px] rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">İade Talepleri</h1>
        <p className="max-w-3xl text-muted-foreground">
          Bekleyen iade ve iptal taleplerini inceleyin, onaylayın veya gerekçeyle reddedin.
          Mevcut backend bu aşamada yalnızca bekleyen kayıtları döndürüyor.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Bekleyen Talep"
          value={summary.pendingCount.toLocaleString('tr-TR')}
          helperText="İnceleme bekleyen toplam talep."
          icon={Clock3}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="İptal Talebi"
          value={summary.cancellationCount.toLocaleString('tr-TR')}
          helperText="Ödeme veya hazırlık aşamasındaki siparişler."
          icon={ShieldX}
          accentClass="text-rose-600 dark:text-rose-300"
          surfaceClass="bg-rose-500/10"
        />
        <KpiCard
          title="İade Talebi"
          value={summary.returnCount.toLocaleString('tr-TR')}
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
                      <TableCell className="max-w-[22rem] truncate">{request.reason}</TableCell>
                      <TableCell>{formatCurrency(request.requestedRefundAmount)}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {new Date(request.createdAt).toLocaleDateString('tr-TR')}
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
                      <TableCell colSpan={7} className="py-12 text-center text-muted-foreground">
                        Bekleyen iade talebi bulunmuyor.
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
            <CardContent className="flex flex-col items-center gap-4 p-12 text-center">
              <AlertCircle className="h-10 w-10 text-muted-foreground" />
              <div className="space-y-1">
                <p className="font-medium">Bu sekme sonraki backend genişletmesini bekliyor</p>
                <p className="text-sm text-muted-foreground">
                  Admin returns endpoint’i şu an yalnızca bekleyen kayıtları döndürüyor.
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="Rejected">
          <Card className="border-border/70">
            <CardContent className="flex flex-col items-center gap-4 p-12 text-center">
              <PackageSearch className="h-10 w-10 text-muted-foreground" />
              <div className="space-y-1">
                <p className="font-medium">Reddedilen talepler burada listelenecek</p>
                <p className="text-sm text-muted-foreground">
                  Liste endpoint’i statü filtresi desteklediğinde bu sekme otomatik olarak anlam kazanacak.
                </p>
              </div>
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
                      <Badge className={returnStatusClasses[selectedRequest.status]} variant="secondary">
                        {returnStatusLabels[selectedRequest.status]}
                      </Badge>
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
