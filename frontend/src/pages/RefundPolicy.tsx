import { StaticPageLayout } from '@/components/common/StaticPageLayout';

export default function RefundPolicy() {
  return (
    <StaticPageLayout
      eyebrow="Yasal"
      title="İptal ve İade Politikası"
      description="Sipariş iptali, cayma hakkı ve ödeme iadesi süreçlerinin nasıl işlediğini bu sayfada özetliyoruz."
      lastUpdated="Mart 2026"
    >
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">Sipariş İptali</h2>
        <p>
          Siparişiniz henüz kargo sürecine alınmadıysa sipariş detay ekranı üzerinden iptal talebi oluşturabilirsiniz.
          İptal onaylandığında ödeme iadesi ilgili ödeme kuruluşu kurallarına göre başlatılır.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">14 Günlük Cayma Hakkı</h2>
        <p>
          Mesafeli Satışlar Yönetmeliği kapsamında, yasal istisnalar saklı kalmak kaydıyla teslimattan itibaren 14 gün
          içinde cayma hakkınızı kullanabilirsiniz. Ürünün kullanılmamış ve yeniden satılabilir durumda olması gerekir.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">İade Süreci</h2>
        <p>
          İade talebinizi <strong>İadelerim</strong> ekranından veya ilgili sipariş detayından oluşturabilirsiniz.
          Talep incelendikten sonra uygun durumlarda iade kargo yönlendirmesi ve ödeme iadesi süreci başlatılır.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">İade Edilemeyen Ürünler</h2>
        <p>
          Hijyen nedeniyle yeniden satışı mümkün olmayan ürünler, kişiye özel hazırlanan ürünler ve mevzuat gereği
          cayma hakkı kapsamında değerlendirilemeyen ürünlerde iade kabul edilmeyebilir.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">Para İadesi Süresi</h2>
        <p>
          İade onayını takip eden süreçte ücret iadesi ödeme kuruluşu üzerinden başlatılır. Banka ve kart sağlayıcısına
          bağlı olarak iadenin yansıması genellikle birkaç iş günü sürebilir.
        </p>
      </section>
    </StaticPageLayout>
  );
}
