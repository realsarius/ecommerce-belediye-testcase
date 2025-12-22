import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { ShoppingCart, User, LogOut, Menu, Package, Wrench, CreditCard, Users } from 'lucide-react';
import { Button } from '@/components/common/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/common/dropdown-menu';
import { Sheet, SheetContent, SheetTrigger } from '@/components/common/sheet';
import { Badge } from '@/components/common/badge';
import { useAppSelector, useAppDispatch } from '@/app/hooks';
import { logout } from '@/features/auth/authSlice';
import { useGetCartQuery } from '@/features/cart/cartApi';
import { ThemeToggle } from '@/components/common/ThemeToggle';
import { CartDrawer } from '@/components/common/CartDrawer';
import { useDevTools } from '@/components/common/DevToolsProvider';
import { TestCardsDialog } from '@/components/common/TestCardsDialog';
import { TestUsersDialog } from '@/components/common/TestUsersDialog';

export function Header() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { data: cart } = useGetCartQuery(undefined, { skip: !isAuthenticated });
  const { isDevToolsEnabled } = useDevTools();
  const [showTestCards, setShowTestCards] = useState(false);
  const [showTestUsers, setShowTestUsers] = useState(false);

  const cartItemCount = cart?.items.reduce((sum, item) => sum + item.quantity, 0) || 0;

  const handleLogout = () => {
    dispatch(logout());
    navigate('/');
  };

  return (
    <>
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container mx-auto flex h-16 items-center justify-between px-4">
        {/* Logo */}
        <Link to="/" className="flex items-center space-x-2">
          <Package className="h-6 w-6 text-primary" />
          <span className="text-xl font-bold">E-Ticaret</span>
        </Link>

        {/* Desktop Navigation */}
        <nav className="hidden md:flex items-center space-x-6">
          <Link to="/" className="text-sm font-medium hover:text-primary transition-colors">
            Ürünler
          </Link>
          {isAuthenticated && (
            <Link to="/orders" className="text-sm font-medium hover:text-primary transition-colors">
              Siparişlerim
            </Link>
          )}
          {user?.role === 'Admin' && (
            <Link to="/admin" className="text-sm font-medium hover:text-primary transition-colors">
              Admin Panel
            </Link>
          )}
        </nav>

        {/* Right Side */}
        <div className="flex items-center space-x-4">
          {/* Cart */}
          {isAuthenticated && (
            <CartDrawer>
              <Button variant="ghost" size="icon" className="relative">
                <ShoppingCart className="h-5 w-5" />
                {cartItemCount > 0 && (
                  <Badge
                    variant="destructive"
                    className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 text-xs"
                  >
                    {cartItemCount}
                  </Badge>
                )}
              </Button>
          </CartDrawer>
          )}

          {/* Dev Tools Dropdown */}
          {isDevToolsEnabled && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="gap-2">
                  <Wrench className="h-4 w-4" />
                  <span className="hidden sm:inline">Dev Tools</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => setShowTestCards(true)}>
                  <CreditCard className="mr-2 h-4 w-4" />
                  Test Kartları
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => setShowTestUsers(true)}>
                  <Users className="mr-2 h-4 w-4" />
                  Kullanıcılar
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}

          <ThemeToggle />

          {isAuthenticated ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon">
                  <User className="h-5 w-5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <div className="px-2 py-1.5">
                  <p className="text-sm font-medium">{user?.firstName} {user?.lastName}</p>
                  <p className="text-xs text-muted-foreground">{user?.email}</p>
                </div>
                <DropdownMenuSeparator />
                <DropdownMenuItem asChild>
                  <Link to="/orders">Siparişlerim</Link>
                </DropdownMenuItem>
                {user?.role === 'Admin' && (
                  <DropdownMenuItem asChild>
                    <Link to="/admin">Admin Panel</Link>
                  </DropdownMenuItem>
                )}
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleLogout} className="text-destructive">
                  <LogOut className="mr-2 h-4 w-4" />
                  Çıkış Yap
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : (
            <div className="hidden md:flex items-center space-x-2">
              <Button variant="ghost" asChild>
                <Link to="/login">Giriş Yap</Link>
              </Button>
              <Button asChild>
                <Link to="/register">Kayıt Ol</Link>
              </Button>
            </div>
          )}

          {/* Mobile Menu */}
          <Sheet>
            <SheetTrigger asChild className="md:hidden">
              <Button variant="ghost" size="icon">
                <Menu className="h-5 w-5" />
              </Button>
            </SheetTrigger>
            <SheetContent side="right" className="w-72">
              <nav className="flex flex-col space-y-4 mt-8">
                <Link to="/" className="text-lg font-medium">
                  Ürünler
                </Link>
                {isAuthenticated ? (
                  <>
                    <Link to="/cart" className="text-lg font-medium">
                      Sepetim ({cartItemCount})
                    </Link>
                    <Link to="/orders" className="text-lg font-medium">
                      Siparişlerim
                    </Link>
                    {user?.role === 'Admin' && (
                      <Link to="/admin" className="text-lg font-medium">
                        Admin Panel
                      </Link>
                    )}
                    <Button variant="destructive" onClick={handleLogout}>
                      Çıkış Yap
                    </Button>
                  </>
                ) : (
                  <>
                    <Button asChild>
                      <Link to="/login">Giriş Yap</Link>
                    </Button>
                    <Button variant="outline" asChild>
                      <Link to="/register">Kayıt Ol</Link>
                    </Button>
                  </>
                )}
              </nav>
            </SheetContent>
          </Sheet>
        </div>
      </div>
    </header>

      {/* Test Cards Dialog */}
      <TestCardsDialog open={showTestCards} onOpenChange={setShowTestCards} />
      
      {/* Test Users Dialog */}
      <TestUsersDialog open={showTestUsers} onOpenChange={setShowTestUsers} />
    </>
  );
}
