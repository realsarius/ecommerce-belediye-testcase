import { Link, useSearchParams } from 'react-router-dom';
import { ShoppingCart, Package } from 'lucide-react';
import { Card, CardContent, CardFooter } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import { useAppSelector } from '@/app/hooks';
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
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {Array.from({ length: 8 }).map((_, i) => (
          <Card key={i} className="overflow-hidden">
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
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {productsData?.items?.map((product) => (
          <Card key={product.id} className="overflow-hidden group">
            <div className="relative h-48 bg-muted flex items-center justify-center">
              <Package className="h-16 w-16 text-muted-foreground" />
              {product.stockQuantity === 0 && (
                <Badge variant="destructive" className="absolute top-2 right-2">
                  Stokta Yok
                </Badge>
              )}
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
