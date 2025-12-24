import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { ShoppingCart, User, LogOut, Menu, Package, Wrench, CreditCard, Users, MapPin, HelpCircle, Ticket, Store } from 'lucide-react';
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
  const { isDevToolsEnabled, openCouponsDialog } = useDevTools();
  const [showTestCards, setShowTestCards] = useState(false);
  const [showTestUsers, setShowTestUsers] = useState(false);

  const cartItemCount = cart?.items?.reduce((sum, item) => sum + item.quantity, 0) || 0;

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
                  {/* <span className="hidden sm:inline">Dev Tools</span> */}
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
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={openCouponsDialog}>
                  <Ticket className="mr-2 h-4 w-4" />
                  Kuponlar
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
              <DropdownMenuContent align="end" className="w-64">
                {/* User Info Header */}
                <div className="px-3 py-3 bg-muted/50">
                  <p className="text-sm font-semibold">{user?.firstName} {user?.lastName}</p>
                  <p className="text-xs text-muted-foreground">{user?.email}</p>
                </div>
                
                {/* Siparişlerim Section */}
                <div className="px-3 py-2">
                  <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Siparişlerim</p>
                </div>
                <DropdownMenuItem asChild>
                  <Link to="/orders" className="flex items-center gap-2">
                    <Package className="h-4 w-4 text-primary" />
                    Tüm Siparişlerim
                  </Link>
                </DropdownMenuItem>
                
                <DropdownMenuSeparator />
                
                {/* Hesabım Section */}
                <div className="px-3 py-2">
                  <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Hesabım</p>
                </div>
                <DropdownMenuItem asChild>
                  <Link to="/account" className="flex items-center gap-2">
                    <User className="h-4 w-4 text-primary" />
                    Kullanıcı Bilgilerim
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link to="/account/addresses" className="flex items-center gap-2">
                    <MapPin className="h-4 w-4 text-primary" />
                    Adres Bilgilerim
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link to="/help" className="flex items-center gap-2">
                    <HelpCircle className="h-4 w-4 text-primary" />
                    Yardım
                  </Link>
                </DropdownMenuItem>
                
                {user?.role === 'Admin' && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem asChild>
                      <Link to="/admin" className="flex items-center gap-2">
                        <Wrench className="h-4 w-4 text-primary" />
                        Admin Panel
                      </Link>
                    </DropdownMenuItem>
                  </>
                )}
                
                {user?.role === 'Seller' && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem asChild>
                      <Link to="/seller" className="flex items-center gap-2">
                        <Store className="h-4 w-4 text-amber-600" />
                        Satıcı Paneli
                      </Link>
                    </DropdownMenuItem>
                  </>
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
                    <div className="border-t pt-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wider mb-2">Siparişlerim</p>
                      <Link to="/orders" className="text-lg font-medium">
                        Tüm Siparişlerim
                      </Link>
                    </div>
                    <div className="border-t pt-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wider mb-2">Hesabım</p>
                      <div className="flex flex-col space-y-3">
                        <Link to="/account" className="text-lg font-medium">
                          Kullanıcı Bilgilerim
                        </Link>
                        <Link to="/account/addresses" className="text-lg font-medium">
                          Adres Bilgilerim
                        </Link>
                        <Link to="/help" className="text-lg font-medium">
                          Yardım
                        </Link>
                      </div>
                    </div>
                    {user?.role === 'Admin' && (
                      <div className="border-t pt-4">
                        <Link to="/admin" className="text-lg font-medium">
                          Admin Panel
                        </Link>
                      </div>
                    )}
                    {user?.role === 'Seller' && (
                      <div className="border-t pt-4">
                        <Link to="/seller" className="text-lg font-medium text-amber-600">
                          Satıcı Paneli
                        </Link>
                      </div>
                    )}
                    <div className="border-t pt-4">
                      <Button variant="destructive" onClick={handleLogout} className="w-full">
                        Çıkış Yap
                      </Button>
                    </div>
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
