import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import {
  DEFAULT_SITE_NAME,
  getSiteUrl,
  joinUrl,
  truncateDescription,
  upsertLink,
  upsertMeta,
} from '@/lib/seo';

type JsonLdValue = Record<string, unknown> | Array<Record<string, unknown>>;

type SeoMetaOptions = {
  title?: string;
  description?: string;
  canonicalPath?: string;
  canonicalUrl?: string;
  robots?: string;
  type?: 'website' | 'product' | 'article';
  jsonLd?: JsonLdValue;
};

const DEFAULT_DESCRIPTION = 'En kaliteli urunleri kesfet, avantajli fiyatlarla guvenli alisveris yap.';

const routeSeo: Record<string, { title: string; description: string; robots?: string }> = {
  '/': {
    title: 'Ana Sayfa',
    description: 'Populer urunleri, kampanyalari ve kategorileri tek ekranda kesfedin.',
  },
  '/about': {
    title: 'Hakkimizda',
    description: 'E-Ticaret platformunun hizmet yaklasimini ve magazacilik vizyonunu kesfedin.',
  },
  '/faq': {
    title: 'Sikca Sorulan Sorular',
    description: 'Siparis, teslimat, iade ve odeme surecleriyle ilgili sik sorulan sorularin yanitlarini bulun.',
  },
  '/contact': {
    title: 'Iletisim',
    description: 'Destek, is birligi ve genel iletisim talepleriniz icin bize ulasin.',
  },
  '/shipping': {
    title: 'Kargo Bilgileri',
    description: 'Teslimat, kargo ucretleri ve siparis takip surecleri hakkinda bilgi alin.',
  },
  '/privacy-policy': {
    title: 'Gizlilik Politikasi',
    description: 'Kisisel verilerinizin nasil islendigi ve korundugu hakkinda bilgi alin.',
  },
  '/terms-of-service': {
    title: 'Kullanim Kosullari',
    description: 'Platform kullanimina dair temel kural ve sorumluluklari inceleyin.',
  },
  '/kvkk': {
    title: 'KVKK Aydinlatma Metni',
    description: 'Kisisel verilerin korunmasi kapsamindaki aydinlatma metnini okuyun.',
  },
  '/refund-policy': {
    title: 'Iptal ve Iade Politikasi',
    description: 'Iptal, iade ve geri odeme sureclerinin nasil isledigini ogrenin.',
  },
  '/distance-sales-contract': {
    title: 'Mesafeli Satis Sozlesmesi',
    description: 'Mesafeli satis kapsamindaki kosullari ve yukumlulukleri inceleyin.',
  },
  '/seller/guide': {
    title: 'Satici Rehberi',
    description: 'Satici olarak platformda nasil baslayacaginiza dair temel adimlari ogrenin.',
  },
  '/seller/pricing': {
    title: 'Komisyon Oranlari',
    description: 'Saticilar icin fiyatlandirma ve komisyon yapisini inceleyin.',
  },
  '/login': {
    title: 'Giris Yap',
    description: 'Hesabiniza giris yaparak siparislerinizi, favorilerinizi ve bildirimlerinizi yonetin.',
    robots: 'noindex,follow',
  },
  '/register': {
    title: 'Kayit Ol',
    description: 'Yeni hesap olusturarak alisverise baslayin ve kampanyalardan yararlanin.',
    robots: 'noindex,follow',
  },
};

const privatePathPrefixes = [
  '/account',
  '/cart',
  '/checkout',
  '/credit-cards',
  '/gift-cards',
  '/loyalty',
  '/notifications',
  '/orders',
  '/referrals',
  '/returns',
  '/support',
  '/admin',
  '/seller/products',
  '/seller/profile',
];

const privateExactPaths = [
  '/seller',
  '/seller/register',
];

export function useSeoMeta(options: SeoMetaOptions = {}) {
  const location = useLocation();

  useEffect(() => {
    const siteUrl = getSiteUrl();
    const routeDefaults = routeSeo[location.pathname];
    const pageTitle = options.title ?? routeDefaults?.title ?? DEFAULT_SITE_NAME;
    const title = pageTitle.includes(DEFAULT_SITE_NAME) ? pageTitle : `${pageTitle} | ${DEFAULT_SITE_NAME}`;
    const description = truncateDescription(options.description ?? routeDefaults?.description ?? DEFAULT_DESCRIPTION);
    const canonical = options.canonicalUrl
      ? options.canonicalUrl
      : joinUrl(siteUrl, options.canonicalPath ?? location.pathname);
    const inferredRobots = privateExactPaths.includes(location.pathname)
      || privatePathPrefixes.some((prefix) => location.pathname.startsWith(prefix))
      ? 'noindex,follow'
      : 'index,follow';
    const robots = options.robots ?? routeDefaults?.robots ?? inferredRobots;
    const type = options.type ?? 'website';

    document.title = title;

    upsertMeta('meta[name="description"]', { name: 'description', content: description });
    upsertMeta('meta[property="og:title"]', { property: 'og:title', content: title });
    upsertMeta('meta[property="og:description"]', { property: 'og:description', content: description });
    upsertMeta('meta[property="og:type"]', { property: 'og:type', content: type });
    upsertMeta('meta[property="og:url"]', { property: 'og:url', content: canonical });
    upsertMeta('meta[property="og:site_name"]', { property: 'og:site_name', content: DEFAULT_SITE_NAME });
    upsertMeta('meta[property="og:image"]', { property: 'og:image', content: `${siteUrl}/favicon.svg` });
    upsertMeta('meta[name="twitter:card"]', { name: 'twitter:card', content: 'summary_large_image' });
    upsertMeta('meta[name="twitter:title"]', { name: 'twitter:title', content: title });
    upsertMeta('meta[name="twitter:description"]', { name: 'twitter:description', content: description });
    upsertMeta('meta[name="robots"]', { name: 'robots', content: robots });
    upsertLink('link[rel="canonical"]', { rel: 'canonical', href: canonical });

    const existingJsonLd = document.head.querySelector('script[data-seo-json-ld="active"]');
    if (existingJsonLd) {
      existingJsonLd.remove();
    }

    if (options.jsonLd) {
      const script = document.createElement('script');
      script.type = 'application/ld+json';
      script.setAttribute('data-seo-json-ld', 'active');
      script.textContent = JSON.stringify(options.jsonLd);
      document.head.appendChild(script);
    }
  }, [
    location.pathname,
    options.canonicalPath,
    options.canonicalUrl,
    options.description,
    options.jsonLd,
    options.robots,
    options.title,
    options.type,
  ]);
}
