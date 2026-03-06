import { useParams, Link } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { useSeoMeta } from '@/hooks/useSeoMeta';
import { getSiteUrl, truncateDescription } from '@/lib/seo';
import { useGetProductQuery } from '@/features/products/productsApi';
import { useAddToCartMutation } from '@/features/cart/cartApi';
import { useAppSelector } from '@/app/hooks';
import { Button } from '@/components/common/button';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import { Separator } from '@/components/common/separator';
import { ShoppingCart, Package, ArrowLeft, Check, X, Heart, GitCompareArrows, ChevronLeft, ChevronRight } from 'lucide-react';
import { toast } from 'sonner';
import { ReviewList } from '@/components/reviews/ReviewList';
import { StarRating } from '@/components/reviews/StarRating';
import { useGetWishlistQuery, useAddWishlistItemMutation, useRemoveWishlistItemMutation } from '@/features/wishlist/wishlistApi';
import { getWishlistErrorMessage, useGuestWishlist } from '@/features/wishlist';
import {
  useGetAlsoViewedRecommendationsQuery,
  useGetFrequentlyBoughtRecommendationsQuery,
  useTrackProductViewMutation,
  useTrackRecommendationClickMutation,
} from '@/features/products/productsApi';
import { getRecommendationSessionId } from '@/features/products/recommendationSession';
import { ProductRecommendationSection } from '@/components/products/ProductRecommendationSection';
import { CampaignCountdown } from '@/components/campaigns/CampaignCountdown';
import { buildCompareUrl, useProductCompare } from '@/features/compare';

