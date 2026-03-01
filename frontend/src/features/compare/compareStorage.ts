import { useEffect, useState } from 'react';

const COMPARE_STORAGE_KEY = 'product_compare_ids';
const COMPARE_UPDATED_EVENT = 'compare:updated';
const MAX_COMPARE_ITEMS = 4;

type CompareResult = {
  ids: number[];
  added: boolean;
  removed: boolean;
  alreadyExists: boolean;
  limitReached: boolean;
};

function sanitizeCompareIds(ids: number[]) {
  return Array.from(new Set(ids.filter((id) => Number.isInteger(id) && id > 0))).slice(0, MAX_COMPARE_ITEMS);
}

function notifyCompareUpdated() {
  if (typeof window === 'undefined') {
    return;
  }

  window.dispatchEvent(new Event(COMPARE_UPDATED_EVENT));
}

function writeCompareIds(ids: number[]) {
  if (typeof window === 'undefined') {
    return;
  }

  const nextIds = sanitizeCompareIds(ids);
  window.localStorage.setItem(COMPARE_STORAGE_KEY, JSON.stringify(nextIds));
  notifyCompareUpdated();
}

export function readCompareIds() {
  if (typeof window === 'undefined') {
    return [] as number[];
  }

  try {
    const raw = window.localStorage.getItem(COMPARE_STORAGE_KEY);
    if (!raw) {
      return [] as number[];
    }

    return sanitizeCompareIds(JSON.parse(raw) as number[]);
  } catch {
    return [] as number[];
  }
}

export function addCompareProduct(productId: number): CompareResult {
  const currentIds = readCompareIds();

  if (currentIds.includes(productId)) {
    return {
      ids: currentIds,
      added: false,
      removed: false,
      alreadyExists: true,
      limitReached: false,
    };
  }

  if (currentIds.length >= MAX_COMPARE_ITEMS) {
    return {
      ids: currentIds,
      added: false,
      removed: false,
      alreadyExists: false,
      limitReached: true,
    };
  }

  const nextIds = [...currentIds, productId];
  writeCompareIds(nextIds);

  return {
    ids: nextIds,
    added: true,
    removed: false,
    alreadyExists: false,
    limitReached: false,
  };
}

export function removeCompareProduct(productId: number) {
  const nextIds = readCompareIds().filter((id) => id !== productId);
  writeCompareIds(nextIds);
  return nextIds;
}

export function replaceCompareProducts(productIds: number[]) {
  const nextIds = sanitizeCompareIds(productIds);
  writeCompareIds(nextIds);
  return nextIds;
}

export function clearCompareProducts() {
  writeCompareIds([]);
}

export function buildCompareUrl(productIds: number[] = readCompareIds()) {
  const ids = sanitizeCompareIds(productIds);
  return ids.length > 0 ? `/compare?ids=${ids.join(',')}` : '/compare';
}

export function parseCompareIds(value: string | null) {
  if (!value) {
    return [] as number[];
  }

  return sanitizeCompareIds(
    value
      .split(',')
      .map((part) => Number.parseInt(part, 10))
      .filter((id) => Number.isInteger(id))
  );
}

export function useProductCompare() {
  const [compareIds, setCompareIds] = useState(() => readCompareIds());

  useEffect(() => {
    const syncCompareIds = () => setCompareIds(readCompareIds());
    const handleStorage = (event: StorageEvent) => {
      if (event.key && event.key !== COMPARE_STORAGE_KEY) {
        return;
      }
      syncCompareIds();
    };

    window.addEventListener('storage', handleStorage);
    window.addEventListener(COMPARE_UPDATED_EVENT, syncCompareIds);

    return () => {
      window.removeEventListener('storage', handleStorage);
      window.removeEventListener(COMPARE_UPDATED_EVENT, syncCompareIds);
    };
  }, []);

  return {
    compareIds,
    compareCount: compareIds.length,
    maxCompareItems: MAX_COMPARE_ITEMS,
    containsProduct: (productId: number) => compareIds.includes(productId),
    addProduct: (productId: number) => addCompareProduct(productId),
    removeProduct: (productId: number) => removeCompareProduct(productId),
    clearProducts: () => clearCompareProducts(),
    replaceProducts: (productIds: number[]) => replaceCompareProducts(productIds),
  };
}
