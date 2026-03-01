import { useEffect, useMemo, useRef } from 'react';
import { Timer, Zap } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/common/badge';
import { Card, CardContent } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { CampaignCountdown } from '@/components/campaigns/CampaignCountdown';
import { useGetActiveCampaignsQuery, useTrackCampaignInteractionMutation } from '@/features/campaigns/campaignsApi';
import { getCampaignSessionId } from '@/features/campaigns/campaignSession';

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
    <section className="relative mb-8 overflow-hidden rounded-[2rem] border border-amber-400/15 bg-[radial-gradient(circle_at_top_left,_rgba(251,191,36,0.14),_transparent_22%),radial-gradient(circle_at_top_right,_rgba(249,115,22,0.14),_transparent_26%),linear-gradient(135deg,_rgba(24,24,27,0.96),_rgba(9,9,11,0.96))] shadow-[0_24px_80px_rgba(0,0,0,0.45)]">
      <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(255,255,255,0.05),transparent_24%,transparent_78%,rgba(255,255,255,0.03))]" />

      <div className="relative border-b border-white/10 px-6 py-5">
        <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <Badge className="mb-3 border border-amber-400/20 bg-amber-500/10 text-amber-100">
              Zaman sınırlı fırsatlar
            </Badge>
            <h2 className="text-2xl font-bold tracking-tight text-white">Canlı Kampanyalar</h2>
            <p className="mt-2 max-w-2xl text-sm text-white/60">
              Şu anda aktif olan kampanyaları, öne çıkan ürünleri ve kalan süreyi tek bakışta görün.
            </p>
          </div>
          <div className="flex items-center gap-2 rounded-full border border-amber-400/15 bg-white/5 px-3 py-1.5 text-sm text-amber-100/90 backdrop-blur-sm">
            <Timer className="h-4 w-4" />
            <span>Flash sale ve kampanya görünürlüğü</span>
          </div>
        </div>
      </div>

      <div className="relative grid gap-4 p-6 lg:grid-cols-3">
        {isLoading
          ? Array.from({ length: 3 }).map((_, index) => (
              <Card key={index} className="border-white/10 bg-white/[0.04] text-white shadow-none backdrop-blur-sm">
                <CardContent className="space-y-4 p-5">
                  <Skeleton className="h-5 w-24 bg-white/10" />
                  <Skeleton className="h-6 w-2/3 bg-white/10" />
                  <Skeleton className="h-4 w-full bg-white/10" />
                  <Skeleton className="h-20 w-full bg-white/10" />
                </CardContent>
              </Card>
            ))
          : campaigns.map((campaign) => {
              const featuredProducts = campaign.products
                .filter((product) => product.isFeatured)
                .slice(0, 3);
              const products = featuredProducts.length > 0 ? featuredProducts : campaign.products.slice(0, 3);

              return (
                <Card
                  key={campaign.id}
                  className="border-white/10 bg-white/[0.04] text-white shadow-none backdrop-blur-sm transition-all duration-200 hover:-translate-y-1.5 hover:border-amber-400/30 hover:bg-white/[0.06]"
                >
                  <CardContent className="flex h-full flex-col gap-5 p-5">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <Badge className="border border-white/10 bg-white/10 text-white">
                          {campaign.badgeText || campaign.type}
                        </Badge>
                        <h3 className="mt-3 text-lg font-semibold text-white">{campaign.name}</h3>
                      </div>
                      <div className="flex items-center gap-1 rounded-full border border-amber-400/20 bg-amber-500/10 px-2.5 py-1 text-xs font-medium text-amber-100">
                        <Zap className="h-3.5 w-3.5" />
                        {campaign.products.length} ürün
                      </div>
                    </div>

                    <p className="line-clamp-2 text-sm text-white/55">
                      {campaign.description || 'Seçili ürünlerde sınırlı süreli kampanya fiyatları aktif.'}
                    </p>

                    <div className="rounded-2xl border border-white/8 bg-black/20 p-4">
                      <CampaignCountdown
                        endsAt={campaign.endsAt}
                        className="text-sm font-medium text-amber-100"
                      />
                      <div className="mt-3 space-y-2">
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
                            className="flex items-center justify-between gap-3 rounded-xl border border-white/8 bg-white/[0.03] px-3 py-2 text-sm transition-colors hover:bg-white/[0.06]"
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
