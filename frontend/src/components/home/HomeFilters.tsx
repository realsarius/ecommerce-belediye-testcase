import { Search, ChevronDown, Check } from 'lucide-react';
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
import { Label } from '@/components/common/label'; // Label yoksa div kullanırım ama genelde vardır. Yoksa div kullanayım.
import { Separator } from '@/components/common/separator';

import { useAppDispatch, useAppSelector } from '@/app/hooks';
import {
  setSearch,
  setCategoryId,
  setSortBy,
  setSortDesc,
} from '@/features/products/productsSlice';
import type { Category } from '@/features/products/types';

interface HomeFiltersProps {
  categories: Category[] | undefined;
}

export const HomeFilters = ({ categories }: HomeFiltersProps) => {
  const dispatch = useAppDispatch();
  const { search, categoryId, sortBy, sortDesc } = useAppSelector(
    (state) => state.products
  );

  return (
    <Card className="h-fit sticky top-24">
      <CardContent className="p-6 space-y-6">
        {/* Arama */}
        <div className="space-y-2">
          <h3 className="font-semibold text-sm">Ürün Ara</h3>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Ara..."
              value={search}
              onChange={(e) => dispatch(setSearch(e.target.value))}
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
              variant={!categoryId || categoryId === 'all' || categoryId === '' ? 'secondary' : 'ghost'}
              className="justify-start h-8 px-2 font-normal"
              onClick={() => dispatch(setCategoryId(''))}
            >
              Tüm Kategoriler
            </Button>
            {categories?.map((cat) => (
              <Button
                key={cat.id}
                variant={categoryId === cat.id.toString() ? 'secondary' : 'ghost'}
                className="justify-start h-8 px-2 font-normal"
                onClick={() => dispatch(setCategoryId(cat.id.toString()))}
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
                onValueChange={(value) => dispatch(setSortBy(value))}
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
                onValueChange={(value) => dispatch(setSortDesc(value === 'desc'))}
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
    </Card>
  );
};
