import { Link, useSearchParams } from 'react-router-dom';
import { useEffect, useMemo } from 'react';
import { ArrowLeft, CheckCircle2, GitCompareArrows, Package, Trash2 } from 'lucide-react';
import { skipToken } from '@reduxjs/toolkit/query';
import { Button } from '@/components/common/button';
import { Badge } from '@/components/common/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { useGetProductQuery } from '@/features/products/productsApi';
import type { Product } from '@/features/products/types';
import {
  buildCompareUrl,
  parseCompareIds,
  useProductCompare,
} from '@/features/compare';

type CompareFieldKey =
  | 'price'
  | 'campaign'
  | 'stock'
  | 'rating'
  | 'wishlist'
  | 'reviews'
  | 'sku';

type CompareField = {
  key: CompareFieldKey;
  label: string;
  render: (product: Product) => string;
};

const compareFields: CompareField[] = [
  {
    key: 'price',
    label: 'Fiyat',
    render: (product) => `${product.price.toLocaleString('tr-TR')} ${product.currency}`,
  },
  {
    key: 'campaign',
    label: 'Kampanya',
    render: (product) => (product.hasActiveCampaign ? product.campaignName || product.campaignBadgeText || 'Aktif' : 'Yok'),
  },
  {
    key: 'stock',
    label: 'Stok',
    render: (product) => (product.stockQuantity > 0 ? `${product.stockQuantity} adet` : 'Stokta yok'),
  },
  {
    key: 'rating',
    label: 'Puan',
    render: (product) => `${product.averageRating.toFixed(1)} / 5`,
  },
  {
    key: 'wishlist',
    label: 'Favori Sayısı',
    render: (product) => product.wishlistCount.toLocaleString('tr-TR'),
  },
  {
    key: 'reviews',
    label: 'Değerlendirme',
    render: (product) => product.reviewCount.toLocaleString('tr-TR'),
  },
  {
    key: 'sku',
    label: 'SKU',
    render: (product) => product.sku,
  },
];

const categoryFocusedFields: Record<string, CompareFieldKey[]> = {
  elektronik: ['price', 'campaign', 'stock', 'rating', 'wishlist', 'sku'],
  moda: ['price', 'campaign', 'stock', 'wishlist', 'reviews'],
  kitap: ['price', 'rating', 'reviews', 'wishlist', 'stock'],
  oyuncak: ['price', 'stock', 'rating', 'wishlist', 'reviews'],
};

