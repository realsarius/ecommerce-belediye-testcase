import { openCookieSettings } from '@/features/cookies/cookieConsent';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { CircleHelp, Cookie, Instagram, Linkedin, MessageSquareText, Package, Play, ShieldCheck, Store } from 'lucide-react';

const disabledSoonClass = 'cursor-not-allowed text-muted-foreground/80 transition-colors dark:text-slate-500';

function SoonBadge() {
  return (
    <span className="ml-2 rounded bg-yellow-500/20 px-1.5 py-0.5 text-xs text-yellow-400">
      Yakında
    </span>
  );
}

function FooterLink({
  to,
  children,
}: {
  to: string;
  children: ReactNode;
}) {
  return (
    <Link
      to={to}
      className="block text-muted-foreground transition-colors hover:text-foreground dark:hover:text-white"
    >
      {children}
    </Link>
  );
}

function ExternalIconLink({
  href,
  label,
  children,
}: {
  href: string;
  label: string;
  children: ReactNode;
}) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={label}
      className="rounded-full border border-border/70 bg-background/70 p-2 text-muted-foreground transition-colors hover:border-border hover:text-foreground dark:border-white/10 dark:bg-white/5 dark:text-slate-400 dark:hover:border-white/20 dark:hover:text-white"
    >
      {children}
    </a>
  );
}

function PaymentMark({ label }: { label: string }) {
  return (
    <span className="rounded-full border border-border/70 bg-background/70 px-3 py-1 text-xs font-semibold tracking-wide text-foreground/80 dark:border-white/10 dark:bg-white/5 dark:text-slate-200">
      {label}
    </span>
  );
}

export function Footer() {
  return (
    <footer className="border-t border-border bg-[linear-gradient(180deg,rgba(250,250,252,0.98),rgba(244,244,247,0.96))] text-foreground dark:border-white/10 dark:bg-none dark:bg-[#111318] dark:text-white">
      <div className="mx-auto max-w-7xl px-6 py-12">
        <div className="grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-foreground/5 dark:bg-white/8">
                <Package className="h-6 w-6 text-rose-300" />
              </div>
              <div>
                <p className="text-lg font-semibold">E-Ticaret</p>
                <p className="text-xs uppercase tracking-[0.22em] text-muted-foreground">Kurumsal</p>
              </div>
            </div>
            <p className="max-w-xs text-sm leading-7 text-muted-foreground">
              Güvenli ödeme, hızlı operasyon ve şeffaf destek akışlarıyla modern bir alışveriş deneyimi sunuyoruz.
            </p>
            <div className="space-y-3 text-sm">
              <FooterLink to="/about">Hakkımızda</FooterLink>
              <div className={disabledSoonClass}>
                Kariyer
                <SoonBadge />
              </div>
              <div className={disabledSoonClass}>
                Basın
                <SoonBadge />
              </div>
              <div className={disabledSoonClass}>
                Sürdürülebilirlik
                <SoonBadge />
              </div>
              <FooterLink to="/contact">İletişim</FooterLink>
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <CircleHelp className="h-4 w-4 text-rose-300" />
              <h2 className="font-semibold">Müşteri Hizmetleri</h2>
            </div>
            <div className="space-y-3 text-sm">
              <FooterLink to="/help">Yardım Merkezi</FooterLink>
              <FooterLink to="/orders">Sipariş Takibi</FooterLink>
              <FooterLink to="/refund-policy">İade &amp; Değişim</FooterLink>
              <FooterLink to="/shipping">Kargo Bilgileri</FooterLink>
              <FooterLink to="/support">Canlı Destek</FooterLink>
              <FooterLink to="/faq">SSS</FooterLink>
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <Store className="h-4 w-4 text-rose-300" />
              <h2 className="font-semibold">Satıcı Ol</h2>
            </div>
            <div className="space-y-3 text-sm">
              <FooterLink to="/seller/register">Satıcı Başvurusu</FooterLink>
              <FooterLink to="/seller">Satıcı Paneli</FooterLink>
              <FooterLink to="/seller/guide">Satıcı Rehberi</FooterLink>
              <FooterLink to="/seller/pricing">Komisyon Oranları</FooterLink>
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 text-rose-300" />
              <h2 className="font-semibold">Güvenlik &amp; Gizlilik</h2>
            </div>
            <div className="space-y-3 text-sm">
              <FooterLink to="/privacy-policy">Gizlilik Politikası</FooterLink>
              <FooterLink to="/terms-of-service">Kullanım Koşulları</FooterLink>
              <div className="flex items-center gap-3">
                <FooterLink to="/cookie-policy">Çerez Politikası</FooterLink>
                <button
                  type="button"
                  onClick={openCookieSettings}
                  className="inline-flex items-center rounded-full border border-border/70 bg-background/70 px-2 py-1 text-[11px] text-muted-foreground transition-colors hover:border-border hover:text-foreground dark:border-white/10 dark:bg-white/5 dark:text-slate-300 dark:hover:border-white/20 dark:hover:text-white"
                >
                  <Cookie className="mr-1 h-3 w-3" />
                  Çerez Ayarları
                </button>
              </div>
              <FooterLink to="/kvkk">KVKK Aydınlatma Metni</FooterLink>
              <FooterLink to="/distance-sales-contract">Mesafeli Satış Sözleşmesi</FooterLink>
              <FooterLink to="/refund-policy">İptal &amp; İade Politikası</FooterLink>
            </div>
          </div>
        </div>

        <div className="mt-10 flex flex-col gap-5 border-t border-border/70 pt-6 dark:border-gray-700 lg:flex-row lg:items-center lg:justify-between">
          <p className="text-sm text-muted-foreground dark:text-slate-400">© 2026 E-Ticaret. Tüm hakları saklıdır.</p>

          <div className="flex items-center gap-3">
            <ExternalIconLink href="https://instagram.com" label="Instagram">
              <Instagram className="h-4 w-4" />
            </ExternalIconLink>
            <ExternalIconLink href="https://x.com" label="X">
              <MessageSquareText className="h-4 w-4" />
            </ExternalIconLink>
            <ExternalIconLink href="https://linkedin.com" label="LinkedIn">
              <Linkedin className="h-4 w-4" />
            </ExternalIconLink>
            <ExternalIconLink href="https://youtube.com" label="YouTube">
              <Play className="h-4 w-4" />
            </ExternalIconLink>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <PaymentMark label="iyzico" />
            <PaymentMark label="VISA" />
            <PaymentMark label="Mastercard" />
            <PaymentMark label="TROY" />
          </div>
        </div>
      </div>
    </footer>
  );
}
