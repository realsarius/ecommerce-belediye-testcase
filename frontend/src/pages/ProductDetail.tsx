import { useParams, Link } from 'react-router-dom';
import { useGetProductQuery } from '@/features/products/productsApi';
import { useAddToCartMutation } from '@/features/cart/cartApi';
import { useAppSelector } from '@/app/hooks';
import { Button } from '@/components/common/button';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import { Separator } from '@/components/common/separator';
import { ShoppingCart, Package, ArrowLeft, Check, X } from 'lucide-react';
import { toast } from 'sonner';
import { ReviewList } from '@/components/reviews/ReviewList';
import { StarRating } from '@/components/reviews/StarRating';

export default function ProductDetail() {
  const { id } = useParams<{ id: string }>();
  const productId = parseInt(id || '0');

  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const { data: product, isLoading, error } = useGetProductQuery(productId);
  const [addToCart, { isLoading: isAddingToCart }] = useAddToCartMutation();

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

          <Button
            size="lg"
            className="w-full sm:w-auto"
            disabled={product.stockQuantity === 0 || isAddingToCart}
            onClick={handleAddToCart}
          >
            <ShoppingCart className="mr-2 h-5 w-5" />
            Sepete Ekle
          </Button>
        </div>
      </div>

      {/* Yorumlar Bölümü */}
      <ReviewList productId={product.id} />
    </div>
  );
}
