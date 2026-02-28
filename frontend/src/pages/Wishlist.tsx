import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Trash2, ShoppingCart, Package, Heart, LayoutGrid, List, TrendingDown, TrendingUp, BellRing, BellOff, Share2, Copy, Link2Off } from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Card, CardContent } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { Input } from '@/components/common/input';
import { useAppSelector } from '@/app/hooks';
import type { Wishlist as WishlistResponse } from '@/features/wishlist/types';
import { clearGuestWishlistProducts, getWishlistErrorMessage, useGuestWishlist } from '@/features/wishlist';
import {
    useLazyGetWishlistQuery,
    useRemoveWishlistItemMutation,
    useClearWishlistMutation,
    useGetWishlistPriceAlertsQuery,
    useUpsertWishlistPriceAlertMutation,
    useRemoveWishlistPriceAlertMutation,
    useAddAllWishlistItemsToCartMutation,
    useGetWishlistShareSettingsQuery,
    useEnableWishlistSharingMutation,
    useDisableWishlistSharingMutation,
} from '@/features/wishlist/wishlistApi';
import { useAddToCartMutation } from '@/features/cart/cartApi';
import { toast } from 'sonner';

const WISHLIST_PAGE_SIZE = 20;

export default function Wishlist() {
    const { isAuthenticated } = useAppSelector((state) => state.auth);
    const { pendingCount } = useGuestWishlist();
    const [fetchWishlistPage] = useLazyGetWishlistQuery();
    const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
    const [removeFromWishlist] = useRemoveWishlistItemMutation();
    const [clearWishlist] = useClearWishlistMutation();
    const { data: priceAlerts = [] } = useGetWishlistPriceAlertsQuery(undefined, { skip: !isAuthenticated });
    const { data: shareSettings } = useGetWishlistShareSettingsQuery(undefined, { skip: !isAuthenticated });
    const [enableWishlistSharing, { isLoading: isEnablingSharing }] = useEnableWishlistSharingMutation();
    const [disableWishlistSharing, { isLoading: isDisablingSharing }] = useDisableWishlistSharingMutation();
    const [upsertWishlistPriceAlert] = useUpsertWishlistPriceAlertMutation();
    const [removeWishlistPriceAlert] = useRemoveWishlistPriceAlertMutation();
    const [addAllWishlistItemsToCart, { isLoading: isBulkAddingToCart }] = useAddAllWishlistItemsToCartMutation();
    const [addToCart, { isLoading: isAddingToCart }] = useAddToCartMutation();
    const [wishlist, setWishlist] = useState<WishlistResponse | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [isLoadingMore, setIsLoadingMore] = useState(false);
    const [loadFailed, setLoadFailed] = useState(false);
    const [alertDrafts, setAlertDrafts] = useState<Record<number, string>>({});
    const [bulkAddSummary, setBulkAddSummary] = useState<{
        message: string;
        addedCount: number;
        skippedItems: { productId: number; productName: string; reason: string }[];
    } | null>(null);

    const shareUrl = shareSettings?.sharePath
        ? `${window.location.origin}${shareSettings.sharePath}`
        : null;

    useEffect(() => {
        if (!isAuthenticated) {
            setWishlist(null);
            setLoadFailed(false);
            setIsLoading(false);
            setIsLoadingMore(false);
            setAlertDrafts({});
            setBulkAddSummary(null);
            return;
        }

        const loadInitialPage = async () => {
            setIsLoading(true);
            setLoadFailed(false);

            try {
                const nextWishlist = await fetchWishlistPage({ limit: WISHLIST_PAGE_SIZE }).unwrap();
                setWishlist(nextWishlist);
            } catch {
                setLoadFailed(true);
            } finally {
                setIsLoading(false);
            }
        };

        void loadInitialPage();
    }, [fetchWishlistPage, isAuthenticated]);

    useEffect(() => {
        if (!wishlist?.items?.length) {
            return;
        }

        setAlertDrafts((current) => {
            const next = { ...current };
            let changed = false;

            for (const item of wishlist.items) {
                if (next[item.productId] !== undefined) {
                    continue;
                }

                const existingAlert = priceAlerts.find((alert) => alert.productId === item.productId);
                const defaultTargetPrice = existingAlert?.targetPrice
                    ?? Math.max(item.productPrice - 1, 1);

                next[item.productId] = defaultTargetPrice.toFixed(2);
                changed = true;
            }

            return changed ? next : current;
        });
    }, [priceAlerts, wishlist]);

    if (!isAuthenticated) {
        const hasPendingWishlist = pendingCount > 0;

        return (
            <div className="container mx-auto px-4 py-16 text-center">
                <Heart className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                <h2 className="text-2xl font-semibold mb-2">
                    {hasPendingWishlist ? 'Bekleyen Favorileriniz Hazır' : 'Giriş Yapmanız Gerekiyor'}
                </h2>
                <p className="text-muted-foreground mb-6 max-w-xl mx-auto">
                    {hasPendingWishlist
                        ? `${pendingCount} urun favorilerinize eklenmek icin bekliyor. Giris yaptiginizda bu urunleri hesabinizla otomatik olarak senkronize edecegiz.`
                        : 'Favorilerinizi hesabinizla senkronize etmek ve tum cihazlarinizda gormek icin lutfen giris yapin.'}
                </p>
                <div className="max-w-md mx-auto mb-6">
                    <Card>
                        <CardContent className="p-6 space-y-3 text-left">
                            <div className="flex items-center justify-between gap-4">
                                <span className="text-sm text-muted-foreground">Bekleyen favori sayisi</span>
                                <Badge variant={hasPendingWishlist ? 'default' : 'secondary'}>
                                    {pendingCount}
                                </Badge>
                            </div>
                            <p className="text-sm text-muted-foreground">
                                {hasPendingWishlist
                                    ? 'Giris yaptiginiz anda bu urunler hesabinizdaki favori listenize aktarilacak.'
                                    : 'Urun sayfalarindaki kalp butonu ile favori urunleri burada biriktirebilirsiniz.'}
                            </p>
                        </CardContent>
                    </Card>
                </div>
                <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
                    <Button asChild>
                        <Link to="/login">Giris Yap</Link>
                    </Button>
                    <Button asChild variant="outline">
                        <Link to="/">Urunlere Goz At</Link>
                    </Button>
                    {hasPendingWishlist && (
                        <Button
                            variant="ghost"
                            onClick={() => {
                                clearGuestWishlistProducts();
                                toast.success('Bekleyen favoriler temizlendi.');
                            }}
                        >
                            <Trash2 className="h-4 w-4 mr-2" />
                            Bekleyenleri Temizle
                        </Button>
                    )}
                </div>
            </div>
        );
    }

    if (loadFailed && !wishlist) {
        return (
            <div className="container mx-auto px-4 py-16 text-center">
                <Heart className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                <h2 className="text-2xl font-semibold mb-2">Favoriler Yüklenemedi</h2>
                <p className="text-muted-foreground mb-6">
                    Favorileriniz şu anda yüklenemedi. Lütfen tekrar deneyin.
                </p>
                <Button
                    onClick={async () => {
                        setIsLoading(true);
                        setLoadFailed(false);

                        try {
                            const nextWishlist = await fetchWishlistPage({ limit: WISHLIST_PAGE_SIZE }).unwrap();
                            setWishlist(nextWishlist);
                        } catch {
                            setLoadFailed(true);
                        } finally {
                            setIsLoading(false);
                        }
                    }}
                >
                    Tekrar Dene
                </Button>
            </div>
        );
    }

    if (isLoading || (!wishlist && !loadFailed)) {
        return (
            <div className="container mx-auto px-4 py-8">
                <h1 className="text-3xl font-bold mb-8">Favorilerim</h1>
                <div className="space-y-4">
                    {[1, 2, 3].map((i) => (
                        <Card key={i}>
                            <CardContent className="p-4 flex items-center justify-between">
                                <div className="flex items-center space-x-4">
                                    <Skeleton className="h-16 w-16 rounded" />
                                    <div className="space-y-2">
                                        <Skeleton className="h-4 w-48" />
                                        <Skeleton className="h-4 w-24" />
                                    </div>
                                </div>
                                <Skeleton className="h-10 w-32" />
                            </CardContent>
                        </Card>
                    ))}
                </div>
            </div>
        );
    }

    if (!wishlist?.items || wishlist.items.length === 0) {
        return (
            <div className="container mx-auto px-4 py-16 text-center">
                <Heart className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                <h2 className="text-2xl font-semibold mb-2">Favorileriniz Boş</h2>
                <p className="text-muted-foreground mb-6">
                    Henüz favorilerinize hiçbir ürün eklemediniz.
                </p>
                <Button asChild>
                    <Link to="/">Ürünlere Göz At</Link>
                </Button>
            </div>
        );
    }

    const handleRemove = async (productId: number) => {
        try {
            await removeFromWishlist(productId).unwrap();
            setWishlist((current) => current
                ? { ...current, items: current.items.filter((item) => item.productId !== productId) }
                : current);
            toast.success('Ürün favorilerden çıkarıldı.');
        } catch {
            toast.error('Ürün favorilerden çıkarılamadı.');
        }
    };

    const handleClear = async () => {
        try {
            await clearWishlist().unwrap();
            setWishlist((current) => current
                ? { ...current, items: [], hasMore: false, nextCursor: null }
                : current);
            toast.success('Favoriler temizlendi.');
        } catch {
            toast.error('Favoriler temizlenirken bir hata oluştu.');
        }
    };

    const handleAddToCart = async (productId: number, productName: string) => {
        try {
            await addToCart({ productId, quantity: 1 }).unwrap();
            toast.success(`${productName} sepete eklendi.`);
        } catch {
            toast.error('Ürün sepete eklenemedi.');
        }
    };

    const handleLoadMore = async () => {
        if (!wishlist?.nextCursor || isLoadingMore) {
            return;
        }

        setIsLoadingMore(true);
        try {
            const nextPage = await fetchWishlistPage({
                cursor: wishlist.nextCursor,
                limit: WISHLIST_PAGE_SIZE,
            }).unwrap();

            setWishlist((current) => {
                if (!current) {
                    return nextPage;
                }

                const existingIds = new Set(current.items.map((item) => item.id));
                const mergedItems = [
                    ...current.items,
                    ...nextPage.items.filter((item) => !existingIds.has(item.id)),
                ];

                return {
                    ...current,
                    ...nextPage,
                    items: mergedItems,
                };
            });
        } catch {
            toast.error('Favorilerin devamı yüklenemedi.');
        } finally {
            setIsLoadingMore(false);
        }
    };

    const handleAlertDraftChange = (productId: number, value: string) => {
        setAlertDrafts((current) => ({
            ...current,
            [productId]: value,
        }));
    };

    const handleSavePriceAlert = async (item: WishlistResponse['items'][number]) => {
        const rawValue = alertDrafts[item.productId]?.trim() ?? '';
        const targetPrice = Number.parseFloat(rawValue.replace(',', '.'));

        if (!Number.isFinite(targetPrice) || targetPrice <= 0) {
            toast.error('Geçerli bir hedef fiyat giriniz.');
            return;
        }

        try {
            const savedAlert = await upsertWishlistPriceAlert({
                productId: item.productId,
                targetPrice,
            }).unwrap();

            setAlertDrafts((current) => ({
                ...current,
                [item.productId]: savedAlert.targetPrice.toFixed(2),
            }));

            toast.success('Fiyat alarmı kaydedildi.');
        } catch (error) {
            toast.error(getWishlistErrorMessage(error, 'Fiyat alarmı kaydedilemedi.'));
        }
    };

    const handleRemovePriceAlert = async (productId: number) => {
        try {
            await removeWishlistPriceAlert(productId).unwrap();
            toast.success('Fiyat alarmı kaldırıldı.');
        } catch (error) {
            toast.error(getWishlistErrorMessage(error, 'Fiyat alarmı kaldırılamadı.'));
        }
    };

    const renderPriceAlertControls = (item: WishlistResponse['items'][number]) => {
        const activeAlert = priceAlertsByProductId[item.productId];
        const alertDraft = alertDrafts[item.productId] ?? '';

        return (
            <div className="mt-3 rounded-lg border border-border/60 bg-muted/20 p-3">
                <div className="flex items-start justify-between gap-3">
                    <div>
                        <p className="text-sm font-medium flex items-center gap-2">
                            <BellRing className="h-4 w-4 text-amber-500" />
                            Fiyat alarmı
                        </p>
                        <p className="text-xs text-muted-foreground mt-1">
                            Ürün {alertDraft || 'hedef'} {item.productCurrency} seviyesine indiğinde haber verelim.
                        </p>
                        {activeAlert && (
                            <p className="text-xs text-muted-foreground mt-2">
                                Aktif hedef: {activeAlert.targetPrice.toLocaleString('tr-TR')} {activeAlert.currency}
                                {activeAlert.lastNotifiedAt && ` • Son bildirim: ${new Date(activeAlert.lastNotifiedAt).toLocaleString('tr-TR')}`}
                            </p>
                        )}
                    </div>
                    {activeAlert && (
                        <Badge variant="secondary">
                            Alarm aktif
                        </Badge>
                    )}
                </div>

                <div className="mt-3 flex flex-col sm:flex-row gap-2">
                    <Input
                        type="number"
                        min="0"
                        step="0.01"
                        value={alertDraft}
                        disabled={!item.isAvailable}
                        onChange={(event) => handleAlertDraftChange(item.productId, event.target.value)}
                        placeholder="Hedef fiyat"
                        className="sm:max-w-[180px]"
                    />
                    <Button
                        variant="outline"
                        disabled={!item.isAvailable}
                        onClick={() => void handleSavePriceAlert(item)}
                    >
                        <BellRing className="h-4 w-4 mr-2" />
                        {activeAlert ? 'Alarmı Güncelle' : 'Alarm Kur'}
                    </Button>
                    {activeAlert && (
                        <Button
                            variant="ghost"
                            onClick={() => void handleRemovePriceAlert(item.productId)}
                        >
                            <BellOff className="h-4 w-4 mr-2" />
                            Alarmı Kaldır
                        </Button>
                    )}
                </div>
            </div>
        );
    };

    const handleAddAllToCart = async () => {
        try {
            const result = await addAllWishlistItemsToCart().unwrap();
            const message = result.skippedCount === 0
                ? `${result.addedCount} ürün sepete eklendi.`
                : `${result.requestedCount} üründen ${result.addedCount} ürün sepete eklendi, ${result.skippedCount} ürün atlandı.`;

            setBulkAddSummary({
                message,
                addedCount: result.addedCount,
                skippedItems: result.skippedItems,
            });

            if (result.addedCount > 0) {
                toast.success(message);
            } else {
                toast.info(message);
            }
        } catch (error) {
            toast.error(getWishlistErrorMessage(error, 'Ürünler sepete eklenemedi.'));
        }
    };

    const handleEnableSharing = async () => {
        try {
            const nextSettings = await enableWishlistSharing().unwrap();
            const nextShareUrl = nextSettings.sharePath
                ? `${window.location.origin}${nextSettings.sharePath}`
                : null;

            if (nextShareUrl) {
                try {
                    await navigator.clipboard.writeText(nextShareUrl);
                    toast.success('Paylaşım linki oluşturuldu ve kopyalandı.');
                } catch {
                    toast.success('Paylaşım linki oluşturuldu.');
                }
            } else {
                toast.success('Paylaşım linki oluşturuldu.');
            }
        } catch (error) {
            toast.error(getWishlistErrorMessage(error, 'Paylaşım linki oluşturulamadı.'));
        }
    };

    const handleDisableSharing = async () => {
        try {
            await disableWishlistSharing().unwrap();
            toast.success('Wishlist paylaşımı kapatıldı.');
        } catch (error) {
            toast.error(getWishlistErrorMessage(error, 'Wishlist paylaşımı kapatılamadı.'));
        }
    };

    const handleCopyShareUrl = async () => {
        if (!shareUrl) {
            toast.error('Önce paylaşımı açmanız gerekiyor.');
            return;
        }

        try {
            await navigator.clipboard.writeText(shareUrl);
            toast.success('Paylaşım bağlantısı kopyalandı.');
        } catch {
            toast.error('Paylaşım bağlantısı kopyalanamadı.');
        }
    };

    return (
        <div className="container mx-auto px-4 py-8">
            <div className="flex justify-between items-center mb-8">
                <h1 className="text-3xl font-bold">Favorilerim</h1>
                <div className="flex items-center gap-2">
                    <div className="flex items-center border rounded-md p-1 bg-muted/20">
                        <Button
                            variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => setViewMode('grid')}
                        >
                            <LayoutGrid className="h-4 w-4" />
                        </Button>
                        <Button
                            variant={viewMode === 'list' ? 'secondary' : 'ghost'}
                            size="icon"
                            className="h-8 w-8"
                            onClick={() => setViewMode('list')}
                        >
                            <List className="h-4 w-4" />
                        </Button>
                    </div>
                    {shareSettings?.isPublic ? (
                        <>
                            <Button variant="outline" onClick={() => void handleCopyShareUrl()}>
                                <Copy className="h-4 w-4 mr-2" />
                                Linki Kopyala
                            </Button>
                            <Button
                                variant="ghost"
                                onClick={() => void handleDisableSharing()}
                                disabled={isDisablingSharing}
                            >
                                <Link2Off className="h-4 w-4 mr-2" />
                                {isDisablingSharing ? 'Kapatılıyor...' : 'Paylaşımı Kapat'}
                            </Button>
                        </>
                    ) : (
                        <Button
                            variant="outline"
                            onClick={() => void handleEnableSharing()}
                            disabled={isEnablingSharing}
                        >
                            <Share2 className="h-4 w-4 mr-2" />
                            {isEnablingSharing ? 'Hazırlanıyor...' : 'Paylaşılabilir Link Oluştur'}
                        </Button>
                    )}
                    <Button
                        onClick={() => void handleAddAllToCart()}
                        disabled={!wishlist.items.some((item) => item.isAvailable) || isBulkAddingToCart}
                    >
                        <ShoppingCart className="h-4 w-4 mr-2" />
                        {isBulkAddingToCart ? 'Ekleniyor...' : 'Tümünü Sepete Ekle'}
                    </Button>
                    <Button variant="outline" onClick={handleClear} disabled={wishlist.items.length === 0}>
                        <Trash2 className="h-4 w-4 mr-2" />
                        Listeyi Temizle
                    </Button>
                </div>
            </div>

            {bulkAddSummary && (
                <Card className="mb-6 border-emerald-200/60 bg-emerald-50/60">
                    <CardContent className="p-4">
                        <p className="font-medium text-sm">{bulkAddSummary.message}</p>
                        {bulkAddSummary.skippedItems.length > 0 && (
                            <div className="mt-3 space-y-1 text-sm text-muted-foreground">
                                {bulkAddSummary.skippedItems.slice(0, 4).map((item) => (
                                    <p key={`${item.productId}-${item.reason}`}>
                                        {item.productName}: {item.reason}
                                    </p>
                                ))}
                                {bulkAddSummary.skippedItems.length > 4 && (
                                    <p>+ {bulkAddSummary.skippedItems.length - 4} ürün daha atlandı.</p>
                                )}
                            </div>
                        )}
                    </CardContent>
                </Card>
            )}

            {shareSettings?.isPublic && shareUrl && (
                <Card className="mb-6 border-sky-200/70 bg-sky-50/60">
                    <CardContent className="p-4">
                        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                            <div>
                                <p className="text-sm font-medium">Bu favori listesi paylaşılabilir durumda.</p>
                                <p className="mt-1 break-all text-sm text-muted-foreground">{shareUrl}</p>
                            </div>
                            <div className="flex items-center gap-2">
                                <Button variant="outline" onClick={() => void handleCopyShareUrl()}>
                                    <Copy className="mr-2 h-4 w-4" />
                                    Kopyala
                                </Button>
                                <Button asChild variant="ghost">
                                    <Link to={shareSettings.sharePath ?? '/wishlist'}>
                                        <Share2 className="mr-2 h-4 w-4" />
                                        Önizle
                                    </Link>
                                </Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            )}

            <div className={viewMode === 'grid' ? "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6" : "grid grid-cols-1 gap-4"}>
                {wishlist.items.map((item) => (
                    viewMode === 'list' ? (
                        <Card key={item.id} className="overflow-hidden">
                            <CardContent className="p-0 sm:p-4">
                                <div className="flex flex-col sm:flex-row items-center gap-4">
                                    <div className="w-full sm:w-24 h-24 bg-muted flex items-center justify-center shrink-0">
                                        <Package className="h-8 w-8 text-muted-foreground" />
                                    </div>

                                    <div className="flex-1 flex flex-col sm:flex-row items-center justify-between w-full p-4 sm:p-0 gap-4">
                                        <div className="flex-1 min-w-0 text-center sm:text-left">
                                            {item.isAvailable ? (
                                                <Link to={`/products/${item.productId}`} className="font-semibold hover:text-primary transition-colors hover:underline truncate block">
                                                    {item.productName}
                                                </Link>
                                            ) : (
                                                <span className="font-semibold text-muted-foreground truncate block">
                                                    {item.productName}
                                                </span>
                                            )}
                                            {!item.isAvailable && (
                                                <Badge variant="secondary" className="mt-2">
                                                    Satışta Değil
                                                </Badge>
                                            )}
                                            <div className="flex items-center gap-2 justify-center sm:justify-start mt-1">
                                                <p className="text-sm font-medium">
                                                    {item.productPrice.toLocaleString('tr-TR')} {item.productCurrency}
                                                </p>
                                                {item.isAvailable && item.priceChange !== 0 && item.priceChange !== undefined && (
                                                    <span className={`text-xs flex items-center gap-1 ${item.priceChange > 0 ? 'text-red-500' : 'text-green-500'}`}>
                                                        {item.priceChange > 0 ? <TrendingUp className="h-3 w-3" /> : <TrendingDown className="h-3 w-3" />}
                                                        {Math.abs(item.priceChangePercentage || 0).toFixed(1)}%
                                                    </span>
                                                )}
                                            </div>
                                            <p className="text-xs text-muted-foreground mt-1">
                                                Eklenme: {new Date(item.addedAt).toLocaleDateString('tr-TR')} • {item.addedAtPrice.toLocaleString('tr-TR')} {item.productCurrency}
                                            </p>
                                            {!item.isAvailable && item.unavailableReason && (
                                                <p className="text-xs text-muted-foreground mt-1">
                                                    {item.unavailableReason}
                                                </p>
                                            )}
                                            {renderPriceAlertControls(item)}
                                        </div>

                                        <div className="flex flex-col sm:flex-row items-center gap-2 w-full sm:w-auto">
                                            <Button
                                                variant="default"
                                                className="w-full sm:w-auto"
                                                disabled={!item.isAvailable || isAddingToCart}
                                                onClick={() => handleAddToCart(item.productId, item.productName)}
                                            >
                                                <ShoppingCart className="h-4 w-4 mr-2" />
                                                Sepete Ekle
                                            </Button>
                                            <Button
                                                variant="destructive"
                                                size="icon"
                                                className="w-full sm:w-10"
                                                onClick={() => handleRemove(item.productId)}
                                            >
                                                <Trash2 className="h-4 w-4" />
                                            </Button>
                                        </div>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    ) : (
                        <Card key={item.id} className="overflow-hidden flex flex-col h-full group">
                            <div className="relative h-48 bg-muted flex items-center justify-center">
                                <Package className="h-16 w-16 text-muted-foreground" />
                                <Button
                                    variant="ghost"
                                    size="icon"
                                    className="absolute top-2 right-2 rounded-full h-8 w-8 bg-background/50 hover:bg-background/80"
                                    onClick={() => handleRemove(item.productId)}
                                >
                                    <Heart className="h-5 w-5 fill-red-500 text-red-500" />
                                </Button>
                            </div>
                            <CardContent className="p-4 flex-1">
                                {item.isAvailable ? (
                                    <Link to={`/products/${item.productId}`}>
                                        <h3 className="font-semibold truncate group-hover:text-primary transition-colors">
                                            {item.productName}
                                        </h3>
                                    </Link>
                                ) : (
                                    <h3 className="font-semibold truncate text-muted-foreground">
                                        {item.productName}
                                    </h3>
                                )}
                                {!item.isAvailable && (
                                    <Badge variant="secondary" className="mt-2">
                                        Satışta Değil
                                    </Badge>
                                )}
                                <div className="flex items-center gap-2 mt-2">
                                    <p className="text-lg font-bold">
                                        {item.productPrice.toLocaleString('tr-TR')} {item.productCurrency}
                                    </p>
                                    {item.isAvailable && item.priceChange !== 0 && item.priceChange !== undefined && (
                                        <span className={`text-sm flex items-center gap-1 ${item.priceChange > 0 ? 'text-red-500' : 'text-green-500'}`}>
                                            {item.priceChange > 0 ? <TrendingUp className="h-4 w-4" /> : <TrendingDown className="h-4 w-4" />}
                                            {Math.abs(item.priceChangePercentage || 0).toFixed(1)}%
                                        </span>
                                    )}
                                </div>
                                {item.addedAtPrice !== item.productPrice && (
                                    <p className="text-xs text-muted-foreground mt-1">
                                        Eklendiğinde: {item.addedAtPrice.toLocaleString('tr-TR')} {item.productCurrency}
                                    </p>
                                )}
                                {!item.isAvailable && item.unavailableReason && (
                                    <p className="text-xs text-muted-foreground mt-2">
                                        {item.unavailableReason}
                                    </p>
                                )}
                                {renderPriceAlertControls(item)}
                            </CardContent>
                            <div className="p-4 pt-0 mt-auto">
                                <Button
                                    className="w-full"
                                    disabled={!item.isAvailable || isAddingToCart}
                                    onClick={() => handleAddToCart(item.productId, item.productName)}
                                >
                                    <ShoppingCart className="mr-2 h-4 w-4" />
                                    Sepete Ekle
                                </Button>
                            </div>
                        </Card>
                    )
                ))}
            </div>

            {wishlist.hasMore && (
                <div className="flex justify-center mt-8">
                    <Button
                        variant="outline"
                        onClick={() => void handleLoadMore()}
                        disabled={isLoadingMore}
                    >
                        {isLoadingMore ? 'Yukleniyor...' : 'Daha Fazla Yukle'}
                    </Button>
                </div>
            )}
        </div>
    );
}
