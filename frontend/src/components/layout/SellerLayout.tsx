import { Link, Outlet, useLocation } from 'react-router-dom';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard,
  Package,
  User,
  ArrowLeft,
  Store,
} from 'lucide-react';
import { Button } from '@/components/common/button';

const sidebarItems = [
  { href: '/seller', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/seller/products', label: 'Ürünlerim', icon: Package },
  { href: '/seller/profile', label: 'Marka Profili', icon: User },
];

export function SellerLayout() {
  const location = useLocation();

  return (
    <div className="min-h-screen flex">
      {/* Sidebar */}
      <aside className="w-64 border-r bg-gradient-to-b from-amber-50/50 to-background dark:from-amber-950/20 hidden md:block">
        <div className="p-4 border-b">
          <div className="flex items-center gap-2">
            <Store className="h-5 w-5 text-amber-600" />
            <h2 className="text-lg font-semibold">Satıcı Paneli</h2>
          </div>
        </div>
        <nav className="p-4 space-y-2">
          {sidebarItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.href;
            return (
              <Link
                key={item.href}
                to={item.href}
                className={cn(
                  'flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors',
                  isActive
                    ? 'bg-amber-500 text-white'
                    : 'hover:bg-amber-100 dark:hover:bg-amber-900/30'
                )}
              >
                <Icon className="h-5 w-5" />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>
        <div className="p-4 mt-auto border-t">
          <Button variant="ghost" asChild className="w-full justify-start">
            <Link to="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Mağazaya Dön
            </Link>
          </Button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1">
        {/* Mobile header */}
        <header className="md:hidden border-b p-4 flex items-center justify-between bg-amber-50 dark:bg-amber-950/20">
          <div className="flex items-center gap-2">
            <Store className="h-5 w-5 text-amber-600" />
            <h2 className="font-semibold">Satıcı Paneli</h2>
          </div>
          <Button variant="ghost" size="sm" asChild>
            <Link to="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Mağaza
            </Link>
          </Button>
        </header>

        {/* Content */}
        <main className="p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
