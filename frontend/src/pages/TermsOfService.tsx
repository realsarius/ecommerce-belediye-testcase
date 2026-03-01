import { StaticPageLayout } from '@/components/common/StaticPageLayout';

export default function TermsOfService() {
  return (
    <StaticPageLayout
      eyebrow="Yasal"
      title="Kullanım Koşulları"
      description="Platformu kullanırken geçerli olan temel üyelik, sipariş ve kullanım kurallarını bu sayfada bulabilirsiniz."
      lastUpdated="Mart 2026"
    >
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">1. Genel Hükümler</h2>
        <p>
          Bu platforma erişen her kullanıcı, yürürlükteki mevzuata ve burada belirtilen kurallara uygun
          davranacağını kabul eder. Platform üzerinde sunulan içerikler önceden haber verilmeksizin güncellenebilir.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">2. Üyelik</h2>
        <p>
          Kullanıcılar hesap oluştururken doğru ve güncel bilgi vermekle yükümlüdür. Hesap güvenliği kullanıcıya
          aittir; şifre ve oturum bilgilerinin korunması kullanıcı sorumluluğundadır.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">3. Satın Alma</h2>
        <p>
          Siparişin kesinleşmesi, ödemenin onaylanması ve stok uygunluğunun doğrulanması ile gerçekleşir. Kampanya,
          kupon ve sadakat puanı kuralları sipariş anındaki aktif koşullara göre uygulanır.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">4. Yasaklı Kullanımlar</h2>
        <p>
          Sistemi kötüye kullanmak, yetkisiz erişim girişimlerinde bulunmak, bot davranışı sergilemek, sahte sipariş
          oluşturmak veya başka kullanıcıların deneyimini bozacak davranışlarda bulunmak yasaktır.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">5. Fikri Mülkiyet</h2>
        <p>
          Sitede yer alan tasarımlar, yazılımlar, logolar, içerikler ve kullanıcı arayüzü unsurları aksi açıkça
          belirtilmedikçe ilgili hak sahiplerine aittir ve izinsiz kullanılamaz.
        </p>
      </section>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-white">6. Uyuşmazlık</h2>
        <p>
          Taraflar arasında doğabilecek uyuşmazlıklarda Türk hukuku uygulanır. Tüketici işlemlerinde ilgili tüketici
          hakem heyetleri ve tüketici mahkemeleri yetkilidir.
        </p>
      </section>
    </StaticPageLayout>
  );
}
