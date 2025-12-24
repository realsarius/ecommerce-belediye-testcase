import { Package, Store, TrendingUp, UserCheck } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Link } from 'react-router-dom';
import { useGetSellerProductsQuery, useGetSellerProfileQuery } from '@/features/seller/sellerApi';
import { Skeleton } from '@/components/common/skeleton';

export default function SellerDashboard() {
  const { data: products, isLoading: productsLoading } = useGetSellerProductsQuery({ page: 1, pageSize: 100 });
  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();

  const stats = [
    {
      title: 'Toplam Ürün',
      value: productsLoading ? '-' : (products?.totalCount || 0),
      icon: Package,
      color: 'text-blue-600',
      bgColor: 'bg-blue-100 dark:bg-blue-900',
    },
    {
      title: 'Aktif Ürün',
      value: productsLoading ? '-' : (products?.items?.filter((p) => p.isActive).length || 0),
      icon: TrendingUp,
      color: 'text-green-600',
      bgColor: 'bg-green-100 dark:bg-green-900',
    },
    {
      title: 'Marka',
      value: profileLoading ? '-' : (profile?.brandName || 'Tanımlanmadı'),
      icon: Store,
      color: 'text-amber-600',
      bgColor: 'bg-amber-100 dark:bg-amber-900',
    },
    {
      title: 'Onay Durumu',
      value: profileLoading ? '-' : (profile?.isVerified ? 'Onaylı' : 'Beklemede'),
      icon: UserCheck,
      color: profile?.isVerified ? 'text-green-600' : 'text-orange-600',
      bgColor: profile?.isVerified ? 'bg-green-100 dark:bg-green-900' : 'bg-orange-100 dark:bg-orange-900',
    },
  ];

  if (productsLoading || profileLoading) {
    return (
      <div>
        <h1 className="text-3xl font-bold mb-8">Satıcı Paneli</h1>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[1, 2, 3, 4].map((i) => (
            <Card key={i}>
              <CardHeader>
                <Skeleton className="h-4 w-24" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-8 w-16" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-3xl font-bold mb-8">Satıcı Paneli</h1>

      {/* Profile Warning */}
      {!profile && (
        <Card className="mb-6 border-amber-500 bg-amber-50 dark:bg-amber-950/30">
          <CardContent className="p-4 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Store className="h-5 w-5 text-amber-600" />
              <p>Ürün ekleyebilmek için önce marka profilinizi oluşturun.</p>
            </div>
            <Button asChild variant="default" className="bg-amber-600 hover:bg-amber-700">
              <Link to="/seller/profile">Profil Oluştur</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {stats.map((stat) => {
          const Icon = stat.icon;
          return (
            <Card key={stat.title}>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  {stat.title}
                </CardTitle>
                <div className={`p-2 rounded-lg ${stat.bgColor}`}>
                  <Icon className={`h-5 w-5 ${stat.color}`} />
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-2xl font-bold truncate">{stat.value}</p>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Hızlı İşlemler</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <Button asChild className="w-full justify-start" variant="outline" disabled={!profile}>
              <Link to="/seller/products/new">
                <Package className="mr-2 h-4 w-4" />
                Yeni Ürün Ekle
              </Link>
            </Button>
            <Button asChild className="w-full justify-start" variant="outline">
              <Link to="/seller/products">
                <TrendingUp className="mr-2 h-4 w-4" />
                Ürünlerimi Yönet
              </Link>
            </Button>
            <Button asChild className="w-full justify-start" variant="outline">
              <Link to="/seller/profile">
                <Store className="mr-2 h-4 w-4" />
                Marka Profilim
              </Link>
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Profil Bilgileri</CardTitle>
          </CardHeader>
          <CardContent>
            {profile ? (
              <div className="space-y-2">
                <p><span className="text-muted-foreground">Marka:</span> {profile.brandName}</p>
                <p><span className="text-muted-foreground">Açıklama:</span> {profile.brandDescription || '-'}</p>
                <p><span className="text-muted-foreground">Onay:</span> {profile.isVerified ? '✅ Onaylı' : '⏳ Beklemede'}</p>
              </div>
            ) : (
              <p className="text-muted-foreground">
                Henüz marka profili oluşturulmadı.
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
