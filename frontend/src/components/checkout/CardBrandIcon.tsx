import type { CardBrand } from '@/lib/cardBrand';
import { getCardBrandLabel } from '@/lib/cardBrand';
import { cn } from '@/lib/utils';

interface CardBrandIconProps {
  brand: CardBrand;
  className?: string;
}

export function CardBrandIcon({ brand, className }: CardBrandIconProps) {
  if (brand === 'Mastercard') {
    return (
      <div
        className={cn('relative flex h-7 w-12 items-center justify-center overflow-hidden rounded-full border bg-background', className)}
        aria-label={getCardBrandLabel(brand)}
        title={getCardBrandLabel(brand)}
      >
        <span className="absolute left-2 h-4 w-4 rounded-full bg-orange-500/90" />
        <span className="absolute right-2 h-4 w-4 rounded-full bg-red-500/90" />
      </div>
    );
  }

  const styleMap: Record<CardBrand, string> = {
    Unknown: 'border-border bg-muted text-muted-foreground',
    Visa: 'border-sky-200 bg-sky-50 text-sky-700 dark:border-sky-900 dark:bg-sky-950/40 dark:text-sky-300',
    Troy: 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950/40 dark:text-rose-300',
    Amex: 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300',
    Mastercard: '',
  };

  const textMap: Record<CardBrand, string> = {
    Unknown: 'CARD',
    Visa: 'VISA',
    Troy: 'TROY',
    Amex: 'AMEX',
    Mastercard: 'MC',
  };

  return (
    <div
      className={cn(
        'flex h-7 min-w-[3rem] items-center justify-center rounded-full border px-2 text-[10px] font-semibold tracking-[0.24em]',
        styleMap[brand],
        className,
      )}
      aria-label={getCardBrandLabel(brand)}
      title={getCardBrandLabel(brand)}
    >
      {textMap[brand]}
    </div>
  );
}
