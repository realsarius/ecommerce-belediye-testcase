import { Heart, Sparkles, Package } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import { useSearchProductsQuery } from '@/features/products/productsApi';

export function TopWishlistedProducts() {
  const { data, isLoading } = useSearchProductsQuery({
    page: 1,
    pageSize: 4,
    sortBy: 'wishlistCount',
    sortDescending: true,
  });

  const items = (data?.items ?? []).filter((item) => item.wishlistCount > 0).slice(0, 4);

  if (!isLoading && items.length === 0) {
    return null;
  }

  return (
    <section className="mb-8 overflow-hidden rounded-3xl border border-rose-200/60 bg-gradient-to-br from-rose-50 via-background to-orange-50">
      <div className="border-b border-rose-200/60 px-6 py-5">
        <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <Badge variant="secondary" className="mb-3 bg-rose-100 text-rose-700">
              Bu hafta öne çıkanlar
            </Badge>
            <h2 className="text-2xl font-bold tracking-tight">En Çok Favorilenenler</h2>
            <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
              Kullanıcıların tekrar tekrar favorilerine eklediği ürünleri keşfedin.
            </p>
          </div>
          <div className="flex items-center gap-2 text-sm text-rose-700">
            <Sparkles className="h-4 w-4" />
            <span>Sosyal kanıtla öne çıkan seçimler</span>
          </div>
        </div>
      </div>

      <div className="grid gap-4 p-6 md:grid-cols-2 xl:grid-cols-4">
        {isLoading
          ? Array.from({ length: 4 }).map((_, index) => (
              <Card key={index} className="border-white/70 bg-white/70 backdrop-blur">
                <CardContent className="space-y-4 p-5">
                  <Skeleton className="h-5 w-24" />
                  <Skeleton className="h-12 w-full" />
                  <Skeleton className="h-4 w-32" />
                  <Skeleton className="h-10 w-full" />
                </CardContent>
              </Card>
            ))
          : items.map((product, index) => (
              <Link key={product.id} to={`/products/${product.id}`} className="block">
                <Card className="h-full border-white/70 bg-white/80 transition-transform duration-200 hover:-translate-y-1 hover:shadow-lg">
                  <CardContent className="flex h-full flex-col p-5">
                    <div className="mb-4 flex items-start justify-between gap-3">
                      <Badge className="bg-foreground text-background">
                        #{index + 1}
                      </Badge>
                      <div className="flex items-center gap-1 rounded-full bg-rose-100 px-2.5 py-1 text-xs font-medium text-rose-700">
                        <Heart className="h-3.5 w-3.5 fill-current" />
                        {product.wishlistCount}
                      </div>
                    </div>

                    <div className="mb-4 flex h-24 items-center justify-center rounded-2xl bg-muted/50">
                      <Package className="h-10 w-10 text-muted-foreground" />
                    </div>

                    <div className="flex-1">
                      <h3 className="line-clamp-2 font-semibold">{product.name}</h3>
                      <p className="mt-1 text-sm text-muted-foreground">{product.categoryName}</p>
                    </div>

                    <div className="mt-5">
                      <p className="text-lg font-bold">
                        {product.price.toLocaleString('tr-TR')} {product.currency}
                      </p>
                      <p className="mt-1 text-xs text-muted-foreground">
                        {product.wishlistCount} kişi favorilerine ekledi
                      </p>
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
      </div>
    </section>
  );
}
