import { useSearchParams } from 'react-router-dom';
import { Package } from 'lucide-react';
import { Card, CardContent } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { InlineProductRail } from '@/components/home/InlineProductRail';
import { ProductFeedCard } from '@/components/products/ProductFeedCard';
import { useAppSelector } from '@/app/hooks';
import { toast } from 'sonner';
import { useGetWishlistQuery, useAddWishlistItemMutation, useRemoveWishlistItemMutation } from '@/features/wishlist/wishlistApi';
import { getWishlistErrorMessage, useGuestWishlist } from '@/features/wishlist';
import { useGetPersonalizedRecommendationsQuery, useSearchProductsQuery } from '@/features/products/productsApi';
import { isDiscoveryFeedContext } from '@/features/products/productsSlice';
import {
  buildDedupedRailItems,
  getRailInsertionConfig,
  getRailFetchFlags,
  getRailRenderFlags,
  splitProductsForInlineRails,
} from '@/components/home/productFeedRailRules';
import type { PaginatedResponse } from '@/types/api';
import type { Product } from '@/features/products/types';

interface ProductListProps {
  isLoading: boolean;
  productsData: PaginatedResponse<Product> | undefined;
  isAddingToCart: boolean;
  handleAddToCart: (productId: number, productName: string) => void;
}

export const ProductList = ({
  isLoading,
  productsData,
  isAddingToCart,
  handleAddToCart,
}: ProductListProps) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const productFilters = useAppSelector((state) => state.products);
  const { page } = productFilters;
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const hasDiscoveryFeedContext = isDiscoveryFeedContext(productFilters);
  const railInsertionConfig = getRailInsertionConfig(searchParams.get('railMode'));

  const { data: wishlistData } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const [addToWishlist] = useAddWishlistItemMutation();
  const [removeFromWishlist] = useRemoveWishlistItemMutation();
  const { addProduct, isPending, removeProduct } = useGuestWishlist();
  const allProducts = productsData?.items ?? [];
  const {
    firstSegment: firstProductSegment,
    secondSegment: secondProductSegment,
    remainingSegment: remainingProductSegment,
  } = splitProductsForInlineRails(allProducts, railInsertionConfig);
  const { shouldFetchPersonalizedRail, shouldFetchTopWishlistedRail } = getRailFetchFlags({
    hasDiscoveryFeedContext,
    isAuthenticated,
    totalProducts: allProducts.length,
    insertionConfig: railInsertionConfig,
  });
  const { data: personalizedRecommendations, isLoading: isPersonalizedLoading } = useGetPersonalizedRecommendationsQuery(
    { take: 6 },
    { skip: !shouldFetchPersonalizedRail },
  );
  const { data: topWishlistedData, isLoading: isTopWishlistedLoading } = useSearchProductsQuery(
    {
      page: 1,
      pageSize: 8,
      sortBy: 'wishlistCount',
      sortDescending: true,
    },
    { skip: !shouldFetchTopWishlistedRail },
  );

  const isProductInServerWishlist = (productId: number) =>
    wishlistData?.items?.some((item: { productId: number }) => item.productId === productId) ?? false;

  const isProductInWishlist = (productId: number) =>
    isProductInServerWishlist(productId) || isPending(productId);

  const handleWishlistToggle = async (e: React.MouseEvent, productId: number) => {
    e.preventDefault();
    e.stopPropagation();

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
      if (isPending(productId) && !isProductInServerWishlist(productId)) {
        removeProduct(productId);
        toast.info('Ürün senkronizasyon kuyruğundan çıkarıldı.');
      } else if (isProductInServerWishlist(productId)) {
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

  const { personalizedItems, topWishlistedItems } = buildDedupedRailItems({
    feedProducts: allProducts,
    personalizedCandidates: personalizedRecommendations ?? [],
    topWishlistedCandidates: topWishlistedData?.items ?? [],
  });

  const { shouldRenderPersonalizedRail, shouldRenderTopWishlistedRail } = getRailRenderFlags({
    shouldFetchPersonalizedRail,
    shouldFetchTopWishlistedRail,
    isPersonalizedLoading,
    isTopWishlistedLoading,
    personalizedCount: personalizedItems.length,
    topWishlistedCount: topWishlistedItems.length,
  });

  const renderProductCards = (items: Product[]) =>
    items.map((product) => (
      <ProductFeedCard
        key={product.id}
        product={product}
        isAddingToCart={isAddingToCart}
        isInWishlist={isProductInWishlist(product.id)}
        onAddToCart={handleAddToCart}
        onWishlistToggle={handleWishlistToggle}
      />
    ));

  const handlePageChange = (newPage: number) => {
    setSearchParams(prev => {
      const p = new URLSearchParams(prev);
      p.set('page', newPage.toString());
      return p;
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 place-items-center sm:place-items-stretch">
        {Array.from({ length: 8 }).map((_, i) => (
          <Card key={i} className="overflow-hidden w-full max-w-sm">
            <Skeleton className="h-48 w-full" />
            <CardContent className="p-4">
              <Skeleton className="h-4 w-3/4 mb-2" />
              <Skeleton className="h-4 w-1/2" />
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  if (productsData?.items?.length === 0) {
    return (
      <div className="text-center py-12">
        <Package className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
        <p className="text-lg text-muted-foreground">Ürün bulunamadı</p>
      </div>
    );
  }

  return (
    <>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 place-items-center sm:place-items-stretch">
        {renderProductCards(firstProductSegment)}

        {shouldRenderPersonalizedRail ? (
          <InlineProductRail
            title="Senin İçin Öneriler"
            badgeText="Sana özel seçkiler"
            helperText="Kişiselleştirilmiş öneri motoru"
            description="Wishlist kategorilerin ve son aramalarına göre öneriler."
            tone="personalized"
            products={personalizedItems}
            isLoading={isPersonalizedLoading}
            isAddingToCart={isAddingToCart}
            isInWishlist={isProductInWishlist}
            onAddToCart={handleAddToCart}
            onWishlistToggle={handleWishlistToggle}
          />
        ) : null}

        {renderProductCards(secondProductSegment)}

        {shouldRenderTopWishlistedRail ? (
          <InlineProductRail
            title="En Çok Favorilenenler"
            badgeText="Bu hafta öne çıkanlar"
            helperText="Sosyal kanıtla öne çıkan seçimler"
            description="Kullanıcıların sık favorilediği ürünler."
            tone="wishlisted"
            products={topWishlistedItems}
            isLoading={isTopWishlistedLoading}
            isAddingToCart={isAddingToCart}
            isInWishlist={isProductInWishlist}
            onAddToCart={handleAddToCart}
            onWishlistToggle={handleWishlistToggle}
          />
        ) : null}

        {renderProductCards(remainingProductSegment)}
      </div>

      {/* Pagination */}
      {productsData && productsData.totalPages > 1 && (
        <div className="flex justify-center items-center space-x-2 mt-8">
          <Button
            variant="outline"
            disabled={!productsData.hasPreviousPage}
            onClick={() => handlePageChange(page - 1)}
          >
            Önceki
          </Button>
          <span className="text-sm text-muted-foreground">
            Sayfa {productsData.page} / {productsData.totalPages}
          </span>
          <Button
            variant="outline"
            disabled={!productsData.hasNextPage}
            onClick={() => handlePageChange(page + 1)}
          >
            Sonraki
          </Button>
        </div>
      )}
    </>
  );
};
