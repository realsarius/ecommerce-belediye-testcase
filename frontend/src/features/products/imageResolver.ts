import type { Product, ProductImage } from './types';

function compareImages(left: ProductImage, right: ProductImage): number {
  if (left.isPrimary !== right.isPrimary) {
    return left.isPrimary ? -1 : 1;
  }

  return left.sortOrder - right.sortOrder;
}

export function getProductImageUrls(product: Pick<Product, 'images' | 'primaryImageUrl'>): string[] {
  const urls: string[] = [];
  const seen = new Set<string>();

  const append = (url?: string | null) => {
    const normalized = url?.trim();
    if (!normalized || seen.has(normalized)) {
      return;
    }

    seen.add(normalized);
    urls.push(normalized);
  };

  append(product.primaryImageUrl);

  (product.images ?? [])
    .slice()
    .sort(compareImages)
    .forEach((image) => append(image.imageUrl));

  return urls;
}
