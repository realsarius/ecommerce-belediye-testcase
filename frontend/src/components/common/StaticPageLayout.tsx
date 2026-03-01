import type { ReactNode } from 'react';
import { LastUpdated } from '@/components/common/LastUpdated';

type StaticPageLayoutProps = {
  title: string;
  description: string;
  lastUpdated?: string;
  eyebrow?: string;
  badges?: string[];
  actions?: ReactNode;
  children: ReactNode;
};

export function StaticPageLayout({
  title,
  description,
  lastUpdated,
  eyebrow,
  badges = [],
  actions,
  children,
}: StaticPageLayoutProps) {
  return (
    <div className="bg-background text-foreground">
      <div className="mx-auto max-w-6xl px-6 py-16 sm:px-8 sm:py-18 lg:px-10 lg:py-20">
        <div className="mb-14 overflow-hidden rounded-3xl border border-border/70 bg-[radial-gradient(circle_at_top_left,_rgba(244,114,182,0.12),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(59,130,246,0.10),_transparent_28%),linear-gradient(180deg,rgba(255,255,255,0.78),rgba(255,255,255,0.62))] p-9 shadow-xl shadow-black/5 dark:bg-[radial-gradient(circle_at_top_left,_rgba(244,114,182,0.14),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(59,130,246,0.12),_transparent_28%),linear-gradient(180deg,rgba(255,255,255,0.06),rgba(255,255,255,0.03))] dark:shadow-black/30 sm:p-11 lg:p-12">
          <div className="flex flex-col gap-6">
            {eyebrow && (
              <span className="inline-flex w-fit items-center rounded-full border border-rose-500/20 bg-rose-500/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-rose-600 dark:text-rose-200">
                {eyebrow}
              </span>
            )}
            <div className="flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
              <div className="space-y-5">
                <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">{title}</h1>
                <p className="max-w-2xl text-sm leading-7 text-muted-foreground sm:text-base">{description}</p>
                {badges.length > 0 && (
                  <div className="flex flex-wrap gap-2 pt-1">
                    {badges.map((badge) => (
                      <span
                        key={badge}
                        className="inline-flex items-center rounded-full border border-border/70 bg-background/70 px-3 py-1 text-xs font-medium text-muted-foreground shadow-sm dark:bg-background/20"
                      >
                        {badge}
                      </span>
                    ))}
                  </div>
                )}
              </div>
              <div className="flex flex-col items-start gap-3 md:items-end">
                {lastUpdated && <LastUpdated date={lastUpdated} />}
                {actions}
              </div>
            </div>
          </div>
        </div>
        <div className="space-y-14 text-sm leading-7 text-muted-foreground">{children}</div>
      </div>
    </div>
  );
}

export default StaticPageLayout;
