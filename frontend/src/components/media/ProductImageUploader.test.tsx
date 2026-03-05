import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ProductImageUploader, type ProductImageFormValue } from './ProductImageUploader';

const mockMediaApi = vi.hoisted(() => ({
  usePresignMediaUploadMutation: vi.fn(),
  useConfirmMediaUploadMutation: vi.fn(),
  useDeleteMediaImageMutation: vi.fn(),
  useReorderProductImagesMutation: vi.fn(),
}));

const mockUpload = vi.hoisted(() => ({
  uploadToPresignedUrl: vi.fn(),
}));

const mockToast = vi.hoisted(() => ({
  success: vi.fn(),
  error: vi.fn(),
  info: vi.fn(),
}));

vi.mock('@/features/media/mediaApi', () => ({
  usePresignMediaUploadMutation: mockMediaApi.usePresignMediaUploadMutation,
  useConfirmMediaUploadMutation: mockMediaApi.useConfirmMediaUploadMutation,
  useDeleteMediaImageMutation: mockMediaApi.useDeleteMediaImageMutation,
  useReorderProductImagesMutation: mockMediaApi.useReorderProductImagesMutation,
}));

vi.mock('@/lib/uploadToPresignedUrl', () => ({
  uploadToPresignedUrl: mockUpload.uploadToPresignedUrl,
}));

vi.mock('sonner', () => ({
  toast: mockToast,
}));

function buildMutation<TArg, TResult>(impl: (arg: TArg) => Promise<TResult>) {
  return vi.fn((arg: TArg) => ({
    unwrap: () => impl(arg),
  }));
}

type SetupMutationsOptions = {
  presignImpl?: (arg: unknown) => Promise<{ uploadUrl: string; publicUrl: string; objectKey: string }>;
  confirmImpl?: (arg: unknown) => Promise<{ imageId: number; imageUrl: string; objectKey: string; isPrimary: boolean; sortOrder: number }>;
  deleteImpl?: (imageId: number) => Promise<void>;
  reorderImpl?: (arg: unknown) => Promise<void>;
};

function setupMutations(options: SetupMutationsOptions = {}) {
  const presignMutation = buildMutation(
    options.presignImpl ??
      (async () => ({
        uploadUrl: 'https://upload.example.com/signed',
        publicUrl: 'https://img.example.com/products/p-1.webp',
        objectKey: 'products/seller-45/product-1001/p-1.webp',
      })),
  );

  const confirmMutation = buildMutation(
    options.confirmImpl ??
      (async () => ({
        imageId: 101,
        imageUrl: 'https://img.example.com/products/p-1.webp',
        objectKey: 'products/seller-45/product-1001/p-1.webp',
        isPrimary: true,
        sortOrder: 0,
      })),
  );

  const deleteMutation = buildMutation(options.deleteImpl ?? (async () => undefined));
  const reorderMutation = buildMutation(options.reorderImpl ?? (async () => undefined));

  mockMediaApi.usePresignMediaUploadMutation.mockReturnValue([presignMutation]);
  mockMediaApi.useConfirmMediaUploadMutation.mockReturnValue([confirmMutation]);
  mockMediaApi.useDeleteMediaImageMutation.mockReturnValue([deleteMutation]);
  mockMediaApi.useReorderProductImagesMutation.mockReturnValue([reorderMutation]);

  return { presignMutation, confirmMutation, deleteMutation, reorderMutation };
}

