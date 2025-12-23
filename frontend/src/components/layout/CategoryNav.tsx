import { Link, useSearchParams } from 'react-router-dom';
import { useGetCategoriesQuery } from '@/features/admin/adminApi';
import { cn } from '@/lib/utils';

export function CategoryNav() {
  const { data: categories, isLoading } = useGetCategoriesQuery();
  const [searchParams] = useSearchParams();
  const activeCategoryId = searchParams.get('categoryId');

  if (isLoading) {
    return (
      <nav className="w-full border-b bg-background">
        <div className="container mx-auto px-4">
          <div className="flex items-center gap-6 h-12 overflow-x-auto scrollbar-hide">
            {[...Array(8)].map((_, i) => (
              <div
                key={i}
                className="h-4 w-20 bg-muted animate-pulse rounded flex-shrink-0"
              />
            ))}
          </div>
        </div>
      </nav>
    );
  }

  return (
    <nav className="w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-center gap-1 h-12 overflow-x-auto scrollbar-hide">
          {/* Tüm Ürünler */}
          <Link
            to="/"
            className={cn(
              'flex-shrink-0 px-4 py-2 text-sm font-medium rounded-md transition-all duration-200',
              'hover:bg-accent hover:text-accent-foreground',
              !activeCategoryId
                ? 'bg-primary text-primary-foreground'
                : 'text-muted-foreground'
            )}
          >
            Tümü
          </Link>

          {/* Kategoriler */}
          {categories?.map((category) => (
            <Link
              key={category.id}
              to={`/?categoryId=${category.id}`}
              className={cn(
                'flex-shrink-0 px-4 py-2 text-sm font-medium rounded-md transition-all duration-200',
                'hover:bg-accent hover:text-accent-foreground',
                activeCategoryId === String(category.id)
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground'
              )}
            >
              {category.name}
            </Link>
          ))}
        </div>
      </div>
    </nav>
  );
}
