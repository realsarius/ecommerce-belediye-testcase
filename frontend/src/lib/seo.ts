export const DEFAULT_SITE_URL = 'https://ecommerce.berkansozer.com';
export const DEFAULT_SITE_NAME = 'E-Ticaret';
export const DEFAULT_OG_IMAGE = `${DEFAULT_SITE_URL}/favicon.svg`;

export function getSiteUrl() {
  const envSiteUrl = import.meta.env.VITE_SITE_URL as string | undefined;
  const candidate = envSiteUrl?.trim() || (typeof window !== 'undefined' ? window.location.origin : DEFAULT_SITE_URL);
  return candidate.replace(/\/+$/, '');
}

export function joinUrl(baseUrl: string, pathOrUrl: string) {
  if (/^https?:\/\//i.test(pathOrUrl)) {
    return pathOrUrl;
  }

  const normalizedBase = baseUrl.replace(/\/+$/, '');
  const normalizedPath = pathOrUrl.startsWith('/') ? pathOrUrl : `/${pathOrUrl}`;
  return `${normalizedBase}${normalizedPath}`;
}

export function upsertMeta(selector: string, attributes: Record<string, string>) {
  let element = document.head.querySelector(selector) as HTMLMetaElement | null;
  if (!element) {
    element = document.createElement('meta');
    document.head.appendChild(element);
  }

  Object.entries(attributes).forEach(([key, value]) => {
    element?.setAttribute(key, value);
  });
}

export function upsertLink(selector: string, attributes: Record<string, string>) {
  let element = document.head.querySelector(selector) as HTMLLinkElement | null;
  if (!element) {
    element = document.createElement('link');
    document.head.appendChild(element);
  }

  Object.entries(attributes).forEach(([key, value]) => {
    element?.setAttribute(key, value);
  });
}

export function truncateDescription(value: string, maxLength = 160) {
  const normalized = value.replace(/\s+/g, ' ').trim();
  if (normalized.length <= maxLength) {
    return normalized;
  }

  return `${normalized.slice(0, maxLength - 1).trimEnd()}…`;
}