export default function ProductDetail() {
  const { id } = useParams<{ id: string }>();
  const productId = parseInt(id || '0');
  const [imageSelection, setImageSelection] = useState<{ productId: number | null; index: number }>({
    productId: null,
    index: 0,
  });
  const [lightbox, setLightbox] = useState<{ isOpen: boolean; index: number }>({
    isOpen: false,
    index: 0,
  });

  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const { data: product, isLoading, error } = useGetProductQuery(productId);
  const [addToCart, { isLoading: isAddingToCart }] = useAddToCartMutation();
  const [trackProductView] = useTrackProductViewMutation();
  const [trackRecommendationClick] = useTrackRecommendationClickMutation();
  const { data: alsoViewedRecommendations = [], isLoading: isLoadingAlsoViewed } = useGetAlsoViewedRecommendationsQuery(
    { productId, take: 4 },
    { skip: productId <= 0 }
  );
  const { data: frequentlyBoughtRecommendations = [], isLoading: isLoadingFrequentlyBought } = useGetFrequentlyBoughtRecommendationsQuery(
    { productId, take: 4 },
    { skip: productId <= 0 }
  );

  const { data: wishlistData } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const [addToWishlist] = useAddWishlistItemMutation();
  const [removeFromWishlist] = useRemoveWishlistItemMutation();
  const { addProduct, isPending, removeProduct } = useGuestWishlist();
  const { addProduct: addCompareProduct, compareIds, containsProduct, removeProduct: removeCompareProduct } = useProductCompare();
  const siteUrl = getSiteUrl();
  const canonicalPath = product ? `/products/${product.id}` : `/products/${productId}`;
  const productDescription = truncateDescription(
    product?.description || 'Urun detaylarini, fiyat bilgisini ve stok durumunu inceleyin.'
  );
  const productJsonLd = product ? [
    {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: [
        {
          '@type': 'ListItem',
          position: 1,
          name: 'Ana Sayfa',
          item: siteUrl,
        },
        {
          '@type': 'ListItem',
          position: 2,
          name: product.categoryName,
          item: `${siteUrl}/?categoryId=${product.categoryId}`,
        },
        {
          '@type': 'ListItem',
          position: 3,
          name: product.name,
          item: `${siteUrl}${canonicalPath}`,
        },
      ],
    },
    {
      '@context': 'https://schema.org',
      '@type': 'Product',
      name: product.name,
      description: productDescription,
      image: product.images?.map((image) => image.imageUrl),
      sku: product.sku,
      category: product.categoryName,
      offers: {
        '@type': 'Offer',
        priceCurrency: product.currency,
        price: product.price,
        availability: product.stockQuantity > 0
          ? 'https://schema.org/InStock'
          : 'https://schema.org/OutOfStock',
        url: `${siteUrl}${canonicalPath}`,
      },
      aggregateRating: product.reviewCount > 0
        ? {
            '@type': 'AggregateRating',
            ratingValue: Number(product.averageRating.toFixed(1)),
            reviewCount: product.reviewCount,
          }
        : undefined,
    },
  ] : undefined;

  useSeoMeta({
    title: product?.name ?? 'Urun Detayi',
    description: productDescription,
    canonicalPath,
    type: 'product',
    robots: error || !product ? 'noindex,follow' : 'index,follow',
    jsonLd: productJsonLd,
  });

  const isProductInServerWishlist = wishlistData?.items?.some((item: { productId: number }) => item.productId === productId) ?? false;
  const isProductInWishlist = isProductInServerWishlist || isPending(productId);

  useEffect(() => {
    if (productId <= 0) {
      return;
    }

    const sessionId = getRecommendationSessionId();
    void trackProductView({ productId, sessionId }).unwrap().catch(() => undefined);
  }, [productId, trackProductView]);

  const lightboxImageCount = product?.images?.length
    ? product.images.length
    : product?.primaryImageUrl
      ? 1
      : 0;

  useEffect(() => {
    if (!lightbox.isOpen) {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setLightbox((current) => ({ ...current, isOpen: false }));
        return;
      }

      if (event.key === 'ArrowLeft' && lightboxImageCount > 1) {
        event.preventDefault();
        setLightbox((current) => ({
          ...current,
          index: (current.index - 1 + lightboxImageCount) % lightboxImageCount,
        }));
        return;
      }

      if (event.key === 'ArrowRight' && lightboxImageCount > 1) {
        event.preventDefault();
        setLightbox((current) => ({
          ...current,
          index: (current.index + 1) % lightboxImageCount,
        }));
      }
    };

    window.addEventListener('keydown', handleKeyDown);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [lightbox.isOpen, lightboxImageCount]);

  const handleRecommendationClick = (targetProductId: number, source: 'also-viewed' | 'frequently-bought' | 'for-you') => {
    const sessionId = getRecommendationSessionId();
    void trackRecommendationClick({
      productId,
      targetProductId,
      source,
      sessionId,
    }).unwrap().catch(() => undefined);
  };

  const handleWishlistToggle = async () => {
    if (!isAuthenticated) {
      if (isPending(productId)) {
        removeProduct(productId);
        toast.info('Ürün giriş öncesi favori listenizden çıkarıldı.');
        return;
      }

      const result = addProduct(productId);
      if (result.limitReached) {
        toast.error('Bekleyen favoriler listesi maksimum kapasiteye ulaştı (500 ürün).');
        return;
      }

      toast.success('Ürün giriş sonrası favorilerinize eklenmek üzere kaydedildi.');
      return;
    }

    try {
      if (isPending(productId) && !isProductInServerWishlist) {
        removeProduct(productId);
        toast.info('Ürün senkronizasyon kuyruğundan çıkarıldı.');
      } else if (isProductInServerWishlist) {
        await removeFromWishlist(productId).unwrap();
        toast.success('Ürün favorilerden çıkarıldı.');
      } else {
        await addToWishlist({ productId }).unwrap();
        toast.success('Ürün favorilere eklendi.');
      }
    } catch (error) {
      toast.error(getWishlistErrorMessage(error, 'İşlem başarısız oldu.'));
    }
  };

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      toast.error('Sepete eklemek için giriş yapmalısınız');
      return;
    }

    if (user?.isEmailVerified === false) {
      toast.warning('Alışveriş yapabilmek için e-posta adresinizi doğrulamanız gerekiyor.');
      return;
    }

    if (!product) return;
    try {
      await addToCart({ productId: product.id, quantity: 1 }).unwrap();
      toast.success(`${product.name} sepete eklendi`);
    } catch {
      toast.error('Ürün sepete eklenemedi');
    }
  };

  const handleCompareToggle = () => {
    if (!product) {
      return;
    }

    if (containsProduct(product.id)) {
      removeCompareProduct(product.id);
      toast.info(`${product.name} karşılaştırma listesinden çıkarıldı.`);
      return;
    }

    const result = addCompareProduct(product.id);
    if (result.limitReached) {
      toast.error('Karşılaştırma listesi en fazla 4 ürün içerebilir.');
      return;
    }

    toast.success(`${product.name} karşılaştırma listesine eklendi.`);
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-32 mb-8" />
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
          <Skeleton className="aspect-square rounded-lg" />
          <div className="space-y-4">
            <Skeleton className="h-10 w-3/4" />
            <Skeleton className="h-6 w-1/4" />
            <Skeleton className="h-8 w-1/3" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-12 w-48" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <Package className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
        <h2 className="text-2xl font-semibold mb-2">Ürün Bulunamadı</h2>
        <p className="text-muted-foreground mb-6">
          Aradığınız ürün mevcut değil veya kaldırılmış olabilir.
        </p>
        <Button asChild>
          <Link to="/">Ürünlere Dön</Link>
        </Button>
      </div>
    );
  }

  const productImages = product.images?.length
    ? product.images
    : product.primaryImageUrl
      ? [{ imageUrl: product.primaryImageUrl, isPrimary: true, sortOrder: 0 }]
      : [];

  const selectedImageIndex = imageSelection.productId === product.id
    ? Math.min(imageSelection.index, Math.max(productImages.length - 1, 0))
    : 0;
  const activeImageUrl = productImages[selectedImageIndex]?.imageUrl;
  const groupedVariants = Object.entries(
    (product.variants ?? []).reduce<Record<string, string[]>>((acc, variant) => {
      if (!acc[variant.name]) {
        acc[variant.name] = [];
      }

      acc[variant.name].push(variant.value);
      return acc;
    }, {})
  );

  const lightboxImageIndex = Math.min(lightbox.index, Math.max(productImages.length - 1, 0));
  const lightboxImageUrl = productImages[lightboxImageIndex]?.imageUrl ?? null;
  const hasMultipleImages = productImages.length > 1;

  const openLightbox = (index: number) => {
    setLightbox({
      isOpen: true,
      index: Math.min(Math.max(index, 0), Math.max(productImages.length - 1, 0)),
    });
  };

  const closeLightbox = () => {
    setLightbox((current) => ({ ...current, isOpen: false }));
  };

  const goToPreviousImage = () => {
    if (!hasMultipleImages) {
      return;
    }

    setLightbox((current) => ({
      ...current,
      index: (current.index - 1 + productImages.length) % productImages.length,
    }));
  };

  const goToNextImage = () => {
    if (!hasMultipleImages) {
      return;
    }

    setLightbox((current) => ({
      ...current,
      index: (current.index + 1) % productImages.length,
    }));
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <Button variant="ghost" asChild className="mb-8">
        <Link to="/">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Ürünlere Dön
        </Link>
      </Button>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
        {/* Product Image */}
        <div className="space-y-4">
          <div className="aspect-square overflow-hidden rounded-3xl border border-border/70 bg-muted/40 flex items-center justify-center">
            {activeImageUrl ? (
              <button
                type="button"
                className="h-full w-full cursor-zoom-in"
                onClick={() => openLightbox(selectedImageIndex)}
                aria-label="Gorseli buyuk boyutta ac"
              >
                <img
                  src={activeImageUrl}
                  alt={product.name}
                  className="h-full w-full object-cover"
                />
              </button>
            ) : (
              <Package className="h-32 w-32 text-muted-foreground" />
            )}
          </div>

          {productImages.length > 1 ? (
            <div className="grid grid-cols-4 gap-3 sm:grid-cols-5">
              {productImages.map((image, index) => (
                <button
                  key={`${image.imageUrl}-${index}`}
                  type="button"
                  className={`aspect-square overflow-hidden rounded-2xl border transition ${
                    index === selectedImageIndex
                      ? 'border-primary ring-2 ring-primary/20'
                      : 'border-border/60 hover:border-primary/40'
                  }`}
                  onClick={() => setImageSelection({ productId: product.id, index })}
                >
                  <img
                    src={image.imageUrl}
                    alt={`${product.name} gorsel ${index + 1}`}
                    className="h-full w-full object-cover"
                  />
                </button>
              ))}
            </div>
          ) : null}
        </div>

        {/* Product Info */}
        <div className="space-y-6">
          <div>
            <Badge variant="secondary" className="mb-2">
              {product.categoryName}
            </Badge>
            {product.hasActiveCampaign && (
              <div className="mb-3 flex flex-wrap items-center gap-2">
                <Badge className="bg-amber-500/10 text-amber-700 dark:text-amber-200">
                  {product.campaignBadgeText || product.campaignName || 'Kampanya'}
                </Badge>
                <CampaignCountdown
                  endsAt={product.campaignEndsAt}
                  className="text-sm font-medium text-amber-700 dark:text-amber-200"
                />
              </div>
            )}
            <h1 className="text-3xl font-bold">{product.name}</h1>
            <p className="text-muted-foreground mt-1">SKU: {product.sku}</p>
          </div>

          <div className="space-y-2">
            {product.hasActiveCampaign && (
              <p className="text-lg text-muted-foreground line-through">
                {product.originalPrice.toLocaleString('tr-TR')} {product.currency}
              </p>
            )}
            <div className="text-4xl font-bold text-primary">
              {product.price.toLocaleString('tr-TR')} {product.currency}
            </div>
          </div>

          {product.wishlistCount > 0 && (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Heart className="h-4 w-4 text-red-500" />
              <span>{product.wishlistCount} kisi bu urunu favorilerine ekledi</span>
            </div>
          )}

          {/* Average Rating (Eğer varsa) */}
          <div className="flex items-center gap-2 mt-2">
            <StarRating rating={Math.round(product.averageRating || 0)} readOnly size="sm" />
            <span className="text-sm text-muted-foreground">
              {product.averageRating ? product.averageRating.toFixed(1) : '0.0'} ({product.reviewCount || 0} değerlendirme)
            </span>
          </div>

          <Separator />

          <div>
            <h3 className="font-semibold mb-2">Açıklama</h3>
            <p className="text-muted-foreground">
              {product.description || 'Bu ürün için açıklama bulunmamaktadır.'}
            </p>
          </div>

          {groupedVariants.length > 0 ? (
            <>
              <Separator />
              <div>
                <h3 className="mb-3 font-semibold">Varyantlar</h3>
                <div className="space-y-3">
                  {groupedVariants.map(([name, values]) => (
                    <div key={name} className="space-y-2">
                      <p className="text-sm font-medium">{name}</p>
                      <div className="flex flex-wrap gap-2">
                        {values.map((value) => (
                          <Badge key={`${name}-${value}`} variant="outline">
                            {value}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </>
          ) : null}

          <Separator />

          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              {product.stockQuantity > 0 ? (
                <>
                  <Check className="h-5 w-5 text-green-600" />
                  <span className="text-green-600 font-medium">Stokta</span>
                  <span className="text-muted-foreground">
                    ({product.stockQuantity} adet)
                  </span>
                </>
              ) : (
                <>
                  <X className="h-5 w-5 text-red-600" />
                  <span className="text-red-600 font-medium">Stokta Yok</span>
                </>
              )}
            </div>
          </div>

          <div className="flex gap-4">
            <Button
              size="lg"
              className="flex-1 sm:flex-none"
              disabled={product.stockQuantity === 0 || isAddingToCart}
              onClick={handleAddToCart}
            >
              <ShoppingCart className="mr-2 h-5 w-5" />
              Sepete Ekle
            </Button>

            <Button
              size="lg"
              variant="outline"
              className="px-4"
              onClick={handleWishlistToggle}
            >
              <Heart
                className={`h-5 w-5 ${isProductInWishlist ? 'fill-red-500 text-red-500' : ''}`}
              />
            </Button>
            <Button
              size="lg"
              variant={containsProduct(product.id) ? 'secondary' : 'outline'}
              className="px-4"
              onClick={handleCompareToggle}
            >
              <GitCompareArrows className="h-5 w-5" />
            </Button>
          </div>

          {compareIds.length > 0 && (
            <Button variant="ghost" asChild className="w-fit px-0">
              <Link to={buildCompareUrl(compareIds)}>
                Karşılaştırma listesini aç ({compareIds.length})
              </Link>
            </Button>
          )}
        </div>
      </div>

      {/* Yorumlar Bölümü */}
      <div className="mt-12 space-y-10">
        <ProductRecommendationSection
          title="Bu ürünü görüntüleyenler bunlara da baktı"
          description="Redis tabanlı davranış sinyallerinden gelen hızlı öneriler."
          source="also-viewed"
          products={alsoViewedRecommendations}
          isLoading={isLoadingAlsoViewed}
          onProductClick={handleRecommendationClick}
        />

        <ProductRecommendationSection
          title="Bunu alanlar şunu da aldı"
          description="Sipariş birlikteliği ve cache katmanı ile oluşturulan tamamlayıcı öneriler."
          source="frequently-bought"
          products={frequentlyBoughtRecommendations}
          isLoading={isLoadingFrequentlyBought}
          onProductClick={handleRecommendationClick}
        />
      </div>

      <ReviewList productId={product.id} />

      {lightbox.isOpen && lightboxImageUrl ? (
        <div
          className="fixed inset-0 z-[100] bg-black/85 backdrop-blur-sm"
          onClick={closeLightbox}
          role="dialog"
          aria-modal="true"
          aria-label={`${product.name} gorsel galerisi`}
        >
          <button
            type="button"
            className="absolute right-4 top-4 z-[101] rounded-full bg-white/95 p-2 text-black shadow transition hover:bg-white"
            onClick={closeLightbox}
            aria-label="Galeri penceresini kapat"
          >
            <X className="h-5 w-5" />
          </button>

          <div
            className="mx-auto flex h-full w-full max-w-7xl items-center justify-center gap-4 px-4 py-6 sm:px-6"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="relative flex w-full flex-1 items-center justify-center">
              {hasMultipleImages ? (
                <button
                  type="button"
                  className="absolute left-2 z-[101] rounded-full bg-white/90 p-2 text-black shadow transition hover:bg-white sm:left-4"
                  onClick={goToPreviousImage}
                  aria-label="Onceki gorsele gec"
                >
                  <ChevronLeft className="h-6 w-6" />
                </button>
              ) : null}

              <img
                src={lightboxImageUrl}
                alt={`${product.name} buyuk gorsel ${lightboxImageIndex + 1}`}
                className="max-h-[84vh] w-auto max-w-full rounded-2xl object-contain shadow-2xl"
              />

              {hasMultipleImages ? (
                <button
                  type="button"
                  className="absolute right-2 z-[101] rounded-full bg-white/90 p-2 text-black shadow transition hover:bg-white sm:right-4"
                  onClick={goToNextImage}
                  aria-label="Sonraki gorsele gec"
                >
                  <ChevronRight className="h-6 w-6" />
                </button>
              ) : null}
            </div>

            {hasMultipleImages ? (
              <div className="hidden h-[84vh] w-28 shrink-0 flex-col gap-3 overflow-y-auto rounded-2xl bg-black/35 p-2 lg:flex">
                {productImages.map((image, index) => (
                  <button
                    key={`lightbox-thumb-desktop-${image.imageUrl}-${index}`}
                    type="button"
                    className={`overflow-hidden rounded-xl border-2 transition ${
                      index === lightboxImageIndex
                        ? 'border-white'
                        : 'border-white/30 hover:border-white/60'
                    }`}
                    onClick={() => setLightbox((current) => ({ ...current, index }))}
                    aria-label={`${index + 1}. gorsele git`}
                  >
                    <img
                      src={image.imageUrl}
                      alt={`${product.name} thumbnail ${index + 1}`}
                      className="h-20 w-full object-cover"
                    />
                  </button>
                ))}
              </div>
            ) : null}
          </div>

          {hasMultipleImages ? (
            <div className="absolute bottom-4 left-1/2 z-[101] w-[calc(100%-2rem)] max-w-3xl -translate-x-1/2 lg:hidden">
              <div className="flex gap-2 overflow-x-auto rounded-2xl bg-black/45 p-2">
                {productImages.map((image, index) => (
                  <button
                    key={`lightbox-thumb-mobile-${image.imageUrl}-${index}`}
                    type="button"
                    className={`h-16 w-16 shrink-0 overflow-hidden rounded-lg border-2 transition ${
                      index === lightboxImageIndex
                        ? 'border-white'
                        : 'border-white/30'
                    }`}
                    onClick={() => setLightbox((current) => ({ ...current, index }))}
                    aria-label={`${index + 1}. gorsele git`}
                  >
                    <img
                      src={image.imageUrl}
                      alt={`${product.name} mobil thumbnail ${index + 1}`}
                      className="h-full w-full object-cover"
                    />
                  </button>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
