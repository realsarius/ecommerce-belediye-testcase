import { useHome } from '@/hooks/useHome';
import { HomeFilters } from '@/components/home/HomeFilters';
import { ProductList } from '@/components/home/ProductList';

export default function Home() {
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
