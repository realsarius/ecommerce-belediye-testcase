import { useEffect, useMemo, useRef } from 'react';
import { Timer, Zap } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/common/badge';
import { Card, CardContent } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { CampaignCountdown } from '@/components/campaigns/CampaignCountdown';
import { useGetActiveCampaignsQuery, useTrackCampaignInteractionMutation } from '@/features/campaigns/campaignsApi';
import { getCampaignSessionId } from '@/features/campaigns/campaignSession';
import { cn } from '@/lib/utils';

const getCampaignVisibilityClass = (index: number) => {
  if (index === 0) {
    return '';
  }

  if (index === 1) {
    return 'hidden md:block';
  }

  return 'hidden lg:block';
};

export function ActiveCampaignSpotlight() {
  const { data, isLoading } = useGetActiveCampaignsQuery();
  const [trackCampaignInteraction] = useTrackCampaignInteractionMutation();
  const trackedImpressionsRef = useRef<Set<number>>(new Set());
  const campaigns = (data ?? []).filter((campaign) => campaign.products.length > 0).slice(0, 3);
  const sessionId = useMemo(() => getCampaignSessionId(), []);

  useEffect(() => {
    if (campaigns.length === 0) {
      return;
    }

    campaigns.forEach((campaign) => {
      if (trackedImpressionsRef.current.has(campaign.id)) {
        return;
      }

      trackedImpressionsRef.current.add(campaign.id);
      void trackCampaignInteraction({
        campaignId: campaign.id,
        interactionType: 'impression',
        sessionId,
      }).unwrap().catch(() => undefined);
    });
  }, [campaigns, sessionId, trackCampaignInteraction]);

  if (!isLoading && campaigns.length === 0) {
    return null;
  }

  return (
    <section className="relative mb-6 overflow-hidden rounded-xl border border-amber-400/12 bg-[linear-gradient(180deg,rgba(251,191,36,0.10),rgba(10,10,12,0.94)_52%)] shadow-[0_6px_16px_rgba(0,0,0,0.24)]">
      <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(255,255,255,0.04),transparent_30%,transparent_100%)]" />

      <div className="relative border-b border-white/8 px-4 py-3 sm:px-5">
        <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <Badge className="mb-2 border border-amber-400/18 bg-amber-500/10 text-amber-100">
              Zaman sınırlı fırsatlar
            </Badge>
            <h2 className="text-xl font-bold tracking-tight text-white sm:text-2xl">Canlı Kampanyalar</h2>
            <p className="mt-1 hidden max-w-2xl text-xs text-white/60 sm:block sm:text-sm">
              Şu anda aktif olan kampanyaları, öne çıkan ürünleri ve kalan süreyi tek bakışta görün.
            </p>
          </div>
          <div className="hidden items-center gap-2 rounded-full border border-amber-400/12 bg-white/[0.05] px-3 py-1.5 text-xs text-amber-100/85 backdrop-blur-sm md:flex">
            <Timer className="h-4 w-4" />
            <span>Flash sale ve kampanya görünürlüğü</span>
          </div>
        </div>
      </div>

      <div className="relative grid gap-3 p-4 sm:p-5 md:grid-cols-2 lg:grid-cols-3">
        {isLoading
          ? Array.from({ length: 3 }).map((_, index) => (
              <Card
                key={index}
                className={cn(
                  'border-white/10 bg-white/[0.04] text-white shadow-none backdrop-blur-sm',
                  'border-white/8 bg-white/[0.03]',
                  getCampaignVisibilityClass(index),
                )}
              >
                <CardContent className="space-y-3 p-4">
                  <Skeleton className="h-5 w-24 bg-white/10" />
                  <Skeleton className="h-6 w-2/3 bg-white/10" />
                  <Skeleton className="h-4 w-full bg-white/10" />
                  <Skeleton className="h-20 w-full bg-white/10" />
                </CardContent>
              </Card>
            ))
          : campaigns.map((campaign, index) => {
              const featuredProducts = campaign.products
                .filter((product) => product.isFeatured)
                .slice(0, 3);
              const products = featuredProducts.length > 0 ? featuredProducts : campaign.products.slice(0, 3);

              return (
                <Card
                  key={campaign.id}
                  className={cn(
                    'border-white/8 bg-white/[0.03] text-white shadow-none backdrop-blur-sm transition-all duration-200 hover:-translate-y-1 hover:border-amber-400/22 hover:bg-white/[0.05]',
                    getCampaignVisibilityClass(index),
                  )}
                >
                  <CardContent className="flex h-full flex-col gap-4 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <Badge className="border border-white/10 bg-white/10 text-white">
                          {campaign.badgeText || campaign.type}
                        </Badge>
                        <h3 className="mt-2 text-base font-semibold text-white sm:text-lg">{campaign.name}</h3>
                      </div>
                      <div className="flex items-center gap-1 rounded-full border border-amber-400/18 bg-amber-500/10 px-2.5 py-1 text-xs font-medium text-amber-100">
                        <Zap className="h-3.5 w-3.5" />
                        {campaign.products.length} ürün
                      </div>
                    </div>

                    <p className="line-clamp-2 text-sm text-white/55">
                      {campaign.description || 'Seçili ürünlerde sınırlı süreli kampanya fiyatları aktif.'}
                    </p>

                    <div className="rounded-lg border border-white/8 bg-black/20 p-3">
                      <CampaignCountdown
                        endsAt={campaign.endsAt}
                        className="text-xs font-medium text-amber-100 sm:text-sm"
                      />
                      <div className="mt-2 space-y-1.5">
                        {products.map((product) => (
                          <Link
                            key={`${campaign.id}-${product.productId}`}
                            to={`/products/${product.productId}`}
                            onClick={() => {
                              void trackCampaignInteraction({
                                campaignId: campaign.id,
                                interactionType: 'click',
                                productId: product.productId,
                                sessionId,
                              }).unwrap().catch(() => undefined);
                            }}
                            className="flex items-center justify-between gap-3 rounded-lg border border-white/8 bg-white/[0.03] px-3 py-2 text-sm transition-colors hover:bg-white/[0.06]"
                          >
                            <div className="min-w-0">
                              <p className="truncate font-medium text-white/90">{product.productName}</p>
                              <p className="text-xs text-white/45 line-through">
                                {product.originalPrice.toLocaleString('tr-TR')} TRY
                              </p>
                            </div>
                            <p className="shrink-0 font-semibold text-amber-100">
                              {product.campaignPrice.toLocaleString('tr-TR')} TRY
                            </p>
                          </Link>
                        ))}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
      </div>
    </section>
  );
}
