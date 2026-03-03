import { useMemo, useState } from 'react';
import {
  CheckCheck,
  CheckCircle2,
  Eye,
  Search,
  ShieldAlert,
  Star,
  XCircle,
} from 'lucide-react';
import { toast } from 'sonner';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Checkbox } from '@/components/common/checkbox';
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
import {
  useApproveAdminReviewMutation,
  useBulkApproveAdminReviewsMutation,
  useGetAdminReviewsQuery,
  useRejectAdminReviewMutation,
} from '@/features/admin/adminApi';
import type { ProductReviewDto, ReviewModerationStatus } from '@/features/products/types';

type ReviewTab = 'Pending' | 'Reported' | 'Approved' | 'Rejected';

const reviewTabs: Array<{ value: ReviewTab; label: string }> = [
  { value: 'Pending', label: 'Onay Bekleyen' },
  { value: 'Reported', label: 'Şikayet Edilenler' },
  { value: 'Approved', label: 'Onaylananlar' },
  { value: 'Rejected', label: 'Reddedilenler' },
];

function formatDate(value: string) {
  return new Date(value).toLocaleString('tr-TR', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getReviewTone(status: ReviewModerationStatus) {
  switch (status) {
    case 'Pending':
      return 'warning' as const;
    case 'Approved':
      return 'success' as const;
    case 'Rejected':
      return 'danger' as const;
    default:
      return 'neutral' as const;
  }
}

function getReviewLabel(status: ReviewModerationStatus) {
  switch (status) {
    case 'Pending':
      return 'Onay Bekliyor';
    case 'Approved':
      return 'Onaylandı';
    case 'Rejected':
      return 'Reddedildi';
    default:
      return status;
  }
}

function filterReviewsByTab(reviews: ProductReviewDto[], tab: ReviewTab) {
  if (tab === 'Reported') {
    return [];
  }

  return reviews.filter((review) => review.moderationStatus === tab);
}

export default function ReviewsPage() {
  const [activeTab, setActiveTab] = useState<ReviewTab>('Pending');
  const [search, setSearch] = useState('');
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [detailReview, setDetailReview] = useState<ProductReviewDto | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const { data: reviews = [], isLoading } = useGetAdminReviewsQuery();
  const [approveReview, { isLoading: isApproving }] = useApproveAdminReviewMutation();
  const [rejectReview, { isLoading: isRejecting }] = useRejectAdminReviewMutation();
  const [bulkApproveReviews, { isLoading: isBulkApproving }] = useBulkApproveAdminReviewsMutation();

  const searchedReviews = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('tr-TR');

    if (!term) {
      return reviews;
    }

    return reviews.filter((review) =>
      review.productName.toLocaleLowerCase('tr-TR').includes(term)
      || review.userFullName.toLocaleLowerCase('tr-TR').includes(term)
      || review.comment.toLocaleLowerCase('tr-TR').includes(term)
    );
  }, [reviews, search]);

  const visibleReviews = useMemo(() => filterReviewsByTab(searchedReviews, activeTab), [searchedReviews, activeTab]);
  const pendingReviews = useMemo(() => filterReviewsByTab(reviews, 'Pending'), [reviews]);
  const approvedReviews = useMemo(() => filterReviewsByTab(reviews, 'Approved'), [reviews]);
  const rejectedReviews = useMemo(() => filterReviewsByTab(reviews, 'Rejected'), [reviews]);

  const selectableIds = useMemo(() => {
    return visibleReviews
      .filter((review) => review.moderationStatus === 'Pending')
      .map((review) => review.id);
  }, [visibleReviews]);

  const allSelectableChecked = selectableIds.length > 0 && selectableIds.every((id) => selectedIds.includes(id));

  const resetDialogState = () => {
    setDetailReview(null);
    setRejectReason('');
  };

  const handleToggleRow = (reviewId: number, checked: boolean) => {
    setSelectedIds((current) =>
      checked ? [...new Set([...current, reviewId])] : current.filter((id) => id !== reviewId)
    );
  };

  const handleToggleAll = (checked: boolean) => {
    if (!checked) {
      setSelectedIds((current) => current.filter((id) => !selectableIds.includes(id)));
      return;
    }

    setSelectedIds((current) => [...new Set([...current, ...selectableIds])]);
  };

  const handleApprove = async (reviewId: number) => {
    try {
      await approveReview(reviewId).unwrap();
      toast.success('Yorum onaylandı');
      setSelectedIds((current) => current.filter((id) => id !== reviewId));
      if (detailReview?.id === reviewId) {
        resetDialogState();
      }
    } catch (error) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Yorum onaylanamadı');
    }
  };

  const handleReject = async () => {
    if (!detailReview) {
      return;
    }

    try {
      await rejectReview({
        id: detailReview.id,
        data: { moderationNote: rejectReason.trim() || undefined },
      }).unwrap();
      toast.success('Yorum reddedildi');
      setSelectedIds((current) => current.filter((id) => id !== detailReview.id));
      resetDialogState();
    } catch (error) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Yorum reddedilemedi');
    }
  };

  const handleBulkApprove = async () => {
    if (selectedIds.length === 0) {
      return;
    }

    try {
      await bulkApproveReviews(selectedIds).unwrap();
      toast.success('Seçilen yorumlar onaylandı');
      setSelectedIds([]);
    } catch (error) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Toplu onay işlemi başarısız');
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
        <Skeleton className="h-[560px] rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Yorum Moderasyonu</h1>
        <p className="max-w-3xl text-muted-foreground">
          Yeni yorumlari onaylayin, uygun olmayanlari reddedin ve yayin akisini tek ekrandan yonetin.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Onay Bekleyen"
          value={pendingReviews.length.toLocaleString('tr-TR')}
          helperText="Moderasyon bekleyen yeni yorumlar."
          icon={ShieldAlert}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="Onaylanan"
          value={approvedReviews.length.toLocaleString('tr-TR')}
          helperText="Yayinda olan yorumlar."
          icon={CheckCircle2}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Reddedilen"
          value={rejectedReviews.length.toLocaleString('tr-TR')}
          helperText="Yayindan alinan yorumlar."
          icon={XCircle}
          accentClass="text-rose-600 dark:text-rose-300"
          surfaceClass="bg-rose-500/10"
        />
        <KpiCard
          title="Ortalama Puan"
          value={
            reviews.length > 0
              ? `${(reviews.reduce((sum, review) => sum + review.rating, 0) / reviews.length).toFixed(1)} / 5`
              : '0.0 / 5'
          }
          helperText="Tum review havuzunun puan ortalamasi."
          icon={Star}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
      </div>

      <Card className="border-border/70">
        <CardHeader className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <CardTitle>Moderasyon Kuyrugu</CardTitle>
            <CardDescription>
              Sikayet edilenler akisinin backend raporlama destegi henuz olmadigi icin bu sekme bilgilendirici bos state gosterir.
            </CardDescription>
          </div>
          <div className="flex w-full flex-col gap-3 sm:flex-row xl:w-auto">
            <div className="relative sm:min-w-80">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Urun, kullanici veya yorum ara..."
                className="pl-10"
              />
            </div>
            <Button
              onClick={handleBulkApprove}
              disabled={selectedIds.length === 0 || isBulkApproving}
            >
              <CheckCheck className="mr-2 h-4 w-4" />
              Secilenleri Onayla
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as ReviewTab)} className="space-y-4">
            <TabsList className="h-auto w-full flex-wrap justify-start gap-2 bg-transparent p-0">
              {reviewTabs.map((tab) => {
                const count = filterReviewsByTab(reviews, tab.value).length;
                return (
                  <TabsTrigger
                    key={tab.value}
                    value={tab.value}
                    className="rounded-full border bg-muted/50 px-4 py-2 data-[state=active]:border-primary"
                  >
                    {tab.label}
                    <Badge variant="secondary" className="ml-1 rounded-full">
                      {count.toLocaleString('tr-TR')}
                    </Badge>
                  </TabsTrigger>
                );
              })}
            </TabsList>

            {reviewTabs.map((tab) => {
              const tabReviews = filterReviewsByTab(visibleReviews, tab.value);

              return (
                <TabsContent key={tab.value} value={tab.value} className="space-y-4">
                  {tab.value === 'Reported' ? (
                    <div className="rounded-2xl border border-dashed bg-muted/20 p-10 text-center">
                      <p className="text-lg font-semibold">Sikayet akisina backend destegi bekleniyor</p>
                      <p className="mt-2 text-sm text-muted-foreground">
                        Uretimsel raporlama endpoint'i geldikten sonra bu sekme dogrudan sikayet edilen yorumlari gosterecek.
                      </p>
                    </div>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead className="w-10">
                            <Checkbox
                              checked={allSelectableChecked}
                              onCheckedChange={(checked) => handleToggleAll(Boolean(checked))}
                              disabled={selectableIds.length === 0}
                              aria-label="Tum yorumlari sec"
                            />
                          </TableHead>
                          <TableHead>Kullanici</TableHead>
                          <TableHead>Urun</TableHead>
                          <TableHead>Puan</TableHead>
                          <TableHead>Yorum Ozeti</TableHead>
                          <TableHead>Durum</TableHead>
                          <TableHead>Tarih</TableHead>
                          <TableHead className="text-right">Islemler</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {tabReviews.map((review) => (
                          <TableRow key={review.id}>
                            <TableCell>
                              <Checkbox
                                checked={selectedIds.includes(review.id)}
                                onCheckedChange={(checked) => handleToggleRow(review.id, Boolean(checked))}
                                disabled={review.moderationStatus !== 'Pending'}
                                aria-label={`Yorum ${review.id} sec`}
                              />
                            </TableCell>
                            <TableCell>
                              <div>
                                <p className="font-medium">{review.userFullName}</p>
                                <p className="text-xs text-muted-foreground">#{review.userId}</p>
                              </div>
                            </TableCell>
                            <TableCell>
                              <div>
                                <p className="font-medium">{review.productName}</p>
                                <p className="text-xs text-muted-foreground">Urun ID #{review.productId}</p>
                              </div>
                            </TableCell>
                            <TableCell>
                              <span className="font-medium">{review.rating} / 5</span>
                            </TableCell>
                            <TableCell className="max-w-sm">
                              <p className="line-clamp-2 text-sm text-muted-foreground">{review.comment}</p>
                            </TableCell>
                            <TableCell>
                              <StatusBadge
                                label={getReviewLabel(review.moderationStatus)}
                                tone={getReviewTone(review.moderationStatus)}
                              />
                            </TableCell>
                            <TableCell className="text-sm text-muted-foreground">
                              {formatDate(review.createdAt)}
                            </TableCell>
                            <TableCell className="text-right">
                              <div className="flex justify-end gap-2">
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={() => {
                                    setDetailReview(review);
                                    setRejectReason(review.moderationNote || '');
                                  }}
                                >
                                  <Eye className="mr-2 h-4 w-4" />
                                  Detay
                                </Button>
                                {review.moderationStatus !== 'Approved' ? (
                                  <Button
                                    size="sm"
                                    onClick={() => void handleApprove(review.id)}
                                    disabled={isApproving}
                                  >
                                    Onayla
                                  </Button>
                                ) : null}
                              </div>
                            </TableCell>
                          </TableRow>
                        ))}
                        {tabReviews.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={8} className="p-0">
                              <EmptyState
                                icon={ShieldAlert}
                                title="Bu sekmede yorum bulunmuyor"
                                description="Filtrelenen moderasyon durumuna uyan yorumlar oluştuğunda bu tablo otomatik dolacak."
                                className="border-0 shadow-none"
                              />
                            </TableCell>
                          </TableRow>
                        ) : null}
                      </TableBody>
                    </Table>
                  )}
                </TabsContent>
              );
            })}
          </Tabs>
        </CardContent>
      </Card>

      <Dialog open={!!detailReview} onOpenChange={(open) => (!open ? resetDialogState() : undefined)}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Yorum Detayi</DialogTitle>
            <DialogDescription>
              Yorumu inceleyin, moderasyon notu ekleyin ve uygun aksiyonu uygulayin.
            </DialogDescription>
          </DialogHeader>

          {detailReview ? (
            <div className="space-y-6">
              <div className="grid gap-4 rounded-2xl border bg-muted/20 p-4 md:grid-cols-2">
                <div>
                  <p className="text-sm text-muted-foreground">Kullanici</p>
                  <p className="font-medium">{detailReview.userFullName}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Urun</p>
                  <p className="font-medium">{detailReview.productName}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Puan</p>
                  <p className="font-medium">{detailReview.rating} / 5</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Durum</p>
                  <StatusBadge
                    label={getReviewLabel(detailReview.moderationStatus)}
                    tone={getReviewTone(detailReview.moderationStatus)}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label>Yorum Metni</Label>
                <div className="rounded-2xl border bg-background p-4 leading-7 text-foreground">
                  {detailReview.comment}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="moderation-note">Moderasyon Notu</Label>
                <Textarea
                  id="moderation-note"
                  rows={4}
                  placeholder="Reddetme nedeni veya dahili notlar..."
                  value={rejectReason}
                  onChange={(event) => setRejectReason(event.target.value)}
                />
              </div>

              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => resetDialogState()}
                >
                  Kapat
                </Button>
                <Button
                  variant="destructive"
                  onClick={() => void handleReject()}
                  disabled={isRejecting}
                >
                  Reddet
                </Button>
                {detailReview.moderationStatus !== 'Approved' ? (
                  <Button
                    onClick={() => void handleApprove(detailReview.id)}
                    disabled={isApproving}
                  >
                    Onayla
                  </Button>
                ) : null}
              </DialogFooter>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  );
}
