import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '@/test-utils';
import ProductForm from './ProductForm';

const mockProductsApi = vi.hoisted(() => ({
  useGetProductQuery: vi.fn(),
  useCreateProductMutation: vi.fn(),
  useUpdateProductMutation: vi.fn(),
}));

const mockAdminApi = vi.hoisted(() => ({
  useGetCategoriesQuery: vi.fn(),
}));

vi.mock('@/features/products/productsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/products/productsApi')>('@/features/products/productsApi');
  return {
    ...actual,
    ...mockProductsApi,
  };
});

vi.mock('@/features/admin/adminApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/admin/adminApi')>('@/features/admin/adminApi');
  return {
    ...actual,
    ...mockAdminApi,
  };
});

vi.mock('@/components/media/ProductImageUploader', () => ({
  ProductImageUploader: ({
    canUpload,
    images,
    onChange,
  }: {
    canUpload: boolean;
    images: Array<{ id?: number; imageUrl: string; objectKey?: string; sortOrder: number; isPrimary: boolean }>;
    onChange: (images: Array<{ id?: number; imageUrl: string; objectKey?: string; sortOrder: number; isPrimary: boolean }>) => void;
  }) => (
    <div>
      <p data-testid="product-image-uploader">{canUpload ? 'upload-open' : 'upload-locked'}-{images.length}</p>
      <button
        type="button"
        onClick={() =>
          onChange([
            {
              id: 999,
              imageUrl: 'https://cdn.test/new-image.webp',
              objectKey: 'products/seller-10/product-77/new-image.webp',
              sortOrder: 4,
              isPrimary: true,
            },
          ])
        }
      >
        mock-image-change
      </button>
    </div>
  ),
}));

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

function setupBaseMocks() {
  mockAdminApi.useGetCategoriesQuery.mockReturnValue({
    data: [
      { id: 1, name: 'Elektronik' },
      { id: 2, name: 'Moda' },
    ],
    isLoading: false,
  });

  mockProductsApi.useCreateProductMutation.mockReturnValue([
    vi.fn(() => ({
      unwrap: async () => ({ id: 1 }),
    })),
    { isLoading: false },
  ]);
}

function renderCreateForm() {
  mockProductsApi.useGetProductQuery.mockReturnValue({
    data: undefined,
    isLoading: false,
  });

  const updateMutation = vi.fn(() => ({
    unwrap: async () => ({ id: 1 }),
  }));
  mockProductsApi.useUpdateProductMutation.mockReturnValue([updateMutation, { isLoading: false }]);

  renderWithProviders(
    <Routes>
      <Route path="/admin/products/new" element={<ProductForm />} />
    </Routes>,
    { route: '/admin/products/new' },
  );

  return { updateMutation };
}

function renderEditForm() {
  mockProductsApi.useGetProductQuery.mockReturnValue({
    data: {
      id: 77,
      name: 'Admin Test Ürün',
      description: 'Açıklama',
      price: 123,
      currency: 'TRY',
      sku: 'ADM-TEST-77',
      categoryId: 1,
      stockQuantity: 7,
      isActive: true,
      images: [
        {
          id: 44,
          imageUrl: 'https://cdn.test/existing.webp',
          objectKey: 'products/seller-10/product-77/existing.webp',
          sortOrder: 0,
          isPrimary: true,
        },
      ],
    },
    isLoading: false,
  });

  const updateMutation = vi.fn(() => ({
    unwrap: async () => ({ id: 77 }),
  }));
  mockProductsApi.useUpdateProductMutation.mockReturnValue([updateMutation, { isLoading: false }]);

  renderWithProviders(
    <Routes>
      <Route path="/admin/products/:id" element={<ProductForm />} />
    </Routes>,
    { route: '/admin/products/77' },
  );

  return { updateMutation };
}

describe('Admin ProductForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setupBaseMocks();
  });

  it('yeni ürün modunda uploaderı kilitli render etmeli', () => {
    renderCreateForm();
    expect(screen.getByTestId('product-image-uploader')).toHaveTextContent('upload-locked-0');
  });

  it('düzenleme modunda images alanını update payloadına map etmeli', async () => {
    const user = userEvent.setup();
    const { updateMutation } = renderEditForm();

    await waitFor(() => {
      expect(screen.getByTestId('product-image-uploader')).toHaveTextContent('upload-open-1');
    });

    await user.click(screen.getByRole('button', { name: 'mock-image-change' }));
    await user.click(screen.getByRole('button', { name: 'Güncelle' }));

    await waitFor(() => {
      expect(updateMutation).toHaveBeenCalledTimes(1);
    });

    expect(updateMutation).toHaveBeenCalledWith({
      id: 77,
      data: expect.objectContaining({
        images: [
          {
            imageUrl: 'https://cdn.test/new-image.webp',
            objectKey: 'products/seller-10/product-77/new-image.webp',
            isPrimary: true,
            sortOrder: 0,
          },
        ],
      }),
    });
  });
});
