import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { CategoryNav } from './CategoryNav';
import { Footer } from './Footer';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { useGetMeQuery } from '@/features/auth/authApi';
import { setUser } from '@/features/auth/authSlice';
import { GuestWishlistSync, WishlistPriceAlertListener } from '@/features/wishlist';
import { useEffect } from 'react';
import { usePageTitle } from '@/hooks/usePageTitle';

export function MainLayout() {
  usePageTitle();
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  
  const { data } = useGetMeQuery(undefined, { 
    skip: !isAuthenticated || !!user 
  });

  useEffect(() => {
    if (data) {
      dispatch(setUser(data));
    }
  }, [data, dispatch]);

  return (
    <div className="min-h-screen flex flex-col">
      <GuestWishlistSync />
      <WishlistPriceAlertListener />
      <Header />
      <CategoryNav />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}
