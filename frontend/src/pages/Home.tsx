import { useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useHome } from '@/hooks/useHome';
import { HomeFilters } from '@/components/home/HomeFilters';
import { ProductList } from '@/components/home/ProductList';
import { useAppDispatch } from '@/app/hooks';
import { setCategoryId } from '@/features/products/productsSlice';

export default function Home() {
  const dispatch = useAppDispatch();
  const [searchParams] = useSearchParams();
  
  // Sync URL categoryId to Redux state
  useEffect(() => {
    const urlCategoryId = searchParams.get('categoryId');
    dispatch(setCategoryId(urlCategoryId || ''));
  }, [searchParams, dispatch]);

  const {
    categories,
    productsData,
    isLoading,
    isAddingToCart,
    handleAddToCart,
  } = useHome();

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Hero Section */}
      <div className="mb-12 text-center">
        <h1 className="text-4xl font-bold mb-4">Hoş Geldiniz</h1>
        <p className="text-xl text-muted-foreground">
          En kaliteli ürünleri keşfedin
        </p>
      </div>

      <HomeFilters categories={categories} />

      <ProductList
        isLoading={isLoading}
        productsData={productsData}
        isAddingToCart={isAddingToCart}
        handleAddToCart={handleAddToCart}
      />
    </div>
  );
}
