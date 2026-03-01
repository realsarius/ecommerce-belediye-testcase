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
    <div className="bg-[#050816] text-white">
      <div className="mx-auto max-w-4xl px-6 py-12">
        <div className="mb-10 overflow-hidden rounded-3xl border border-white/10 bg-[radial-gradient(circle_at_top_left,_rgba(244,114,182,0.14),_transparent_30%),radial-gradient(circle_at_top_right,_rgba(251,191,36,0.12),_transparent_30%),linear-gradient(180deg,rgba(255,255,255,0.04),rgba(255,255,255,0.02))] p-8 shadow-2xl shadow-black/30">
          <div className="flex flex-col gap-4">
            {eyebrow && (
              <span className="inline-flex w-fit items-center rounded-full border border-rose-400/30 bg-rose-400/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-rose-200">
                {eyebrow}
              </span>
            )}
            <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
              <div className="space-y-3">
                <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">{title}</h1>
                <p className="max-w-2xl text-sm leading-7 text-gray-300 sm:text-base">{description}</p>
              </div>
              <div className="flex flex-col items-start gap-3 md:items-end">
                {lastUpdated && <LastUpdated date={lastUpdated} />}
                {actions}
              </div>
            </div>
          </div>
        </div>
        <div className="space-y-10 text-sm leading-7 text-gray-300">{children}</div>
      </div>
    </div>
  );
}

export default StaticPageLayout;
