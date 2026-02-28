import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Heart, Package, Share2, TrendingDown, TrendingUp } from 'lucide-react';
import { Button } from '@/components/common/button';
import { Card, CardContent } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Skeleton } from '@/components/common/skeleton';
import type { SharedWishlist as SharedWishlistResponse } from '@/features/wishlist/types';
import { useLazyGetSharedWishlistQuery } from '@/features/wishlist/wishlistApi';
import { toast } from 'sonner';

const WISHLIST_PAGE_SIZE = 20;

export default function SharedWishlist() {
    const { token } = useParams<{ token: string }>();
    const [fetchSharedWishlist] = useLazyGetSharedWishlistQuery();
    const [wishlist, setWishlist] = useState<SharedWishlistResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isLoadingMore, setIsLoadingMore] = useState(false);
    const [loadFailed, setLoadFailed] = useState(false);

    useEffect(() => {
        if (!token) {
            setLoadFailed(true);
            setIsLoading(false);
            return;
        }

        const loadInitialPage = async () => {
            setIsLoading(true);
            setLoadFailed(false);

            try {
                const nextWishlist = await fetchSharedWishlist({
                    shareToken: token,
                    limit: WISHLIST_PAGE_SIZE,
                }).unwrap();
                setWishlist(nextWishlist);
            } catch {
                setLoadFailed(true);
            } finally {
                setIsLoading(false);
            }
        };

        void loadInitialPage();
    }, [fetchSharedWishlist, token]);

    const handleLoadMore = async () => {
        if (!token || !wishlist?.nextCursor || isLoadingMore) {
            return;
        }

        setIsLoadingMore(true);
        try {
            const nextPage = await fetchSharedWishlist({
                shareToken: token,
                cursor: wishlist.nextCursor,
                limit: WISHLIST_PAGE_SIZE,
            }).unwrap();

            setWishlist((current) => {
                if (!current) {
                    return nextPage;
                }

                const existingIds = new Set(current.items.map((item) => item.id));
                return {
                    ...current,
                    ...nextPage,
                    items: [
                        ...current.items,
                        ...nextPage.items.filter((item) => !existingIds.has(item.id)),
                    ],
                };
            });
        } catch {
            toast.error('Paylaşılan favorilerin devamı yüklenemedi.');
        } finally {
            setIsLoadingMore(false);
        }
    };

    if (isLoading) {
        return (
            <div className="container mx-auto px-4 py-8">
                <div className="mb-8 space-y-3">
                    <Skeleton className="h-10 w-72" />
                    <Skeleton className="h-5 w-96" />
                </div>
                <div className="space-y-4">
                    {[1, 2, 3].map((i) => (
                        <Card key={i}>
                            <CardContent className="p-4 flex items-center gap-4">
                                <Skeleton className="h-16 w-16 rounded" />
                                <div className="space-y-2">
                                    <Skeleton className="h-4 w-48" />
                                    <Skeleton className="h-4 w-24" />
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            </div>
        );
    }

    if (loadFailed || !wishlist) {
        return (
            <div className="container mx-auto px-4 py-16 text-center">
                <Share2 className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                <h1 className="text-2xl font-semibold mb-2">Paylaşılan Liste Bulunamadı</h1>
                <p className="text-muted-foreground mb-6">
                    Bu paylaşım bağlantısı geçersiz olabilir ya da liste artık herkese açık değil.
                </p>
                <Button asChild>
                    <Link to="/">Ürünlere Göz At</Link>
                </Button>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8">
            <div className="mb-8 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                <div>
                    <Badge variant="secondary" className="mb-3">Paylaşılan liste</Badge>
                    <h1 className="text-3xl font-bold">{wishlist.ownerDisplayName} için Favoriler</h1>
                    <p className="mt-2 text-muted-foreground">
                        Bu liste salt okunur olarak paylaşıldı. Ürünleri inceleyebilir ve bağlantıyı başkalarıyla paylaşabilirsiniz.
                    </p>
                </div>
                <Button
                    variant="outline"
                    onClick={async () => {
                        try {
                            await navigator.clipboard.writeText(window.location.href);
                            toast.success('Paylaşım bağlantısı kopyalandı.');
                        } catch {
                            toast.error('Bağlantı kopyalanamadı.');
                        }
                    }}
                >
                    <Share2 className="mr-2 h-4 w-4" />
                    Linki Kopyala
                </Button>
            </div>

            {wishlist.items.length === 0 ? (
                <Card>
                    <CardContent className="py-12 text-center">
                        <Heart className="mx-auto mb-4 h-12 w-12 text-muted-foreground" />
                        <h2 className="text-xl font-semibold">Bu listede henüz ürün yok</h2>
                        <p className="mt-2 text-muted-foreground">
                            Liste sahibi ürün ekledikçe burada görünmeye başlayacak.
                        </p>
                    </CardContent>
                </Card>
            ) : (
                <>
                    <div className="grid grid-cols-1 gap-4">
                        {wishlist.items.map((item) => (
                            <Card key={item.id} className="overflow-hidden">
                                <CardContent className="p-4">
                                    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                                        <div className="flex items-center gap-4">
                                            <div className="flex h-20 w-20 shrink-0 items-center justify-center rounded-lg bg-muted">
                                                <Package className="h-8 w-8 text-muted-foreground" />
                                            </div>
                                            <div>
                                                {item.isAvailable ? (
                                                    <Link to={`/products/${item.productId}`} className="font-semibold hover:text-primary hover:underline">
                                                        {item.productName}
                                                    </Link>
                                                ) : (
                                                    <span className="font-semibold text-muted-foreground">
                                                        {item.productName}
                                                    </span>
                                                )}
                                                <div className="mt-1 flex items-center gap-2">
                                                    <span className="text-sm font-medium">
                                                        {item.productPrice.toLocaleString('tr-TR')} {item.productCurrency}
                                                    </span>
                                                    {item.isAvailable && item.priceChange !== 0 && item.priceChange !== undefined && (
                                                        <span className={`text-xs flex items-center gap-1 ${item.priceChange > 0 ? 'text-red-500' : 'text-green-500'}`}>
                                                            {item.priceChange > 0 ? <TrendingUp className="h-3 w-3" /> : <TrendingDown className="h-3 w-3" />}
                                                            {Math.abs(item.priceChangePercentage || 0).toFixed(1)}%
                                                        </span>
                                                    )}
                                                </div>
                                                <p className="mt-2 text-xs text-muted-foreground">
                                                    Eklenme tarihi: {new Date(item.addedAt).toLocaleDateString('tr-TR')}
                                                </p>
                                                {!item.isAvailable && (
                                                    <Badge variant="secondary" className="mt-2">
                                                        {item.unavailableReason ?? 'Bu ürün artık mevcut değil.'}
                                                    </Badge>
                                                )}
                                            </div>
                                        </div>

                                        <div className="flex shrink-0 items-center gap-2">
                                            <Button asChild variant="outline" disabled={!item.isAvailable}>
                                                <Link to={`/products/${item.productId}`}>Ürünü İncele</Link>
                                            </Button>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                    </div>

                    {wishlist.hasMore && (
                        <div className="mt-8 flex justify-center">
                            <Button variant="outline" onClick={() => void handleLoadMore()} disabled={isLoadingMore}>
                                {isLoadingMore ? 'Yükleniyor...' : 'Daha Fazla Yükle'}
                            </Button>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}
