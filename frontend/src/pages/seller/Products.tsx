import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import {
  AlertTriangle,
  Package as PackageIcon,
  Pencil,
  Plus,
  Search,
  Store,
  Trash2,
} from 'lucide-react';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { Card, CardContent } from '@/components/common/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { ConfirmModal } from '@/components/admin/ConfirmModal';
import { KpiCard } from '@/components/admin/KpiCard';
import { useDebounce } from '@/hooks/useDebounce';
import {
  useDeleteSellerProductMutation,
  useGetSellerProductsQuery,
  useGetSellerProfileQuery,
} from '@/features/seller/sellerApi';

type StatusFilter = 'all' | 'active' | 'inactive';
type StockFilter = 'all' | 'critical' | 'out';

function formatCurrency(value: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

export default function SellerProducts() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [stockFilter, setStockFilter] = useState<StockFilter>('all');
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null);
  const debouncedSearch = useDebounce(search, 400);

  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const { data: products, isLoading } = useGetSellerProductsQuery({
    page,
    pageSize: 20,
    search: debouncedSearch || undefined,
  });
  const [deleteProduct, { isLoading: isDeleting }] = useDeleteSellerProductMutation();

  const items = products?.items ?? [];
  const visibleItems = useMemo(() => {
    if (!profile?.id) {
      return items;
    }

    return items.filter((product) => product.sellerId == null || product.sellerId === profile.id);
  }, [items, profile?.id]);
  const filteredOutForeignItemsCount = Math.max(0, items.length - visibleItems.length);

  const filteredItems = useMemo(() => {
    return visibleItems.filter((product) => {
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
  }, [statusFilter, stockFilter, visibleItems]);

  const summary = useMemo(() => {
    return {
      total: visibleItems.length,
      active: visibleItems.filter((product) => product.isActive).length,
      critical: visibleItems.filter((product) => product.stockQuantity > 0 && product.stockQuantity <= 5).length,
      out: visibleItems.filter((product) => product.stockQuantity <= 0).length,
    };
  }, [visibleItems]);

  const handleDelete = async (id: number, name: string) => {
    try {
      await deleteProduct(id).unwrap();
      toast.success(`"${name}" ürünü silindi`);
      setDeleteTarget(null);
    } catch {
      toast.error('Ürün silinemedi');
    }
  };

  if (!profileLoading && !profile) {
    return (
      <div>
        <h1 className="mb-6 text-3xl font-bold">Ürünlerim</h1>
        <Card className="border-amber-500 bg-amber-50 dark:bg-amber-950/30">
          <CardContent className="p-6 text-center">
            <Store className="mx-auto mb-4 h-12 w-12 text-amber-600" />
            <h2 className="mb-2 text-xl font-semibold">Mağaza Profili Gerekli</h2>
            <p className="mb-4 text-muted-foreground">
              Ürün ekleyebilmek ve sipariş akışına geçebilmek için önce mağaza profilinizi oluşturmanız gerekiyor.
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
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Ürünlerim</h1>
          <p className="max-w-3xl text-muted-foreground">
            Mağazanızdaki ürünleri filtreleyin, kritik stokları görün ve düzenleme akışına hızlıca geçin.
          </p>
        </div>
        <Button asChild className="bg-amber-600 hover:bg-amber-700">
          <Link to="/seller/products/new">
            <Plus className="mr-2 h-4 w-4" />
            Yeni Ürün
          </Link>
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Listelenen Ürün"
          value={summary.total.toLocaleString('tr-TR')}
          helperText="Mevcut sonuç kümesindeki ürünler."
          icon={PackageIcon}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Ürün"
          value={summary.active.toLocaleString('tr-TR')}
          helperText="Satışa açık ürünler."
          icon={Store}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Kritik Stok"
          value={summary.critical.toLocaleString('tr-TR')}
          helperText="Stok seviyesi 5 ve altında olanlar."
          icon={AlertTriangle}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="Tükenen"
          value={summary.out.toLocaleString('tr-TR')}
          helperText="Stok seviyesi sıfır veya altında olanlar."
          icon={Trash2}
          accentClass="text-rose-600 dark:text-rose-300"
          surfaceClass="bg-rose-500/10"
        />
      </div>

      {filteredOutForeignItemsCount > 0 ? (
        <Card className="border-amber-500/30 bg-amber-50 dark:bg-amber-950/20">
          <CardContent className="flex gap-3 p-5">
            <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-amber-600 dark:text-amber-300" />
            <div className="space-y-1 text-sm text-muted-foreground">
              <p className="font-medium text-foreground">Liste yalnızca mağazanıza ait ürünlerle sınırlandı</p>
              <p>
                Arka uç seller filtresi uygulasa da, güvenlik için farklı seller kimliği taşıyan
                {` ${filteredOutForeignItemsCount} `}
                kayıt kullanıcı arayüzünde gizlendi.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : null}

      <Card className="border-border/70">
        <CardContent className="grid gap-4 p-6 md:grid-cols-2 xl:grid-cols-4">
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

          <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as StatusFilter)}>
            <SelectTrigger>
              <SelectValue placeholder="Durum seçin" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tüm Durumlar</SelectItem>
              <SelectItem value="active">Aktif</SelectItem>
              <SelectItem value="inactive">Pasif</SelectItem>
            </SelectContent>
          </Select>

          <Select value={stockFilter} onValueChange={(value) => setStockFilter(value as StockFilter)}>
            <SelectTrigger>
              <SelectValue placeholder="Stok seçin" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tüm Stoklar</SelectItem>
              <SelectItem value="critical">Kritik Stok</SelectItem>
              <SelectItem value="out">Tükenenler</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-16 rounded-xl" />
          ))}
        </div>
      ) : filteredItems.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="p-12 text-center">
            <PackageIcon className="mx-auto mb-4 h-12 w-12 text-muted-foreground" />
            <h2 className="mb-2 text-xl font-semibold">Uygun ürün bulunamadı</h2>
            <p className="mb-4 text-muted-foreground">
              Filtreleri değiştirin veya yeni bir ürün ekleyin.
            </p>
            <Button asChild className="bg-amber-600 hover:bg-amber-700">
              <Link to="/seller/products/new">
                <Plus className="mr-2 h-4 w-4" />
                Yeni Ürün Ekle
              </Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="overflow-hidden rounded-xl border border-border/70 bg-card">
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
                      <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-100 dark:bg-amber-900/30">
                          <PackageIcon className="h-5 w-5 text-amber-600" />
                        </div>
                        <div>
                          <p className="font-medium">{product.name}</p>
                          <p className="text-sm text-muted-foreground">{product.sku}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>{product.categoryName}</TableCell>
                    <TableCell>{formatCurrency(product.price, product.currency)}</TableCell>
                    <TableCell className={product.stockQuantity <= 0 ? 'font-semibold text-rose-600' : product.stockQuantity <= 5 ? 'font-semibold text-amber-600' : ''}>
                      {product.stockQuantity}
                    </TableCell>
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
                          onClick={() => setDeleteTarget({ id: product.id, name: product.name })}
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

      <ConfirmModal
        open={!!deleteTarget}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteTarget(null);
          }
        }}
        title="Ürün silinsin mi?"
        description={deleteTarget ? `"${deleteTarget.name}" ürünü mağazanızdan kaldırılacak.` : ''}
        confirmLabel="Ürünü Sil"
        isLoading={isDeleting}
        onConfirm={() => deleteTarget ? handleDelete(deleteTarget.id, deleteTarget.name) : Promise.resolve()}
      />
    </div>
  );
}
