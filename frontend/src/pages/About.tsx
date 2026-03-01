import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';

const values = [
  {
    title: 'Güvenli alışveriş',
    description:
      'Ödeme, sipariş ve destek süreçlerimizi güvenlik, şeffaflık ve kullanıcı kontrolü etrafında tasarlıyoruz.',
  },
  {
    title: 'Hızlı operasyon',
    description:
      'Siparişten iadeye kadar tüm akışlarda net durum takibi, merkezi bildirimler ve güvenilir süreçler sunuyoruz.',
  },
  {
    title: 'Satıcı dostu yapı',
    description:
      'Satıcıların ürünlerini daha görünür kılabilmesi, kampanya yönetebilmesi ve performansını izlemesi için araçlar geliştiriyoruz.',
  },
];

export default function About() {
  return (
    <StaticPageLayout
      eyebrow="Kurumsal"
      title="Hakkımızda"
      description="E-Ticaret, müşteri deneyimini, güvenliği ve operasyonel netliği aynı anda önemseyen modern bir alışveriş platformudur."
      lastUpdated="Mart 2026"
    >
      <section className="grid gap-6 md:grid-cols-3">
        {values.map((value) => (
          <Card key={value.title} className="border-border/70 bg-card/80 py-0 shadow-sm">
            <CardHeader className="px-7 pt-7 pb-3">
              <CardTitle>{value.title}</CardTitle>
            </CardHeader>
            <CardContent className="px-7 pb-7 pt-1">
              <p>{value.description}</p>
            </CardContent>
          </Card>
        ))}
      </section>

      <section className="space-y-5">
        <h2 className="text-xl font-semibold text-foreground">Ne sunuyoruz?</h2>
        <p>
          Platformumuz; ürün keşfi, kampanya yönetimi, istek listeleri, iade süreçleri, sadakat puanı,
          bildirim merkezi ve kişiselleştirilmiş öneriler gibi akışları tek bir deneyimde birleştirir.
        </p>
        <p>
          Amacımız yalnızca ürün listeleyen bir vitrin olmak değil, kullanıcıların tekrar tekrar dönmek
          isteyeceği güvenilir ve akıcı bir alışveriş deneyimi oluşturmaktır.
        </p>
      </section>
    </StaticPageLayout>
  );
}
