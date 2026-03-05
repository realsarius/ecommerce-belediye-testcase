import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderWithProviders } from '@/test-utils';
import ForgotPassword from './ForgotPassword';

const mockAuthApi = vi.hoisted(() => ({
  useForgotPasswordMutation: vi.fn(),
}));

const mockToast = vi.hoisted(() => ({
  success: vi.fn(),
  error: vi.fn(),
}));

vi.mock('@/features/auth/authApi', () => ({
  useForgotPasswordMutation: mockAuthApi.useForgotPasswordMutation,
}));

vi.mock('sonner', () => ({
  toast: mockToast,
}));

describe('ForgotPassword sayfası', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('geçersiz e-posta girildiğinde doğrulama hatası göstermeli', async () => {
    const user = userEvent.setup();
    const forgotPasswordMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({ message: 'ok' }),
    }));

    mockAuthApi.useForgotPasswordMutation.mockReturnValue([
      forgotPasswordMock,
      { isLoading: false },
    ]);

    renderWithProviders(<ForgotPassword />, { route: '/forgot-password' });

    await user.click(screen.getByRole('button', { name: /Sıfırlama Linki Gönder/i }));

    expect(await screen.findByText(/Geçerli bir e-posta adresi girin/i)).toBeInTheDocument();
    expect(forgotPasswordMock).not.toHaveBeenCalled();
  });

  it('geçerli form submit edildiğinde başarı mesajını göstermeli', async () => {
    const user = userEvent.setup();
    const forgotPasswordMock = vi.fn(() => ({
      unwrap: vi.fn().mockResolvedValue({
        success: true,
        message: 'Şifre sıfırlama linki gönderildi',
      }),
    }));

    mockAuthApi.useForgotPasswordMutation.mockReturnValue([
      forgotPasswordMock,
      { isLoading: false },
    ]);

    renderWithProviders(<ForgotPassword />, { route: '/forgot-password' });

    await user.type(screen.getByLabelText(/E-posta Adresi/i), 'user@example.com');
    await user.click(screen.getByRole('button', { name: /Sıfırlama Linki Gönder/i }));

    await waitFor(() => {
      expect(forgotPasswordMock).toHaveBeenCalledWith({ email: 'user@example.com' });
      expect(mockToast.success).toHaveBeenCalled();
    });

    expect(
      screen.getByText(/E-posta adresinize sıfırlama linki gönderildi/i)
    ).toBeInTheDocument();
  });
});
