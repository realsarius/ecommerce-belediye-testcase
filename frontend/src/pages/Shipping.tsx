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
      <section className="grid gap-7 md:grid-cols-3">
        {shippingCards.map((card) => (
          <Card key={card.title} className="border-border/70 bg-card/80 py-0 shadow-sm">
            <CardHeader className="px-8 pt-8 pb-4">
              <CardTitle>{card.title}</CardTitle>
            </CardHeader>
            <CardContent className="px-8 pb-8 pt-1">
              <p>{card.description}</p>
            </CardContent>
          </Card>
        ))}
      </section>

      <section className="rounded-3xl border border-border/70 bg-card/80 p-8 shadow-sm sm:p-10">
        <h2 className="text-xl font-semibold text-foreground">Teslimat sürecinde bilmeniz gerekenler</h2>
        <p className="mt-4">
          Siparişiniz kargoya verildiğinde hesabınızdaki sipariş detay sayfası üzerinden gönderi durumunu ve kargo
          takip numarasını görüntüleyebilirsiniz. Resmi tatiller, hava koşulları veya yoğun kampanya dönemleri teslimat
          süresini etkileyebilir.
        </p>
      </section>
    </StaticPageLayout>
  );
}
