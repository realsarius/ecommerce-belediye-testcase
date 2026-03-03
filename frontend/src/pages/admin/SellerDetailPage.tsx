import { Link, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  BadgeCheck,
  Boxes,
  Globe,
  Mail,
  Package,
  Phone,
  Store,
  Wallet,
} from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/common/avatar';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/common/table';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { useGetAdminSellerDetailQuery } from '@/features/admin/adminApi';
import type { AdminSellerStatus } from '@/features/admin/types';

function getInitials(value: string) {
  return value
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('tr-TR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function formatCurrency(value: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

function getStatusTone(status: AdminSellerStatus) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Pending':
      return 'warning';
    case 'Suspended':
      return 'danger';
    case 'Closed':
      return 'neutral';
    default:
      return 'neutral';
  }
}

function getStatusLabel(status: AdminSellerStatus) {
  switch (status) {
    case 'Active':
      return 'Aktif';
    case 'Pending':
      return 'Başvuru';
    case 'Suspended':
      return 'Askıda';
    case 'Closed':
      return 'Kapalı';
    default:
      return status;
  }
}

export default function SellerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const sellerId = Number(id);
  const { data: seller, isLoading } = useGetAdminSellerDetailQuery(sellerId, {
    skip: !sellerId,
  });

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-44" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-36 rounded-xl" />
          ))}
        </div>
        <Skeleton className="h-[420px] rounded-xl" />
      </div>
    );
  }

  if (!seller) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" asChild>
          <Link to="/admin/sellers">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Seller listesine dön
          </Link>
        </Button>
        <Card className="border-border/70">
          <CardContent className="flex flex-col items-center gap-4 p-12 text-center">
            <Store className="h-12 w-12 text-muted-foreground" />
            <div className="space-y-1">
              <p className="text-xl font-semibold">Seller profili bulunamadı</p>
              <p className="text-muted-foreground">Bu seller kaydı mevcut değil veya admin endpoint’inden dönmüyor.</p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Button variant="ghost" asChild>
        <Link to="/admin/sellers">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Seller listesine dön
        </Link>
      </Button>

      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div className="flex items-center gap-4">
          <Avatar className="h-16 w-16">
            {seller.logoUrl ? <AvatarImage src={seller.logoUrl} alt={seller.brandName} /> : null}
            <AvatarFallback>{getInitials(seller.brandName)}</AvatarFallback>
          </Avatar>
          <div className="space-y-2">
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-3xl font-bold tracking-tight">{seller.brandName}</h1>
              <StatusBadge label={getStatusLabel(seller.status)} tone={getStatusTone(seller.status)} />
              {seller.isVerified ? (
                <Badge variant="default">Doğrulanmış</Badge>
              ) : (
                <Badge variant="secondary">Doğrulama bekliyor</Badge>
              )}
            </div>
            <p className="text-muted-foreground">
              {seller.sellerFirstName} {seller.sellerLastName} · Seller ID #{seller.id}
            </p>
            <p className="text-sm text-muted-foreground">{seller.ownerEmail}</p>
          </div>
        </div>

        <Card className="w-full max-w-md border-border/70 bg-muted/20">
          <CardContent className="space-y-3 p-5 text-sm">
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Komisyon Oranı</span>
              <span className="font-medium">%{seller.commissionRate.toLocaleString('tr-TR')}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Override</span>
              <span className="font-medium">
                {seller.commissionRateOverride == null ? 'Yok' : `%${seller.commissionRateOverride.toLocaleString('tr-TR')}`}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Başvuru İncelendi</span>
              <span className="font-medium">
                {seller.applicationReviewedAt ? formatDate(seller.applicationReviewedAt) : 'Henüz değil'}
              </span>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Toplam Ürün"
          value={seller.productCount.toLocaleString('tr-TR')}
          helperText="Bu seller’a bağlı katalog ürünleri."
          icon={Package}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Ürün"
          value={seller.activeProductCount.toLocaleString('tr-TR')}
          helperText="Şu anda satışa açık ürün sayısı."
          icon={BadgeCheck}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Toplam Stok"
          value={seller.totalStock.toLocaleString('tr-TR')}
          helperText="Tüm seller ürünlerindeki stok toplamı."
          icon={Boxes}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Toplam Satış"
          value={formatCurrency(seller.totalSales, seller.currency)}
          helperText={`Ortalama puan ${seller.averageRating.toFixed(1)} / 5`}
          icon={Wallet}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.8fr_1.2fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Mağaza Bilgileri</CardTitle>
            <CardDescription>Admin seller detail endpoint’inden gelen gerçek profil detayları.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            {seller.bannerImageUrl ? (
              <div
                className="h-36 rounded-2xl border bg-cover bg-center"
                style={{ backgroundImage: `url(${seller.bannerImageUrl})` }}
              />
            ) : null}
            <div className="flex items-center justify-between gap-4">
              <span className="text-muted-foreground">Sahibi</span>
              <span className="font-medium">{seller.sellerFirstName} {seller.sellerLastName}</span>
            </div>
            <div className="flex items-center justify-between gap-4">
              <span className="text-muted-foreground">Kullanıcı ID</span>
              <span className="font-medium">{seller.userId}</span>
            </div>
            <div className="flex items-center justify-between gap-4">
              <span className="text-muted-foreground">Oluşturulma</span>
              <span className="font-medium">{formatDate(seller.createdAt)}</span>
            </div>
            <div className="space-y-2">
              <span className="text-muted-foreground">Açıklama</span>
              <p className="rounded-xl border bg-muted/30 p-4 leading-6">
                {seller.brandDescription || 'Açıklama girilmemiş.'}
              </p>
            </div>
            <div className="space-y-3 rounded-2xl border bg-muted/20 p-4">
              <p className="font-medium">İletişim</p>
              <div className="flex items-center gap-2 text-muted-foreground">
                <Mail className="h-4 w-4 text-amber-600" />
                <span>{seller.contactEmail || 'İletişim e-postası girilmemiş.'}</span>
              </div>
              <div className="flex items-center gap-2 text-muted-foreground">
                <Phone className="h-4 w-4 text-amber-600" />
                <span>{seller.contactPhone || 'Telefon bilgisi girilmemiş.'}</span>
              </div>
              <div className="flex items-center gap-2 text-muted-foreground">
                <Globe className="h-4 w-4 text-amber-600" />
                <span>{seller.websiteUrl || 'Web sitesi girilmemiş.'}</span>
              </div>
            </div>
            {seller.applicationReviewNote ? (
              <div className="space-y-2 rounded-2xl border bg-muted/20 p-4">
                <p className="font-medium">Başvuru Notu</p>
                <p className="leading-6 text-muted-foreground">{seller.applicationReviewNote}</p>
              </div>
            ) : null}
            <div className="grid gap-3 sm:grid-cols-3">
              <div className="rounded-xl border bg-muted/20 p-3">
                <p className="text-xs uppercase tracking-wide text-muted-foreground">Instagram</p>
                <p className="mt-2 break-all font-medium">{seller.instagramUrl || 'Yok'}</p>
              </div>
              <div className="rounded-xl border bg-muted/20 p-3">
                <p className="text-xs uppercase tracking-wide text-muted-foreground">Facebook</p>
                <p className="mt-2 break-all font-medium">{seller.facebookUrl || 'Yok'}</p>
              </div>
              <div className="rounded-xl border bg-muted/20 p-3">
                <p className="text-xs uppercase tracking-wide text-muted-foreground">X</p>
                <p className="mt-2 break-all font-medium">{seller.xUrl || 'Yok'}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Seller Ürünleri</CardTitle>
            <CardDescription>Detay endpoint’i üzerinden gelen seller ürün özeti.</CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Kategori</TableHead>
                  <TableHead>Fiyat</TableHead>
                  <TableHead>Stok</TableHead>
                  <TableHead>Durum</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {seller.products.map((product) => (
                  <TableRow key={product.productId}>
                    <TableCell>
                      <div>
                        <p className="font-medium">{product.productName}</p>
                        <p className="text-sm text-muted-foreground">{product.averageRating.toFixed(1)} / 5</p>
                      </div>
                    </TableCell>
                    <TableCell>{product.categoryName}</TableCell>
                    <TableCell>{formatCurrency(product.price, product.currency)}</TableCell>
                    <TableCell>{product.stockQuantity}</TableCell>
                    <TableCell>
                      <Badge variant={product.isActive ? 'default' : 'secondary'}>
                        {product.isActive ? 'Aktif' : 'Pasif'}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
                {seller.products.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="py-12 text-center text-muted-foreground">
                      Bu seller için katalogda ürün görünmüyor.
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
