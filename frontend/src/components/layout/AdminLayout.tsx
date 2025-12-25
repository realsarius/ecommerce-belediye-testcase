import { Link, Outlet, useLocation } from 'react-router-dom';
import { useState } from 'react';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard,
  Package,
  FolderTree,
  ShoppingBag,
  Ticket,
  ArrowLeft,
  Menu,
} from 'lucide-react';
import { Button } from '@/components/common/button';
import { usePageTitle } from '@/hooks/usePageTitle';
import {
  Sheet,
  SheetContent,
  SheetTrigger,
  SheetTitle,
  SheetDescription,
} from '@/components/common/sheet';

const sidebarItems = [
  { href: '/admin', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/admin/products', label: 'Ürünler', icon: Package },
  { href: '/admin/categories', label: 'Kategoriler', icon: FolderTree },
  { href: '/admin/orders', label: 'Siparişler', icon: ShoppingBag },
  { href: '/admin/coupons', label: 'Kuponlar', icon: Ticket },
];

export function AdminLayout() {
  usePageTitle();
  const location = useLocation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const NavItems = () => (
    <>
      {sidebarItems.map((item) => {
        const Icon = item.icon;
        const isActive = location.pathname === item.href;
        return (
          <Link
            key={item.href}
            to={item.href}
            onClick={() => setMobileMenuOpen(false)}
            className={cn(
              'flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors',
              isActive
                ? 'bg-primary text-primary-foreground'
                : 'hover:bg-muted'
            )}
          >
            <Icon className="h-5 w-5" />
            <span>{item.label}</span>
          </Link>
        );
      })}
    </>
  );

  return (
    <div className="min-h-screen flex">
      {/* Desktop Sidebar */}
      <aside className="w-64 border-r bg-muted/30 hidden md:block">
        <div className="p-4 border-b">
          <h2 className="text-lg font-semibold">Admin Panel</h2>
        </div>
        <nav className="p-4 space-y-2">
          <NavItems />
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
        <header className="md:hidden border-b p-4 flex items-center justify-between sticky top-0 bg-background z-40">
          <div className="flex items-center gap-3">
            <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
              <SheetTrigger asChild>
                <Button variant="ghost" size="icon">
                  <Menu className="h-5 w-5" />
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="w-64 p-0">
                <SheetTitle className="sr-only">Admin Panel Menüsü</SheetTitle>
                <SheetDescription className="sr-only">
                  Admin panel navigasyon menüsü
                </SheetDescription>
                <div className="p-4 border-b">
                  <h2 className="text-lg font-semibold">Admin Panel</h2>
                </div>
                <nav className="p-4 space-y-2">
                  <NavItems />
                </nav>
                <div className="p-4 border-t mt-auto">
                  <Button 
                    variant="ghost" 
                    asChild 
                    className="w-full justify-start"
                    onClick={() => setMobileMenuOpen(false)}
                  >
                    <Link to="/">
                      <ArrowLeft className="mr-2 h-4 w-4" />
                      Mağazaya Dön
                    </Link>
                  </Button>
                </div>
              </SheetContent>
            </Sheet>
            <h2 className="font-semibold">Admin Panel</h2>
          </div>
          <Button variant="ghost" size="sm" asChild>
            <Link to="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Mağaza
            </Link>
          </Button>
        </header>

        {/* Content */}
        <main className="p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
