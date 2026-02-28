import { useEffect, useState } from 'react';

const GUEST_WISHLIST_STORAGE_KEY = 'guestWishlistProductIds';
const GUEST_WISHLIST_EVENT = 'guest-wishlist-updated';
const GUEST_WISHLIST_LIMIT = 500;

type GuestWishlistResult = {
  alreadyExists: boolean;
  limitReached: boolean;
  productIds: number[];
  wasAdded: boolean;
};

const sanitizeProductIds = (value: unknown): number[] => {
  if (!Array.isArray(value)) {
    return [];
  }

  return [...new Set(
    value
      .map((item) => Number(item))
      .filter((item) => Number.isInteger(item) && item > 0),
  )];
};

const notifyGuestWishlistChange = () => {
  window.dispatchEvent(new Event(GUEST_WISHLIST_EVENT));
};

export const readGuestWishlistProductIds = (): number[] => {
  if (typeof window === 'undefined') {
    return [];
  }

  try {
    const savedValue = window.localStorage.getItem(GUEST_WISHLIST_STORAGE_KEY);
    return sanitizeProductIds(savedValue ? JSON.parse(savedValue) : []);
  } catch {
    return [];
  }
};

export const replaceGuestWishlistProductIds = (productIds: number[]) => {
  if (typeof window === 'undefined') {
    return;
  }

  const nextProductIds = sanitizeProductIds(productIds);
  window.localStorage.setItem(GUEST_WISHLIST_STORAGE_KEY, JSON.stringify(nextProductIds));
  notifyGuestWishlistChange();
};

export const clearGuestWishlistProducts = () => {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.removeItem(GUEST_WISHLIST_STORAGE_KEY);
  notifyGuestWishlistChange();
};

export const addGuestWishlistProduct = (productId: number): GuestWishlistResult => {
  const currentProductIds = readGuestWishlistProductIds();

  if (currentProductIds.includes(productId)) {
    return {
      alreadyExists: true,
      limitReached: false,
      productIds: currentProductIds,
      wasAdded: false,
    };
  }

  if (currentProductIds.length >= GUEST_WISHLIST_LIMIT) {
    return {
      alreadyExists: false,
      limitReached: true,
      productIds: currentProductIds,
      wasAdded: false,
    };
  }

  const nextProductIds = [...currentProductIds, productId];
  replaceGuestWishlistProductIds(nextProductIds);

  return {
    alreadyExists: false,
    limitReached: false,
    productIds: nextProductIds,
    wasAdded: true,
  };
};

export const removeGuestWishlistProduct = (productId: number): number[] => {
  const nextProductIds = readGuestWishlistProductIds().filter((item) => item !== productId);
  replaceGuestWishlistProductIds(nextProductIds);
  return nextProductIds;
};

export const getWishlistErrorMessage = (error: unknown, fallbackMessage: string) => {
  const maybeError = error as { data?: { message?: string }; message?: string };
  return maybeError?.data?.message || maybeError?.message || fallbackMessage;
};

export const useGuestWishlist = () => {
  const [pendingProductIds, setPendingProductIds] = useState<number[]>(() => readGuestWishlistProductIds());

  useEffect(() => {
    const syncPendingWishlist = () => {
      setPendingProductIds(readGuestWishlistProductIds());
    };

    const handleStorage = (event: StorageEvent) => {
      if (event.key && event.key !== GUEST_WISHLIST_STORAGE_KEY) {
        return;
      }

      syncPendingWishlist();
    };

    window.addEventListener('storage', handleStorage);
    window.addEventListener(GUEST_WISHLIST_EVENT, syncPendingWishlist);

    return () => {
      window.removeEventListener('storage', handleStorage);
      window.removeEventListener(GUEST_WISHLIST_EVENT, syncPendingWishlist);
    };
  }, []);

  return {
    addProduct: (productId: number) => {
      const result = addGuestWishlistProduct(productId);
      setPendingProductIds(result.productIds);
      return result;
    },
    isPending: (productId: number) => pendingProductIds.includes(productId),
    pendingCount: pendingProductIds.length,
    pendingProductIds,
    removeProduct: (productId: number) => {
      const nextProductIds = removeGuestWishlistProduct(productId);
      setPendingProductIds(nextProductIds);
      return nextProductIds;
    },
  };
};
