import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import Home from './Home';
import { renderWithProviders } from '@/test-utils';

vi.mock('@/hooks/useHome', () => ({
  useHome: () => ({
    categories: [],
    productsData: {
      items: [],
      page: 1,
      pageSize: 12,
      totalCount: 0,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    },
    isLoading: false,
    isAddingToCart: false,
    handleAddToCart: vi.fn(),
  }),
}));

vi.mock('@/hooks/useSeoMeta', () => ({
  useSeoMeta: vi.fn(),
}));

vi.mock('@/components/home/HomeFilters', () => ({
  HomeFilters: () => <div data-testid="home-filters">Filters</div>,
}));

vi.mock('@/components/home/ProductList', () => ({
  ProductList: () => <div data-testid="product-list">Product List</div>,
}));

vi.mock('@/components/home/ActiveCampaignSpotlight', () => ({
  ActiveCampaignSpotlight: () => <div data-testid="campaign-spotlight">Campaign Spotlight</div>,
}));

describe('Home discovery hero görünürlüğü', () => {
  it('discovery akışında hero başlığını gösterir', () => {
    renderWithProviders(<Home />, { route: '/' });
    expect(screen.getByRole('heading', { name: 'Hoş Geldiniz' })).toBeInTheDocument();
  });

  it('arama aktifken hero başlığını gizler', () => {
    renderWithProviders(<Home />, { route: '/?q=telefon' });
    expect(screen.queryByRole('heading', { name: 'Hoş Geldiniz' })).not.toBeInTheDocument();
  });

  it('varsayılan dışı sıralama seçiliyken hero başlığını gizler', () => {
    renderWithProviders(<Home />, { route: '/?sort=price&order=asc' });
    expect(screen.queryByRole('heading', { name: 'Hoş Geldiniz' })).not.toBeInTheDocument();
  });
});
