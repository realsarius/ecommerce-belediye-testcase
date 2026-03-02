import { promises as fs } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const frontendDir = path.resolve(__dirname, '..');
const repoDir = path.resolve(frontendDir, '..');
const publicDir = path.join(frontendDir, 'public');
const envFilePath = path.join(frontendDir, '.env');
const envExamplePath = path.join(frontendDir, '.env.example');

function parseEnvFile(content) {
  return content
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line && !line.startsWith('#'))
    .reduce((acc, line) => {
      const separatorIndex = line.indexOf('=');
      if (separatorIndex === -1) {
        return acc;
      }

      const key = line.slice(0, separatorIndex).trim();
      const value = line.slice(separatorIndex + 1).trim();
      acc[key] = value;
      return acc;
    }, {});
}

async function readEnv() {
  const env = {};

  for (const filePath of [envExamplePath, envFilePath]) {
    try {
      const content = await fs.readFile(filePath, 'utf8');
      Object.assign(env, parseEnvFile(content));
    } catch {
      // ignore missing env files
    }
  }

  return env;
}

async function fetchJson(url) {
  const response = await fetch(url, {
    headers: {
      Accept: 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

async function loadSeedFallback() {
  const [productsJson, categoriesJson] = await Promise.all([
    fs.readFile(path.join(repoDir, 'seed-data', 'products.json'), 'utf8'),
    fs.readFile(path.join(repoDir, 'seed-data', 'categories.json'), 'utf8'),
  ]);

  const products = JSON.parse(productsJson).filter((product) => product.isActive);
  const categories = JSON.parse(categoriesJson).filter((category) => category.isActive);
  return { products, categories, source: 'seed-data' };
}

async function loadCatalogData(apiBaseUrl) {
  try {
    const categoriesPayload = await fetchJson(`${apiBaseUrl}/categories`);
    const categories = (categoriesPayload.data ?? []).filter((category) => category.isActive !== false);

    const firstPagePayload = await fetchJson(`${apiBaseUrl}/products?page=1&pageSize=500`);
    const firstPage = firstPagePayload.data;
    const totalPages = Math.max(1, firstPage.totalPages ?? 1);
    const products = [...(firstPage.items ?? [])];

    for (let page = 2; page <= totalPages; page += 1) {
      const nextPagePayload = await fetchJson(`${apiBaseUrl}/products?page=${page}&pageSize=500`);
      products.push(...(nextPagePayload.data?.items ?? []));
    }

    return {
      categories,
      products: products.filter((product) => product.isActive !== false),
      source: 'api',
    };
  } catch (error) {
    console.warn(`[seo] Catalog fetch failed, falling back to seed data: ${error.message}`);
    return loadSeedFallback();
  }
}

function xmlEscape(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&apos;');
}

function normalizeDate(value) {
  if (!value) {
    return new Date().toISOString();
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return new Date().toISOString();
  }

  return parsed.toISOString();
}

function buildUrlEntries(siteUrl, products, categories) {
  const staticRoutes = [
    { loc: `${siteUrl}/`, changefreq: 'daily', priority: '1.0' },
    { loc: `${siteUrl}/about`, changefreq: 'monthly', priority: '0.6' },
    { loc: `${siteUrl}/faq`, changefreq: 'weekly', priority: '0.7' },
    { loc: `${siteUrl}/contact`, changefreq: 'monthly', priority: '0.5' },
    { loc: `${siteUrl}/shipping`, changefreq: 'monthly', priority: '0.5' },
    { loc: `${siteUrl}/privacy-policy`, changefreq: 'yearly', priority: '0.3' },
    { loc: `${siteUrl}/terms-of-service`, changefreq: 'yearly', priority: '0.3' },
    { loc: `${siteUrl}/kvkk`, changefreq: 'yearly', priority: '0.3' },
    { loc: `${siteUrl}/refund-policy`, changefreq: 'monthly', priority: '0.4' },
    { loc: `${siteUrl}/distance-sales-contract`, changefreq: 'yearly', priority: '0.3' },
    { loc: `${siteUrl}/seller/guide`, changefreq: 'monthly', priority: '0.4' },
    { loc: `${siteUrl}/seller/pricing`, changefreq: 'monthly', priority: '0.4' },
  ];

  const categoryRoutes = categories.map((category) => ({
    loc: `${siteUrl}/?categoryId=${category.id}`,
    lastmod: normalizeDate(category.updatedAt ?? category.createdAt),
    changefreq: 'daily',
    priority: '0.8',
  }));

  const productRoutes = products.map((product) => ({
    loc: `${siteUrl}/products/${product.id}`,
    lastmod: normalizeDate(product.updatedAt ?? product.createdAt),
    changefreq: 'daily',
    priority: '0.9',
  }));

  return [...staticRoutes, ...categoryRoutes, ...productRoutes];
}

function renderSitemap(entries) {
  const urls = entries
    .map((entry) => {
      const tags = [
        `<loc>${xmlEscape(entry.loc)}</loc>`,
        entry.lastmod ? `<lastmod>${xmlEscape(entry.lastmod)}</lastmod>` : null,
        entry.changefreq ? `<changefreq>${xmlEscape(entry.changefreq)}</changefreq>` : null,
        entry.priority ? `<priority>${xmlEscape(entry.priority)}</priority>` : null,
      ].filter(Boolean);

      return `  <url>\n    ${tags.join('\n    ')}\n  </url>`;
    })
    .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`;
}

function renderRobots(siteUrl) {
  return `User-agent: *\nAllow: /\nDisallow: /admin\nDisallow: /seller/products\nDisallow: /seller/profile\nDisallow: /account\nDisallow: /cart\nDisallow: /checkout\nDisallow: /orders\nDisallow: /returns\nDisallow: /support\nDisallow: /notifications\nDisallow: /loyalty\nDisallow: /gift-cards\nDisallow: /referrals\nDisallow: /login\nDisallow: /register\n\nSitemap: ${siteUrl}/sitemap.xml\n`;
}

async function main() {
  const env = await readEnv();
  const siteUrl = (process.env.VITE_SITE_URL || env.VITE_SITE_URL || 'https://ecommerce.berkansozer.com').replace(/\/+$/, '');
  const apiBaseUrl = (process.env.VITE_API_URL || env.VITE_API_URL || 'http://localhost:5000/api/v1').replace(/\/+$/, '');

  const catalog = await loadCatalogData(apiBaseUrl);
  const entries = buildUrlEntries(siteUrl, catalog.products, catalog.categories);
  const sitemapXml = renderSitemap(entries);
  const robotsTxt = renderRobots(siteUrl);

  await fs.mkdir(publicDir, { recursive: true });
  await Promise.all([
    fs.writeFile(path.join(publicDir, 'sitemap.xml'), sitemapXml, 'utf8'),
    fs.writeFile(path.join(publicDir, 'robots.txt'), robotsTxt, 'utf8'),
  ]);

  console.info(`[seo] Generated sitemap.xml (${entries.length} urls) using ${catalog.source}`);
}

await main();
