import type { ReactNode } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';

type DataTableProps = {
  title: string;
  description?: string;
  actions?: ReactNode;
  children: ReactNode;
};

export function DataTable({
  title,
  description,
  actions,
  children,
}: DataTableProps) {
  return (
    <Card className="border-border/70">
      <CardHeader className="flex flex-row items-center justify-between gap-4">
        <div>
          <CardTitle>{title}</CardTitle>
          {description ? <CardDescription>{description}</CardDescription> : null}
        </div>
        {actions ?? null}
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

export default DataTable;
