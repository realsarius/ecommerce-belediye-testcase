import { Link, useSearchParams } from 'react-router-dom';
import { ShoppingCart, Package, Heart } from 'lucide-react';
import { Card, CardContent, CardFooter } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import { useAppSelector } from '@/app/hooks';
import { toast } from 'sonner';
import { useGetWishlistQuery, useAddWishlistItemMutation, useRemoveWishlistItemMutation } from '@/features/wishlist/wishlistApi';
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
  const [, setSearchParams] = useSearchParams();
  const { page } = useAppSelector((state) => state.products);
  const { isAuthenticated } = useAppSelector((state) => state.auth);

  const { data: wishlistData } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const [addToWishlist] = useAddWishlistItemMutation();
  const [removeFromWishlist] = useRemoveWishlistItemMutation();

  const isProductInWishlist = (productId: number) => {
    return wishlistData?.items?.some((item: { productId: number }) => item.productId === productId);
  };

  const handleWishlistToggle = async (e: React.MouseEvent, productId: number) => {
    e.preventDefault();
    e.stopPropagation();

    if (!isAuthenticated) {
      toast.error('Favorilere eklemek için giriş yapmalısınız');
      return;
    }

    try {
      if (isProductInWishlist(productId)) {
        await removeFromWishlist(productId).unwrap();
        toast.success('Ürün favorilerden çıkarıldı.');
      } else {
        await addToWishlist({ productId }).unwrap();
        toast.success('Ürün favorilere eklendi.');
      }
    } catch {
      toast.error('İşlem başarısız oldu.');
    }
  };


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
        {productsData?.items?.map((product) => (
          <Card key={product.id} className="overflow-hidden group w-full max-w-sm relative">
            <div className="relative h-48 bg-muted flex items-center justify-center">
              <Package className="h-16 w-16 text-muted-foreground" />
              {product.stockQuantity === 0 && (
                <Badge variant="destructive" className="absolute top-2 left-2">
                  Stokta Yok
                </Badge>
              )}
              <Button
                variant="ghost"
                size="icon"
                className="absolute top-2 right-2 rounded-full bg-background/50 backdrop-blur-sm hover:bg-background/80"
                onClick={(e) => handleWishlistToggle(e, product.id)}
              >
                <Heart
                  className={`h-5 w-5 ${isProductInWishlist(product.id) ? 'fill-red-500 text-red-500' : 'text-foreground'}`}
                />
              </Button>
            </div>
            <CardContent className="p-4">
              <Link to={`/products/${product.id}`}>
                <h3 className="font-semibold truncate group-hover:text-primary transition-colors">
                  {product.name}
                </h3>
              </Link>
              <p className="text-sm text-muted-foreground truncate">
                {product.categoryName}
              </p>
              <p className="text-lg font-bold mt-2">
                {product.price.toLocaleString('tr-TR')} {product.currency}
              </p>
            </CardContent>
            <CardFooter className="p-4 pt-0">
              <Button
                className="w-full"
                disabled={product.stockQuantity === 0 || isAddingToCart}
                onClick={() => handleAddToCart(product.id, product.name)}
              >
                <ShoppingCart className="mr-2 h-4 w-4" />
                Sepete Ekle
              </Button>
            </CardFooter>
          </Card>
        ))}
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