describe('ProductImageUploader', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUpload.uploadToPresignedUrl.mockResolvedValue(undefined);
  });

  it('yükleme sırasında progress göstermeli ve başarılı yüklemede listeyi güncellemeli', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    const { presignMutation, confirmMutation } = setupMutations();
    mockUpload.uploadToPresignedUrl.mockImplementation(async (_url, _file, onProgress) => {
      onProgress?.(35);
      onProgress?.(100);
    });

    const { container } = render(
      <ProductImageUploader
        productId={1001}
        canUpload
        images={[]}
        onChange={onChange}
      />,
    );

    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['binary-data'], 'urun.webp', { type: 'image/webp' });
    await user.upload(fileInput, file);

    await waitFor(() => {
      expect(presignMutation).toHaveBeenCalledTimes(1);
      expect(confirmMutation).toHaveBeenCalledTimes(1);
      expect(onChange).toHaveBeenLastCalledWith([
        expect.objectContaining({
          id: 101,
          imageUrl: 'https://img.example.com/products/p-1.webp',
          objectKey: 'products/seller-45/product-1001/p-1.webp',
          isPrimary: true,
          sortOrder: 0,
        }),
      ]);
      expect(mockToast.success).toHaveBeenCalledWith('1 görsel yüklendi');
    });

    expect(screen.getByText(/%100/i)).toBeInTheDocument();
  });

  it('başarısız upload sonrası tekrar dene ile aynı dosyayı yükleyebilmeli', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    setupMutations();
    mockUpload.uploadToPresignedUrl
      .mockRejectedValueOnce(new Error('network-error'))
      .mockImplementationOnce(async (_url, _file, onProgress) => {
        onProgress?.(100);
      });

    const { container } = render(
      <ProductImageUploader
        productId={1001}
        canUpload
        images={[]}
        onChange={onChange}
      />,
    );

    const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['binary-data'], 'hata-sonrasi.webp', { type: 'image/webp' });
    await user.upload(fileInput, file);

    await waitFor(() => {
      expect(screen.getByText('Yüklenemeyen dosyalar')).toBeInTheDocument();
      expect(mockToast.error).toHaveBeenCalledWith('1 görsel yüklenemedi, tekrar deneyebilirsiniz');
    });

    await user.click(screen.getByRole('button', { name: 'Tekrar Dene' }));

    await waitFor(() => {
      expect(onChange).toHaveBeenLastCalledWith([
        expect.objectContaining({
          id: 101,
          imageUrl: 'https://img.example.com/products/p-1.webp',
          isPrimary: true,
          sortOrder: 0,
        }),
      ]);
      expect(mockToast.success).toHaveBeenCalledWith('Görsel tekrar yükleme ile eklendi');
    });
  });

  it('ana görsel atamada primary bilgisini güncelleyip reorder mutation göndermeli', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    const { reorderMutation } = setupMutations();
    const images: ProductImageFormValue[] = [
      { id: 1, imageUrl: 'https://img.example.com/1.webp', objectKey: 'k1', sortOrder: 0, isPrimary: true },
      { id: 2, imageUrl: 'https://img.example.com/2.webp', objectKey: 'k2', sortOrder: 1, isPrimary: false },
    ];

    render(
      <ProductImageUploader
        productId={1001}
        canUpload
        images={images}
        onChange={onChange}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Ana Yap' }));

    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith([
        expect.objectContaining({ id: 1, isPrimary: false, sortOrder: 0 }),
        expect.objectContaining({ id: 2, isPrimary: true, sortOrder: 1 }),
      ]);
      expect(reorderMutation).toHaveBeenCalledWith({
        productId: 1001,
        data: {
          imageOrders: [
            { imageId: 1, displayOrder: 0, isPrimary: false },
            { imageId: 2, displayOrder: 1, isPrimary: true },
          ],
        },
      });
    });
  });

  it('primary görsel silinince kalan görseli primary yapıp reorder göndermeli', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    const { deleteMutation, reorderMutation } = setupMutations();
    const images: ProductImageFormValue[] = [
      { id: 1, imageUrl: 'https://img.example.com/1.webp', objectKey: 'k1', sortOrder: 0, isPrimary: true },
      { id: 2, imageUrl: 'https://img.example.com/2.webp', objectKey: 'k2', sortOrder: 5, isPrimary: false },
    ];

    render(
      <ProductImageUploader
        productId={1001}
        canUpload
        images={images}
        onChange={onChange}
      />,
    );

    const deleteButtons = screen.getAllByRole('button', { name: 'Görseli sil' });
    await user.click(deleteButtons[0]);

    await waitFor(() => {
      expect(deleteMutation).toHaveBeenCalledWith(1);
      expect(onChange).toHaveBeenCalledWith([
        expect.objectContaining({ id: 2, isPrimary: true, sortOrder: 0 }),
      ]);
      expect(reorderMutation).toHaveBeenCalledWith({
        productId: 1001,
        data: {
          imageOrders: [
            { imageId: 2, displayOrder: 0, isPrimary: true },
          ],
        },
      });
    });
  });
});
