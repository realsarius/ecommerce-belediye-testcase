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
      <Card className="border-white/10 bg-white/[0.03] py-0">
        <CardHeader>
          <CardTitle>Başlangıç kontrol listesi</CardTitle>
        </CardHeader>
        <CardContent>
          <ul className="space-y-3">
            {guideSteps.map((step) => (
              <li key={step} className="flex gap-3">
                <span className="mt-2 h-2 w-2 rounded-full bg-rose-300" />
                <span>{step}</span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </StaticPageLayout>
  );
}
