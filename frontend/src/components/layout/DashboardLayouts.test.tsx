import { screen } from '@testing-library/react';
import { describe, it, beforeEach, vi } from 'vitest';
import { Route, Routes } from 'react-router-dom';
import { AdminLayout } from './AdminLayout';
import { SellerLayout } from './SellerLayout';
import { createTestStore, renderWithProviders } from '@/test-utils';
import type { User } from '@/features/auth/types';

vi.mock('@/hooks/usePageTitle', () => ({
  usePageTitle: vi.fn(),
}));

vi.mock('@/features/notifications/notificationsApi', () => ({
  useGetUnreadNotificationCountQuery: vi.fn(() => ({
    data: { unreadCount: 3 },
  })),
}));

vi.mock('@/features/auth/authApi', () => ({
  useRevokeMutation: vi.fn(() => [
    vi.fn(() => ({
      unwrap: async () => undefined,
    })),
  ]),
}));

vi.mock('@/features/seller/sellerApi', () => ({
  useGetSellerProfileQuery: vi.fn(() => ({
    data: {
      brandName: 'Test Mağaza',
    },
  })),
}));

const adminUser: User = {
  id: 1,
  email: 'admin@test.com',
  firstName: 'Admin',
  lastName: 'User',
  role: 'Admin',
};

const sellerUser: User = {
  id: 2,
  email: 'seller@test.com',
  firstName: 'Seller',
  lastName: 'User',
  role: 'Seller',
};

function createAuthStore(user: User) {
  return createTestStore({
    auth: {
      user,
      token: 'test-token',
      refreshToken: 'refresh-token',
      isAuthenticated: true,
    },
  });
}

describe('Dashboard Layout render testleri', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('AdminLayout çökmeden render edilmeli ve child route görünmeli', () => {
    renderWithProviders(
      <Routes>
        <Route path="/admin" element={<AdminLayout />}>
          <Route path="dashboard" element={<div>Admin İçerik</div>} />
        </Route>
      </Routes>,
      {
        route: '/admin/dashboard',
        store: createAuthStore(adminUser),
      },
    );

    expect(screen.getAllByText('Operasyon Merkezi')[0]).toBeInTheDocument();
    expect(screen.getByText('Admin İçerik')).toBeInTheDocument();
    expect(screen.getAllByText('Dashboard')[0]).toBeInTheDocument();
  });

  it('SellerLayout çökmeden render edilmeli ve mağaza bilgisi görünmeli', () => {
    renderWithProviders(
      <Routes>
        <Route path="/seller" element={<SellerLayout />}>
          <Route path="dashboard" element={<div>Seller İçerik</div>} />
        </Route>
      </Routes>,
      {
        route: '/seller/dashboard',
        store: createAuthStore(sellerUser),
      },
    );

    expect(screen.getAllByText('Test Mağaza')[0]).toBeInTheDocument();
    expect(screen.getByText('Seller İçerik')).toBeInTheDocument();
    expect(screen.getAllByText('Dashboard')[0]).toBeInTheDocument();
  });
});
