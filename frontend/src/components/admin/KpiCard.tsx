import type { LucideIcon } from 'lucide-react';
import { ArrowDownRight, ArrowUpRight } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { cn } from '@/lib/utils';

type KpiCardProps = {
  title: string;
  value: string;
  icon: LucideIcon;
  accentClass: string;
  surfaceClass: string;
  helperText?: string;
  delta?: number | null;
  deltaLabel?: string;
  badge?: string;
};

export function KpiCard({
  title,
  value,
  icon: Icon,
  accentClass,
  surfaceClass,
  helperText,
  delta,
  deltaLabel,
  badge,
}: KpiCardProps) {
  const isPositive = (delta ?? 0) >= 0;

  return (
    <Card className="border-border/70">
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0 pb-3">
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
            {badge ? (
              <Badge variant="secondary" className="rounded-full px-2 py-0 text-[10px] uppercase tracking-wide">
                {badge}
              </Badge>
            ) : null}
          </div>
          <div className="text-3xl font-semibold tracking-tight">{value}</div>
        </div>
        <div className={cn('rounded-2xl p-3', surfaceClass)}>
          <Icon className={cn('h-5 w-5', accentClass)} />
        </div>
      </CardHeader>
      <CardContent className="space-y-2">
        {delta != null && deltaLabel ? (
          <div
            className={cn(
              'inline-flex items-center gap-1 rounded-full px-2 py-1 text-xs font-medium',
              isPositive
                ? 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300'
                : 'bg-rose-500/10 text-rose-700 dark:text-rose-300'
            )}
          >
            {isPositive ? <ArrowUpRight className="h-3.5 w-3.5" /> : <ArrowDownRight className="h-3.5 w-3.5" />}
            {Math.abs(delta).toFixed(1)}% {deltaLabel}
          </div>
        ) : null}
        {helperText ? <p className="text-sm text-muted-foreground">{helperText}</p> : null}
      </CardContent>
    </Card>
  );
}
