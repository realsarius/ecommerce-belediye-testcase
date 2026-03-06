import { useCallback, useEffect, useRef, useState } from 'react';
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
    'border-sky-500/15 bg-[linear-gradient(180deg,rgba(56,189,248,0.12),rgba(6,10,18,0.88)_55%)] text-white/95 shadow-[0_6px_16px_rgba(2,6,23,0.18)]',
  wishlisted:
    'border-rose-500/15 bg-[linear-gradient(180deg,rgba(244,63,94,0.11),rgba(20,10,16,0.90)_55%)] text-white/95 shadow-[0_6px_16px_rgba(15,23,42,0.16)]',
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
  const [hasOverflow, setHasOverflow] = useState(false);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const updateScrollState = useCallback(() => {
    const container = scrollContainerRef.current;
    if (!container) {
      return;
    }

    const maxLeft = container.scrollWidth - container.clientWidth;
    const hasScrollableContent = maxLeft > 2;

    setHasOverflow(hasScrollableContent);
    setCanScrollLeft(container.scrollLeft > 2);
    setCanScrollRight(container.scrollLeft < maxLeft - 2);
  }, []);

  useEffect(() => {
    updateScrollState();

    const handleResize = () => {
      updateScrollState();
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
    // Scroll dimensions depend on rendered cards and loading skeletons.
  }, [products.length, isLoading, updateScrollState]);

  const scrollByAmount = (direction: 'left' | 'right') => {
    if (!scrollContainerRef.current || !hasOverflow) {
      return;
    }

    const amount = direction === 'left' ? -320 : 320;
    scrollContainerRef.current.scrollBy({ left: amount, behavior: 'smooth' });
  };

  return (
    <div className={cn('col-span-full', className)}>
      <section className={cn('relative overflow-hidden rounded-xl border', railToneClasses[tone])}>
        <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(255,255,255,0.04),transparent_40%,transparent_100%)]" />

        <header className="relative border-b border-white/8 px-4 py-3 sm:px-5">
          <div className="flex items-center justify-between gap-3">
            <div className="min-w-0">
              <div className="flex items-center gap-2 sm:mb-1">
                <Badge className="border border-white/15 bg-white/10 text-[11px] font-medium text-white/90">
                  {badgeText}
                </Badge>
                <h3 className="truncate text-sm font-semibold sm:hidden">{title}</h3>
              </div>
              <h3 className="hidden truncate text-base font-semibold sm:block">{title}</h3>
              {description ? (
                <p className="mt-0.5 hidden truncate text-[11px] text-white/65 sm:block">
                  {description}
                </p>
              ) : null}
            </div>

            {helperText ? (
              <div className="hidden shrink-0 items-center gap-2 rounded-full border border-white/12 bg-white/[0.08] px-3 py-1 text-xs text-white/75 md:flex">
                <Sparkles className="h-3.5 w-3.5" />
                <span>{helperText}</span>
              </div>
            ) : null}
          </div>
        </header>

        <div className="relative px-3 py-2 sm:px-4">
          <div className={cn(
            'absolute right-4 top-0 hidden -translate-y-1/2 items-center gap-2 md:flex',
            !hasOverflow && 'md:hidden',
          )}
          >
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="h-8 w-8 border-white/15 bg-black/25 text-white hover:bg-black/35"
              onClick={() => scrollByAmount('left')}
              disabled={!canScrollLeft}
              aria-label={`${title} satırını sola kaydır`}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="h-8 w-8 border-white/15 bg-black/25 text-white hover:bg-black/35"
              onClick={() => scrollByAmount('right')}
              disabled={!canScrollRight}
              aria-label={`${title} satırını sağa kaydır`}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>

          <div
            ref={scrollContainerRef}
            onScroll={updateScrollState}
            className="flex snap-x snap-mandatory gap-3 overflow-x-auto px-1 pb-1 [scrollbar-width:thin]"
          >
            {isLoading
              ? Array.from({ length: 4 }).map((_, index) => (
                  <Card
                    key={`rail-skeleton-${index}`}
                    className="h-[212px] w-[min(74vw,15rem)] shrink-0 rounded-lg border-white/10 bg-white/[0.04] py-0"
                  >
                    <Skeleton className="h-28 w-full rounded-b-none rounded-t-lg bg-white/10" />
                    <div className="space-y-1.5 p-2.5">
                      <Skeleton className="h-4 w-3/4 bg-white/10" />
                      <Skeleton className="h-3 w-1/2 bg-white/10" />
                      <Skeleton className="h-7 w-full bg-white/10" />
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
