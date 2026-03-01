import { useEffect, useEffectEvent, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Trash2, ShoppingCart, Heart, LayoutGrid, List, Share2, Copy, Link2Off, FolderPlus } from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Card, CardContent } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { useAppSelector } from '@/app/hooks';
import type { Wishlist as WishlistResponse } from '@/features/wishlist/types';
import { clearGuestWishlistProducts, getWishlistErrorMessage, useGuestWishlist } from '@/features/wishlist';
import {
    useLazyGetWishlistQuery,
    useGetWishlistCollectionsQuery,
    useCreateWishlistCollectionMutation,
    useMoveWishlistItemToCollectionMutation,
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
import { WishlistCreateCollectionDialog } from '@/features/wishlist/components/WishlistCreateCollectionDialog';
import { WishlistGuestState } from '@/features/wishlist/components/WishlistGuestState';
import { WishlistItemsView } from '@/features/wishlist/components/WishlistItemsView';

const WISHLIST_PAGE_SIZE = 20;

export default function Wishlist() {
    const { isAuthenticated } = useAppSelector((state) => state.auth);
    const { pendingCount } = useGuestWishlist();
    const [fetchWishlistPage] = useLazyGetWishlistQuery();
    const { data: collections = [] } = useGetWishlistCollectionsQuery(undefined, { skip: !isAuthenticated });
    const [createWishlistCollection, { isLoading: isCreatingCollection }] = useCreateWishlistCollectionMutation();
    const [moveWishlistItemToCollection] = useMoveWishlistItemToCollectionMutation();
    const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
    const [selectedCollectionId, setSelectedCollectionId] = useState<number | null>(null);
    const [isCreateCollectionOpen, setIsCreateCollectionOpen] = useState(false);
    const [newCollectionName, setNewCollectionName] = useState('');
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

    const totalWishlistItemCount = useMemo(
        () => collections.reduce((total, collection) => total + collection.itemCount, 0),
        [collections],
    );

    const collectionNameById = useMemo(
        () => new Map(collections.map((collection) => [collection.id, collection.name])),
        [collections],
    );
    const totalVisibleCount = wishlist?.items.length ?? 0;
    const totalCollectionItemCount = totalWishlistItemCount > 0
        ? totalWishlistItemCount
        : totalVisibleCount;
    const availableItemCount = wishlist?.items.filter((item) => item.isAvailable).length ?? 0;
    const activePriceAlertCount = priceAlerts.filter((alert) => alert.isActive).length;
    const selectedCollectionName = selectedCollectionId === null
        ? 'Tüm Favoriler'
        : collectionNameById.get(selectedCollectionId) ?? 'Seçili Koleksiyon';

    const shareUrl = shareSettings?.sharePath
        ? `${window.location.origin}${shareSettings.sharePath}`
        : null;

    const loadWishlistPage = useEffectEvent(async (collectionId: number | null, cursor?: string) => {
        const nextWishlist = await fetchWishlistPage({
            limit: WISHLIST_PAGE_SIZE,
            cursor,
            collectionId: collectionId ?? undefined,
        }).unwrap();

        setWishlist((current) => {
            if (!cursor || !current) {
                return nextWishlist;
            }

            const existingIds = new Set(current.items.map((item) => item.id));
            return {
                ...current,
                ...nextWishlist,
                items: [
                    ...current.items,
                    ...nextWishlist.items.filter((item) => !existingIds.has(item.id)),
                ],
            };
        });
    });

    useEffect(() => {
        if (!isAuthenticated) {
            setWishlist(null);
            setLoadFailed(false);
            setIsLoading(false);
            setIsLoadingMore(false);
            setAlertDrafts({});
            setBulkAddSummary(null);
            setSelectedCollectionId(null);
            setIsCreateCollectionOpen(false);
            setNewCollectionName('');
            return;
        }

        setIsLoading(true);
        setLoadFailed(false);

        void loadWishlistPage(selectedCollectionId)
            .catch(() => {
                setLoadFailed(true);
            })
            .finally(() => {
                setIsLoading(false);
            });
    }, [isAuthenticated, selectedCollectionId]);

    useEffect(() => {
        if (selectedCollectionId === null) {
            return;
        }

        if (collections.some((collection) => collection.id === selectedCollectionId)) {
            return;
        }

        setSelectedCollectionId(null);
    }, [collections, selectedCollectionId]);

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
        return (
            <WishlistGuestState
                pendingCount={pendingCount}
                onClearPending={() => {
                    clearGuestWishlistProducts();
                    toast.success('Bekleyen favoriler temizlendi.');
                }}
            />
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
                            const nextWishlist = await fetchWishlistPage({
                                limit: WISHLIST_PAGE_SIZE,
                                collectionId: selectedCollectionId ?? undefined,
                            }).unwrap();
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
        if (selectedCollectionId !== null) {
            return (
                <div className="container mx-auto px-4 py-16 text-center">
                    <Heart className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                    <h2 className="text-2xl font-semibold mb-2">{selectedCollectionName} Koleksiyonu Boş</h2>
                    <p className="text-muted-foreground mb-6">
                        Bu koleksiyonda henüz ürün yok. İsterseniz tüm favorilerinize dönün ya da yeni ürünler ekleyin.
                    </p>
                    <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
                        <Button variant="outline" onClick={() => setSelectedCollectionId(null)}>
                            Tüm Favorilere Dön
                        </Button>
                        <Button asChild>
                            <Link to="/">Ürünlere Göz At</Link>
                        </Button>
                    </div>
                </div>
            );
        }

        return (
            <div className="container mx-auto px-4 py-16 text-center">
                <Heart className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                <h2 className="text-2xl font-semibold mb-2">Favorileriniz Boş</h2>
                <p className="text-muted-foreground mb-6">
                    Henüz favorilerinize hiçbir ürün eklemediniz.
                </p>
                <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
                    <Button
                        variant="outline"
                        onClick={() => setIsCreateCollectionOpen(true)}
                    >
                        <FolderPlus className="h-4 w-4 mr-2" />
                        Yeni Koleksiyon
                    </Button>
                    <Button asChild>
                        <Link to="/">Ürünlere Göz At</Link>
                    </Button>
                </div>

                <WishlistCreateCollectionDialog
                    open={isCreateCollectionOpen}
                    title="Yeni Koleksiyon Oluştur"
                    description="Favori ürünlerinizi başlıklara ayırarak daha kolay takip edin."
                    placeholder="Örn. Hediye Fikirleri"
                    confirmLabel="Koleksiyonu Oluştur"
                    name={newCollectionName}
                    isCreating={isCreatingCollection}
                    onOpenChange={setIsCreateCollectionOpen}
                    onNameChange={setNewCollectionName}
                    onCreate={() => {
                        void handleCreateCollection();
                    }}
                />
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
                collectionId: selectedCollectionId ?? undefined,
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

    const handleCreateCollection = async () => {
        const name = newCollectionName.trim();
        if (name.length < 2) {
            toast.error('Koleksiyon adı en az 2 karakter olmalıdır.');
            return;
        }

        try {
            const collection = await createWishlistCollection({ name }).unwrap();
            setSelectedCollectionId(collection.id);
            setNewCollectionName('');
            setIsCreateCollectionOpen(false);
            toast.success(`${collection.name} koleksiyonu oluşturuldu.`);
        } catch (error) {
            toast.error(getWishlistErrorMessage(error, 'Koleksiyon oluşturulamadı.'));
        }
    };

    const handleMoveToCollection = async (productId: number, nextCollectionId: string) => {
        const parsedCollectionId = Number.parseInt(nextCollectionId, 10);
        if (!Number.isFinite(parsedCollectionId)) {
            return;
        }

        const targetCollectionName = collectionNameById.get(parsedCollectionId) ?? 'seçilen koleksiyon';
        const targetItem = wishlist.items.find((item) => item.productId === productId);
        if (!targetItem || targetItem.collectionId === parsedCollectionId) {
            return;
        }

        try {
            await moveWishlistItemToCollection({
                productId,
                body: { collectionId: parsedCollectionId },
            }).unwrap();

            setWishlist((current) => {
                if (!current) {
                    return current;
                }

                if (selectedCollectionId !== null) {
                    return {
                        ...current,
                        items: current.items.filter((item) => item.productId !== productId),
                    };
                }

                return {
                    ...current,
                    items: current.items.map((item) => item.productId === productId
                        ? { ...item, collectionId: parsedCollectionId, collectionName: targetCollectionName }
                        : item),
                };
            });

            toast.success(`Ürün ${targetCollectionName} koleksiyonuna taşındı.`);
        } catch (error) {
            toast.error(getWishlistErrorMessage(error, 'Ürün koleksiyona taşınamadı.'));
        }
    };

    return (
        <div className="container mx-auto px-4 py-8">
            <div className="mb-8 overflow-hidden rounded-[2rem] border border-white/10 bg-[radial-gradient(circle_at_top_left,_rgba(244,63,94,0.14),_transparent_24%),linear-gradient(135deg,_rgba(24,24,27,0.92),_rgba(10,10,12,0.96))] p-6 shadow-[0_18px_60px_rgba(0,0,0,0.24)]">
                <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
                    <div className="max-w-2xl">
                        <Badge variant="secondary" className="mb-3 border border-rose-400/20 bg-rose-500/10 text-rose-100">
                            Kişisel alan
                        </Badge>
                        <h1 className="text-3xl font-bold text-white">Favorilerim</h1>
                        <p className="mt-2 text-sm text-white/65">
                            Fiyatını takip etmek, koleksiyonlara ayırmak ve doğru zamanda sepete taşımak istediğiniz ürünleri burada yönetin.
                        </p>

                        <div className="mt-4 flex flex-wrap gap-2">
                            <Badge variant="outline" className="border-white/10 bg-white/5 text-white/80">
                                {selectedCollectionName}
                            </Badge>
                            <Badge variant="outline" className="border-white/10 bg-white/5 text-white/80">
                                {totalVisibleCount} ürün görüntüleniyor
                            </Badge>
                            <Badge variant="outline" className="border-white/10 bg-white/5 text-white/80">
                                {availableItemCount} ürün sepete uygun
                            </Badge>
                            <Badge variant="outline" className="border-white/10 bg-white/5 text-white/80">
                                {activePriceAlertCount} aktif alarm
                            </Badge>
                            <Badge variant="outline" className="border-white/10 bg-white/5 text-white/80">
                                {shareSettings?.isPublic ? 'Paylaşım açık' : 'Özel liste'}
                            </Badge>
                        </div>
                    </div>

                    <div className="flex flex-wrap items-center gap-2 lg:justify-end">
                        <div className="flex items-center border border-white/10 rounded-md p-1 bg-white/5">
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
                            {isBulkAddingToCart
                                ? 'Ekleniyor...'
                                : selectedCollectionId === null
                                    ? 'Tümünü Sepete Ekle'
                                    : 'Tüm Favorileri Sepete Ekle'}
                        </Button>
                        <Button variant="outline" onClick={handleClear} disabled={wishlist.items.length === 0}>
                            <Trash2 className="h-4 w-4 mr-2" />
                            Listeyi Temizle
                        </Button>
                    </div>
                </div>
            </div>

            <div className="mb-8 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <Card className="border-border/60 bg-muted/20">
                    <CardContent className="p-4">
                        <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Toplam birikim</p>
                        <p className="mt-2 text-2xl font-semibold">{totalCollectionItemCount}</p>
                        <p className="mt-1 text-sm text-muted-foreground">Koleksiyonlar içindeki toplam favori</p>
                    </CardContent>
                </Card>
                <Card className="border-border/60 bg-muted/20">
                    <CardContent className="p-4">
                        <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Görünen alan</p>
                        <p className="mt-2 text-2xl font-semibold">{totalVisibleCount}</p>
                        <p className="mt-1 text-sm text-muted-foreground">{selectedCollectionName} için aktif görünüm</p>
                    </CardContent>
                </Card>
                <Card className="border-border/60 bg-muted/20">
                    <CardContent className="p-4">
                        <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Fiyat takibi</p>
                        <p className="mt-2 text-2xl font-semibold">{activePriceAlertCount}</p>
                        <p className="mt-1 text-sm text-muted-foreground">Aktif fiyat alarmı bulunuyor</p>
                    </CardContent>
                </Card>
                <Card className="border-border/60 bg-muted/20">
                    <CardContent className="p-4">
                        <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Paylaşım durumu</p>
                        <p className="mt-2 text-2xl font-semibold">{shareSettings?.isPublic ? 'Açık' : 'Kapalı'}</p>
                        <p className="mt-1 text-sm text-muted-foreground">Liste erişimi {shareSettings?.isPublic ? 'link ile açık' : 'yalnızca size özel'}</p>
                    </CardContent>
                </Card>
            </div>

            <div className="flex justify-between items-center mb-8">
                <div>
                    <h2 className="text-xl font-semibold">{selectedCollectionName}</h2>
                    <p className="mt-1 text-sm text-muted-foreground">
                        Ürünleri koleksiyonlara ayırın, alarm kurun ve satın alma zamanlamanızı yönetin.
                    </p>
                </div>
                <div className="hidden lg:flex items-center gap-2">
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

            <div className="mb-6 rounded-2xl border border-border/60 bg-muted/20 p-4">
                <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                    <div>
                        <p className="text-sm font-medium">Koleksiyonlar</p>
                        <p className="text-sm text-muted-foreground mt-1">
                            Favorilerinizi konulara ayırın, ürünleri koleksiyonlar arasında taşıyın.
                        </p>
                    </div>
                    <Button variant="outline" onClick={() => setIsCreateCollectionOpen(true)}>
                        <FolderPlus className="h-4 w-4 mr-2" />
                        Yeni Koleksiyon
                    </Button>
                </div>

                <div className="mt-4 flex flex-wrap gap-2">
                    <Button
                        variant={selectedCollectionId === null ? 'default' : 'outline'}
                        onClick={() => setSelectedCollectionId(null)}
                    >
                        Tüm Favoriler
                        <Badge variant={selectedCollectionId === null ? 'secondary' : 'outline'} className="ml-2">
                            {totalCollectionItemCount}
                        </Badge>
                    </Button>

                    {collections.map((collection) => (
                        <Button
                            key={collection.id}
                            variant={selectedCollectionId === collection.id ? 'default' : 'outline'}
                            onClick={() => setSelectedCollectionId(collection.id)}
                        >
                            {collection.name}
                            <Badge
                                variant={selectedCollectionId === collection.id ? 'secondary' : 'outline'}
                                className="ml-2"
                            >
                                {collection.itemCount}
                            </Badge>
                        </Button>
                    ))}
                </div>
            </div>

            <WishlistItemsView
                items={wishlist.items}
                viewMode={viewMode}
                collections={collections}
                priceAlerts={priceAlerts}
                alertDrafts={alertDrafts}
                isAddingToCart={isAddingToCart}
                onAlertDraftChange={handleAlertDraftChange}
                onSavePriceAlert={(item) => {
                    void handleSavePriceAlert(item);
                }}
                onRemovePriceAlert={(productId) => {
                    void handleRemovePriceAlert(productId);
                }}
                onMoveToCollection={(productId, value) => {
                    void handleMoveToCollection(productId, value);
                }}
                onAddToCart={(productId, productName) => {
                    void handleAddToCart(productId, productName);
                }}
                onRemove={(productId) => {
                    void handleRemove(productId);
                }}
            />

            {wishlist.hasMore && (
                <div className="flex justify-center mt-8">
                    <Button
                        variant="outline"
                        onClick={() => void handleLoadMore()}
                        disabled={isLoadingMore}
                    >
                        {isLoadingMore ? 'Yükleniyor...' : 'Daha Fazla Yükle'}
                    </Button>
                </div>
            )}

            <WishlistCreateCollectionDialog
                open={isCreateCollectionOpen}
                title="Yeni Koleksiyon Oluştur"
                description='Örneğin "Ev Dekorasyonu", "Teknoloji" veya "Hediye Fikirleri" gibi ayrı listeler oluşturabilirsiniz.'
                placeholder="Koleksiyon adı"
                confirmLabel="Koleksiyon Oluştur"
                name={newCollectionName}
                isCreating={isCreatingCollection}
                onOpenChange={(open) => {
                    setIsCreateCollectionOpen(open);
                    if (!open) {
                        setNewCollectionName('');
                    }
                }}
                onNameChange={setNewCollectionName}
                onCreate={() => {
                    void handleCreateCollection();
                }}
            />
        </div>
    );
}
