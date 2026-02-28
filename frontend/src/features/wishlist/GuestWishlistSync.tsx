import { useEffect, useRef } from 'react';
import { toast } from 'sonner';
import { useAppSelector } from '@/app/hooks';
import { useAddWishlistItemMutation } from './wishlistApi';
import { clearGuestWishlistProducts, getWishlistErrorMessage, replaceGuestWishlistProductIds, useGuestWishlist } from './guestWishlist';

export function GuestWishlistSync() {
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const { pendingProductIds } = useGuestWishlist();
  const [addWishlistItem] = useAddWishlistItemMutation();
  const lastSyncSignatureRef = useRef<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      lastSyncSignatureRef.current = null;
      return;
    }

    if (pendingProductIds.length === 0) {
      lastSyncSignatureRef.current = null;
      return;
    }

    const signature = pendingProductIds.join(',');
    if (lastSyncSignatureRef.current === signature) {
      return;
    }

    lastSyncSignatureRef.current = signature;

    let cancelled = false;

    const syncGuestWishlist = async () => {
      const remainingProductIds: number[] = [];
      let syncedCount = 0;

      for (const productId of pendingProductIds) {
        try {
          await addWishlistItem({ productId }).unwrap();
          syncedCount += 1;
        } catch (error) {
          if (cancelled) {
            return;
          }

          remainingProductIds.push(productId);

          if (remainingProductIds.length === pendingProductIds.length) {
            toast.error(getWishlistErrorMessage(error, 'Bekleyen favoriler senkronize edilemedi.'));
          }
        }
      }

      if (cancelled) {
        return;
      }

      if (remainingProductIds.length === 0) {
        clearGuestWishlistProducts();
        lastSyncSignatureRef.current = null;
        toast.success(`${syncedCount} ürün favorilerinize aktarıldı.`);
        return;
      }

      replaceGuestWishlistProductIds(remainingProductIds);
      lastSyncSignatureRef.current = remainingProductIds.join(',');

      if (syncedCount > 0) {
        toast.info(`${syncedCount} ürün favorilerinize aktarıldı. Kalan ürünler daha sonra yeniden denenecek.`);
      }
    };

    void syncGuestWishlist();

    return () => {
      cancelled = true;
    };
  }, [addWishlistItem, isAuthenticated, pendingProductIds]);

  return null;
}
