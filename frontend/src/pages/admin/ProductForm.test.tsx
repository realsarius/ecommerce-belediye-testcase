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
  useGetAdminSellersQuery: vi.fn(),
}));

const mockSettingsApi = vi.hoisted(() => ({
  useGetFrontendFeaturesQuery: vi.fn(),
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

vi.mock('@/features/settings/settingsApi', async () => {
  const actual = await vi.importActual<typeof import('@/features/settings/settingsApi')>('@/features/settings/settingsApi');
  return {
    ...actual,
    ...mockSettingsApi,
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
  mockAdminApi.useGetAdminSellersQuery.mockReturnValue({
    data: [
      {
        id: 21,
        userId: 1001,
        brandName: 'Test Seller',
        isPlatformAccount: false,
        sellerFirstName: 'Test',
        sellerLastName: 'Seller',
        ownerEmail: 'seller@test.local',
        status: 'Active',
        productCount: 0,
        activeProductCount: 0,
        totalStock: 0,
        totalSales: 0,
        averageRating: 0,
        commissionRate: 10,
        hasCommissionOverride: false,
        createdAt: '2026-03-01T00:00:00Z',
        isVerified: true,
      },
    ],
    isLoading: false,
  });

  mockProductsApi.useCreateProductMutation.mockReturnValue([
    vi.fn(() => ({
      unwrap: async () => ({ id: 1 }),
    })),
    { isLoading: false },
  ]);

  mockSettingsApi.useGetFrontendFeaturesQuery.mockReturnValue({
    data: {
      enableCheckoutLegalConsents: true,
      enableCheckoutInvoiceInfo: true,
        enableShipmentTimeline: true,
        enableReturnAttachments: true,
        enableAdminProductImageUploader: true,
        enableAdminProductSellerPicker: false,
      },
      isLoading: false,
  });
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
    expect(screen.getByText('Bu panelden oluşturulan ürünler otomatik olarak Platform Seller hesabına atanır')).toBeInTheDocument();
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

  it('admin image uploader feature flag kapaliyken uploader render etmemeli', () => {
    mockSettingsApi.useGetFrontendFeaturesQuery.mockReturnValue({
      data: {
        enableCheckoutLegalConsents: true,
        enableCheckoutInvoiceInfo: true,
        enableShipmentTimeline: true,
        enableReturnAttachments: true,
        enableAdminProductImageUploader: false,
        enableAdminProductSellerPicker: false,
      },
      isLoading: false,
    });

    renderCreateForm();

    expect(screen.queryByTestId('product-image-uploader')).not.toBeInTheDocument();
    expect(screen.getByText('Admin ürün görsel yükleme özelliği geçici olarak kapalı')).toBeInTheDocument();
  });

  it('seller picker aktifken satıcı atama alanını göstermeli ve aktif seller listesini istemeli', () => {
    mockSettingsApi.useGetFrontendFeaturesQuery.mockReturnValue({
      data: {
        enableCheckoutLegalConsents: true,
        enableCheckoutInvoiceInfo: true,
        enableShipmentTimeline: true,
        enableReturnAttachments: true,
        enableAdminProductImageUploader: true,
        enableAdminProductSellerPicker: true,
      },
      isLoading: false,
    });

    renderCreateForm();
    expect(screen.getByText('Satıcı Ataması')).toBeInTheDocument();
    expect(screen.getByText('Satıcı seçmezseniz ürün Platform Seller hesabı altında oluşturulur')).toBeInTheDocument();
    expect(mockAdminApi.useGetAdminSellersQuery).toHaveBeenCalledWith(
      { status: 'active' },
      { skip: false },
    );
  });
});
