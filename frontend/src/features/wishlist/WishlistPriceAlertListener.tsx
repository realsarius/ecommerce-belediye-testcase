import { useEffect } from 'react';
import { useAppSelector } from '@/app/hooks';
import { createWishlistConnection, ensureWishlistConnectionStarted } from './wishlistHub';
import type { WishlistLowStockNotification, WishlistPriceAlertNotification } from './types';
import { toast } from 'sonner';

export function WishlistPriceAlertListener() {
    const { isAuthenticated, token } = useAppSelector((state) => state.auth);

    useEffect(() => {
        if (!isAuthenticated || !token) {
            return;
        }

        const connection = createWishlistConnection(() => token);

        connection.on('PriceAlertTriggered', (payload: WishlistPriceAlertNotification) => {
            toast.success(
                `${payload.productName} için fiyat alarmı tetiklendi`,
                {
                    description: `${payload.oldPrice.toLocaleString('tr-TR')} ${payload.currency} yerine ${payload.newPrice.toLocaleString('tr-TR')} ${payload.currency}`,
                });
        });

        connection.on('LowStockAlertTriggered', (payload: WishlistLowStockNotification) => {
            toast.info(
                `${payload.productName} için stok azalıyor`,
                {
                    description: `Favorilerinizdeki ürün için yalnızca ${payload.stockQuantity} adet kaldı.`,
                });
        });

        void ensureWishlistConnectionStarted(connection).catch(() => {
            // Fiyat alarmı dinleyicisi arka planda sessizce yeniden denenecek.
        });

        return () => {
            connection.off('PriceAlertTriggered');
            connection.off('LowStockAlertTriggered');
            void connection.stop();
        };
    }, [isAuthenticated, token]);

    return null;
}
