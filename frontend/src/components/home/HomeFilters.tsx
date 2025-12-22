import { Search } from 'lucide-react';
import { Input } from '@/components/common/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/common/select';
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
    <div className="mb-8 grid grid-cols-1 md:grid-cols-4 gap-4">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Ürün ara..."
          value={search}
          onChange={(e) => dispatch(setSearch(e.target.value))}
          className="pl-10"
        />
      </div>

      <Select 
        value={categoryId} 
        onValueChange={(value) => dispatch(setCategoryId(value))}
      >
        <SelectTrigger>
          <SelectValue placeholder="Kategori seçin" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">Tüm Kategoriler</SelectItem>
          {categories?.map((cat) => (
            <SelectItem key={cat.id} value={cat.id.toString()}>
              {cat.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

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
  );
};
