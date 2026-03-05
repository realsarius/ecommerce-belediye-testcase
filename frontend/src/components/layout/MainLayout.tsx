import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { EmailVerificationBanner } from './EmailVerificationBanner';
import { CategoryNav } from './CategoryNav';
import { Footer } from './Footer';
import { ConsentBanner } from '@/components/ui/ConsentBanner';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { useGetMeQuery } from '@/features/auth/authApi';
import { setUser } from '@/features/auth/authSlice';
import { GuestWishlistSync, WishlistPriceAlertListener } from '@/features/wishlist';
import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { useSeoMeta } from '@/hooks/useSeoMeta';

export function MainLayout() {
  const location = useLocation();
  useSeoMeta({
    canonicalPath: location.pathname,
  });
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  const shouldFetchCurrentUser = !user || typeof user.isEmailVerified !== 'boolean';
  
  const { data } = useGetMeQuery(undefined, { 
    skip: !isAuthenticated || !shouldFetchCurrentUser 
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
      <EmailVerificationBanner />
      <CategoryNav />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
      <ConsentBanner />
    </div>
  );
}
