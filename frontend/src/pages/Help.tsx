import { Link } from 'react-router-dom';
import { CircleDollarSign, LifeBuoy, Package, Truck, Undo2, UserRound } from 'lucide-react';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';

const helpCards = [
  {
    title: 'Siparişlerim',
    description: 'Sipariş takibi, teslimat durumu ve sipariş detayları için ilgili akışa gidin.',
    icon: Package,
    to: '/orders',
  },
  {
    title: 'Ödeme',
    description: 'Ödeme yöntemleri, kampanyalar, kuponlar ve sadakat puanı soruları için hızlı rehber.',
    icon: CircleDollarSign,
    to: '/faq',
  },
  {
    title: 'İade ve İptal',
    description: 'Yasal haklar, iade adımları ve geri ödeme süreçlerini tek yerde inceleyin.',
    icon: Undo2,
    to: '/refund-policy',
  },
  {
    title: 'Hesabım',
    description: 'Adresler, kartlar, bildirimler ve güvenlik ayarları hakkında bilgi alın.',
    icon: UserRound,
    to: '/account',
  },
  {
    title: 'Kargo',
    description: 'Teslimat süreleri, kargo partnerleri ve takip süreçleri için bilgi merkezi.',
    icon: Truck,
    to: '/shipping',
  },
  {
    title: 'Canlı Destek',
    description: 'Anlık yardıma ihtiyacınız varsa mevcut destek kanalına doğrudan geçin.',
    icon: LifeBuoy,
    to: '/support',
  },
];

export default function Help() {
  return (
    <StaticPageLayout
      eyebrow="Destek"
      title="Yardım Merkezi"
      description="En sık ihtiyaç duyulan destek konularını kategoriler halinde bir araya getirdik. Doğru sayfaya daha hızlı ulaşmak için aşağıdaki kartlardan birini seçin."
      lastUpdated="Mart 2026"
    >
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {helpCards.map((item) => (
          <Card
            key={item.title}
            className="border-border/70 bg-card/80 py-0 shadow-sm transition-transform duration-200 hover:-translate-y-1"
          >
            <CardHeader className="px-7 pt-7 pb-3">
              <div className="mb-2 flex h-11 w-11 items-center justify-center rounded-2xl bg-rose-500/10 dark:bg-white/10">
                <item.icon className="h-5 w-5 text-rose-500 dark:text-rose-200" />
              </div>
              <CardTitle>{item.title}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-5 px-7 pb-7 pt-1">
              <p className="text-sm leading-7 text-muted-foreground">{item.description}</p>
              <Button asChild variant="outline">
                <Link to={item.to}>İlgili içeriğe git</Link>
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>

      <section className="rounded-3xl border border-border/70 bg-card/80 p-7 shadow-sm sm:p-8">
        <h2 className="text-xl font-semibold text-foreground">Ek destek kanalları</h2>
        <p className="mt-3 text-sm leading-7 text-muted-foreground">
          Genel sorular için SSS ve yardım merkezi içeriklerini, siparişe bağlı işlemler için sipariş ekranlarını,
          daha özel durumlar için ise iletişim ve canlı destek akışlarını kullanabilirsiniz.
        </p>
      </section>
    </StaticPageLayout>
  );
}
