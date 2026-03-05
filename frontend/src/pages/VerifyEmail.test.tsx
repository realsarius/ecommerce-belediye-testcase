import { screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import VerifyEmail from './VerifyEmail';

const mockAuthApi = vi.hoisted(() => ({
  useVerifyEmailMutation: vi.fn(),
}));

vi.mock('@/features/auth/authApi', () => ({
  useVerifyEmailMutation: mockAuthApi.useVerifyEmailMutation,
}));

describe('VerifyEmail sayfası', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('token yoksa geçersiz link ekranı göstermeli', () => {
    const verifyEmailMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({}),
    }));

    mockAuthApi.useVerifyEmailMutation.mockReturnValue([
      verifyEmailMock,
      {
        isLoading: false,
        isSuccess: false,
        isError: false,
        error: null,
        data: null,
      },
    ]);

    renderWithProviders(<VerifyEmail />, { route: '/verify-email' });

    expect(screen.getByText(/Geçersiz Doğrulama Linki/i)).toBeInTheDocument();
    expect(verifyEmailMock).not.toHaveBeenCalled();
  });

  it('başarılı doğrulamada success ekranını göstermeli', async () => {
    const verifyEmailMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({
        success: true,
        token: 'new-token',
        refreshToken: 'new-refresh-token',
        user: {
          id: 1,
          email: 'verified@example.com',
          firstName: 'Verified',
          lastName: 'User',
          role: 'Customer',
          isEmailVerified: true,
        },
      }),
    }));

    mockAuthApi.useVerifyEmailMutation.mockReturnValue([
      verifyEmailMock,
      {
        isLoading: false,
        isSuccess: true,
        isError: false,
        error: null,
        data: {
          message: 'E-posta doğrulandı',
        },
      },
    ]);

    renderWithProviders(<VerifyEmail />, { route: '/verify-email?token=test-token' });

    expect(screen.getByText('E-posta Doğrulandı')).toBeInTheDocument();
    expect(screen.getByText('E-posta doğrulandı')).toBeInTheDocument();

    await waitFor(() => {
      expect(verifyEmailMock).toHaveBeenCalledWith({ token: 'test-token' });
    });
  });

  it('hatalı doğrulamada error ekranını göstermeli', async () => {
    const verifyEmailMock = vi.fn(() => ({
      unwrap: vi.fn().mockRejectedValue(new Error('invalid token')),
    }));

    mockAuthApi.useVerifyEmailMutation.mockReturnValue([
      verifyEmailMock,
      {
        isLoading: false,
        isSuccess: false,
        isError: true,
        error: {
          data: {
            message: 'Doğrulama linkinin süresi dolmuş',
          },
        },
        data: null,
      },
    ]);

    renderWithProviders(<VerifyEmail />, { route: '/verify-email?token=expired-token' });

    expect(screen.getByText(/Geçersiz veya Süresi Dolmuş Link/i)).toBeInTheDocument();
    expect(screen.getByText(/Doğrulama linkinin süresi dolmuş/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(verifyEmailMock).toHaveBeenCalledWith({ token: 'expired-token' });
    });
  });
});
