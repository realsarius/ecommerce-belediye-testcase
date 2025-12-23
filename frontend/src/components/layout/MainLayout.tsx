import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { CategoryNav } from './CategoryNav';
import { Footer } from './Footer';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { useGetMeQuery } from '@/features/auth/authApi';
import { setUser } from '@/features/auth/authSlice';
import { useEffect } from 'react';

export function MainLayout() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  
  const { data } = useGetMeQuery(undefined, { 
    skip: !isAuthenticated || !!user 
  });

  useEffect(() => {
    if (data?.user) {
      dispatch(setUser(data.user));
    }
  }, [data, dispatch]);

  return (
    <div className="min-h-screen flex flex-col">
      <Header />
      <CategoryNav />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}
