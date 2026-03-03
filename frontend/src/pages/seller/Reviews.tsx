import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Loader2,
  MessageSquareQuote,
  Search,
  Send,
  Star,
  Store,
} from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Skeleton } from '@/components/common/skeleton';
import { Textarea } from '@/components/common/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import {
  useGetReviewSummaryQuery,
} from '@/features/products/productsApi';
import {
  useGetSellerAnalyticsSummaryQuery,
  useGetSellerProductsQuery,
  useGetSellerProfileQuery,
  useGetSellerReviewsQuery,
  useReplySellerReviewMutation,
} from '@/features/seller/sellerApi';
import { toast } from 'sonner';

function maskCustomerName(value: string) {
  return value
    .split(' ')
    .filter(Boolean)
    .map((part) => `${part.charAt(0)}**`)
    .join(' ');
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('tr-TR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function renderStars(rating: number) {
  return '★'.repeat(Math.round(rating)).padEnd(5, '☆');
}

export default function SellerReviewsPage() {
  const [search, setSearch] = useState('');
  const [selectedProductId, setSelectedProductId] = useState<number | null>(null);
  const [ratingFilter, setRatingFilter] = useState<'all' | '5' | '4' | '3' | '2' | '1'>('all');
  const [replyStatusFilter, setReplyStatusFilter] = useState<'all' | 'answered' | 'unanswered'>('all');
  const [replyDrafts, setReplyDrafts] = useState<Record<number, string>>({});

  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const shouldSkipProtectedQueries = profileLoading || !profile;
  const { data: summary, isLoading: summaryLoading } = useGetSellerAnalyticsSummaryQuery(undefined, {
    skip: shouldSkipProtectedQueries,
  });
  const { data: sellerProducts, isLoading: productsLoading } = useGetSellerProductsQuery(
    { page: 1, pageSize: 100 },
    { skip: shouldSkipProtectedQueries }
  );

  const products = sellerProducts?.items ?? [];
  const reviewedProducts = useMemo(() => {
    const filtered = products
      .filter((product) => product.reviewCount > 0)
      .filter((product) => {
        const term = search.trim().toLocaleLowerCase('tr-TR');
        if (!term) {
          return true;
        }

        return (
          product.name.toLocaleLowerCase('tr-TR').includes(term)
          || product.categoryName.toLocaleLowerCase('tr-TR').includes(term)
        );
      })
      .sort((a, b) => {
        if (b.reviewCount !== a.reviewCount) {
          return b.reviewCount - a.reviewCount;
        }

        return b.averageRating - a.averageRating;
      });

    return filtered;
  }, [products, search]);

  useEffect(() => {
    if (!selectedProductId && reviewedProducts.length > 0) {
      setSelectedProductId(reviewedProducts[0].id);
    }

    if (selectedProductId && reviewedProducts.every((product) => product.id !== selectedProductId)) {
      setSelectedProductId(reviewedProducts[0]?.id ?? null);
    }
  }, [reviewedProducts, selectedProductId]);

  const selectedProduct = reviewedProducts.find((product) => product.id === selectedProductId) ?? null;

  const { data: productSummary, isLoading: productSummaryLoading } = useGetReviewSummaryQuery(selectedProductId ?? 0, {
    skip: !selectedProductId,
  });
  const { data: productReviews = [], isLoading: productReviewsLoading } = useGetSellerReviewsQuery(
    selectedProductId
      ? {
          productId: selectedProductId,
          rating: ratingFilter === 'all' ? undefined : Number(ratingFilter),
          replied: replyStatusFilter === 'all' ? undefined : replyStatusFilter === 'answered',
        }
      : undefined,
    { skip: !selectedProductId }
  );
  const [replyToReview, { isLoading: isReplySaving }] = useReplySellerReviewMutation();

  const isLoading = profileLoading || summaryLoading || productsLoading;

  const handleReplySubmit = async (reviewId: number) => {
    if (!selectedProductId) {
      return;
    }

    const replyText = (replyDrafts[reviewId] ?? '').trim();
    if (!replyText) {
      toast.error('Lütfen bir yanıt metni girin.');
      return;
    }

    try {
      await replyToReview({
        reviewId,
        productId: selectedProductId,
        data: { replyText },
      }).unwrap();
      toast.success('Satıcı yanıtı kaydedildi.');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Yanıt kaydedilemedi.');
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
        <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
          <Skeleton className="h-[420px] rounded-xl" />
          <Skeleton className="h-[420px] rounded-xl" />
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <Card className="border-amber-500/30 bg-amber-50 dark:bg-amber-950/20">
        <CardContent className="flex flex-col gap-4 p-6 md:flex-row md:items-center md:justify-between">
          <div className="flex items-start gap-3">
            <Store className="mt-0.5 h-5 w-5 text-amber-600 dark:text-amber-300" />
            <div className="space-y-1">
              <p className="font-medium">Önce mağaza profilinizi tamamlayın</p>
              <p className="text-sm text-muted-foreground">
                Yorum ekranını kullanabilmek için seller profilinizin aktif olması gerekiyor.
              </p>
            </div>
          </div>
          <Button asChild>
            <Link to="/seller/profile">Profil Oluştur</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Müşteri Yorumları</h1>
        <p className="max-w-3xl text-muted-foreground">
          Ürün bazlı değerlendirmeleri takip edin, müşteri geri bildirimlerini okuyun ve uygun yorumlara
          doğrudan mağaza yanıtı ekleyin.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Genel Puan"
          value={`${summary?.averageRating.toFixed(1) ?? '0.0'} / 5`}
          helperText="Seller analytics özetinden gelen genel değerlendirme."
          icon={Star}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="Toplam Yorum"
          value={(summary?.reviewCount ?? 0).toLocaleString('tr-TR')}
          helperText="Mağaza ürünlerindeki toplam değerlendirme sayısı."
          icon={MessageSquareQuote}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Yorum Alan Ürün"
          value={reviewedProducts.length.toLocaleString('tr-TR')}
          helperText="En az bir değerlendirme alan ürünler."
          icon={Store}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Aktif Ürün"
          value={(summary?.activeProducts ?? 0).toLocaleString('tr-TR')}
          helperText="Yorum görünümüyle ilişkili aktif katalog ürünleri."
          icon={MessageSquareQuote}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Ürün Bazlı Yorum Özeti</CardTitle>
            <CardDescription>Yorum sayısı ve puana göre ürünlerinizi karşılaştırın.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Ürün veya kategori ara..."
                className="pl-10"
              />
            </div>

            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Yorum</TableHead>
                  <TableHead>Puan</TableHead>
                  <TableHead className="text-right">İncele</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {reviewedProducts.map((product) => (
                  <TableRow key={product.id}>
                    <TableCell>
                      <div>
                        <p className="font-medium">{product.name}</p>
                        <p className="text-sm text-muted-foreground">{product.categoryName}</p>
                      </div>
                    </TableCell>
                    <TableCell>{product.reviewCount.toLocaleString('tr-TR')}</TableCell>
                    <TableCell>
                      <div className="space-y-1">
                        <p className="font-medium">{product.averageRating.toFixed(1)} / 5</p>
                        <p className="text-xs text-muted-foreground">{renderStars(product.averageRating)}</p>
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant={selectedProductId === product.id ? 'default' : 'ghost'}
                        size="sm"
                        onClick={() => setSelectedProductId(product.id)}
                      >
                        Aç
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {reviewedProducts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} className="p-0">
                      <EmptyState
                        icon={MessageSquareQuote}
                        title="Henüz yorum alan ürün bulunmuyor"
                        description="Onaylı yorum geldikçe ürün bazlı performans tablosu bu alanda oluşacak."
                        className="border-0 shadow-none"
                      />
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Seçili Ürün Yorumları</CardTitle>
            <CardDescription>
              {selectedProduct
                ? `${selectedProduct.name} için gerçek yorum akışı`
                : 'Yorum detayını görmek için soldan bir ürün seçin'}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            {!selectedProduct ? (
              <EmptyState
                icon={Store}
                title="Görüntülemek için bir ürün seçin"
                description="Sol taraftaki listeden yorum alan bir ürünü seçtiğinizde detay ve yanıt alanı burada açılacak."
                className="border-dashed shadow-none"
              />
            ) : productSummaryLoading || productReviewsLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-20 rounded-xl" />
                <Skeleton className="h-24 rounded-xl" />
                <Skeleton className="h-24 rounded-xl" />
              </div>
            ) : (
              <>
                <div className="grid gap-4 md:grid-cols-2">
                  <Card className="border-border/70 bg-muted/20">
                    <CardContent className="p-5">
                      <p className="text-sm text-muted-foreground">Ürün Ortalaması</p>
                      <p className="mt-1 text-2xl font-semibold">
                        {productSummary?.averageRating.toFixed(1) ?? selectedProduct.averageRating.toFixed(1)} / 5
                      </p>
                      <p className="mt-2 text-xs text-muted-foreground">
                        {renderStars(productSummary?.averageRating ?? selectedProduct.averageRating)}
                      </p>
                    </CardContent>
                  </Card>
                  <Card className="border-border/70 bg-muted/20">
                    <CardContent className="p-5">
                      <p className="text-sm text-muted-foreground">Toplam Yorum</p>
                      <p className="mt-1 text-2xl font-semibold">
                        {(productSummary?.totalReviews ?? selectedProduct.reviewCount).toLocaleString('tr-TR')}
                      </p>
                      <p className="mt-2 text-xs text-muted-foreground">
                        İlgili ürün için görünen toplam değerlendirme sayısı.
                      </p>
                    </CardContent>
                  </Card>
                </div>

                <div className="space-y-3">
                  <p className="text-sm font-medium">Puan Dağılımı</p>
                  {[5, 4, 3, 2, 1].map((star) => {
                    const count = productSummary?.ratingDistribution?.[star] ?? 0;
                    const total = productSummary?.totalReviews ?? 0;
                    const percentage = total > 0 ? (count / total) * 100 : 0;

                    return (
                      <div key={star} className="flex items-center gap-3">
                        <div className="w-10 text-sm font-medium">{star}★</div>
                        <div className="h-2 flex-1 overflow-hidden rounded-full bg-muted">
                          <div
                            className="h-full rounded-full bg-amber-500"
                            style={{ width: `${percentage}%` }}
                          />
                        </div>
                        <div className="w-12 text-right text-sm text-muted-foreground">
                          {count}
                        </div>
                      </div>
                    );
                  })}
                </div>

                <div className="space-y-3">
                  <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                    <p className="text-sm font-medium">Yorum Akışı</p>
                    <div className="grid gap-3 md:grid-cols-2">
                      <Select value={ratingFilter} onValueChange={(value) => setRatingFilter(value as typeof ratingFilter)}>
                        <SelectTrigger className="w-full md:w-[180px]">
                          <SelectValue placeholder="Puana göre filtrele" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="all">Tüm Puanlar</SelectItem>
                          <SelectItem value="5">5 yıldız</SelectItem>
                          <SelectItem value="4">4 yıldız</SelectItem>
                          <SelectItem value="3">3 yıldız</SelectItem>
                          <SelectItem value="2">2 yıldız</SelectItem>
                          <SelectItem value="1">1 yıldız</SelectItem>
                        </SelectContent>
                      </Select>
                      <Select value={replyStatusFilter} onValueChange={(value) => setReplyStatusFilter(value as typeof replyStatusFilter)}>
                        <SelectTrigger className="w-full md:w-[200px]">
                          <SelectValue placeholder="Yanıt durumuna göre filtrele" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="all">Tüm Yorumlar</SelectItem>
                          <SelectItem value="answered">Yanıtlananlar</SelectItem>
                          <SelectItem value="unanswered">Yanıt Bekleyenler</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                  {productReviews.length === 0 ? (
                    <EmptyState
                      icon={Search}
                      title="Seçili filtrelerle eşleşen yorum bulunmuyor"
                      description="Puan veya yanıt filtresini değiştirerek farklı yorum gruplarını görüntüleyebilirsiniz."
                      className="border-dashed shadow-none"
                    />
                  ) : (
                    <div className="space-y-3">
                      {productReviews.map((review) => (
                        <div key={review.id} className="rounded-xl border p-4">
                          <div className="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
                            <div>
                              <p className="font-medium">{maskCustomerName(review.userFullName)}</p>
                              <p className="text-xs text-muted-foreground">{formatDate(review.createdAt)}</p>
                            </div>
                            <Badge variant="secondary">
                              {review.rating} / 5
                            </Badge>
                          </div>
                          <p className="mt-3 text-sm leading-6 text-muted-foreground">{review.comment}</p>
                          {review.sellerReply ? (
                            <div className="mt-4 rounded-xl border border-primary/20 bg-primary/5 p-4">
                              <div className="flex items-center justify-between gap-3">
                                <p className="text-sm font-medium">Mağaza Yanıtı</p>
                                <p className="text-xs text-muted-foreground">
                                  {review.sellerRepliedAt ? formatDate(review.sellerRepliedAt) : ''}
                                </p>
                              </div>
                              <p className="mt-2 text-sm leading-6 text-muted-foreground">{review.sellerReply}</p>
                            </div>
                          ) : null}
                          <div className="mt-4 space-y-2 rounded-xl border border-dashed p-3">
                            <div className="flex items-center justify-between gap-3">
                              <p className="text-sm font-medium">
                                {review.sellerReply ? 'Yanıtı Güncelle' : 'Yanıt Yaz'}
                              </p>
                              {review.sellerReply ? (
                                <Badge variant="secondary">Yanıtlandı</Badge>
                              ) : (
                                <Badge variant="outline">Yanıt bekliyor</Badge>
                              )}
                            </div>
                            <Textarea
                              rows={3}
                              placeholder="Müşteriye görünmesini istediğiniz kısa ve net yanıtı yazın..."
                              value={replyDrafts[review.id] ?? review.sellerReply ?? ''}
                              onChange={(event) =>
                                setReplyDrafts((current) => ({
                                  ...current,
                                  [review.id]: event.target.value,
                                }))
                              }
                            />
                            <div className="flex justify-end">
                              <Button
                                size="sm"
                                onClick={() => void handleReplySubmit(review.id)}
                                disabled={isReplySaving}
                              >
                                {isReplySaving ? (
                                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                ) : (
                                  <Send className="mr-2 h-4 w-4" />
                                )}
                                Yanıtı Kaydet
                              </Button>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
