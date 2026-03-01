import { useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useHome } from '@/hooks/useHome';
import { HomeFilters } from '@/components/home/HomeFilters';
import { ProductList } from '@/components/home/ProductList';
import { TopWishlistedProducts } from '@/components/home/TopWishlistedProducts';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { 
  setCategoryId, 
  setSearch, 
  setSortBy, 
  setSortDesc, 
  setPage 
} from '@/features/products/productsSlice';

export default function Home() {
  const dispatch = useAppDispatch();
  const [searchParams] = useSearchParams();
  

  const { page, search, categoryId, sortBy, sortDesc } = useAppSelector((state) => state.products);


  useEffect(() => {

    const urlCategoryId = searchParams.get('categoryId') || '';
    const urlSearch = searchParams.get('q') || '';
    const urlSortBy = searchParams.get('sort') || 'createdAt';
    const urlOrder = searchParams.get('order') || 'desc';
    const urlPage = parseInt(searchParams.get('page') || '1');


    if (urlCategoryId !== categoryId) dispatch(setCategoryId(urlCategoryId));
    if (urlSearch !== search) dispatch(setSearch(urlSearch));
    if (urlSortBy !== sortBy) dispatch(setSortBy(urlSortBy));
    
    const isDesc = urlOrder === 'desc';
    if (isDesc !== sortDesc) dispatch(setSortDesc(isDesc));
    
    if (urlPage !== page) dispatch(setPage(urlPage));

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]);



  const {
    categories,
    productsData,
    isLoading,
    isAddingToCart,
    handleAddToCart,
  } = useHome();

  const selectedCategory = categories?.find(
    (category) => category.id.toString() === categoryId
  );

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Hero Section */}
      <div className="mb-8 text-center bg-muted/30 py-12 rounded-lg">
        <h1 className="text-4xl font-bold mb-4">Hoş Geldiniz</h1>
        <p className="text-xl text-muted-foreground">
          En kaliteli ürünleri keşfedin ve avantajlı fiyatlarla alışveriş yapın
        </p>
      </div>

      <div className="flex flex-col lg:flex-row gap-6 items-start">
        {/* Sidebar Filters */}
        <aside className="w-full lg:w-64 xl:w-72 flex-shrink-0">
          <HomeFilters categories={categories} />
        </aside>

        {/* Product Grid */}
        <main className="flex-1 w-full">
          <TopWishlistedProducts
            categoryId={selectedCategory?.id}
            categoryName={selectedCategory?.name}
          />
          <ProductList
            isLoading={isLoading}
            productsData={productsData}
            isAddingToCart={isAddingToCart}
            handleAddToCart={handleAddToCart}
          />
        </main>
      </div>
    </div>
  );
}
