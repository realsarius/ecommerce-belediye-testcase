import { StaticPageLayout } from '@/components/common/StaticPageLayout';

export default function DistanceSalesContract() {
  return (
    <StaticPageLayout
      eyebrow="Yasal"
      title="Mesafeli Satış Sözleşmesi"
      description="Sipariş, teslimat, cayma hakkı ve tarafların yükümlülüklerine ilişkin temel sözleşme şartları burada yer alır."
      lastUpdated="Mart 2026"
    >
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">Taraflar</h2>
        <p>
          Bu sözleşme, bir tarafta platform üzerinden ürün satın alan tüketici ile diğer tarafta satışı gerçekleştiren
          satıcı veya platform aracısı arasında elektronik ortamda kurulmaktadır.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">Konu</h2>
        <p>
          Sözleşmenin konusu; siparişe konu ürün veya hizmetin nitelikleri, satış bedeli, ödeme, teslimat ve cayma
          hakkına ilişkin esasların belirlenmesidir.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">Ödeme</h2>
        <p>
          Sipariş bedeli, ödeme ekranında seçilen yönteme göre tahsil edilir. Kampanya, kupon ve sadakat puanı
          uygulamaları sipariş anındaki kurallara göre toplam bedelden düşülebilir.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">Teslimat</h2>
        <p>
          Teslimat süresi, sipariş özetinde ve ürün sayfalarında bildirilen tahmini süreler esas alınarak yürütülür.
          Kargo firması ve teslimat detayları sipariş sonrasında kullanıcı hesabında görüntülenebilir.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">Cayma Hakkı</h2>
        <p>
          Tüketici, mevzuattaki istisnalar saklı kalmak üzere teslimat tarihinden itibaren 14 gün içinde cayma hakkını
          kullanabilir. İade ve geri ödeme şartları ilgili politika sayfalarında ayrıca açıklanır.
        </p>
      </section>
    </StaticPageLayout>
  );
}
