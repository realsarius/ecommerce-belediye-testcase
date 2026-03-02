import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Search,
  ShieldCheck,
  Store,
  Tags,
  Users,
} from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/common/avatar';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { KpiCard } from '@/components/admin/KpiCard';
import { useSearchProductsQuery } from '@/features/products/productsApi';

type AggregatedSeller = {
  id: number;
  brandName: string;
  productCount: number;
  activeProductCount: number;
  totalStock: number;
  averageRating: number;
};

function getInitials(value: string) {
  return value
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');
}

export default function SellersPage() {
  const [search, setSearch] = useState('');
  const { data: products, isLoading } = useSearchProductsQuery({ page: 1, pageSize: 500 });

  const sellers = useMemo<AggregatedSeller[]>(() => {
    const map = new Map<number, AggregatedSeller>();

    for (const product of products?.items ?? []) {
      if (!product.sellerId) {
        continue;
      }

      const current = map.get(product.sellerId) ?? {
        id: product.sellerId,
        brandName: product.sellerBrandName || `Seller #${product.sellerId}`,
        productCount: 0,
        activeProductCount: 0,
        totalStock: 0,
        averageRating: 0,
      };

      current.productCount += 1;
      current.activeProductCount += product.isActive ? 1 : 0;
      current.totalStock += product.stockQuantity;
      current.averageRating += product.averageRating;

      map.set(product.sellerId, current);
    }

    return [...map.values()]
      .map((seller) => ({
        ...seller,
        averageRating: seller.productCount > 0 ? seller.averageRating / seller.productCount : 0,
      }))
      .sort((a, b) => b.productCount - a.productCount);
  }, [products?.items]);

  const filteredSellers = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('tr-TR');

    if (!term) {
      return sellers;
    }

    return sellers.filter((seller) => (
      seller.brandName.toLocaleLowerCase('tr-TR').includes(term)
      || String(seller.id).includes(term)
    ));
  }, [search, sellers]);

  const summary = useMemo(() => {
    return {
      total: sellers.length,
      active: sellers.filter((seller) => seller.activeProductCount > 0).length,
      totalProducts: sellers.reduce((sum, seller) => sum + seller.productCount, 0),
      healthyStock: sellers.filter((seller) => seller.totalStock > 5).length,
    };
  }, [sellers]);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-36 rounded-xl" />
          ))}
        </div>
        <Skeleton className="h-[420px] rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Seller Listesi</h1>
        <p className="max-w-3xl text-muted-foreground">
          Bu ekran mevcut katalog verisinden seller profillerini türetir. Ayrı admin seller liste endpoint’i geldiğinde doğrudan o veri kaynağına geçeceğiz.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Toplam Seller"
          value={summary.total.toLocaleString('tr-TR')}
          helperText="Katalogda en az bir ürünü görünen satıcılar."
          icon={Users}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Seller"
          value={summary.active.toLocaleString('tr-TR')}
          helperText="En az bir aktif ürünü olan seller hesapları."
          icon={ShieldCheck}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Toplam Ürün"
          value={summary.totalProducts.toLocaleString('tr-TR')}
          helperText="Seller'lara bağlı katalog ürünleri."
          icon={Tags}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Stok Sağlıklı"
          value={summary.healthyStock.toLocaleString('tr-TR')}
          helperText="Toplam stoğu 5’in üstünde olan seller hesapları."
          icon={Store}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Arama</CardTitle>
          <CardDescription>Mağaza adı veya seller profil numarasına göre filtreleyin.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="relative max-w-md">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Mağaza adı veya seller ID ara..."
              className="pl-10"
            />
          </div>
        </CardContent>
      </Card>

      <div className="overflow-hidden rounded-xl border border-border/70 bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Mağaza</TableHead>
              <TableHead>Seller ID</TableHead>
              <TableHead>Ürün</TableHead>
              <TableHead>Aktif</TableHead>
              <TableHead>Toplam Stok</TableHead>
              <TableHead>Ort. Puan</TableHead>
              <TableHead className="text-right">Detay</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredSellers.map((seller) => (
              <TableRow key={seller.id}>
                <TableCell>
                  <div className="flex items-center gap-3">
                    <Avatar className="h-10 w-10">
                      <AvatarFallback>{getInitials(seller.brandName)}</AvatarFallback>
                    </Avatar>
                    <div>
                      <p className="font-medium">{seller.brandName}</p>
                      <p className="text-sm text-muted-foreground">Katalogdan türetilen mağaza görünümü</p>
                    </div>
                  </div>
                </TableCell>
                <TableCell>{seller.id}</TableCell>
                <TableCell>{seller.productCount.toLocaleString('tr-TR')}</TableCell>
                <TableCell>
                  <Badge variant={seller.activeProductCount > 0 ? 'default' : 'secondary'}>
                    {seller.activeProductCount.toLocaleString('tr-TR')}
                  </Badge>
                </TableCell>
                <TableCell>{seller.totalStock.toLocaleString('tr-TR')}</TableCell>
                <TableCell>{seller.averageRating.toFixed(1)} / 5</TableCell>
                <TableCell className="text-right">
                  <Button variant="ghost" size="sm" asChild>
                    <Link to={`/admin/sellers/${seller.id}`}>
                      Detay
                    </Link>
                  </Button>
                </TableCell>
              </TableRow>
            ))}
            {filteredSellers.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="py-12 text-center text-muted-foreground">
                  Filtreye uygun seller bulunamadı.
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </div>

      <Card className="border-border/70 bg-muted/20">
        <CardContent className="p-5 text-sm text-muted-foreground">
          Başvurular, komisyon override ve seller statü değişikliği için ayrı backend endpoint’leri henüz olmadığı için bu ilk sürüm yalnızca görünürlük ve detay inceleme odaklıdır.
        </CardContent>
      </Card>
    </div>
  );
}
