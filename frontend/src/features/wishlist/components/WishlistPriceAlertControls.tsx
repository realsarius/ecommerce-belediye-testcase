import { BellOff, BellRing } from 'lucide-react';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import type { WishlistItem, WishlistPriceAlert } from '@/features/wishlist/types';

interface WishlistPriceAlertControlsProps {
    item: WishlistItem;
    activeAlert?: WishlistPriceAlert;
    alertDraft: string;
    onAlertDraftChange: (productId: number, value: string) => void;
    onSavePriceAlert: (item: WishlistItem) => void;
    onRemovePriceAlert: (productId: number) => void;
}

export function WishlistPriceAlertControls({
    item,
    activeAlert,
    alertDraft,
    onAlertDraftChange,
    onSavePriceAlert,
    onRemovePriceAlert,
}: WishlistPriceAlertControlsProps) {
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
                {activeAlert && <Badge variant="secondary">Alarm aktif</Badge>}
            </div>

            <div className="mt-3 flex flex-col sm:flex-row gap-2">
                <Input
                    type="number"
                    min="0"
                    step="0.01"
                    value={alertDraft}
                    disabled={!item.isAvailable}
                    onChange={(event) => onAlertDraftChange(item.productId, event.target.value)}
                    placeholder="Hedef fiyat"
                    className="sm:max-w-[180px]"
                />
                <Button
                    variant="outline"
                    disabled={!item.isAvailable}
                    onClick={() => onSavePriceAlert(item)}
                >
                    <BellRing className="h-4 w-4 mr-2" />
                    {activeAlert ? 'Alarmı Güncelle' : 'Alarm Kur'}
                </Button>
                {activeAlert && (
                    <Button variant="ghost" onClick={() => onRemovePriceAlert(item.productId)}>
                        <BellOff className="h-4 w-4 mr-2" />
                        Alarmı Kaldır
                    </Button>
                )}
            </div>
        </div>
    );
}
