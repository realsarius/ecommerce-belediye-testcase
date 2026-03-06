import { describe, expect, it } from 'vitest';
import { isDiscoveryFeedContext, type ProductState } from './productsSlice';

const createState = (overrides: Partial<ProductState> = {}): ProductState => ({
  page: 1,
  search: '',
  categoryId: '',
  sortBy: 'createdAt',
  sortDesc: true,
  ...overrides,
});

describe('isDiscoveryFeedContext', () => {
  it('varsayılan keşif koşullarında true döner', () => {
    expect(isDiscoveryFeedContext(createState())).toBe(true);
  });

  it('boşluk içeren arama değeri keşif akışını bozmaz', () => {
    expect(isDiscoveryFeedContext(createState({ search: '   ' }))).toBe(true);
  });

  it.each([
    createState({ page: 2 }),
    createState({ search: 'telefon' }),
    createState({ categoryId: '3' }),
    createState({ sortBy: 'price' }),
    createState({ sortDesc: false }),
  ])('filtreli/sıralı akışlarda false döner', (state) => {
    expect(isDiscoveryFeedContext(state)).toBe(false);
  });

  it('"all" kategori değeri ile keşif akışını korur', () => {
    expect(isDiscoveryFeedContext(createState({ categoryId: 'all' }))).toBe(true);
  });
});
