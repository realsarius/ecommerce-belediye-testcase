import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Trash2, ShoppingCart, Package, Heart, LayoutGrid, List, TrendingDown, TrendingUp } from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Card, CardContent } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { useAppSelector } from '@/app/hooks';
import { useGetWishlistQuery, useRemoveWishlistItemMutation, useClearWishlistMutation } from '@/features/wishlist/wishlistApi';
import { useAddToCartMutation } from '@/features/cart/cartApi';
import { toast } from 'sonner';

export default function Wishlist() {
    const { isAuthenticated } = useAppSelector((state) => state.auth);
    const { data: wishlist, isLoading } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
    const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
    const [removeFromWishlist] = useRemoveWishlistItemMutation();
    const [clearWishlist] = useClearWishlistMutation();
    const [addToCart, { isLoading: isAddingToCart }] = useAddToCartMutation();

    if (!isAuthenticated) {
        return (
            <div className="container mx-auto px-4 py-16 text-center">
                <Heart className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
                <h2 className="text-2xl font-semibold mb-2">Giriş Yapmanız Gerekiyor</h2>
                <p className="text-muted-foreground mb-6">
                    Favorilerinizi görmek için lütfen giriş yapın.
                </p>
                <Button asChild>
                    <Link to="/login">Giriş Yap</Link>
                </Button>
            </div>
        );
    }

    if (isLoading) {
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
            toast.success('Ürün favorilerden çıkarıldı.');
        } catch {
            toast.error('Ürün favorilerden çıkarılamadı.');
        }
    };

    const handleClear = async () => {
        try {
            await clearWishlist().unwrap();
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
                    <Button variant="outline" onClick={handleClear} disabled={wishlist.items.length === 0}>
                        <Trash2 className="h-4 w-4 mr-2" />
                        Listeyi Temizle
                    </Button>
                </div>
            </div>

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
        </div>
    );
}
