import { useRef } from 'react';
import { ChevronLeft, ChevronRight, Sparkles } from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { ProductFeedCard } from '@/components/products/ProductFeedCard';
import { cn } from '@/lib/utils';
import type { Product } from '@/features/products/types';

type InlineRailTone = 'personalized' | 'wishlisted';

interface InlineProductRailProps {
  title: string;
  badgeText: string;
  helperText?: string;
  description?: string;
  tone: InlineRailTone;
  products: Product[];
  isLoading?: boolean;
  isAddingToCart: boolean;
  isInWishlist: (productId: number) => boolean;
  onAddToCart: (productId: number, productName: string) => void;
  onWishlistToggle: (event: React.MouseEvent, productId: number) => void;
  className?: string;
}

const railToneClasses: Record<InlineRailTone, string> = {
  personalized:
    'border-sky-500/20 bg-[radial-gradient(circle_at_top_left,_rgba(56,189,248,0.14),_transparent_42%),radial-gradient(circle_at_top_right,_rgba(16,185,129,0.10),_transparent_44%),linear-gradient(145deg,_rgba(18,24,38,0.94),_rgba(11,15,24,0.96))] text-white/95 shadow-[0_10px_24px_rgba(2,6,23,0.28)]',
  wishlisted:
    'border-rose-500/20 bg-[radial-gradient(circle_at_top_left,_rgba(244,63,94,0.12),_transparent_42%),radial-gradient(circle_at_top_right,_rgba(251,146,60,0.08),_transparent_44%),linear-gradient(145deg,_rgba(32,19,24,0.93),_rgba(20,12,18,0.95))] text-white/95 shadow-[0_10px_24px_rgba(15,23,42,0.24)]',
};

export function InlineProductRail({
  title,
  badgeText,
  helperText,
  description,
  tone,
  products,
  isLoading = false,
  isAddingToCart,
  isInWishlist,
  onAddToCart,
  onWishlistToggle,
  className,
}: InlineProductRailProps) {
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const scrollByAmount = (direction: 'left' | 'right') => {
    if (!scrollContainerRef.current) {
      return;
    }

    const amount = direction === 'left' ? -320 : 320;
    scrollContainerRef.current.scrollBy({ left: amount, behavior: 'smooth' });
  };

  return (
    <div className={cn('col-span-full', className)}>
      <section className={cn('relative overflow-hidden rounded-2xl border', railToneClasses[tone])}>
        <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(255,255,255,0.06),transparent_35%,transparent_100%)]" />

        <header className="relative border-b border-white/10 px-4 py-3 sm:px-5">
          <div className="flex items-center justify-between gap-3">
            <div className="min-w-0">
              <div className="mb-1.5 flex items-center gap-2">
                <Badge className="border border-white/15 bg-white/10 text-[11px] font-medium text-white/90">
                  {badgeText}
                </Badge>
              </div>
              <h3 className="truncate text-base font-semibold sm:text-lg">{title}</h3>
              {description ? <p className="mt-1 truncate text-xs text-white/65">{description}</p> : null}
            </div>

            {helperText ? (
              <div className="hidden shrink-0 items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3 py-1 text-xs text-white/80 md:flex">
                <Sparkles className="h-3.5 w-3.5" />
                <span>{helperText}</span>
              </div>
            ) : null}
          </div>
        </header>

        <div className="relative px-3 py-3 sm:px-4">
          <div className="absolute right-4 top-0 hidden -translate-y-1/2 items-center gap-2 md:flex">
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="h-8 w-8 border-white/20 bg-black/20 text-white hover:bg-black/40"
              onClick={() => scrollByAmount('left')}
              aria-label={`${title} satırını sola kaydır`}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="h-8 w-8 border-white/20 bg-black/20 text-white hover:bg-black/40"
              onClick={() => scrollByAmount('right')}
              aria-label={`${title} satırını sağa kaydır`}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>

          <div
            ref={scrollContainerRef}
            className="flex snap-x snap-mandatory gap-3 overflow-x-auto px-1 pb-1 [scrollbar-width:thin]"
          >
            {isLoading
              ? Array.from({ length: 4 }).map((_, index) => (
                  <Card
                    key={`rail-skeleton-${index}`}
                    className="h-[260px] w-[min(80vw,16rem)] shrink-0 rounded-xl border-white/10 bg-white/[0.04] py-0"
                  >
                    <Skeleton className="h-32 w-full rounded-b-none rounded-t-xl bg-white/10" />
                    <div className="space-y-2 p-3">
                      <Skeleton className="h-4 w-3/4 bg-white/10" />
                      <Skeleton className="h-3 w-1/2 bg-white/10" />
                      <Skeleton className="h-8 w-full bg-white/10" />
                    </div>
                  </Card>
                ))
              : products.map((product) => (
                  <div key={product.id} className="snap-start">
                    <ProductFeedCard
                      product={product}
                      variant="compact"
                      isAddingToCart={isAddingToCart}
                      isInWishlist={isInWishlist(product.id)}
                      onAddToCart={onAddToCart}
                      onWishlistToggle={onWishlistToggle}
                    />
                  </div>
                ))}
          </div>
        </div>
      </section>
    </div>
  );
}
