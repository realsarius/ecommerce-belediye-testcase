import { useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useHome } from '@/hooks/useHome';
import { HomeFilters } from '@/components/home/HomeFilters';
import { ProductList } from '@/components/home/ProductList';
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
  const [searchParams, setSearchParams] = useSearchParams();
  
  // Redux state
  const { page, search, categoryId, sortBy, sortDesc } = useAppSelector((state) => state.products);

  // URL -> State Sync (İlk açılış ve Navigasyon değişimleri)
  useEffect(() => {
    // URL'den oku
    const urlCategoryId = searchParams.get('categoryId') || '';
    const urlSearch = searchParams.get('q') || '';
    const urlSortBy = searchParams.get('sort') || 'createdAt';
    const urlOrder = searchParams.get('order') || 'desc';
    const urlPage = parseInt(searchParams.get('page') || '1');

    // State ile karşılaştır ve farklıysa güncelle (Dispatch)
    if (urlCategoryId !== categoryId) dispatch(setCategoryId(urlCategoryId));
    if (urlSearch !== search) dispatch(setSearch(urlSearch));
    if (urlSortBy !== sortBy) dispatch(setSortBy(urlSortBy));
    
    const isDesc = urlOrder === 'desc';
    if (isDesc !== sortDesc) dispatch(setSortDesc(isDesc));
    
    if (urlPage !== page) dispatch(setPage(urlPage));

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]); // Sadece searchParams değişince çalışır

  // 2. State -> URL Sync (Kullanıcı etkileşimleri)
  useEffect(() => {
    const params = new URLSearchParams(searchParams);

    if (categoryId) params.set('categoryId', categoryId);
    else params.delete('categoryId');

    if (search) params.set('q', search);
    else params.delete('q');

    if (sortBy && sortBy !== 'createdAt') params.set('sort', sortBy);
    else params.delete('sort');

    if (!sortDesc) params.set('order', 'asc');
    else params.delete('order'); // Default desc kabul ediyoruz

    if (page > 1) params.set('page', page.toString());
    else params.delete('page');

    // Eğer URL parametreleri değiştiyse güncelle
    if (params.toString() !== searchParams.toString()) {
      setSearchParams(params, { replace: true });
    }
  }, [categoryId, search, sortBy, sortDesc, page, setSearchParams]);

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
      <div className="mb-8 text-center bg-muted/30 py-12 rounded-lg">
        <h1 className="text-4xl font-bold mb-4">Hoş Geldiniz</h1>
        <p className="text-xl text-muted-foreground">
          En kaliteli ürünleri keşfedin ve avantajlı fiyatlarla alışveriş yapın
        </p>
      </div>

      <div className="flex flex-col lg:flex-row gap-8 items-start">
        {/* Sidebar Filters */}
        <aside className="w-full lg:w-72 flex-shrink-0">
          <HomeFilters categories={categories} />
        </aside>

        {/* Product Grid */}
        <main className="flex-1">
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
