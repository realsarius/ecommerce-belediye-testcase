import { useSearchParams } from 'react-router-dom';
import { useDebounce } from '@/hooks/useDebounce';
import { useEffect, useState } from 'react';
import { Search, ChevronDown, Filter } from 'lucide-react';
import { Input } from '@/components/common/input';
import { Button } from '@/components/common/button';
import { Card, CardContent } from '@/components/common/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { Separator } from '@/components/common/separator';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/common/collapsible';
import { cn } from '@/lib/utils';
import type { Category } from '@/features/products/types';

interface HomeFiltersProps {
  categories: Category[] | undefined;
}

export const HomeFilters = ({ categories }: HomeFiltersProps) => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [isOpen, setIsOpen] = useState(false);
  
  const categoryId = searchParams.get('categoryId') || '';
  const search = searchParams.get('q') || '';
  const sortBy = searchParams.get('sort') || 'createdAt';
  const order = searchParams.get('order') || 'desc';
  const sortDesc = order === 'desc';

  const [localSearch, setLocalSearch] = useState(search);
  const debouncedSearch = useDebounce(localSearch, 500);

  useEffect(() => {
    if (search !== localSearch) {
        setLocalSearch(search);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  useEffect(() => {
    if (debouncedSearch !== search) {
       handleFilterChange('q', debouncedSearch);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch]);


  const handleFilterChange = (key: string, value: string) => {
    setSearchParams(prev => {
      const newParams = new URLSearchParams(prev);
      if (value && value !== 'all') {
        newParams.set(key, value);
      } else {
        newParams.delete(key);
      }
      
      newParams.delete('page');
      
      return newParams;
    }, { replace: true });
  };

  const activeFiltersCount = [categoryId, search, sortBy !== 'createdAt' ? sortBy : '', order !== 'desc' ? order : ''].filter(Boolean).length;

  const filterContent = (
    <CardContent className="p-6 space-y-6">
      {/* Arama */}
      <div className="space-y-2">
        <h3 className="font-semibold text-sm">Ürün Ara</h3>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Ara..."
            value={localSearch}
            onChange={(e) => setLocalSearch(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      <Separator />

      {/* Kategoriler */}
      <div className="space-y-2">
        <h3 className="font-semibold text-sm">Kategoriler</h3>
        <div className="flex flex-col space-y-1">
          <Button
            variant={!categoryId ? 'secondary' : 'ghost'}
            className="justify-start h-8 px-2 font-normal"
            onClick={() => handleFilterChange('categoryId', '')}
          >
            Tüm Kategoriler
          </Button>
          {categories?.map((cat) => (
            <Button
              key={cat.id}
              variant={categoryId === cat.id.toString() ? 'secondary' : 'ghost'}
              className="justify-start h-8 px-2 font-normal"
              onClick={() => handleFilterChange('categoryId', cat.id.toString())}
            >
              {cat.name}
              <span className="ml-auto text-xs text-muted-foreground">
                ({cat.productCount})
              </span>
            </Button>
          ))}
        </div>
      </div>

      <Separator />

      {/* Sıralama */}
      <div className="space-y-4">
        <h3 className="font-semibold text-sm">Sıralama</h3>
        
        <div className="space-y-2">
          <label className="text-xs text-muted-foreground">Ölçüt</label>
          <Select 
              value={sortBy} 
              onValueChange={(value) => handleFilterChange('sort', value === 'createdAt' ? '' : value)}
          >
              <SelectTrigger>
              <SelectValue placeholder="Sıralama" />
              </SelectTrigger>
              <SelectContent>
              <SelectItem value="createdAt">Yeni Eklenen</SelectItem>
              <SelectItem value="price">Fiyat</SelectItem>
              <SelectItem value="name">İsim</SelectItem>
              </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <label className="text-xs text-muted-foreground">Yön</label>
          <Select
              value={sortDesc ? 'desc' : 'asc'}
              onValueChange={(value) => handleFilterChange('order', value === 'desc' ? '' : 'asc')}
          >
              <SelectTrigger>
              <SelectValue placeholder="Sıra" />
              </SelectTrigger>
              <SelectContent>
              <SelectItem value="desc">Azalan</SelectItem>
              <SelectItem value="asc">Artan</SelectItem>
              </SelectContent>
          </Select>
        </div>
      </div>
    </CardContent>
  );

  return (
    <>
      {/* Desktop: Normal card görünümü */}
      <Card className="h-fit sticky top-24 hidden lg:block">
        {filterContent}
      </Card>

      {/* Mobile: Açılıp kapanabilir filtre */}
      <div className="lg:hidden">
        <Collapsible open={isOpen} onOpenChange={setIsOpen}>
          <Card>
            <CollapsibleTrigger asChild>
              <Button
                variant="ghost"
                className="w-full flex items-center justify-between p-4 h-auto"
              >
                <div className="flex items-center gap-2">
                  <Filter className="h-4 w-4" />
                  <span className="font-semibold">Filtreler</span>
                  {activeFiltersCount > 0 && (
                    <span className="bg-primary text-primary-foreground text-xs px-2 py-0.5 rounded-full">
                      {activeFiltersCount}
                    </span>
                  )}
                </div>
                <ChevronDown
                  className={cn(
                    "h-4 w-4 transition-transform duration-200",
                    isOpen && "rotate-180"
                  )}
                />
              </Button>
            </CollapsibleTrigger>
            <CollapsibleContent>
              {filterContent}
            </CollapsibleContent>
          </Card>
        </Collapsible>
      </div>
    </>
  );
};
