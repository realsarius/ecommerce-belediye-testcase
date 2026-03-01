import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { CircleHelp, Cookie, Instagram, Linkedin, MessageSquareText, Package, Play, ShieldCheck, Store } from 'lucide-react';

const disabledSoonClass = 'cursor-not-allowed text-gray-500 transition-colors';

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
    <Link to={to} className="text-gray-400 transition-colors hover:text-white">
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
      className="rounded-full border border-white/10 bg-white/5 p-2 text-gray-400 transition-colors hover:border-white/20 hover:text-white"
    >
      {children}
    </a>
  );
}

function PaymentMark({ label }: { label: string }) {
  return (
    <span className="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-xs font-semibold tracking-wide text-gray-200">
      {label}
    </span>
  );
}

export function Footer() {
  return (
    <footer className="border-t border-white/10 bg-[#1a1a2e] text-white">
      <div className="mx-auto max-w-7xl px-6 py-12">
        <div className="grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-white/10">
                <Package className="h-6 w-6 text-rose-200" />
              </div>
              <div>
                <p className="text-lg font-semibold">E-Ticaret</p>
                <p className="text-xs uppercase tracking-[0.22em] text-gray-500">Kurumsal</p>
              </div>
            </div>
            <p className="max-w-xs text-sm leading-7 text-gray-400">
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
              <CircleHelp className="h-4 w-4 text-rose-200" />
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
              <Store className="h-4 w-4 text-rose-200" />
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
              <ShieldCheck className="h-4 w-4 text-rose-200" />
              <h2 className="font-semibold">Güvenlik &amp; Gizlilik</h2>
            </div>
            <div className="space-y-3 text-sm">
              <FooterLink to="/privacy-policy">Gizlilik Politikası</FooterLink>
              <FooterLink to="/terms-of-service">Kullanım Koşulları</FooterLink>
              <div className="flex items-center gap-3">
                <FooterLink to="/cookie-policy">Çerez Politikası</FooterLink>
                <button
                  type="button"
                  className="inline-flex items-center rounded-full border border-white/10 bg-white/5 px-2 py-1 text-[11px] text-gray-300 transition-colors hover:border-white/20 hover:text-white"
                  disabled
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

        <div className="mt-10 flex flex-col gap-5 border-t border-gray-700 pt-6 lg:flex-row lg:items-center lg:justify-between">
          <p className="text-sm text-gray-400">© 2026 E-Ticaret. Tüm hakları saklıdır.</p>

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
