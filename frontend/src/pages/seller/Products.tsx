import { useState } from 'react';
import { useDebounce } from '@/hooks/useDebounce';
import { useGetSellerProductsQuery, useDeleteSellerProductMutation, useGetSellerProfileQuery } from '@/features/seller/sellerApi';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Badge } from '@/components/common/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { Skeleton } from '@/components/common/skeleton';
import { Plus, Pencil, Trash2, Package as PackageIcon, Search, Store } from 'lucide-react';
import { toast } from 'sonner';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/common/card';

export default function SellerProducts() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 400);

  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const { data: products, isLoading } = useGetSellerProductsQuery({
    page,
    pageSize: 10,
    search: debouncedSearch || undefined,
  });
  const [deleteProduct, { isLoading: isDeleting }] = useDeleteSellerProductMutation();

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`"${name}" ürününü silmek istediğinize emin misiniz?`)) return;
    try {
      await deleteProduct(id).unwrap();
      toast.success('Ürün silindi');
    } catch {
      toast.error('Ürün silinemedi');
    }
  };

  // No profile warning
  if (!profileLoading && !profile) {
    return (
      <div>
        <h1 className="text-3xl font-bold mb-6">Ürünlerim</h1>
        <Card className="border-amber-500 bg-amber-50 dark:bg-amber-950/30">
          <CardContent className="p-6 text-center">
            <Store className="h-12 w-12 mx-auto mb-4 text-amber-600" />
            <h2 className="text-xl font-semibold mb-2">Marka Profili Gerekli</h2>
            <p className="text-muted-foreground mb-4">
              Ürün ekleyebilmek için önce marka profilinizi oluşturmanız gerekmektedir.
            </p>
            <Button asChild className="bg-amber-600 hover:bg-amber-700">
              <Link to="/seller/profile">Profil Oluştur</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold">Ürünlerim</h1>
        <Button asChild className="bg-amber-600 hover:bg-amber-700">
          <Link to="/seller/products/new">
            <Plus className="mr-2 h-4 w-4" />
            Yeni Ürün
          </Link>
        </Button>
      </div>

      <div className="mb-6">
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Ürün ara..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            className="pl-10"
          />
        </div>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      ) : products?.items.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="p-12 text-center">
            <PackageIcon className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
            <h2 className="text-xl font-semibold mb-2">Henüz ürün eklenmedi</h2>
            <p className="text-muted-foreground mb-4">
              İlk ürününüzü ekleyerek satışa başlayın.
            </p>
            <Button asChild className="bg-amber-600 hover:bg-amber-700">
              <Link to="/seller/products/new">
                <Plus className="mr-2 h-4 w-4" />
                İlk Ürünü Ekle
              </Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="border rounded-lg">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Kategori</TableHead>
                  <TableHead>Fiyat</TableHead>
                  <TableHead>Stok</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="text-right">İşlemler</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {products?.items.map((product) => (
                  <TableRow key={product.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div className="h-10 w-10 bg-amber-100 dark:bg-amber-900/30 rounded-lg flex items-center justify-center">
                          <PackageIcon className="h-5 w-5 text-amber-600" />
                        </div>
                        <div>
                          <p className="font-medium">{product.name}</p>
                          <p className="text-sm text-muted-foreground">{product.sku}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>{product.categoryName}</TableCell>
                    <TableCell>
                      {product.price.toLocaleString('tr-TR')} {product.currency}
                    </TableCell>
                    <TableCell>{product.stockQuantity}</TableCell>
                    <TableCell>
                      <Badge variant={product.isActive ? 'default' : 'secondary'}>
                        {product.isActive ? 'Aktif' : 'Pasif'}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button variant="ghost" size="icon" asChild>
                          <Link to={`/seller/products/${product.id}`}>
                            <Pencil className="h-4 w-4" />
                          </Link>
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDelete(product.id, product.name)}
                          disabled={isDeleting}
                          className="text-destructive hover:text-destructive"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {products && products.totalPages > 1 && (
            <div className="flex justify-center items-center space-x-2 mt-4">
              <Button
                variant="outline"
                size="sm"
                disabled={!products.hasPreviousPage}
                onClick={() => setPage((p) => p - 1)}
              >
                Önceki
              </Button>
              <span className="text-sm text-muted-foreground">
                {products.page} / {products.totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={!products.hasNextPage}
                onClick={() => setPage((p) => p + 1)}
              >
                Sonraki
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
