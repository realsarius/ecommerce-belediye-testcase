import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';

const shippingCards = [
  {
    title: 'Tahmini teslimat süresi',
    description: 'Stokta olan ürünlerde siparişler çoğunlukla 1-3 iş günü içinde kargoya verilir.',
  },
  {
    title: 'Ücretsiz kargo eşiği',
    description: 'Belirli dönemlerde kampanyaya bağlı ücretsiz kargo avantajı sunulabilir. Eşik tutar checkout ekranında gösterilir.',
  },
  {
    title: 'Kargo iş ortakları',
    description: 'Teslimat süreçleri anlaşmalı ulusal kargo firmaları üzerinden yürütülür ve takip numarası sipariş ekranında paylaşılır.',
  },
];

export default function Shipping() {
  return (
    <StaticPageLayout
      eyebrow="Destek"
      title="Kargo Bilgileri"
      description="Teslimat süreleri, kargo süreci ve takip bilgileri hakkında en sık sorulan başlıkları burada topladık."
      lastUpdated="Mart 2026"
    >
      <section className="grid gap-6 md:grid-cols-3">
        {shippingCards.map((card) => (
          <Card key={card.title} className="border-white/10 bg-white/[0.03] py-0">
            <CardHeader>
              <CardTitle>{card.title}</CardTitle>
            </CardHeader>
            <CardContent>
              <p>{card.description}</p>
            </CardContent>
          </Card>
        ))}
      </section>

      <section className="space-y-4">
        <h2 className="text-xl font-semibold text-white">Teslimat sürecinde bilmeniz gerekenler</h2>
        <p>
          Siparişiniz kargoya verildiğinde hesabınızdaki sipariş detay sayfası üzerinden gönderi durumunu ve kargo
          takip numarasını görüntüleyebilirsiniz. Resmi tatiller, hava koşulları veya yoğun kampanya dönemleri teslimat
          süresini etkileyebilir.
        </p>
      </section>
    </StaticPageLayout>
  );
}
