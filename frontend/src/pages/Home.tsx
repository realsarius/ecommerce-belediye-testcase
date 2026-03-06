import { useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useHome } from '@/hooks/useHome';
import { useSeoMeta } from '@/hooks/useSeoMeta';
import { getSiteUrl, truncateDescription } from '@/lib/seo';
import { HomeFilters } from '@/components/home/HomeFilters';
import { ProductList } from '@/components/home/ProductList';
import { ActiveCampaignSpotlight } from '@/components/home/ActiveCampaignSpotlight';
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
  const siteUrl = getSiteUrl();
  const canonicalPath = selectedCategory ? `/?categoryId=${selectedCategory.id}` : '/';
  const pageTitle = selectedCategory ? `${selectedCategory.name} Urunleri` : 'Ana Sayfa';
  const pageDescription = selectedCategory
    ? `${selectedCategory.name} kategorisindeki urunleri, kampanyalari ve en cok ilgi goren secenekleri kesfedin.`
    : 'Populer urunleri, kampanyalari ve kategorileri tek ekranda kesfedin.';
  const shouldNoIndex = Boolean(search || page > 1 || (sortBy && sortBy !== 'createdAt') || !sortDesc);
  const visibleProducts = productsData?.items ?? [];

  const homeJsonLd: Record<string, unknown>[] = [
    {
      '@context': 'https://schema.org',
      '@type': 'WebSite',
      name: 'E-Ticaret',
      url: siteUrl,
      potentialAction: {
        '@type': 'SearchAction',
        target: `${siteUrl}/?q={search_term_string}`,
        'query-input': 'required name=search_term_string',
      },
    },
  ];

  if (selectedCategory) {
    homeJsonLd.push({
      '@context': 'https://schema.org',
      '@type': 'CollectionPage',
      name: `${selectedCategory.name} kategorisi`,
      description: truncateDescription(pageDescription),
      url: `${siteUrl}${canonicalPath}`,
      mainEntity: {
        '@type': 'ItemList',
        itemListElement: visibleProducts.map((product, index) => ({
          '@type': 'ListItem',
          position: index + 1,
          url: `${siteUrl}/products/${product.id}`,
          name: product.name,
        })),
      },
    });
  }

  useSeoMeta({
    title: pageTitle,
    description: pageDescription,
    canonicalPath,
    robots: shouldNoIndex ? 'noindex,follow' : 'index,follow',
    jsonLd: homeJsonLd,
  });

  return (
    <div className="container mx-auto px-4 py-5 sm:py-6">
      {/* Hero Section */}
      <div className="mb-5 rounded-lg bg-muted/25 px-4 py-5 text-center sm:py-6">
        <h1 className="mb-2 text-2xl font-bold sm:text-3xl">Hoş Geldiniz</h1>
        <p className="text-sm text-muted-foreground sm:text-base">
          En kaliteli ürünleri keşfedin ve avantajlı fiyatlarla alışveriş yapın
        </p>
      </div>

      <div className="flex flex-col items-start gap-4 lg:flex-row">
        {/* Sidebar Filters */}
        <aside className="w-full lg:w-64 xl:w-72 flex-shrink-0">
          <HomeFilters categories={categories} />
        </aside>

        {/* Product Grid */}
        <main className="flex-1 w-full">
          <ActiveCampaignSpotlight />
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
