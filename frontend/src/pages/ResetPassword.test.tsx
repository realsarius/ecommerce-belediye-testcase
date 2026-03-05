import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import ResetPassword from './ResetPassword';

const mockAuthApi = vi.hoisted(() => ({
  useResetPasswordMutation: vi.fn(),
}));

const mockToast = vi.hoisted(() => ({
  success: vi.fn(),
  error: vi.fn(),
}));

vi.mock('@/features/auth/authApi', () => ({
  useResetPasswordMutation: mockAuthApi.useResetPasswordMutation,
}));

vi.mock('sonner', () => ({
  toast: mockToast,
}));

describe('ResetPassword sayfası', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('token yoksa geçersiz link ekranı göstermeli', () => {
    mockAuthApi.useResetPasswordMutation.mockReturnValue([vi.fn(), { isLoading: false }]);

    renderWithProviders(<ResetPassword />, { route: '/reset-password' });

    expect(screen.getByText(/Geçersiz Sıfırlama Linki/i)).toBeInTheDocument();
  });

  it('şifreler eşleşmediğinde validation hatası göstermeli', async () => {
    const user = userEvent.setup();
    const resetPasswordMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({ message: 'ok' }),
    }));

    mockAuthApi.useResetPasswordMutation.mockReturnValue([
      resetPasswordMock,
      { isLoading: false },
    ]);

    renderWithProviders(<ResetPassword />, { route: '/reset-password?token=reset-token' });

    await user.type(screen.getByLabelText(/Yeni Şifre/i), 'NewPassword1');
    await user.type(screen.getByLabelText(/Şifre Tekrar/i), 'DifferentPassword1');
    await user.click(screen.getByRole('button', { name: /Şifreyi Güncelle/i }));

    expect(await screen.findByText(/Şifreler eşleşmiyor/i)).toBeInTheDocument();
    expect(resetPasswordMock).not.toHaveBeenCalled();
  });

  it('geçerli form submit edildiğinde reset mutation çağrılmalı', async () => {
    const user = userEvent.setup();
    const resetPasswordMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({
        success: true,
        message: 'Şifreniz başarıyla güncellendi',
      }),
    }));

    mockAuthApi.useResetPasswordMutation.mockReturnValue([
      resetPasswordMock,
      { isLoading: false },
    ]);

    renderWithProviders(<ResetPassword />, { route: '/reset-password?token=reset-token' });

    await user.type(screen.getByLabelText(/Yeni Şifre/i), 'NewPassword1');
    await user.type(screen.getByLabelText(/Şifre Tekrar/i), 'NewPassword1');
    await user.click(screen.getByRole('button', { name: /Şifreyi Güncelle/i }));

    await waitFor(() => {
      expect(resetPasswordMock).toHaveBeenCalledWith({
        token: 'reset-token',
        newPassword: 'NewPassword1',
        confirmPassword: 'NewPassword1',
      });
      expect(mockToast.success).toHaveBeenCalled();
    });

    expect(screen.getByText(/Şifreniz Güncellendi/i)).toBeInTheDocument();
  });
});
