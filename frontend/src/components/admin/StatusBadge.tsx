import { Badge } from '@/components/common/badge';
import { cn } from '@/lib/utils';

export type StatusBadgeTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

const toneClasses: Record<StatusBadgeTone, string> = {
  neutral: 'bg-slate-500/10 text-slate-700 dark:text-slate-300',
  info: 'bg-sky-500/10 text-sky-700 dark:text-sky-300',
  success: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300',
  warning: 'bg-amber-500/10 text-amber-700 dark:text-amber-300',
  danger: 'bg-rose-500/10 text-rose-700 dark:text-rose-300',
};

type StatusBadgeProps = {
  label: string;
  tone?: StatusBadgeTone;
  className?: string;
};

export function StatusBadge({
  label,
  tone = 'neutral',
  className,
}: StatusBadgeProps) {
  return (
    <Badge
      variant="secondary"
      className={cn(toneClasses[tone], className)}
    >
      {label}
    </Badge>
  );
}

export default StatusBadge;
