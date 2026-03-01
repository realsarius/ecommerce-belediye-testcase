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
    <section className="relative mb-8 overflow-hidden rounded-[2rem] border border-white/12 bg-[radial-gradient(circle_at_top_left,_rgba(244,63,94,0.18),_transparent_24%),radial-gradient(circle_at_top_right,_rgba(251,146,60,0.12),_transparent_26%),linear-gradient(135deg,_rgba(24,24,27,0.96),_rgba(9,9,11,0.96))] shadow-[0_24px_80px_rgba(0,0,0,0.45)]">
      <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(255,255,255,0.06),transparent_22%,transparent_78%,rgba(255,255,255,0.03))]" />

      <div className="relative border-b border-white/10 px-6 py-5">
        <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <Badge
              variant="secondary"
              className="mb-3 border border-rose-400/20 bg-rose-500/10 text-rose-100"
            >
              Bu hafta öne çıkanlar
            </Badge>
            <h2 className="text-2xl font-bold tracking-tight text-white">En Çok Favorilenenler</h2>
            <p className="mt-2 max-w-2xl text-sm text-white/60">
              Kullanıcıların tekrar tekrar favorilerine eklediği ürünleri keşfedin.
            </p>
          </div>
          <div className="flex items-center gap-2 rounded-full border border-rose-400/15 bg-white/5 px-3 py-1.5 text-sm text-rose-100/90 backdrop-blur-sm">
            <Sparkles className="h-4 w-4" />
            <span>Sosyal kanıtla öne çıkan seçimler</span>
          </div>
        </div>
      </div>

      <div className="relative grid gap-4 p-6 md:grid-cols-2 xl:grid-cols-4">
        {isLoading
          ? Array.from({ length: 4 }).map((_, index) => (
              <Card
                key={index}
                className="border-white/10 bg-white/[0.04] text-white shadow-none backdrop-blur-sm"
              >
                <CardContent className="space-y-4 p-5">
                  <Skeleton className="h-5 w-24 bg-white/10" />
                  <Skeleton className="h-12 w-full bg-white/10" />
                  <Skeleton className="h-4 w-32 bg-white/10" />
                  <Skeleton className="h-10 w-full bg-white/10" />
                </CardContent>
              </Card>
            ))
          : items.map((product, index) => (
              <Link key={product.id} to={`/products/${product.id}`} className="block">
                <Card className="h-full border-white/10 bg-white/[0.04] text-white shadow-none backdrop-blur-sm transition-all duration-200 hover:-translate-y-1.5 hover:border-rose-400/30 hover:bg-white/[0.06] hover:shadow-[0_18px_40px_rgba(0,0,0,0.35)]">
                  <CardContent className="flex h-full flex-col p-5">
                    <div className="mb-4 flex items-start justify-between gap-3">
                      <Badge className="border border-white/10 bg-white/10 text-white">
                        #{index + 1}
                      </Badge>
                      <div className="flex items-center gap-1 rounded-full border border-rose-400/20 bg-rose-500/10 px-2.5 py-1 text-xs font-medium text-rose-100">
                        <Heart className="h-3.5 w-3.5 fill-current" />
                        {product.wishlistCount}
                      </div>
                    </div>

                    <div className="mb-4 flex h-24 items-center justify-center rounded-2xl border border-white/8 bg-black/20">
                      <Package className="h-10 w-10 text-white/35" />
                    </div>

                    <div className="flex-1">
                      <h3 className="line-clamp-2 font-semibold text-white/95">{product.name}</h3>
                      <p className="mt-1 text-sm text-white/50">{product.categoryName}</p>
                    </div>

                    <div className="mt-5">
                      <p className="text-lg font-bold text-white">
                        {product.price.toLocaleString('tr-TR')} {product.currency}
                      </p>
                      <p className="mt-1 text-xs text-white/45">
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
