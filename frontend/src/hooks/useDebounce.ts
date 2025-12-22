import { useState, useEffect } from 'react';

/**
 * useDebounce Hook
 * 
 * Bir değerin değişikliklerini belirli bir süre geciktirerek döndürür.
 * API isteklerini optimize etmek için kullanılır.
 * 
 * @param value - Debounce edilecek değer
 * @param delay - Gecikme süresi (ms), varsayılan 300ms
 * @returns Debounce edilmiş değer
 * 
 * @example
 * const [search, setSearch] = useState('');
 * const debouncedSearch = useDebounce(search, 500);
 * 
 * useGetProductsQuery({ search: debouncedSearch });
 */
export function useDebounce<T>(value: T, delay: number = 300): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(timer);
    };
  }, [value, delay]);

  return debouncedValue;
}
