import { screen } from '@testing-library/react';
import { describe, it, beforeEach } from 'vitest';
import { Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { createTestStore, renderWithProviders } from '@/test-utils';
import type { User } from '@/features/auth/types';

const authenticatedUser: User = {
  id: 1,
  email: 'admin@test.com',
  firstName: 'Admin',
  lastName: 'User',
  role: 'Admin',
};

function renderProtectedRoute(options?: {
  route?: string;
  requiredRole?: string;
  user?: User | null;
  isAuthenticated?: boolean;
}) {
  const {
    route = '/admin',
    requiredRole,
    user = authenticatedUser,
    isAuthenticated = true,
  } = options ?? {};

  const store = createTestStore({
    auth: {
      user,
      token: isAuthenticated ? 'test-token' : null,
      refreshToken: isAuthenticated ? 'refresh-token' : null,
      isAuthenticated,
    },
  });

  return renderWithProviders(
    <Routes>
      <Route
        path="/admin"
        element={
          <ProtectedRoute requiredRole={requiredRole}>
            <div>Korunan İçerik</div>
          </ProtectedRoute>
        }
      />
      <Route path="/login" element={<div>Login Sayfası</div>} />
      <Route path="/" element={<div>Ana Sayfa</div>} />
    </Routes>,
    { route, store },
  );
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('giriş yapılmamışsa login sayfasına yönlendirmeli', () => {
    renderProtectedRoute({
      isAuthenticated: false,
      user: null,
    });

    expect(screen.getByText('Login Sayfası')).toBeInTheDocument();
    expect(screen.queryByText('Korunan İçerik')).not.toBeInTheDocument();
  });

  it('rol gerekmiyorsa giriş yapan kullanıcıya içeriği göstermeli', () => {
    renderProtectedRoute();

    expect(screen.getByText('Korunan İçerik')).toBeInTheDocument();
  });

  it('rol uyuşmuyorsa ana sayfaya yönlendirmeli', () => {
    renderProtectedRoute({
      requiredRole: 'Seller',
    });

    expect(screen.getByText('Ana Sayfa')).toBeInTheDocument();
    expect(screen.queryByText('Korunan İçerik')).not.toBeInTheDocument();
  });

  it('rol uyuşuyorsa korunan içeriği göstermeli', () => {
    renderProtectedRoute({
      requiredRole: 'Admin',
    });

    expect(screen.getByText('Korunan İçerik')).toBeInTheDocument();
    expect(screen.queryByText('Ana Sayfa')).not.toBeInTheDocument();
  });
});
