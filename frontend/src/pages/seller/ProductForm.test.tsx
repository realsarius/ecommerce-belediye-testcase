import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '@/test-utils';
import SellerProductForm from './ProductForm';

const mockSellerApi = vi.hoisted(() => ({
  useCreateSellerProductMutation: vi.fn(),
  useGetSellerProductQuery: vi.fn(),
  useGetSellerProfileQuery: vi.fn(),
  useUpdateSellerProductMutation: vi.fn(),
}));

const mockAdminApi = vi.hoisted(() => ({
  useGetCategoriesQuery: vi.fn(),
}));

vi.mock('@/features/seller/sellerApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/seller/sellerApi')>('@/features/seller/sellerApi');
  return {
    ...actual,
    ...mockSellerApi,
  };
});

vi.mock('@/features/admin/adminApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/admin/adminApi')>('@/features/admin/adminApi');
  return {
    ...actual,
    ...mockAdminApi,
  };
});

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

function setupBaseMocks() {
  mockSellerApi.useGetSellerProfileQuery.mockReturnValue({
    data: {
      id: 45,
      storeName: 'Amber Store',
      description: 'Test',
    },
    isLoading: false,
  });

  mockAdminApi.useGetCategoriesQuery.mockReturnValue({
    data: [
      { id: 1, name: 'Giyim' },
      { id: 2, name: 'Aksesuar' },
    ],
    isLoading: false,
  });

  mockSellerApi.useCreateSellerProductMutation.mockReturnValue([
    vi.fn(() => ({
      unwrap: async () => undefined,
    })),
    { isLoading: false },
  ]);

  mockSellerApi.useUpdateSellerProductMutation.mockReturnValue([
    vi.fn(() => ({
      unwrap: async () => undefined,
    })),
    { isLoading: false },
  ]);
}

function renderCreateForm() {
  mockSellerApi.useGetSellerProductQuery.mockReturnValue({
    data: undefined,
    isLoading: false,
  });

  return renderWithProviders(
    <Routes>
      <Route path="/seller/products/new" element={<SellerProductForm />} />
    </Routes>,
    { route: '/seller/products/new' },
  );
}

function renderEditForm() {
  mockSellerApi.useGetSellerProductQuery.mockReturnValue({
    data: {
      id: 77,
      name: 'Deri Ceket',
      description: 'Kışlık ürün',
      price: 2499,
      sku: 'CEKET-777',
      categoryId: 1,
      stockQuantity: 8,
      isActive: true,
      images: [
        { id: 1, imageUrl: 'https://cdn.test/ceket-1.jpg', isPrimary: true, sortOrder: 0 },
        { id: 2, imageUrl: 'https://cdn.test/ceket-2.jpg', isPrimary: false, sortOrder: 1 },
      ],
      variants: [
        { id: 1, name: 'Beden', value: 'L', sortOrder: 0 },
        { id: 2, name: 'Renk', value: 'Siyah', sortOrder: 1 },
      ],
    },
    isLoading: false,
  });

  return renderWithProviders(
    <Routes>
      <Route path="/seller/products/:id/edit" element={<SellerProductForm />} />
    </Routes>,
    { route: '/seller/products/77/edit' },
  );
}

describe('SellerProductForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupBaseMocks();
  });

  it('oluşturma modunda görsel, varyant ve sku akışını göstermeli', async () => {
    const user = userEvent.setup();
    renderCreateForm();

    await user.type(screen.getByLabelText('Ürün Adı *'), 'Deri Ceket');
    await user.type(screen.getByLabelText('Geçici Görsel URL'), 'https://cdn.test/ceket-1.jpg');
    await user.click(screen.getByRole('button', { name: 'Ekle' }));
    await user.click(screen.getByRole('button', { name: 'Varyant Ekle' }));
    await user.type(screen.getByLabelText('Varyant Adı'), 'Beden');
    await user.type(screen.getByLabelText('Varyant Değeri'), 'L');
    await user.click(screen.getByRole('button', { name: 'SKU Oluştur' }));

    expect(screen.getByText('1 adet')).toBeInTheDocument();
    expect(screen.getByText('1 satır')).toBeInTheDocument();
    expect(screen.getByText('1/8')).toBeInTheDocument();
    expect(screen.getByText('Ana Görsel')).toBeInTheDocument();
    expect(screen.getAllByAltText('Ürün görseli')).toHaveLength(1);
    expect((screen.getByLabelText('SKU *') as HTMLInputElement).value).toMatch(/^[A-Z-]+-\d{3}$/);
  });

  it('düzenleme modunda mevcut görsel ve varyantları hydrate etmeli', () => {
    renderEditForm();

    expect(screen.getByDisplayValue('Deri Ceket')).toBeInTheDocument();
    expect(screen.getAllByAltText('Ürün görseli')).toHaveLength(2);
    expect(screen.getByDisplayValue('Beden')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Siyah')).toBeInTheDocument();
    expect(screen.getByText('2 adet')).toBeInTheDocument();
    expect(screen.getByText('2 satır')).toBeInTheDocument();
  });
});
