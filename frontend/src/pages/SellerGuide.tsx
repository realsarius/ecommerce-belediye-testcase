import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';

const guideSteps = [
  'Mağaza profilinizi ve satıcı bilgilerinizi eksiksiz tamamlayın.',
  'Kategori yapısına uygun ürün kartları ve görseller yükleyin.',
  'Kampanya, stok ve fiyat yönetimini güncel tutun.',
  'Sipariş, iade ve destek akışlarını panel üzerinden düzenli takip edin.',
];

export default function SellerGuide() {
  return (
    <StaticPageLayout
      eyebrow="Satıcı"
      title="Satıcı Rehberi"
      description="Platformda satışa hızlı başlamak isteyen satıcılar için kısa başlangıç adımlarını burada topladık."
      lastUpdated="Mart 2026"
    >
      <Card className="border-border/70 bg-card/80 py-0 shadow-sm">
        <CardHeader className="px-8 pt-8 pb-4">
          <CardTitle>Başlangıç kontrol listesi</CardTitle>
        </CardHeader>
        <CardContent className="px-8 pb-8 pt-1">
          <ul className="space-y-4">
            {guideSteps.map((step) => (
              <li key={step} className="flex gap-4 rounded-2xl border border-border/60 bg-background/40 px-4 py-3">
                <span className="mt-2 h-2 w-2 rounded-full bg-rose-500/80 dark:bg-rose-300" />
                <span>{step}</span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </StaticPageLayout>
  );
}
