import { openCookieSettings } from '@/features/cookies/cookieConsent';
import { Button } from '@/components/common/button';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';

export default function CookiePolicy() {
  return (
    <StaticPageLayout
      eyebrow="Yasal"
      title="Çerez Politikası"
      description="Platform deneyimini iyileştirmek ve güvenli oturum yönetimi sağlamak amacıyla kullanılan çerez türlerini burada özetliyoruz."
      lastUpdated="Mart 2026"
      actions={
        <Button variant="outline" onClick={openCookieSettings}>
          Çerez ayarlarını aç
        </Button>
      }
    >
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">Zorunlu çerezler</h2>
        <p>
          Giriş oturumu, güvenlik, sepet durumu ve temel site işlevleri için gerekli çerezlerdir. Platformun doğru
          çalışması için devre dışı bırakılamaz.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">Analitik çerezler</h2>
        <p>
          Kullanıcı deneyimini geliştirmek ve sayfa performansını anlamak amacıyla özet kullanım verileri üreten
          çerezlerdir. Tercihlerinizi yayınlanacak çerez yönetim panelinden yönetebileceksiniz.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-semibold text-foreground">Pazarlama çerezleri</h2>
        <p>
          Kampanya, öneri ve yeniden etkileşim deneyimlerini kişiselleştirmek için kullanılan çerezlerdir. Bu kategori
          için ayrı kullanıcı onayı alınacaktır.
        </p>
      </section>
    </StaticPageLayout>
  );
}
