import { fireEvent, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import VerifyEmail from './VerifyEmail';

const mockAuthApi = vi.hoisted(() => ({
  useVerifyEmailMutation: vi.fn(),
  useVerifyEmailCodeMutation: vi.fn(),
  useResendVerificationCodeMutation: vi.fn(),
}));

vi.mock('@/features/auth/authApi', () => ({
  useVerifyEmailMutation: mockAuthApi.useVerifyEmailMutation,
  useVerifyEmailCodeMutation: mockAuthApi.useVerifyEmailCodeMutation,
  useResendVerificationCodeMutation: mockAuthApi.useResendVerificationCodeMutation,
}));

describe('VerifyEmail sayfası', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockAuthApi.useVerifyEmailMutation.mockReturnValue([
      vi.fn(() => ({
        unwrap: vi.fn().mockResolvedValue({}),
      })),
      {
        isLoading: false,
        isSuccess: false,
        isError: false,
        error: null,
        data: null,
      },
    ]);

    mockAuthApi.useVerifyEmailCodeMutation.mockReturnValue([
      vi.fn(() => ({
        unwrap: vi.fn().mockResolvedValue({}),
      })),
      {
        isLoading: false,
        isSuccess: false,
        isError: false,
        error: null,
        data: null,
      },
    ]);

    mockAuthApi.useResendVerificationCodeMutation.mockReturnValue([
      vi.fn(() => ({
        unwrap: vi.fn().mockResolvedValue({ message: 'Gönderildi' }),
      })),
      {
        isLoading: false,
        isSuccess: false,
        isError: false,
        error: null,
        data: null,
      },
    ]);
  });

  it('token yoksa kod fallback ekranını göstermeli', () => {
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

    expect(screen.getByText(/Kod ile Doğrula/i)).toBeInTheDocument();
    expect(verifyEmailMock).not.toHaveBeenCalled();
  });

  it('token ile başarılı doğrulamada success ekranını göstermeli', async () => {
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
        message: 'E-posta doğrulandı',
      }),
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

    renderWithProviders(<VerifyEmail />, { route: '/verify-email?token=test-token' });

    await waitFor(() => {
      expect(verifyEmailMock).toHaveBeenCalledWith({ token: 'test-token' });
    });

    expect(await screen.findByText('E-posta Doğrulandı')).toBeInTheDocument();
    expect(screen.getByText('E-posta doğrulandı')).toBeInTheDocument();
  });

  it('hatalı token durumunda kod fallback ekranını ve hata mesajını göstermeli', async () => {
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

    await waitFor(() => {
      expect(verifyEmailMock).toHaveBeenCalledWith({ token: 'expired-token' });
    });

    expect(screen.getByText(/Kod ile Doğrula/i)).toBeInTheDocument();
    expect(screen.getByText(/Doğrulama linkinin süresi dolmuş/i)).toBeInTheDocument();
  });

  it('kod formu submit edilince verify-email-code endpointini çağırmalı', async () => {
    const verifyEmailCodeMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({
        success: true,
        token: 'token',
        refreshToken: 'refresh',
        user: {
          id: 2,
          email: 'code@example.com',
          firstName: 'Code',
          lastName: 'User',
          role: 'Customer',
          isEmailVerified: true,
        },
      }),
    }));

    mockAuthApi.useVerifyEmailCodeMutation.mockReturnValue([
      verifyEmailCodeMock,
      {
        isLoading: false,
        isSuccess: false,
        isError: false,
        error: null,
        data: null,
      },
    ]);

    renderWithProviders(<VerifyEmail />, { route: '/verify-email' });

    fireEvent.change(screen.getByLabelText(/E-posta Adresi/i), {
      target: { value: 'code@example.com' },
    });
    fireEvent.change(screen.getByLabelText(/Doğrulama Kodu/i), {
      target: { value: '123456' },
    });
    fireEvent.click(screen.getByRole('button', { name: /Kodu Doğrula/i }));

    await waitFor(() => {
      expect(verifyEmailCodeMock).toHaveBeenCalledWith({
        email: 'code@example.com',
        code: '123456',
      });
    });

    expect(await screen.findByText('E-posta Doğrulandı')).toBeInTheDocument();
  });
});
