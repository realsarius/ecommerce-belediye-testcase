import { useGetProductsQuery } from '@/features/products/productsApi';
import { useGetCategoriesQuery } from '@/features/admin/adminApi';
import { useAddToCartMutation } from '@/features/cart/cartApi';
import { useAppSelector } from '@/app/hooks';
import { toast } from 'sonner';

export const useHome = () => {
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  // Get filter state from Redux
  const { page, search, categoryId, sortBy, sortDesc } = useAppSelector(
    (state) => state.products
  );

  const { data: categories } = useGetCategoriesQuery();
  
  const { data: productsData, isLoading } = useGetProductsQuery({
    page,
    pageSize: 12,
    search: search || undefined,
    categoryId: categoryId && categoryId !== 'all' ? parseInt(categoryId) : undefined,
    sortBy,
    sortDesc,
  });
  
  const [addToCart, { isLoading: isAddingToCart }] = useAddToCartMutation();

  const handleAddToCart = async (productId: number, productName: string) => {
    if (!isAuthenticated) {
      toast.error('Sepete eklemek için giriş yapmalısınız');
      return;
    }
    try {
      await addToCart({ productId, quantity: 1 }).unwrap();
      toast.success(`${productName} sepete eklendi`);
    } catch {
      toast.error('Ürün sepete eklenemedi');
    }
  };

  return {
    categories,
    productsData,
    isLoading,
    isAddingToCart,
    handleAddToCart,
  };
};
