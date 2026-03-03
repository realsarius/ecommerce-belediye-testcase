import { Skeleton } from '@/components/common/skeleton';
import { cn } from '@/lib/utils';

type TableLoadingStateProps = {
  rowCount?: number;
  rowClassName?: string;
  className?: string;
};

export function TableLoadingState({
  rowCount = 6,
  rowClassName = 'h-16 rounded-xl',
  className,
}: TableLoadingStateProps) {
  return (
    <div className={cn('space-y-2', className)}>
      {Array.from({ length: rowCount }).map((_, index) => (
        <Skeleton key={index} className={rowClassName} />
      ))}
    </div>
  );
}

export default TableLoadingState;
