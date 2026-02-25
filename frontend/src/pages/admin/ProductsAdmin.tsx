import { useState } from 'react';
import { useDebounce } from '@/hooks/useDebounce';
import { useSearchProductsQuery, useDeleteProductMutation, useUpdateStockMutation } from '@/features/products/productsApi';
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import { Label } from '@/components/common/label';
import { Skeleton } from '@/components/common/skeleton';
import { Plus, Pencil, Trash2, Package as PackageIcon, Search } from 'lucide-react';
import { toast } from 'sonner';
import { Link } from 'react-router-dom';

export default function AdminProducts() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 400); // 400ms gecikme
  const [stockDialog, setStockDialog] = useState<{ open: boolean; productId: number; productName: string }>({
    open: false,
    productId: 0,
    productName: '',
  });
  const [stockChange, setStockChange] = useState('');
  const [stockReason, setStockReason] = useState('');

  const { data: products, isLoading } = useSearchProductsQuery({
    page,
    pageSize: 10,
    search: debouncedSearch || undefined,
  });
  const [deleteProduct, { isLoading: isDeleting }] = useDeleteProductMutation();
  const [updateStock, { isLoading: isUpdatingStock }] = useUpdateStockMutation();

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`"${name}" ürününü silmek istediğinize emin misiniz?`)) return;
    try {
      await deleteProduct(id).unwrap();
      toast.success('Ürün silindi');
    } catch {
      toast.error('Ürün silinemedi');
    }
  };

  const handleStockUpdate = async () => {
    const quantity = parseInt(stockChange);
    if (isNaN(quantity) || quantity === 0) {
      toast.error('Geçerli bir miktar girin');
      return;
    }
    if (!stockReason.trim()) {
      toast.error('Açıklama girin');
      return;
    }
    try {
      await updateStock({
        id: stockDialog.productId,
        data: { quantityChange: quantity, reason: stockReason },
      }).unwrap();
      toast.success('Stok güncellendi');
      setStockDialog({ open: false, productId: 0, productName: '' });
      setStockChange('');
      setStockReason('');
    } catch {
      toast.error('Stok güncellenemedi');
    }
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold">Ürünler</h1>
        <Button asChild>
          <Link to="/admin/products/new">
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
                        <div className="h-10 w-10 bg-muted rounded-lg flex items-center justify-center">
                          <PackageIcon className="h-5 w-5 text-muted-foreground" />
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
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() =>
                          setStockDialog({
                            open: true,
                            productId: product.id,
                            productName: product.name,
                          })
                        }
                      >
                        {product.stockQuantity}
                      </Button>
                    </TableCell>
                    <TableCell>
                      <Badge variant={product.isActive ? 'default' : 'secondary'}>
                        {product.isActive ? 'Aktif' : 'Pasif'}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button variant="ghost" size="icon" asChild>
                          <Link to={`/admin/products/${product.id}`}>
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

      {/* Stock Update Dialog */}
      <Dialog open={stockDialog.open} onOpenChange={(open) => setStockDialog({ ...stockDialog, open })}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Stok Güncelle</DialogTitle>
            <DialogDescription>{stockDialog.productName}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Miktar Değişimi</Label>
              <Input
                type="number"
                placeholder="+10 veya -5"
                value={stockChange}
                onChange={(e) => setStockChange(e.target.value)}
              />
              <p className="text-sm text-muted-foreground">
                Pozitif sayı ekler, negatif sayı çıkarır
              </p>
            </div>
            <div className="space-y-2">
              <Label>Açıklama</Label>
              <Input
                placeholder="Stok sayımı, satış, iade vb."
                value={stockReason}
                onChange={(e) => setStockReason(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setStockDialog({ open: false, productId: 0, productName: '' })}>
              İptal
            </Button>
            <Button onClick={handleStockUpdate} disabled={isUpdatingStock}>
              Güncelle
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
