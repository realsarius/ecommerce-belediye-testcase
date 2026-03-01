import { useParams, Link } from 'react-router-dom';
import { useEffect } from 'react';
import { useGetProductQuery } from '@/features/products/productsApi';
import { useAddToCartMutation } from '@/features/cart/cartApi';
import { useAppSelector } from '@/app/hooks';
import { Button } from '@/components/common/button';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import { Separator } from '@/components/common/separator';
import { ShoppingCart, Package, ArrowLeft, Check, X, Heart } from 'lucide-react';
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

export default function ProductDetail() {
  const { id } = useParams<{ id: string }>();
  const productId = parseInt(id || '0');

  const { isAuthenticated } = useAppSelector((state) => state.auth);
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

  const isProductInServerWishlist = wishlistData?.items?.some((item: { productId: number }) => item.productId === productId) ?? false;
  const isProductInWishlist = isProductInServerWishlist || isPending(productId);

  useEffect(() => {
    if (productId <= 0) {
      return;
    }

    const sessionId = getRecommendationSessionId();
    void trackProductView({ productId, sessionId }).unwrap().catch(() => undefined);
  }, [productId, trackProductView]);

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
    if (!product) return;
    try {
      await addToCart({ productId: product.id, quantity: 1 }).unwrap();
      toast.success(`${product.name} sepete eklendi`);
    } catch {
      toast.error('Ürün sepete eklenemedi');
    }
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
        <div className="aspect-square bg-muted rounded-lg flex items-center justify-center">
          <Package className="h-32 w-32 text-muted-foreground" />
        </div>

        {/* Product Info */}
        <div className="space-y-6">
          <div>
            <Badge variant="secondary" className="mb-2">
              {product.categoryName}
            </Badge>
            <h1 className="text-3xl font-bold">{product.name}</h1>
            <p className="text-muted-foreground mt-1">SKU: {product.sku}</p>
          </div>

          <div className="text-4xl font-bold text-primary">
            {product.price.toLocaleString('tr-TR')} {product.currency}
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
          </div>
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
    </div>
  );
}
