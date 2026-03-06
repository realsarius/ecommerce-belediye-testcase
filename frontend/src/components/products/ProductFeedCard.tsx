import { Link } from 'react-router-dom';
import { GitCompareArrows, Heart, ShoppingCart } from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardFooter } from '@/components/common/card';
import { CampaignCountdown } from '@/components/campaigns/CampaignCountdown';
import { ProductCardMediaPreview } from '@/components/products/ProductCardMediaPreview';
import { cn } from '@/lib/utils';
import type { Product } from '@/features/products/types';

type ProductFeedCardVariant = 'default' | 'compact';

interface ProductFeedCardProps {
  product: Product;
  variant?: ProductFeedCardVariant;
  className?: string;
  isAddingToCart: boolean;
  isInWishlist: boolean;
  isInCompare?: boolean;
  onAddToCart: (productId: number, productName: string) => void;
  onWishlistToggle: (event: React.MouseEvent, productId: number) => void;
  onCompareToggle?: (event: React.MouseEvent, productId: number, productName: string) => void;
}

export function ProductFeedCard({
  product,
  variant = 'default',
  className,
  isAddingToCart,
  isInWishlist,
  isInCompare = false,
  onAddToCart,
  onWishlistToggle,
  onCompareToggle,
}: ProductFeedCardProps) {
  const isCompact = variant === 'compact';

  return (
    <Card
      className={cn(
        'group relative overflow-hidden border-border/70 bg-card/90',
        isCompact
          ? 'w-[min(74vw,15rem)] shrink-0 gap-2 rounded-lg py-0 shadow-xs dark:border-white/10 dark:bg-white/[0.03]'
          : 'w-full max-w-sm gap-4 py-0',
        className,
      )}
    >
      <div className={cn('relative flex items-center justify-center bg-muted/60', isCompact ? 'h-28' : 'h-48')}>
        <Link
          to={`/products/${product.id}`}
          aria-label={`${product.name} ürün detayına git`}
          className="block h-full w-full"
        >
          <ProductCardMediaPreview product={product} imgClassName={isCompact ? 'object-cover' : undefined} />
        </Link>

        {product.stockQuantity === 0 && (
          <Badge
            variant="destructive"
            className={cn('absolute left-2', isCompact ? 'top-1.5 text-[10px]' : 'top-2')}
          >
            Stokta Yok
          </Badge>
        )}

        <Button
          variant="ghost"
          size={isCompact ? 'icon-sm' : 'icon'}
          className={cn(
            'absolute z-20 rounded-full bg-background/55 backdrop-blur-sm hover:bg-background/80',
            isCompact ? 'right-1.5 top-1.5' : 'right-2 top-2',
          )}
          onClick={(event) => onWishlistToggle(event, product.id)}
          aria-label={isInWishlist ? 'Ürünü favorilerden çıkar' : 'Ürünü favorilere ekle'}
        >
          <Heart className={cn('h-5 w-5', isInWishlist ? 'fill-red-500 text-red-500' : 'text-foreground')} />
        </Button>
      </div>

      <CardContent className={cn(isCompact ? 'space-y-1 px-2.5 pt-2.5' : 'space-y-2 px-4 pt-4')}>
        <Link to={`/products/${product.id}`}>
          <h3
            className={cn(
              'font-semibold transition-colors group-hover:text-primary',
              isCompact ? 'line-clamp-1 text-[13px]' : 'truncate',
            )}
          >
            {product.name}
          </h3>
        </Link>

        <p className={cn('text-muted-foreground', isCompact ? 'truncate text-[11px]' : 'truncate text-sm')}>
          {product.categoryName}
        </p>

        {product.hasActiveCampaign && (
          <div className={cn('flex flex-wrap items-center gap-2', isCompact ? 'pt-0.5' : 'pt-1')}>
            <Badge className="bg-amber-500/10 text-amber-700 dark:text-amber-200">
              {product.campaignBadgeText || 'Kampanya'}
            </Badge>
            {!isCompact ? (
              <CampaignCountdown
                endsAt={product.campaignEndsAt}
                className="text-xs text-amber-700/80 dark:text-amber-200/80"
              />
            ) : null}
          </div>
        )}

        {!isCompact && product.wishlistCount > 0 && (
          <div className={cn('flex items-center gap-1 text-muted-foreground', isCompact ? 'text-[11px]' : 'text-xs')}>
            <Heart className={cn('text-red-500', isCompact ? 'h-3 w-3' : 'h-3.5 w-3.5')} />
            <span>{product.wishlistCount} kişi favoriledi</span>
          </div>
        )}

        {!isCompact && product.hasActiveCampaign && (
          <p className={cn('text-muted-foreground line-through', isCompact ? 'text-[11px]' : 'text-xs')}>
            {product.originalPrice.toLocaleString('tr-TR')} {product.currency}
          </p>
        )}

        <p className={cn('font-bold', isCompact ? 'text-sm' : 'text-lg')}>
          {product.price.toLocaleString('tr-TR')} {product.currency}
        </p>
      </CardContent>

      <CardFooter className={cn('px-4 pb-4 pt-0', isCompact ? 'px-2.5 pb-2.5' : 'flex-col gap-2')}>
        <Button
          size={isCompact ? 'sm' : 'default'}
          className={cn('w-full', isCompact && 'h-8 gap-1 text-[11px]')}
          disabled={product.stockQuantity === 0 || isAddingToCart}
          onClick={() => onAddToCart(product.id, product.name)}
        >
          <ShoppingCart className={cn(isCompact ? 'h-3.5 w-3.5' : 'mr-2 h-4 w-4')} />
          <span>{isCompact ? 'Sepete' : 'Sepete Ekle'}</span>
        </Button>

        {!isCompact && onCompareToggle ? (
          <Button
            variant={isInCompare ? 'secondary' : 'outline'}
            className="w-full"
            onClick={(event) => onCompareToggle(event, product.id, product.name)}
          >
            <GitCompareArrows className="mr-2 h-4 w-4" />
            {isInCompare ? 'Karşılaştırılıyor' : 'Karşılaştırmaya Ekle'}
          </Button>
        ) : null}
      </CardFooter>
    </Card>
  );
}
