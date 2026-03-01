import type { ReactNode } from 'react';
import { LastUpdated } from '@/components/common/LastUpdated';

type StaticPageLayoutProps = {
  title: string;
  description: string;
  lastUpdated?: string;
  eyebrow?: string;
  actions?: ReactNode;
  children: ReactNode;
};

export function StaticPageLayout({
  title,
  description,
  lastUpdated,
  eyebrow,
  actions,
  children,
}: StaticPageLayoutProps) {
  return (
    <div className="bg-background text-foreground">
      <div className="mx-auto max-w-5xl px-6 py-14 sm:px-8">
        <div className="mb-12 overflow-hidden rounded-3xl border border-border/70 bg-[radial-gradient(circle_at_top_left,_rgba(244,114,182,0.12),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(59,130,246,0.10),_transparent_28%),linear-gradient(180deg,rgba(255,255,255,0.78),rgba(255,255,255,0.62))] p-8 shadow-xl shadow-black/5 dark:bg-[radial-gradient(circle_at_top_left,_rgba(244,114,182,0.14),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(59,130,246,0.12),_transparent_28%),linear-gradient(180deg,rgba(255,255,255,0.06),rgba(255,255,255,0.03))] dark:shadow-black/30 sm:p-10">
          <div className="flex flex-col gap-5">
            {eyebrow && (
              <span className="inline-flex w-fit items-center rounded-full border border-rose-500/20 bg-rose-500/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-rose-600 dark:text-rose-200">
                {eyebrow}
              </span>
            )}
            <div className="flex flex-col gap-5 md:flex-row md:items-end md:justify-between">
              <div className="space-y-4">
                <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">{title}</h1>
                <p className="max-w-2xl text-sm leading-7 text-muted-foreground sm:text-base">{description}</p>
              </div>
              <div className="flex flex-col items-start gap-3 md:items-end">
                {lastUpdated && <LastUpdated date={lastUpdated} />}
                {actions}
              </div>
            </div>
          </div>
        </div>
        <div className="space-y-12 text-sm leading-7 text-muted-foreground">{children}</div>
      </div>
    </div>
  );
}

export default StaticPageLayout;
