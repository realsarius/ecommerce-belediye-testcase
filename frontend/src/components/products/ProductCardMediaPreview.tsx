import { useMemo, useState, type MouseEventHandler } from 'react';
import { Package } from 'lucide-react';
import { cn } from '@/lib/utils';
import { getProductImageUrls } from '@/features/products/imageResolver';
import type { Product } from '@/features/products/types';

type ProductCardMediaPreviewProps = {
  product: Pick<Product, 'name' | 'images' | 'primaryImageUrl'>;
  className?: string;
  imgClassName?: string;
};

export function ProductCardMediaPreview({
  product,
  className,
  imgClassName,
}: ProductCardMediaPreviewProps) {
  const imageUrls = useMemo(() => getProductImageUrls(product), [product]);
  const imageSignature = useMemo(() => imageUrls.join('|'), [imageUrls]);
  const [activeImageState, setActiveImageState] = useState({
    signature: imageSignature,
    index: 0,
  });

  const hasPreview = imageUrls.length > 1;
  const activeIndex = activeImageState.signature === imageSignature
    ? Math.min(activeImageState.index, Math.max(imageUrls.length - 1, 0))
    : 0;
  const activeImage = imageUrls[activeIndex] ?? imageUrls[0] ?? null;

  const handleMouseMove: MouseEventHandler<HTMLDivElement> = (event) => {
    if (!hasPreview) {
      return;
    }

    const rect = event.currentTarget.getBoundingClientRect();
    if (rect.width <= 0) {
      return;
    }

    const relativeX = event.clientX - rect.left;
    const zoneWidth = rect.width / imageUrls.length;
    const nextIndex = Math.min(
      imageUrls.length - 1,
      Math.max(0, Math.floor(relativeX / zoneWidth))
    );

    setActiveImageState((current) => {
      if (current.signature === imageSignature && current.index === nextIndex) {
        return current;
      }

      return {
        signature: imageSignature,
        index: nextIndex,
      };
    });
  };

  const resetToPrimary = () => {
    if (activeIndex !== 0 || activeImageState.signature !== imageSignature) {
      setActiveImageState({
        signature: imageSignature,
        index: 0,
      });
    }
  };

  return (
    <div
      className={cn('relative h-full w-full overflow-hidden', className)}
      onMouseMove={handleMouseMove}
      onMouseLeave={resetToPrimary}
      onBlur={resetToPrimary}
    >
      {activeImage ? (
        <img
          src={activeImage}
          alt={product.name}
          loading="lazy"
          decoding="async"
          className={cn('h-full w-full object-cover', imgClassName)}
        />
      ) : (
        <div className="flex h-full w-full items-center justify-center bg-muted">
          <Package className="h-16 w-16 text-muted-foreground" />
        </div>
      )}

      {hasPreview && (
        <div className="pointer-events-none absolute inset-x-0 bottom-2 z-10 flex items-center justify-center gap-1.5">
          {imageUrls.map((_, index) => (
            <span
              key={`dot-${index}`}
              className={cn(
                'h-1.5 w-1.5 rounded-full transition-colors duration-150',
                index === activeIndex ? 'bg-black/80 ring-1 ring-white/70' : 'bg-black/35'
              )}
            />
          ))}
        </div>
      )}

    </div>
  );
}
