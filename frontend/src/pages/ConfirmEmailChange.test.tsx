import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import ConfirmEmailChange from './ConfirmEmailChange';

const mockAuthApi = vi.hoisted(() => ({
  useConfirmEmailChangeMutation: vi.fn(),
}));

vi.mock('@/features/auth/authApi', () => ({
  useConfirmEmailChangeMutation: mockAuthApi.useConfirmEmailChangeMutation,
}));

describe('ConfirmEmailChange sayfası', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('token yoksa geçersiz link ekranı göstermeli', () => {
    const confirmEmailChangeMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({}),
    }));

    mockAuthApi.useConfirmEmailChangeMutation.mockReturnValue([
      confirmEmailChangeMock,
      {
        isLoading: false,
        isSuccess: false,
        isError: false,
        error: null,
        data: null,
      },
    ]);

    renderWithProviders(<ConfirmEmailChange />, { route: '/confirm-email-change' });

    expect(screen.getByText(/Geçersiz Onay Linki/i)).toBeInTheDocument();
    expect(confirmEmailChangeMock).not.toHaveBeenCalled();
  });

  it('başarılı onayda success ekranını göstermeli', async () => {
    const confirmEmailChangeMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({
        success: true,
        token: 'new-token',
        refreshToken: 'new-refresh-token',
        user: {
          id: 1,
          email: 'newmail@example.com',
          firstName: 'New',
          lastName: 'Mail',
          role: 'Customer',
          isEmailVerified: true,
        },
      }),
    }));

    mockAuthApi.useConfirmEmailChangeMutation.mockReturnValue([
      confirmEmailChangeMock,
      {
        isLoading: false,
        isSuccess: true,
        isError: false,
        error: null,
        data: {
          message: 'E-posta adresiniz güncellendi',
        },
      },
    ]);

    renderWithProviders(<ConfirmEmailChange />, { route: '/confirm-email-change?token=test-token' });

    expect(screen.getByText('E-posta Adresi Güncellendi')).toBeInTheDocument();
    expect(screen.getByText('E-posta adresiniz güncellendi')).toBeInTheDocument();

    await waitFor(() => {
      expect(confirmEmailChangeMock).toHaveBeenCalledWith({ token: 'test-token' });
    });
  });

  it('hatalı onayda error ekranını göstermeli', async () => {
    const confirmEmailChangeMock = vi.fn(() => ({
      unwrap: vi.fn().mockRejectedValue(new Error('invalid token')),
    }));

    mockAuthApi.useConfirmEmailChangeMutation.mockReturnValue([
      confirmEmailChangeMock,
      {
        isLoading: false,
        isSuccess: false,
        isError: true,
        error: {
          data: {
            message: 'Onay linkinin süresi dolmuş',
          },
        },
        data: null,
      },
    ]);

    renderWithProviders(<ConfirmEmailChange />, { route: '/confirm-email-change?token=expired-token' });

    expect(screen.getByText(/Geçersiz veya Süresi Dolmuş Link/i)).toBeInTheDocument();
    expect(screen.getByText(/Onay linkinin süresi dolmuş/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(confirmEmailChangeMock).toHaveBeenCalledWith({ token: 'expired-token' });
    });
  });
});
