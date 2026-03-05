import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createTestStore, renderWithProviders } from '@/test-utils';
import { EmailVerificationBanner } from './EmailVerificationBanner';

const mockAuthApi = vi.hoisted(() => ({
  useResendVerificationMutation: vi.fn(),
}));

const mockToast = vi.hoisted(() => ({
  success: vi.fn(),
  error: vi.fn(),
}));

vi.mock('@/features/auth/authApi', () => ({
  useResendVerificationMutation: mockAuthApi.useResendVerificationMutation,
}));

vi.mock('sonner', () => ({
  toast: mockToast,
}));

describe('EmailVerificationBanner', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('doğrulanmamış kullanıcı için banner görünmeli', () => {
    const resendVerificationMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({ message: 'Gönderildi' }),
    }));

    mockAuthApi.useResendVerificationMutation.mockReturnValue([
      resendVerificationMock,
      { isLoading: false },
    ]);

    const store = createTestStore({
      auth: {
        user: {
          id: 10,
          email: 'berkan@example.com',
          firstName: 'Berkan',
          lastName: 'Sözer',
          role: 'Customer',
          isEmailVerified: false,
        },
        token: 'test-token',
        refreshToken: 'test-refresh-token',
        isAuthenticated: true,
      },
    });

    renderWithProviders(<EmailVerificationBanner />, { store });

    expect(screen.getByText(/E-posta adresiniz doğrulanmamış/i)).toBeInTheDocument();
    expect(screen.getByText(/berkan@example.com/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Tekrar gönder/i })).toBeInTheDocument();
  });

  it('doğrulanmış kullanıcı için banner görünmemeli', () => {
    mockAuthApi.useResendVerificationMutation.mockReturnValue([vi.fn(), { isLoading: false }]);

    const store = createTestStore({
      auth: {
        user: {
          id: 11,
          email: 'verified@example.com',
          firstName: 'Verified',
          lastName: 'User',
          role: 'Customer',
          isEmailVerified: true,
        },
        token: 'test-token',
        refreshToken: 'test-refresh-token',
        isAuthenticated: true,
      },
    });

    renderWithProviders(<EmailVerificationBanner />, { store });

    expect(screen.queryByText(/E-posta adresiniz doğrulanmamış/i)).not.toBeInTheDocument();
  });

  it('tekrar gönder tıklandığında mutation çağrılmalı', async () => {
    const user = userEvent.setup();
    const resendVerificationMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({ message: 'Doğrulama e-postası gönderildi' }),
    }));

    mockAuthApi.useResendVerificationMutation.mockReturnValue([
      resendVerificationMock,
      { isLoading: false },
    ]);

    const store = createTestStore({
      auth: {
        user: {
          id: 12,
          email: 'retry@example.com',
          firstName: 'Retry',
          lastName: 'User',
          role: 'Customer',
          isEmailVerified: false,
        },
        token: 'test-token',
        refreshToken: 'test-refresh-token',
        isAuthenticated: true,
      },
    });

    renderWithProviders(<EmailVerificationBanner />, { store });

    await user.click(screen.getByRole('button', { name: /Tekrar gönder/i }));

    await waitFor(() => {
      expect(resendVerificationMock).toHaveBeenCalledTimes(1);
      expect(mockToast.success).toHaveBeenCalled();
    });
  });
});
