import { StaticPageLayout } from '@/components/common/StaticPageLayout';

export default function Kvkk() {
  return (
    <StaticPageLayout
      eyebrow="Yasal"
      title="KVKK Aydınlatma Metni"
      description="6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında veri işleme süreçlerimize ilişkin temel bilgilendirmeyi bu sayfada sunuyoruz."
      lastUpdated="Mart 2026"
    >
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">Veri Sorumlusu</h2>
        <p>
          Bu platform üzerinde işlenen kişisel veriler bakımından veri sorumlusu E-Ticaret platform işletmecisidir.
          Taleplerinizi destek@eticaret.com üzerinden iletebilirsiniz.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">İşlenen Veri Kategorileri</h2>
        <p>
          Kimlik, iletişim, müşteri işlem, sipariş, teslimat, ödeme özet kayıtları, destek talep ve kullanım güvenliği
          verileri hizmetin niteliğine göre işlenebilir.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">İşleme Amaçları</h2>
        <p>
          Üyelik süreçlerinin yürütülmesi, sipariş ve teslimatın tamamlanması, ödeme işlemlerinin doğrulanması, iade
          ve destek süreçlerinin yönetilmesi ile mevzuattan doğan yükümlülüklerin yerine getirilmesi amaçlarıyla veri
          işlenmektedir.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">Aktarım</h2>
        <p>
          Kişisel veriler, ödeme kuruluşları, kargo firmaları, altyapı hizmet sağlayıcıları ve yasal yetki sahibi
          kamu kurumları ile mevzuata uygun ölçüde paylaşılabilir.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">Saklama Süresi</h2>
        <p>
          Veriler, işleme amacının gerektirdiği süre boyunca ve ilgili mevzuatta öngörülen saklama süreleri kadar
          muhafaza edilir; süre sonunda silinir, yok edilir veya anonim hale getirilir.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">Haklarınız</h2>
        <p>
          KVKK'nın 11. maddesi uyarınca veri işlenip işlenmediğini öğrenme, düzeltilmesini veya silinmesini talep etme,
          itiraz etme ve zarar halinde tazminat isteme haklarına sahipsiniz.
        </p>
      </section>
    </StaticPageLayout>
  );
}
