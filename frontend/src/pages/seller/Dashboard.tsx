import { Link } from 'react-router-dom';
import {
  Activity,
  BarChart3,
  Eye,
  Package,
  RefreshCcw,
  ShoppingBag,
  Star,
  Store,
  TrendingUp,
  UserCheck,
  Wallet,
} from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import {
  useGetSellerAnalyticsSummaryQuery,
  useGetSellerAnalyticsTrendsQuery,
  useGetSellerProfileQuery,
} from '@/features/seller/sellerApi';

const numberFormatter = new Intl.NumberFormat('tr-TR');

function formatCurrency(value: number, currency: string) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

function formatPercent(value: number) {
  return `%${value.toFixed(2)}`;
}

type TrendKey = 'views' | 'favorites' | 'orders' | 'revenue' | 'averageRating';

function MiniTrendChart({
  data,
  metric,
  colorClass,
  emptyLabel,
}: {
  data: {
    date: string;
    views: number;
    favorites: number;
    orders: number;
    revenue: number;
    averageRating: number;
  }[];
  metric: TrendKey;
  colorClass: string;
  emptyLabel: string;
}) {
  const maxValue = Math.max(...data.map((point) => point[metric]), 0);

  if (maxValue <= 0) {
    return (
      <div className="flex h-44 items-center justify-center rounded-2xl border border-dashed border-border/70 bg-muted/30 text-sm text-muted-foreground">
        {emptyLabel}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="grid h-44 grid-cols-7 gap-2 rounded-2xl border border-border/70 bg-muted/20 p-4">
        {data.slice(-7).map((point) => {
          const value = point[metric];
          const height = maxValue > 0 ? Math.max((value / maxValue) * 100, value > 0 ? 12 : 0) : 0;

          return (
            <div key={`${metric}-${point.date}`} className="flex min-h-0 flex-col justify-end gap-2">
              <div className="flex flex-1 items-end justify-center rounded-xl bg-background/70 p-2 dark:bg-background/20">
                <div
                  className={`w-full rounded-full transition-all ${colorClass}`}
                  style={{ height: `${height}%` }}
                  title={`${point.date}: ${value}`}
                />
              </div>
              <span className="text-center text-[11px] text-muted-foreground">
                {new Date(point.date).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' })}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default function SellerDashboard() {
  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const shouldSkipAnalytics = profileLoading || !profile;
  const { data: summary, isLoading: summaryLoading } = useGetSellerAnalyticsSummaryQuery(undefined, {
    skip: shouldSkipAnalytics,
  });
  const { data: trends, isLoading: trendsLoading } = useGetSellerAnalyticsTrendsQuery(30, {
    skip: shouldSkipAnalytics,
  });

  const isLoading = profileLoading || summaryLoading || trendsLoading;
  const hasProfile = !!profile;
  const currency = summary?.currency || 'TRY';

  const statCards = [
    {
      title: 'Toplam Görüntülenme',
      value: summary ? numberFormatter.format(summary.totalViews) : '-',
      description: 'Ürün detay ve öneri kaynaklı görünüm toplamı',
      icon: Eye,
      iconClass: 'text-sky-600 dark:text-sky-300',
      surfaceClass: 'bg-sky-100 dark:bg-sky-500/15',
    },
    {
      title: 'Favorilenme Oranı',
      value: summary ? formatPercent(summary.favoriteRate) : '-',
      description: `${summary?.totalWishlistCount || 0} toplam favori kaydı`,
      icon: TrendingUp,
      iconClass: 'text-rose-600 dark:text-rose-300',
      surfaceClass: 'bg-rose-100 dark:bg-rose-500/15',
    },
    {
      title: 'Dönüşüm',
      value: summary ? formatPercent(summary.conversionRate) : '-',
      description: `${summary?.successfulOrderCount || 0} başarılı sipariş`,
      icon: ShoppingBag,
      iconClass: 'text-emerald-600 dark:text-emerald-300',
      surfaceClass: 'bg-emerald-100 dark:bg-emerald-500/15',
    },
    {
      title: 'Brüt Ciro',
      value: summary ? formatCurrency(summary.grossRevenue, currency) : '-',
      description: 'Satıcı ürünlerinden oluşan toplam tahsilat',
      icon: Wallet,
      iconClass: 'text-amber-600 dark:text-amber-300',
      surfaceClass: 'bg-amber-100 dark:bg-amber-500/15',
    },
    {
      title: 'Ortalama Puan',
      value: summary ? `${summary.averageRating.toFixed(2)} / 5` : '-',
      description: `${summary?.reviewCount || 0} değerlendirme`,
      icon: Star,
      iconClass: 'text-fuchsia-600 dark:text-fuchsia-300',
      surfaceClass: 'bg-fuchsia-100 dark:bg-fuchsia-500/15',
    },
    {
      title: 'İade Oranı',
      value: summary ? formatPercent(summary.returnRate) : '-',
      description: `${summary?.returnedRequestCount || 0} iade / iptal talebi`,
      icon: RefreshCcw,
      iconClass: 'text-orange-600 dark:text-orange-300',
      surfaceClass: 'bg-orange-100 dark:bg-orange-500/15',
    },
  ];

  if (isLoading) {
    return (
      <div className="space-y-8">
        <div className="space-y-3">
          <Skeleton className="h-10 w-56" />
          <Skeleton className="h-5 w-80" />
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <Card key={index}>
              <CardHeader className="space-y-3">
                <Skeleton className="h-4 w-28" />
                <Skeleton className="h-8 w-24" />
              </CardHeader>
            </Card>
          ))}
        </div>

        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.3fr_0.7fr]">
          <Skeleton className="h-[420px]" />
          <Skeleton className="h-[420px]" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="space-y-2">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight">Satıcı Analitikleri</h1>
            {profile?.isVerified ? (
              <Badge className="border-emerald-500/20 bg-emerald-500/10 text-emerald-700 dark:text-emerald-200">
                <UserCheck className="mr-1 h-3.5 w-3.5" />
                Onaylı Mağaza
              </Badge>
            ) : (
              <Badge variant="outline" className="border-amber-500/30 text-amber-600 dark:text-amber-300">
                <Store className="mr-1 h-3.5 w-3.5" />
                Doğrulama Bekliyor
              </Badge>
            )}
          </div>
          <p className="max-w-3xl text-muted-foreground">
            Ürün performansınızı, favorilenme eğilimini, sipariş dönüşümünü ve iade oranlarını tek ekranda takip edin.
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <Button asChild variant="outline">
            <Link to="/seller/products">Ürünlerimi Yönet</Link>
          </Button>
          <Button asChild>
            <Link to="/seller/products/new">Yeni Ürün Ekle</Link>
          </Button>
        </div>
      </div>

      {!hasProfile && (
        <Card className="border-amber-500/30 bg-amber-50 dark:bg-amber-950/20">
          <CardContent className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-start gap-3">
              <Store className="mt-0.5 h-5 w-5 text-amber-600 dark:text-amber-300" />
              <div className="space-y-1">
                <p className="font-medium text-foreground">Satıcı profilinizi tamamlayın</p>
                <p className="text-sm text-muted-foreground">
                  Analitik kartlarının dolu gelmesi ve ürün yayınlayabilmeniz için önce marka profilinizi oluşturmanız gerekiyor.
                </p>
              </div>
            </div>
            <Button asChild className="sm:shrink-0">
              <Link to="/seller/profile">Profil Oluştur</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
        {statCards.map((stat) => {
          const Icon = stat.icon;
          return (
            <Card key={stat.title} className="border-border/70">
              <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0 pb-3">
                <div className="space-y-1">
                  <CardTitle className="text-sm font-medium text-muted-foreground">{stat.title}</CardTitle>
                  <div className="text-2xl font-semibold tracking-tight">{stat.value}</div>
                </div>
                <div className={`rounded-2xl p-3 ${stat.surfaceClass}`}>
                  <Icon className={`h-5 w-5 ${stat.iconClass}`} />
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">{stat.description}</p>
              </CardContent>
            </Card>
          );
        })}
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.35fr_0.65fr]">
        <Card className="border-border/70">
          <CardHeader className="space-y-2">
            <CardTitle className="flex items-center gap-2">
              <BarChart3 className="h-5 w-5 text-primary" />
              Son 30 Gün Trendleri
            </CardTitle>
            <CardDescription>
              Görüntülenme, favori, sipariş, ciro ve ortalama puan değişimini günlük olarak izleyin.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Tabs defaultValue="views" className="space-y-5">
              <TabsList className="grid w-full grid-cols-5">
                <TabsTrigger value="views">Görüntüleme</TabsTrigger>
                <TabsTrigger value="favorites">Favori</TabsTrigger>
                <TabsTrigger value="orders">Sipariş</TabsTrigger>
                <TabsTrigger value="revenue">Ciro</TabsTrigger>
                <TabsTrigger value="rating">Puan</TabsTrigger>
              </TabsList>

              <TabsContent value="views" className="space-y-3">
                <MiniTrendChart
                  data={trends || []}
                  metric="views"
                  colorClass="bg-sky-500"
                  emptyLabel="Henüz ürün görüntüleme verisi oluşmadı."
                />
              </TabsContent>
              <TabsContent value="favorites" className="space-y-3">
                <MiniTrendChart
                  data={trends || []}
                  metric="favorites"
                  colorClass="bg-rose-500"
                  emptyLabel="Henüz favori trendi oluşmadı."
                />
              </TabsContent>
              <TabsContent value="orders" className="space-y-3">
                <MiniTrendChart
                  data={trends || []}
                  metric="orders"
                  colorClass="bg-emerald-500"
                  emptyLabel="Henüz sipariş trendi oluşmadı."
                />
              </TabsContent>
              <TabsContent value="revenue" className="space-y-3">
                <MiniTrendChart
                  data={trends || []}
                  metric="revenue"
                  colorClass="bg-amber-500"
                  emptyLabel="Henüz ciro verisi oluşmadı."
                />
              </TabsContent>
              <TabsContent value="rating" className="space-y-3">
                <MiniTrendChart
                  data={trends || []}
                  metric="averageRating"
                  colorClass="bg-fuchsia-500"
                  emptyLabel="Henüz puan trendi oluşmadı."
                />
              </TabsContent>
            </Tabs>
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card className="border-border/70">
            <CardHeader className="space-y-2">
              <CardTitle>Mağaza Özeti</CardTitle>
              <CardDescription>Marka doğrulaması ve ürün varlığınızın hızlı özeti.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-start justify-between gap-4 rounded-2xl border border-border/70 bg-muted/20 p-4">
                <div>
                  <p className="text-sm text-muted-foreground">Marka</p>
                  <p className="mt-1 font-semibold">{profile?.brandName || 'Profil oluşturulmadı'}</p>
                </div>
                <div className="rounded-2xl bg-primary/10 p-3 text-primary">
                  <Store className="h-5 w-5" />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-2xl border border-border/70 p-4">
                  <p className="text-sm text-muted-foreground">Toplam Ürün</p>
                  <p className="mt-2 text-xl font-semibold">{summary?.totalProducts || 0}</p>
                </div>
                <div className="rounded-2xl border border-border/70 p-4">
                  <p className="text-sm text-muted-foreground">Aktif Ürün</p>
                  <p className="mt-2 text-xl font-semibold">{summary?.activeProducts || 0}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="border-border/70">
            <CardHeader className="space-y-2">
              <CardTitle>Operasyon Sağlığı</CardTitle>
              <CardDescription>Satıcı panelinde ilk bakışta önemli olan kalite sinyalleri.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between rounded-2xl border border-border/70 px-4 py-3">
                <div className="flex items-center gap-3">
                  <div className="rounded-xl bg-emerald-500/10 p-2 text-emerald-600 dark:text-emerald-300">
                    <Package className="h-4 w-4" />
                  </div>
                  <div>
                    <p className="font-medium">Ürün Aktivasyonu</p>
                    <p className="text-sm text-muted-foreground">Aktif ürün / toplam ürün</p>
                  </div>
                </div>
                <span className="font-semibold">
                  {summary?.activeProducts || 0} / {summary?.totalProducts || 0}
                </span>
              </div>

              <div className="flex items-center justify-between rounded-2xl border border-border/70 px-4 py-3">
                <div className="flex items-center gap-3">
                  <div className="rounded-xl bg-rose-500/10 p-2 text-rose-600 dark:text-rose-300">
                    <Activity className="h-4 w-4" />
                  </div>
                  <div>
                    <p className="font-medium">Favori / Sipariş Dengesi</p>
                    <p className="text-sm text-muted-foreground">Talep ve dönüşüm performansı</p>
                  </div>
                </div>
                <span className="font-semibold">
                  {summary?.totalWishlistCount || 0} / {summary?.successfulOrderCount || 0}
                </span>
              </div>

              <div className="flex items-center justify-between rounded-2xl border border-border/70 px-4 py-3">
                <div className="flex items-center gap-3">
                  <div className="rounded-xl bg-fuchsia-500/10 p-2 text-fuchsia-600 dark:text-fuchsia-300">
                    <Star className="h-4 w-4" />
                  </div>
                  <div>
                    <p className="font-medium">Puan Kalitesi</p>
                    <p className="text-sm text-muted-foreground">Değerlendirme ve geri iade sinyali</p>
                  </div>
                </div>
                <span className="font-semibold">
                  {summary ? `${summary.averageRating.toFixed(2)} / 5` : '-'}
                </span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