export default function Compare() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { compareIds, removeProduct, clearProducts, replaceProducts } = useProductCompare();

  const queryIds = useMemo(() => parseCompareIds(searchParams.get('ids')), [searchParams]);
  const activeIds = queryIds.length > 0 ? queryIds : compareIds;
  const productIds = activeIds.slice(0, 4);

  useEffect(() => {
    if (queryIds.length === 0 || queryIds.join(',') === compareIds.join(',')) {
      return;
    }

    replaceProducts(queryIds);
  }, [compareIds, queryIds, replaceProducts]);

  useEffect(() => {
    const nextValue = compareIds.join(',');
    const currentValue = searchParams.get('ids') ?? '';

    if (nextValue === currentValue) {
      return;
    }

    if (compareIds.length === 0) {
      setSearchParams({}, { replace: true });
      return;
    }

    setSearchParams({ ids: nextValue }, { replace: true });
  }, [compareIds, searchParams, setSearchParams]);

  const queryOne = useGetProductQuery(productIds[0] ?? skipToken);
  const queryTwo = useGetProductQuery(productIds[1] ?? skipToken);
  const queryThree = useGetProductQuery(productIds[2] ?? skipToken);
  const queryFour = useGetProductQuery(productIds[3] ?? skipToken);

  const products = [queryOne.data, queryTwo.data, queryThree.data, queryFour.data].filter(Boolean) as Product[];
  const isLoading = [queryOne, queryTwo, queryThree, queryFour].some((query, index) => productIds[index] && query.isLoading);

  const primaryCategoryName = products[0]?.categoryName ?? null;
  const isSameCategory = products.length > 0 && products.every((product) => product.categoryName === primaryCategoryName);
  const focusKeys = primaryCategoryName ? categoryFocusedFields[primaryCategoryName.toLowerCase()] ?? ['price', 'stock', 'rating', 'wishlist'] : [];
  const focusFields = compareFields.filter((field) => focusKeys.includes(field.key));

  const handleRemove = (productId: number) => {
    removeProduct(productId);
  };

  return (
    <StaticPageLayout
      eyebrow="Karşılaştırma"
      title="Ürün Karşılaştırma"
      description="En fazla dört ürünü yan yana getirerek fiyat, stok, kampanya ve sosyal kanıt sinyallerini tek ekranda görün."
      badges={[
        `Seçili ürün: ${products.length || compareIds.length}`,
        isSameCategory && primaryCategoryName ? `${primaryCategoryName} kategorisi` : 'Genel karşılaştırma',
      ]}
    >
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <Button variant="ghost" asChild className="w-fit">
          <Link to="/">
            <ArrowLeft className="h-4 w-4" />
            Ürünlere dön
          </Link>
        </Button>
        <div className="flex flex-wrap items-center gap-3">
          {compareIds.length > 0 && (
            <Button variant="outline" onClick={clearProducts}>
              <Trash2 className="h-4 w-4" />
              Listeyi temizle
            </Button>
          )}
          <Button variant="outline" asChild>
            <Link to={buildCompareUrl(compareIds)}>
              <GitCompareArrows className="h-4 w-4" />
              Paylaşılabilir bağlantı
            </Link>
          </Button>
        </div>
      </div>

      {compareIds.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center gap-4 py-16 text-center">
            <Package className="h-12 w-12 text-muted-foreground" />
            <div className="space-y-2">
              <h2 className="text-2xl font-semibold">Karşılaştırma listesi boş</h2>
              <p className="max-w-xl text-muted-foreground">
                Ürün kartlarından veya ürün detay sayfasından `Karşılaştırmaya Ekle` diyerek listenizi oluşturmaya başlayabilirsiniz.
              </p>
            </div>
            <Button asChild>
              <Link to="/">Ürünleri keşfet</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {compareIds.length > 0 && (
        <>
          {!isSameCategory && products.length > 1 && (
            <Card className="border-amber-500/20 bg-amber-500/5">
              <CardContent className="flex items-start gap-3 p-5 text-sm text-amber-800 dark:text-amber-100">
                <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0" />
                <p>
                  Farklı kategorilerden ürünleri de karşılaştırabilirsiniz. Ancak en net sonuç için aynı kategori içindeki ürünleri yan yana koymanız önerilir.
                </p>
              </CardContent>
            </Card>
          )}

          {focusFields.length > 0 && (
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-lg">
                  {primaryCategoryName} kategorisi için öne çıkan alanlar
                </CardTitle>
              </CardHeader>
              <CardContent className="flex flex-wrap gap-2">
                {focusFields.map((field) => (
                  <Badge key={field.key} variant="secondary">
                    {field.label}
                  </Badge>
                ))}
              </CardContent>
            </Card>
          )}

          <Card>
            <CardContent className="p-0">
              <Table className="min-w-[860px]">
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-52 px-4">Özellik</TableHead>
                    {productIds.map((productId, index) => {
                      const product = products.find((item) => item.id === productId);
                      return (
                        <TableHead key={productId ?? index} className="min-w-60 px-4 py-4 align-top">
                          {isLoading && !product ? (
                            <div className="space-y-2">
                              <Skeleton className="h-5 w-32" />
                              <Skeleton className="h-4 w-20" />
                            </div>
                          ) : product ? (
                            <div className="space-y-3">
                              <div>
                                <Link to={`/products/${product.id}`} className="font-semibold hover:text-primary">
                                  {product.name}
                                </Link>
                                <p className="mt-1 text-xs text-muted-foreground">{product.categoryName}</p>
                              </div>
                              <div className="flex flex-wrap gap-2">
                                {product.hasActiveCampaign && (
                                  <Badge className="bg-amber-500/10 text-amber-700 dark:text-amber-200">
                                    {product.campaignBadgeText || product.campaignName || 'Kampanya'}
                                  </Badge>
                                )}
                                <Button variant="ghost" size="sm" onClick={() => handleRemove(product.id)}>
                                  Kaldır
                                </Button>
                              </div>
                            </div>
                          ) : (
                            <p className="text-sm text-muted-foreground">Ürün yüklenemedi</p>
                          )}
                        </TableHead>
                      );
                    })}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {compareFields.map((field) => (
                    <TableRow key={field.key}>
                      <TableCell className="px-4 font-medium">{field.label}</TableCell>
                      {productIds.map((productId, index) => {
                        const product = products.find((item) => item.id === productId);
                        return (
                          <TableCell key={`${field.key}-${productId ?? index}`} className="px-4 whitespace-normal">
                            {product ? field.render(product) : '—'}
                          </TableCell>
                        );
                      })}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </>
      )}
    </StaticPageLayout>
  );
}
