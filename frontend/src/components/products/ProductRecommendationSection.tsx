import { Link } from 'react-router-dom';
import { Heart, Package, Sparkles } from 'lucide-react';
import { Card, CardContent, CardHeader } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import type { Product } from '@/features/products/types';

type RecommendationSource = 'also-viewed' | 'frequently-bought' | 'for-you';

interface ProductRecommendationSectionProps {
  title: string;
  description: string;
  source: RecommendationSource;
  products: Product[];
  isLoading?: boolean;
  onProductClick?: (productId: number, source: RecommendationSource) => void;
}

export function ProductRecommendationSection({
  title,
  description,
  source,
  products,
  isLoading,
  onProductClick,
}: ProductRecommendationSectionProps) {
  if (isLoading) {
    return (
      <section className="space-y-4">
        <div>
          <div className="mb-2 h-6 w-56 animate-pulse rounded bg-muted" />
          <div className="h-4 w-80 animate-pulse rounded bg-muted" />
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Card key={index} className="border-border/60 bg-background/60">
              <CardHeader>
                <Skeleton className="h-5 w-24" />
              </CardHeader>
              <CardContent className="space-y-3">
                <Skeleton className="h-24 w-full rounded-xl" />
                <Skeleton className="h-5 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
              </CardContent>
            </Card>
          ))}
        </div>
      </section>
    );
  }

  if (products.length === 0) {
    return null;
  }

  return (
    <section className="space-y-4">
      <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <div className="mb-3 flex items-center gap-2">
            <Badge variant="outline" className="rounded-full px-3 py-1 text-xs">
              Öneri Motoru
            </Badge>
            <Badge variant="secondary" className="gap-1">
              <Sparkles className="h-3.5 w-3.5" />
              Akıllı seçimler
            </Badge>
          </div>
          <h2 className="text-2xl font-semibold tracking-tight">{title}</h2>
          <p className="mt-2 text-sm text-muted-foreground">{description}</p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {products.map((product) => (
          <Link
            key={`${source}-${product.id}`}
            to={`/products/${product.id}`}
            className="block"
            onClick={() => onProductClick?.(product.id, source)}
          >
            <Card className="h-full border-border/60 bg-background/70 transition-transform duration-200 hover:-translate-y-1 hover:border-primary/40 hover:shadow-lg">
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between gap-2">
                  <Badge variant="outline">{product.categoryName}</Badge>
                  {product.wishlistCount > 0 && (
                    <div className="flex items-center gap-1 text-xs text-muted-foreground">
                      <Heart className="h-3.5 w-3.5 text-rose-500" />
                      {product.wishlistCount}
                    </div>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex h-28 items-center justify-center rounded-2xl border border-border/60 bg-muted/30">
                  <Package className="h-10 w-10 text-muted-foreground" />
                </div>
                <div className="space-y-2">
                  <h3 className="line-clamp-2 font-semibold">{product.name}</h3>
                  <p className="line-clamp-2 text-sm text-muted-foreground">{product.description}</p>
                </div>
                <div className="space-y-1">
                  <p className="text-lg font-bold">
                    {product.price.toLocaleString('tr-TR')} {product.currency}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {product.stockQuantity > 0 ? `${product.stockQuantity} adet stokta` : 'Geçici olarak stokta yok'}
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
