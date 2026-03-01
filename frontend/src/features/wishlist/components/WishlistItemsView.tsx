import { Heart, Package, ShoppingCart, Trash2, TrendingDown, TrendingUp } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent } from '@/components/common/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/common/select';
import type { WishlistCollection, WishlistItem, WishlistPriceAlert } from '@/features/wishlist/types';
import { WishlistPriceAlertControls } from './WishlistPriceAlertControls';

interface WishlistItemsViewProps {
    items: WishlistItem[];
    viewMode: 'grid' | 'list';
    collections: WishlistCollection[];
    priceAlerts: WishlistPriceAlert[];
    alertDrafts: Record<number, string>;
    isAddingToCart: boolean;
    onAlertDraftChange: (productId: number, value: string) => void;
    onSavePriceAlert: (item: WishlistItem) => void;
    onRemovePriceAlert: (productId: number) => void;
    onMoveToCollection: (productId: number, nextCollectionId: string) => void;
    onAddToCart: (productId: number, productName: string) => void;
    onRemove: (productId: number) => void;
}

export function WishlistItemsView({
    items,
    viewMode,
    collections,
    priceAlerts,
    alertDrafts,
    isAddingToCart,
    onAlertDraftChange,
    onSavePriceAlert,
    onRemovePriceAlert,
    onMoveToCollection,
    onAddToCart,
    onRemove,
}: WishlistItemsViewProps) {
    return (
        <div className={viewMode === 'grid' ? 'grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6' : 'grid grid-cols-1 gap-4'}>
            {items.map((item) => {
                const activeAlert = priceAlerts.find((alert) => alert.productId === item.productId);
                const alertDraft = alertDrafts[item.productId] ?? '';

                if (viewMode === 'list') {
                    return (
                        <Card key={item.id} className="overflow-hidden">
                            <CardContent className="p-0 sm:p-4">
                                <div className="flex flex-col sm:flex-row items-center gap-4">
                                    <div className="w-full sm:w-24 h-24 bg-muted flex items-center justify-center shrink-0">
                                        <Package className="h-8 w-8 text-muted-foreground" />
                                    </div>

                                    <div className="flex-1 flex flex-col sm:flex-row items-center justify-between w-full p-4 sm:p-0 gap-4">
                                        <div className="flex-1 min-w-0 text-center sm:text-left">
                                            {item.isAvailable ? (
                                                <Link
                                                    to={`/products/${item.productId}`}
                                                    className="font-semibold hover:text-primary transition-colors hover:underline truncate block"
                                                >
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
                                            <div className="mt-2 flex flex-wrap items-center gap-2 justify-center sm:justify-start">
                                                <Badge variant="outline">{item.collectionName}</Badge>
                                            </div>
                                            {!item.isAvailable && item.unavailableReason && (
                                                <p className="text-xs text-muted-foreground mt-1">
                                                    {item.unavailableReason}
                                                </p>
                                            )}
                                            <WishlistPriceAlertControls
                                                item={item}
                                                activeAlert={activeAlert}
                                                alertDraft={alertDraft}
                                                onAlertDraftChange={onAlertDraftChange}
                                                onSavePriceAlert={onSavePriceAlert}
                                                onRemovePriceAlert={onRemovePriceAlert}
                                            />
                                        </div>

                                        <div className="flex flex-col sm:flex-row items-center gap-2 w-full sm:w-auto">
                                            <Select
                                                value={String(item.collectionId)}
                                                onValueChange={(value) => onMoveToCollection(item.productId, value)}
                                            >
                                                <SelectTrigger className="w-full sm:w-[180px]">
                                                    <SelectValue placeholder="Koleksiyon seçin" />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    {collections.map((collection) => (
                                                        <SelectItem key={collection.id} value={String(collection.id)}>
                                                            {collection.name}
                                                        </SelectItem>
                                                    ))}
                                                </SelectContent>
                                            </Select>
                                            <Button
                                                variant="default"
                                                className="w-full sm:w-auto"
                                                disabled={!item.isAvailable || isAddingToCart}
                                                onClick={() => onAddToCart(item.productId, item.productName)}
                                            >
                                                <ShoppingCart className="h-4 w-4 mr-2" />
                                                Sepete Ekle
                                            </Button>
                                            <Button
                                                variant="destructive"
                                                size="icon"
                                                className="w-full sm:w-10"
                                                onClick={() => onRemove(item.productId)}
                                            >
                                                <Trash2 className="h-4 w-4" />
                                            </Button>
                                        </div>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    );
                }

                return (
                    <Card key={item.id} className="overflow-hidden flex flex-col h-full group">
                        <div className="relative h-48 bg-muted flex items-center justify-center">
                            <Package className="h-16 w-16 text-muted-foreground" />
                            <Button
                                variant="ghost"
                                size="icon"
                                className="absolute top-2 right-2 rounded-full h-8 w-8 bg-background/50 hover:bg-background/80"
                                onClick={() => onRemove(item.productId)}
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
                            <div className="mt-2 flex items-center gap-2">
                                <Badge variant="outline">{item.collectionName}</Badge>
                            </div>
                            {!item.isAvailable && item.unavailableReason && (
                                <p className="text-xs text-muted-foreground mt-2">
                                    {item.unavailableReason}
                                </p>
                            )}
                            <WishlistPriceAlertControls
                                item={item}
                                activeAlert={activeAlert}
                                alertDraft={alertDraft}
                                onAlertDraftChange={onAlertDraftChange}
                                onSavePriceAlert={onSavePriceAlert}
                                onRemovePriceAlert={onRemovePriceAlert}
                            />
                            <div className="mt-3">
                                <Select
                                    value={String(item.collectionId)}
                                    onValueChange={(value) => onMoveToCollection(item.productId, value)}
                                >
                                    <SelectTrigger className="w-full">
                                        <SelectValue placeholder="Koleksiyon seçin" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {collections.map((collection) => (
                                            <SelectItem key={collection.id} value={String(collection.id)}>
                                                {collection.name}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                        </CardContent>
                        <div className="p-4 pt-0 mt-auto">
                            <Button
                                className="w-full"
                                disabled={!item.isAvailable || isAddingToCart}
                                onClick={() => onAddToCart(item.productId, item.productName)}
                            >
                                <ShoppingCart className="mr-2 h-4 w-4" />
                                Sepete Ekle
                            </Button>
                        </div>
                    </Card>
                );
            })}
        </div>
    );
}
