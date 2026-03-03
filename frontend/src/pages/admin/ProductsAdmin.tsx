import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import {
  Boxes,
  Package as PackageIcon,
  Pencil,
  Power,
  Plus,
  Search,
  ShieldAlert,
  Store,
  Trash2,
} from 'lucide-react';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Checkbox } from '@/components/common/checkbox';
import { Label } from '@/components/common/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { ConfirmModal } from '@/components/admin/ConfirmModal';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { TableLoadingState } from '@/components/admin/TableLoadingState';
import { useDebounce } from '@/hooks/useDebounce';
import { useGetAdminCategoriesQuery } from '@/features/admin/adminApi';
import {
  useBulkUpdateProductsMutation,
  useDeleteProductMutation,
  useSearchProductsQuery,
  useUpdateProductMutation,
  useUpdateStockMutation,
} from '@/features/products/productsApi';

type StatusFilter = 'all' | 'active' | 'inactive';
type StockFilter = 'all' | 'critical' | 'out';
type BulkAction = 'activate' | 'deactivate' | 'delete' | '';

function formatCurrency(value: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

export default function AdminProducts() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [sellerFilter, setSellerFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [stockFilter, setStockFilter] = useState<StockFilter>('all');
  const [minPrice, setMinPrice] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [bulkAction, setBulkAction] = useState<BulkAction>('');
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null);
  const [statusTarget, setStatusTarget] = useState<{ id: number; name: string; nextState: boolean } | null>(null);
  const [bulkDeleteOpen, setBulkDeleteOpen] = useState(false);
  const [stockDialog, setStockDialog] = useState<{ open: boolean; productId: number; productName: string }>({
    open: false,
    productId: 0,
    productName: '',
  });
  const [stockChange, setStockChange] = useState('');
  const [stockReason, setStockReason] = useState('');

  const debouncedSearch = useDebounce(search, 400);

  const { data: categories = [] } = useGetAdminCategoriesQuery();
  const { data: products, isLoading } = useSearchProductsQuery({
    page,
    pageSize: 20,
    search: debouncedSearch || undefined,
    categoryId: categoryFilter === 'all' ? undefined : Number(categoryFilter),
    minPrice: minPrice ? Number(minPrice) : undefined,
    maxPrice: maxPrice ? Number(maxPrice) : undefined,
  });
  const [deleteProduct, { isLoading: isDeleting }] = useDeleteProductMutation();
  const [bulkUpdateProducts, { isLoading: isBulkUpdating }] = useBulkUpdateProductsMutation();
  const [updateProduct, { isLoading: isUpdatingProduct }] = useUpdateProductMutation();
  const [updateStock, { isLoading: isUpdatingStock }] = useUpdateStockMutation();

  const items = products?.items ?? [];

  const sellerOptions = useMemo(() => {
    const sellerMap = new Map<number, string>();

    for (const product of items) {
      if (!product.sellerId || sellerMap.has(product.sellerId)) {
        continue;
      }

      sellerMap.set(product.sellerId, product.sellerBrandName?.trim() || `Seller #${product.sellerId}`);
    }

    return Array.from(sellerMap.entries())
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name, 'tr'));
  }, [items]);

  const filteredItems = useMemo(() => {
    return items.filter((product) => {
      if (sellerFilter !== 'all' && String(product.sellerId ?? '') !== sellerFilter) {
        return false;
      }

      if (statusFilter === 'active' && !product.isActive) {
        return false;
      }

      if (statusFilter === 'inactive' && product.isActive) {
        return false;
      }

      if (stockFilter === 'critical' && !(product.stockQuantity > 0 && product.stockQuantity <= 5)) {
        return false;
      }

      if (stockFilter === 'out' && product.stockQuantity > 0) {
        return false;
      }

      return true;
    });
  }, [items, sellerFilter, statusFilter, stockFilter]);

  useEffect(() => {
    setSelectedIds((current) => current.filter((id) => filteredItems.some((product) => product.id === id)));
  }, [filteredItems]);

  const summary = useMemo(() => {
    return {
      total: filteredItems.length,
      active: filteredItems.filter((product) => product.isActive).length,
      inactive: filteredItems.filter((product) => !product.isActive).length,
      critical: filteredItems.filter((product) => product.stockQuantity <= 5).length,
    };
  }, [filteredItems]);

  const allVisibleSelected = filteredItems.length > 0 && filteredItems.every((product) => selectedIds.includes(product.id));

  const resetBulkSelection = () => {
    setSelectedIds([]);
    setBulkAction('');
  };

  const handleSelectAll = (checked: boolean) => {
    setSelectedIds(checked ? filteredItems.map((product) => product.id) : []);
  };

  const handleSelectOne = (productId: number, checked: boolean) => {
    setSelectedIds((current) => (
      checked ? [...new Set([...current, productId])] : current.filter((id) => id !== productId)
    ));
  };

  const handleDelete = async (id: number, name: string) => {
    try {
      await deleteProduct(id).unwrap();
      toast.success(`"${name}" ürünü silindi.`);
      setDeleteTarget(null);
      setSelectedIds((current) => current.filter((selectedId) => selectedId !== id));
    } catch {
      toast.error('Ürün silinemedi.');
    }
  };

  const handleBulkDelete = async () => {
    try {
      await bulkUpdateProducts({ ids: selectedIds, action: 'delete' }).unwrap();
      toast.success(`${selectedIds.length} ürün silindi.`);
      setBulkDeleteOpen(false);
      resetBulkSelection();
    } catch {
      toast.error('Toplu silme işlemi başarısız oldu.');
    }
  };

  const handleBulkAction = async () => {
    if (!bulkAction) {
      toast.error('Önce bir toplu işlem seçin.');
      return;
    }

    if (selectedIds.length === 0) {
      toast.error('Önce en az bir ürün seçin.');
      return;
    }

    if (bulkAction === 'delete') {
      setBulkDeleteOpen(true);
      return;
    }

    try {
      await bulkUpdateProducts({ ids: selectedIds, action: bulkAction }).unwrap();
      toast.success(bulkAction === 'activate' ? 'Seçili ürünler aktifleştirildi.' : 'Seçili ürünler pasife alındı.');
      resetBulkSelection();
    } catch {
      toast.error('Toplu işlem başarısız oldu.');
    }
  };

  const handleStatusToggle = async () => {
    if (!statusTarget) {
      return;
    }

    try {
      await updateProduct({
        id: statusTarget.id,
        data: { isActive: statusTarget.nextState },
      }).unwrap();
      toast.success(
        statusTarget.nextState
          ? `"${statusTarget.name}" ürünü aktifleştirildi.`
          : `"${statusTarget.name}" ürünü pasife alındı.`
      );
      setStatusTarget(null);
    } catch {
      toast.error('Ürün durumu güncellenemedi.');
    }
  };

  const handleStockUpdate = async () => {
    const quantity = parseInt(stockChange, 10);
    if (Number.isNaN(quantity) || quantity === 0) {
      toast.error('Geçerli bir miktar girin.');
      return;
    }

    if (!stockReason.trim()) {
      toast.error('Lütfen bir açıklama girin.');
      return;
    }

    try {
      await updateStock({
        id: stockDialog.productId,
        data: { quantityChange: quantity, reason: stockReason },
      }).unwrap();
      toast.success('Stok güncellendi.');
      setStockDialog({ open: false, productId: 0, productName: '' });
      setStockChange('');
      setStockReason('');
    } catch {
      toast.error('Stok güncellenemedi.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Ürün Yönetimi</h1>
          <p className="max-w-3xl text-muted-foreground">
            Katalogu filtreleyin, düşük stokları görün ve çoklu ürün aksiyonlarını tek yerden yönetin.
          </p>
        </div>
        <Button asChild>
          <Link to="/admin/products/new">
            <Plus className="mr-2 h-4 w-4" />
            Yeni Ürün
          </Link>
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Listelenen Ürün"
          value={summary.total.toLocaleString('tr-TR')}
          helperText="Mevcut arama, fiyat ve seller filtrelerine uyan ürünler."
          icon={PackageIcon}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Ürün"
          value={summary.active.toLocaleString('tr-TR')}
          helperText="Şu anda satışa açık ürünler."
          icon={Boxes}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Pasif Ürün"
          value={summary.inactive.toLocaleString('tr-TR')}
          helperText="Manuel olarak kapatılmış ürünler."
          icon={ShieldAlert}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Kritik Stok"
          value={summary.critical.toLocaleString('tr-TR')}
          helperText="Stok seviyesi 5 ve altında olanlar."
          icon={Trash2}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Katalog Filtreleri</CardTitle>
          <CardDescription>Arama, kategori, seller, fiyat, durum ve stok seviyesine göre görünümü daraltın.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
          <div className="relative xl:col-span-2">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Ürün adı veya SKU ara..."
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
              className="pl-10"
            />
          </div>

          <Select value={categoryFilter} onValueChange={(value) => { setCategoryFilter(value); setPage(1); }}>
            <SelectTrigger>
              <SelectValue placeholder="Kategori seçin" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tüm Kategoriler</SelectItem>
              {categories.map((category) => (
                <SelectItem key={category.id} value={String(category.id)}>
                  {category.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={sellerFilter} onValueChange={(value) => { setSellerFilter(value); setPage(1); }}>
            <SelectTrigger>
              <SelectValue placeholder="Seller seçin" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tüm Seller&apos;lar</SelectItem>
              {sellerOptions.map((seller) => (
                <SelectItem key={seller.id} value={String(seller.id)}>
                  {seller.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Input
            type="number"
            min="0"
            placeholder="Min fiyat"
            value={minPrice}
            onChange={(event) => {
              setMinPrice(event.target.value);
              setPage(1);
            }}
          />

          <Input
            type="number"
            min="0"
            placeholder="Max fiyat"
            value={maxPrice}
            onChange={(event) => {
              setMaxPrice(event.target.value);
              setPage(1);
            }}
          />

          <div className="grid grid-cols-2 gap-3 md:col-span-2 xl:col-span-2">
            <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as StatusFilter)}>
              <SelectTrigger>
                <SelectValue placeholder="Durum" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tüm Durumlar</SelectItem>
                <SelectItem value="active">Aktif</SelectItem>
                <SelectItem value="inactive">Pasif</SelectItem>
              </SelectContent>
            </Select>

            <Select value={stockFilter} onValueChange={(value) => setStockFilter(value as StockFilter)}>
              <SelectTrigger>
                <SelectValue placeholder="Stok" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tüm Stoklar</SelectItem>
                <SelectItem value="critical">Kritik Stok</SelectItem>
                <SelectItem value="out">Tükenenler</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      <Card className="border-border/70">
        <CardHeader className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div>
            <CardTitle>Toplu İşlem</CardTitle>
            <CardDescription>
              Bu sayfadaki seçili ürünlere toplu aktif/pasif veya silme işlemi uygulayın.
            </CardDescription>
          </div>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <p className="text-sm text-muted-foreground">
              {selectedIds.length > 0
                ? `${selectedIds.length} ürün seçildi`
                : 'İşlem için ürün seçin'}
            </p>
            <Select value={bulkAction} onValueChange={(value) => setBulkAction(value as BulkAction)}>
              <SelectTrigger className="w-[210px]">
                <SelectValue placeholder="Toplu işlem seçin" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="activate">Aktife Al</SelectItem>
                <SelectItem value="deactivate">Pasife Al</SelectItem>
                <SelectItem value="delete">Sil</SelectItem>
              </SelectContent>
            </Select>
            <Button onClick={() => void handleBulkAction()} disabled={isUpdatingProduct || isDeleting || isBulkUpdating}>
              Uygula
            </Button>
          </div>
        </CardHeader>
      </Card>

      {isLoading ? (
        <TableLoadingState rowCount={6} className="pt-1" />
      ) : (
        <>
          <div className="overflow-hidden rounded-xl border border-border/70 bg-card">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12">
                    <Checkbox checked={allVisibleSelected} onCheckedChange={(checked) => handleSelectAll(Boolean(checked))} />
                  </TableHead>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Kategori</TableHead>
                  <TableHead>Seller</TableHead>
                  <TableHead>Fiyat</TableHead>
                  <TableHead>Stok</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="text-right">İşlemler</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredItems.map((product) => (
                  <TableRow
                    key={product.id}
                    className={
                      product.stockQuantity <= 0
                        ? 'bg-rose-500/5'
                        : product.stockQuantity <= 5
                          ? 'bg-amber-500/5'
                          : undefined
                    }
                  >
                    <TableCell>
                      <Checkbox
                        checked={selectedIds.includes(product.id)}
                        onCheckedChange={(checked) => handleSelectOne(product.id, Boolean(checked))}
                      />
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                          {product.primaryImageUrl ? (
                            <img
                              src={product.primaryImageUrl}
                              alt={product.name}
                              className="h-full w-full rounded-lg object-cover"
                            />
                          ) : (
                            <PackageIcon className="h-5 w-5 text-muted-foreground" />
                          )}
                        </div>
                        <div>
                          <p className="font-medium">{product.name}</p>
                          <p className="text-sm text-muted-foreground">{product.sku}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>{product.categoryName}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Store className="h-4 w-4" />
                        <span>{product.sellerBrandName || (product.sellerId ? `Seller #${product.sellerId}` : 'Atanmamış')}</span>
                      </div>
                    </TableCell>
                    <TableCell>{formatCurrency(product.price, product.currency)}</TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        className={product.stockQuantity <= 0 ? 'text-rose-600' : product.stockQuantity <= 5 ? 'text-amber-600' : ''}
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
                      <div className="space-y-2">
                        <StatusBadge
                          label={product.isActive ? 'Aktif' : 'Pasif'}
                          tone={product.isActive ? 'success' : 'neutral'}
                        />
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-auto px-0 text-xs"
                          disabled={isUpdatingProduct}
                          onClick={() => setStatusTarget({
                            id: product.id,
                            name: product.name,
                            nextState: !product.isActive,
                          })}
                        >
                          <Power className="mr-1 h-3.5 w-3.5" />
                          {product.isActive ? 'Pasife Al' : 'Aktifleştir'}
                        </Button>
                      </div>
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
                          onClick={() => setDeleteTarget({ id: product.id, name: product.name })}
                          className="text-destructive hover:text-destructive"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {filteredItems.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} className="p-4">
                      <EmptyState
                        icon={Boxes}
                        title="Ürün bulunamadı"
                        description="Seçili filtrelere uyan ürün kaydı bulunamadı. Arama, seller veya fiyat aralığını değiştirerek tekrar deneyin."
                        className="border-none bg-transparent shadow-none"
                      />
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </div>

          {products && products.totalPages > 1 ? (
            <div className="flex items-center justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={!products.hasPreviousPage}
                onClick={() => setPage((current) => current - 1)}
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
                onClick={() => setPage((current) => current + 1)}
              >
                Sonraki
              </Button>
            </div>
          ) : null}
        </>
      )}

      <Dialog open={stockDialog.open} onOpenChange={(open) => setStockDialog((current) => ({ ...current, open }))}>
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
                onChange={(event) => setStockChange(event.target.value)}
              />
              <p className="text-sm text-muted-foreground">Pozitif sayı ekler, negatif sayı çıkarır.</p>
            </div>
            <div className="space-y-2">
              <Label>Açıklama</Label>
              <Input
                placeholder="Stok sayımı, iade, manuel düzenleme..."
                value={stockReason}
                onChange={(event) => setStockReason(event.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setStockDialog({ open: false, productId: 0, productName: '' })}>
              İptal
            </Button>
            <Button onClick={() => void handleStockUpdate()} disabled={isUpdatingStock}>
              Güncelle
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmModal
        open={!!deleteTarget}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteTarget(null);
          }
        }}
        title="Ürün silinsin mi?"
        description={deleteTarget ? `"${deleteTarget.name}" ürünü kalıcı olarak silinecek.` : ''}
        confirmLabel="Ürünü Sil"
        isLoading={isDeleting}
        onConfirm={() => deleteTarget ? handleDelete(deleteTarget.id, deleteTarget.name) : Promise.resolve()}
      />

      <ConfirmModal
        open={bulkDeleteOpen}
        onOpenChange={setBulkDeleteOpen}
        title="Seçili ürünler silinsin mi?"
        description={`${selectedIds.length} ürün kalıcı olarak silinecek. Bu işlem geri alınamaz.`}
        confirmLabel="Toplu Sil"
        isLoading={isBulkUpdating}
        onConfirm={handleBulkDelete}
      />

      <ConfirmModal
        open={!!statusTarget}
        onOpenChange={(open) => {
          if (!open) {
            setStatusTarget(null);
          }
        }}
        title={statusTarget?.nextState ? 'Ürünü aktifleştir' : 'Ürünü pasife al'}
        description={
          statusTarget
            ? `"${statusTarget.name}" ürünü için görünürlük durumu güncellenecek.`
            : ''
        }
        confirmLabel={statusTarget?.nextState ? 'Aktifleştir' : 'Pasife Al'}
        confirmVariant="default"
        isLoading={isUpdatingProduct}
        onConfirm={handleStatusToggle}
      />
    </div>
  );
}
