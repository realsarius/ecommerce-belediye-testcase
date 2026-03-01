import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { createWishlistConnection, ensureWishlistConnectionStarted } from './wishlistHub';
import type { CampaignStatusChangedNotification, WishlistLowStockNotification, WishlistPriceAlertNotification } from './types';
import { toast } from 'sonner';
import { baseApi } from '@/app/api';

export function WishlistPriceAlertListener() {
    const { isAuthenticated, token } = useAppSelector((state) => state.auth);
    const dispatch = useAppDispatch();

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

        connection.on('CampaignStatusChanged', (payload: CampaignStatusChangedNotification) => {
            toast.info(
                `${payload.campaignName} kampanyası sona erdi`,
                {
                    description: 'Takip ettiğiniz kampanya tamamlandı. Yeni fırsatlar için ana sayfayı kontrol edin.',
                });

            dispatch(baseApi.util.invalidateTags(['Products', 'Notifications']));
        });

        void ensureWishlistConnectionStarted(connection).catch(() => {
            // Fiyat alarmı dinleyicisi arka planda sessizce yeniden denenecek.
        });

        return () => {
            connection.off('PriceAlertTriggered');
            connection.off('LowStockAlertTriggered');
            connection.off('CampaignStatusChanged');
            void connection.stop();
        };
    }, [dispatch, isAuthenticated, token]);

    return null;
}
