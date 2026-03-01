import { StaticPageLayout } from '@/components/common/StaticPageLayout';

export default function PrivacyPolicy() {
  return (
    <StaticPageLayout
      eyebrow="Yasal"
      title="Gizlilik Politikası"
      description="Kişisel verilerinizin hangi amaçlarla işlendiğini, kimlerle paylaşıldığını ve hangi haklara sahip olduğunuzu bu sayfada özetliyoruz."
      lastUpdated="Mart 2026"
    >
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">1. Toplanan Veriler</h2>
        <p>
          Üyelik, sipariş, teslimat ve destek süreçlerinde ad, soyad, e-posta adresi, telefon numarası,
          teslimat adresi, fatura bilgileri ve ödeme işlemlerine ilişkin sınırlı özet kayıtlar işlenebilir.
          Kart bilgileriniz ödeme kuruluşu tarafından işlenir; platform üzerinde tam kart verisi tutulmaz.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">2. Verilerin Kullanım Amacı</h2>
        <p>
          Toplanan veriler siparişlerinizi oluşturmak, ödemeleri tamamlamak, teslimatı sağlamak, destek
          taleplerinizi cevaplamak, iade süreçlerini yürütmek ve hesap güvenliğini sağlamak amacıyla kullanılır.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">3. Üçüncü Taraflarla Paylaşım</h2>
        <p>
          Ödeme işlemlerinde Iyzico, teslimat süreçlerinde anlaşmalı kargo firmaları ve yasal yükümlülükler
          kapsamında yetkili kamu kurumları ile sınırlı veri paylaşımı yapılabilir. Paylaşımlar, hizmetin
          gerektirdiği ölçüyle sınırlıdır.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">4. Çerezler</h2>
        <p>
          Oturum yönetimi, güvenlik, tercihlerin hatırlanması ve deneyim iyileştirme amacıyla çerezler
          kullanılabilir. Çerez tercihlerinizi yayınlanacak çerez ayarları paneli üzerinden yönetebilirsiniz.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">5. Haklarınız</h2>
        <p>
          KVKK 11. madde kapsamında kişisel verilerinizin işlenip işlenmediğini öğrenme, düzeltilmesini
          isteme, silinmesini talep etme, aktarıldığı üçüncü kişileri öğrenme ve itiraz etme haklarına sahipsiniz.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">6. İletişim</h2>
        <p>
          Gizlilik politikasıyla ilgili tüm talepleriniz için <strong>destek@eticaret.com</strong> adresine
          yazabilir veya iletişim sayfamızı kullanabilirsiniz.
        </p>
      </section>
    </StaticPageLayout>
  );
}
