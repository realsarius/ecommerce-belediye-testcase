import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/common/accordion';
import { Card, CardContent } from '@/components/common/card';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';

const faqItems = [
  ['Hesabımı nasıl oluşturabilirim?', 'Sağ üst köşedeki kayıt ol bağlantısından e-posta adresiniz ve temel bilgilerinizle hesabınızı oluşturabilirsiniz.'],
  ['Siparişimi nasıl takip ederim?', 'Siparişlerim ekranında her siparişin güncel durumunu, teslimat ve kargo bilgilerini görebilirsiniz.'],
  ['Hangi ödeme yöntemleri destekleniyor?', 'Kredi kartı ve banka kartı ile güvenli ödeme desteklenir. Kayıtlı kart deneyimi olan hesaplarda kart saklama ayarları ayrıca yönetilebilir.'],
  ['Kupon ve sadakat puanını birlikte kullanabilir miyim?', 'Geçerli kupon uygulandıktan sonra kalan tutar üzerinden sadakat puanı kullanabilirsiniz.'],
  ['İade talebini nereden oluştururum?', 'Sipariş detay sayfasından veya İadelerim ekranından iade ya da iptal talebi oluşturabilirsiniz.'],
  ['İade onaylandıktan sonra ödeme ne zaman döner?', 'İade onayını takip eden süreçte ödeme kuruluşu üzerinden iade başlatılır ve bankanıza bağlı olarak birkaç iş günü içinde hesabınıza yansır.'],
  ['Kargo süresi ne kadar?', 'Stokta olan ürünlerde siparişler çoğunlukla 1-3 iş günü içinde kargoya verilir. Bölge ve yoğunluk durumuna göre teslimat süresi değişebilir.'],
  ['Canlı desteğe nasıl ulaşırım?', 'Destek sayfasına giderek mevcut canlı destek akışını kullanabilir veya yardım merkezi üzerinden ilgili kaynağa ulaşabilirsiniz.'],
  ['Wishlist ve fiyat alarmı nasıl çalışır?', 'Bir ürünü favorilerinize ekleyebilir, hedef fiyat belirleyebilir ve fiyat düştüğünde bildirim alabilirsiniz.'],
  ['Bildirimleri nereden görebilirim?', 'Bildirimler sayfasında uygulama içi bildirimlerinizi inceleyebilir, okundu işaretleyebilir ve ilgili sayfalara hızlıca gidebilirsiniz.'],
];

export default function Faq() {
  return (
    <StaticPageLayout
      eyebrow="Destek"
      title="Sıkça Sorulan Sorular"
      description="Hesap, sipariş, ödeme, iade ve teslimat süreçlerinde en çok merak edilen konuları burada topladık."
      lastUpdated="Mart 2026"
    >
      <Card className="border-white/10 bg-white/[0.03] py-0">
        <CardContent className="pt-6">
          <Accordion type="single" collapsible className="w-full">
            {faqItems.map(([question, answer], index) => (
              <AccordionItem key={question} value={`faq-${index}`} className="border-white/10">
                <AccordionTrigger className="text-base text-white hover:no-underline">
                  {question}
                </AccordionTrigger>
                <AccordionContent className="text-sm leading-7 text-gray-300">{answer}</AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        </CardContent>
      </Card>
    </StaticPageLayout>
  );
}
