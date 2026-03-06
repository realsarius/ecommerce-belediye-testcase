import { Link, useSearchParams } from 'react-router-dom';
import { GitCompareArrows, Package } from 'lucide-react';
import { Card, CardContent } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { InlineProductRail } from '@/components/home/InlineProductRail';
import { ProductFeedCard } from '@/components/products/ProductFeedCard';
import { useAppSelector } from '@/app/hooks';
import { toast } from 'sonner';
import { useGetWishlistQuery, useAddWishlistItemMutation, useRemoveWishlistItemMutation } from '@/features/wishlist/wishlistApi';
import { getWishlistErrorMessage, useGuestWishlist } from '@/features/wishlist';
import { buildCompareUrl, useProductCompare } from '@/features/compare';
import { useGetPersonalizedRecommendationsQuery, useSearchProductsQuery } from '@/features/products/productsApi';
import { isDiscoveryFeedContext } from '@/features/products/productsSlice';
import type { PaginatedResponse } from '@/types/api';
import type { Product } from '@/features/products/types';

interface ProductListProps {
  isLoading: boolean;
  productsData: PaginatedResponse<Product> | undefined;
  isAddingToCart: boolean;
  handleAddToCart: (productId: number, productName: string) => void;
}

const FIRST_RAIL_INSERT_INDEX = 8;
const SECOND_RAIL_INSERT_INDEX = 16;

export const ProductList = ({
  isLoading,
  productsData,
  isAddingToCart,
  handleAddToCart,
}: ProductListProps) => {
  const [, setSearchParams] = useSearchParams();
  const productFilters = useAppSelector((state) => state.products);
  const { page } = productFilters;
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const hasDiscoveryFeedContext = isDiscoveryFeedContext(productFilters);

  const { data: wishlistData } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const [addToWishlist] = useAddWishlistItemMutation();
  const [removeFromWishlist] = useRemoveWishlistItemMutation();
  const { addProduct, isPending, removeProduct } = useGuestWishlist();
  const { addProduct: addCompareProduct, containsProduct, removeProduct: removeCompareProduct, compareIds } = useProductCompare();
  const allProducts = productsData?.items ?? [];
  const canInsertFirstRail = allProducts.length > FIRST_RAIL_INSERT_INDEX;
  const canInsertSecondRail = allProducts.length > SECOND_RAIL_INSERT_INDEX;
  const shouldFetchPersonalizedRail = hasDiscoveryFeedContext && isAuthenticated && canInsertFirstRail;
  const shouldFetchTopWishlistedRail = hasDiscoveryFeedContext && canInsertSecondRail;
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

  const handleCompareToggle = (e: React.MouseEvent, productId: number, productName: string) => {
    e.preventDefault();
    e.stopPropagation();

    if (containsProduct(productId)) {
      removeCompareProduct(productId);
      toast.info(`${productName} karşılaştırma listesinden çıkarıldı.`);
      return;
    }

    const result = addCompareProduct(productId);
    if (result.limitReached) {
      toast.error('Karşılaştırma listesi en fazla 4 ürün içerebilir.');
      return;
    }

    if (result.alreadyExists) {
      toast.info(`${productName} zaten karşılaştırma listesinde.`);
      return;
    }

    toast.success(`${productName} karşılaştırma listesine eklendi.`);
  };

  const firstProductSegment = allProducts.slice(0, FIRST_RAIL_INSERT_INDEX);
  const secondProductSegment = allProducts.slice(FIRST_RAIL_INSERT_INDEX, SECOND_RAIL_INSERT_INDEX);
  const remainingProductSegment = allProducts.slice(SECOND_RAIL_INSERT_INDEX);
  const productIdsInFeed = new Set(allProducts.map((product) => product.id));

  const personalizedItems = (personalizedRecommendations ?? [])
    .filter((item) => !productIdsInFeed.has(item.id))
    .slice(0, 6);
  const personalizedIds = new Set(personalizedItems.map((item) => item.id));

  const topWishlistedItems = (topWishlistedData?.items ?? [])
    .filter((item) => item.wishlistCount > 0)
    .filter((item) => !productIdsInFeed.has(item.id))
    .filter((item) => !personalizedIds.has(item.id))
    .slice(0, 8);

  const shouldRenderPersonalizedRail = shouldFetchPersonalizedRail
    && (isPersonalizedLoading || personalizedItems.length > 0);

  const shouldRenderTopWishlistedRail = shouldFetchTopWishlistedRail
    && (isTopWishlistedLoading || topWishlistedItems.length > 0);

  const renderProductCards = (items: Product[]) =>
    items.map((product) => (
      <ProductFeedCard
        key={product.id}
        product={product}
        isAddingToCart={isAddingToCart}
        isInWishlist={isProductInWishlist(product.id)}
        isInCompare={containsProduct(product.id)}
        onAddToCart={handleAddToCart}
        onWishlistToggle={handleWishlistToggle}
        onCompareToggle={handleCompareToggle}
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

      {compareIds.length > 0 && (
        <div className="mt-6 flex justify-center">
          <Button variant="outline" asChild>
            <Link to={buildCompareUrl(compareIds)}>
              <GitCompareArrows className="h-4 w-4" />
              Karşılaştırma listesini aç ({compareIds.length})
            </Link>
          </Button>
        </div>
      )}
    </>
  );
};
