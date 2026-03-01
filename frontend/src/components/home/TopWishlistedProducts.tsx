import { Heart, Sparkles, Package } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import { useSearchProductsQuery } from '@/features/products/productsApi';
import { CampaignCountdown } from '@/components/campaigns/CampaignCountdown';

interface TopWishlistedProductsProps {
  categoryId?: number;
  categoryName?: string;
}

export function TopWishlistedProducts({
  categoryId,
  categoryName,
}: TopWishlistedProductsProps) {
  const { data, isLoading } = useSearchProductsQuery({
    page: 1,
    pageSize: 4,
    categoryId,
    sortBy: 'wishlistCount',
    sortDescending: true,
  });

  const items = (data?.items ?? []).filter((item) => item.wishlistCount > 0).slice(0, 4);
  const isCategoryContext = Boolean(categoryId && categoryName);

  const badgeText = isCategoryContext
    ? `${categoryName} kategorisinde öne çıkanlar`
    : 'Bu hafta öne çıkanlar';

  const title = isCategoryContext
    ? `${categoryName} kategorisinde en çok favorilenenler`
    : 'En Çok Favorilenenler';

  const description = isCategoryContext
    ? `${categoryName} kategorisinde kullanıcıların en çok favorilerine eklediği ürünleri keşfedin.`
    : 'Kullanıcıların tekrar tekrar favorilerine eklediği ürünleri keşfedin.';

  const socialProofText = isCategoryContext
    ? `${categoryName} kategorisinde öne çıkan seçimler`
    : 'Sosyal kanıtla öne çıkan seçimler';

  if (!isLoading && items.length === 0) {
    return null;
  }

  return (
    <section className="relative mb-8 overflow-hidden rounded-[2rem] border border-border/70 bg-[radial-gradient(circle_at_top_left,_rgba(244,63,94,0.10),_transparent_22%),radial-gradient(circle_at_top_right,_rgba(59,130,246,0.08),_transparent_24%),linear-gradient(180deg,rgba(255,255,255,0.86),rgba(255,255,255,0.72))] shadow-[0_18px_50px_rgba(15,23,42,0.08)] dark:border-white/12 dark:bg-[radial-gradient(circle_at_top_left,_rgba(244,63,94,0.14),_transparent_24%),radial-gradient(circle_at_top_right,_rgba(251,146,60,0.10),_transparent_26%),linear-gradient(135deg,_rgba(24,24,27,0.96),_rgba(9,9,11,0.96))] dark:shadow-[0_24px_80px_rgba(0,0,0,0.45)]">
      <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(255,255,255,0.26),transparent_22%,transparent_78%,rgba(255,255,255,0.08))] dark:bg-[linear-gradient(180deg,rgba(255,255,255,0.06),transparent_22%,transparent_78%,rgba(255,255,255,0.03))]" />

      <div className="relative border-b border-border/70 px-6 py-5 dark:border-white/10">
        <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <Badge
              variant="secondary"
              className="mb-3 border border-rose-500/20 bg-rose-500/10 text-rose-700 dark:text-rose-100"
            >
              {badgeText}
            </Badge>
            <h2 className="text-2xl font-bold tracking-tight text-foreground dark:text-white">{title}</h2>
            <p className="mt-2 max-w-2xl text-sm text-muted-foreground dark:text-white/60">
              {description}
            </p>
          </div>
          <div className="flex items-center gap-2 rounded-full border border-rose-500/15 bg-background/70 px-3 py-1.5 text-sm text-rose-700/90 backdrop-blur-sm dark:border-rose-400/15 dark:bg-white/5 dark:text-rose-100/90">
            <Sparkles className="h-4 w-4" />
            <span>{socialProofText}</span>
          </div>
        </div>
      </div>

      <div className="relative grid gap-4 p-6 md:grid-cols-2 xl:grid-cols-4">
        {isLoading
          ? Array.from({ length: 4 }).map((_, index) => (
              <Card
                key={index}
                className="border-border/70 bg-card/80 text-card-foreground shadow-sm backdrop-blur-sm dark:border-white/10 dark:bg-white/[0.04] dark:text-white dark:shadow-none"
              >
                <CardContent className="space-y-4 p-5">
                  <Skeleton className="h-5 w-24 bg-black/8 dark:bg-white/10" />
                  <Skeleton className="h-12 w-full bg-black/8 dark:bg-white/10" />
                  <Skeleton className="h-4 w-32 bg-black/8 dark:bg-white/10" />
                  <Skeleton className="h-10 w-full bg-black/8 dark:bg-white/10" />
                </CardContent>
              </Card>
            ))
          : items.map((product, index) => (
              <Link key={product.id} to={`/products/${product.id}`} className="block">
                <Card className="h-full border-border/70 bg-card/80 text-card-foreground shadow-sm backdrop-blur-sm transition-all duration-200 hover:-translate-y-1 hover:border-rose-500/25 hover:shadow-[0_18px_36px_rgba(15,23,42,0.10)] dark:border-white/10 dark:bg-white/[0.04] dark:text-white dark:shadow-none dark:hover:-translate-y-1.5 dark:hover:border-rose-400/30 dark:hover:bg-white/[0.06] dark:hover:shadow-[0_18px_40px_rgba(0,0,0,0.35)]">
                  <CardContent className="flex h-full flex-col p-5">
                    <div className="mb-4 flex items-start justify-between gap-3">
                      <Badge className="border border-border/70 bg-background/70 text-foreground dark:border-white/10 dark:bg-white/10 dark:text-white">
                        #{index + 1}
                      </Badge>
                      <div className="flex items-center gap-1 rounded-full border border-rose-500/20 bg-rose-500/10 px-2.5 py-1 text-xs font-medium text-rose-700 dark:text-rose-100">
                        <Heart className="h-3.5 w-3.5 fill-current" />
                        {product.wishlistCount}
                      </div>
                    </div>

                    <div className="mb-4 flex h-24 items-center justify-center rounded-2xl border border-border/60 bg-muted/60 dark:border-white/8 dark:bg-black/20">
                      <Package className="h-10 w-10 text-muted-foreground/70 dark:text-white/35" />
                    </div>

                    <div className="flex-1">
                      <h3 className="line-clamp-2 font-semibold text-foreground dark:text-white/95">{product.name}</h3>
                      <p className="mt-1 text-sm text-muted-foreground dark:text-white/50">{product.categoryName}</p>
                    </div>

                    <div className="mt-5">
                      {product.hasActiveCampaign && (
                        <>
                          <div className="mb-2 flex flex-wrap items-center gap-2">
                            <Badge className="border border-amber-500/20 bg-amber-500/10 text-amber-700 dark:text-amber-100">
                              {product.campaignBadgeText || 'Kampanya'}
                            </Badge>
                            <CampaignCountdown
                              endsAt={product.campaignEndsAt}
                              className="text-[11px] text-amber-700/80 dark:text-amber-100/75"
                            />
                          </div>
                          <p className="text-xs text-muted-foreground line-through dark:text-white/40">
                            {product.originalPrice.toLocaleString('tr-TR')} {product.currency}
                          </p>
                        </>
                      )}
                      <p className="text-lg font-bold text-foreground dark:text-white">
                        {product.price.toLocaleString('tr-TR')} {product.currency}
                      </p>
                      <p className="mt-1 text-xs text-muted-foreground dark:text-white/45">
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
