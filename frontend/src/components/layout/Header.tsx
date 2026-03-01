import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useEffect, useRef, useState, type FormEvent } from 'react';
import { ShoppingCart, User, LogOut, Menu, Package, Wrench, CreditCard, Users, MapPin, HelpCircle, Ticket, Store, Search, MessageSquare, Heart, RefreshCw, Bell, Gift } from 'lucide-react';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/common/dropdown-menu';
import { Sheet, SheetContent, SheetTrigger, SheetTitle, SheetDescription } from '@/components/common/sheet';
import { Badge } from '@/components/common/badge';
import { useAppSelector, useAppDispatch } from '@/app/hooks';
import { logout } from '@/features/auth/authSlice';
import { useRevokeMutation } from '@/features/auth/authApi';
import { useGetCartQuery } from '@/features/cart/cartApi';
import { ThemeToggle } from '@/components/common/ThemeToggle';
import { CartDrawer } from '@/components/common/CartDrawer';
import { useDevTools } from '@/components/common/DevToolsProvider';
import { TestCardsDialog } from '@/components/common/TestCardsDialog';
import { TestUsersDialog } from '@/components/common/TestUsersDialog';
import { useDebounce } from '@/hooks/useDebounce';
import { useSearchSuggestionsQuery } from '@/features/products/productsApi';
import { useGetWishlistQuery } from '@/features/wishlist/wishlistApi';
import { useGuestWishlist } from '@/features/wishlist';
import { useGetUnreadNotificationCountQuery } from '@/features/notifications/notificationsApi';

const INITIAL_SUGGESTION_LIMIT = 6;
const SUGGESTION_STEP = 10;

export function Header() {
  const { isAuthenticated, user, refreshToken } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  const location = useLocation();
  const navigate = useNavigate();
  const { data: cart } = useGetCartQuery(undefined, { skip: !isAuthenticated });
  const { data: wishlist } = useGetWishlistQuery(undefined, { skip: !isAuthenticated });
  const { data: notificationSummary } = useGetUnreadNotificationCountQuery(undefined, { skip: !isAuthenticated });
  const { pendingCount } = useGuestWishlist();
  const { isDevToolsEnabled, openCouponsDialog } = useDevTools();
  const [showTestCards, setShowTestCards] = useState(false);
  const [showTestUsers, setShowTestUsers] = useState(false);
  const [searchInput, setSearchInput] = useState('');
  const [showSearchDropdown, setShowSearchDropdown] = useState(false);
  const [suggestionLimit, setSuggestionLimit] = useState(INITIAL_SUGGESTION_LIMIT);
  const [revoke] = useRevokeMutation();
  const searchContainerRef = useRef<HTMLDivElement | null>(null);

  const cartItemCount = cart?.items?.reduce((sum, item) => sum + item.quantity, 0) || 0;
  const wishlistItemCount = isAuthenticated ? (wishlist?.items?.length || 0) : pendingCount;
  const unreadNotificationCount = notificationSummary?.unreadCount || 0;
  const currentSearchQuery = new URLSearchParams(location.search).get('q') ?? '';
  const debouncedSearch = useDebounce(searchInput.trim(), 300);
  const shouldFetchSuggestions = debouncedSearch.length >= 2;

  const { data: suggestions = [], isFetching: isFetchingSuggestions } = useSearchSuggestionsQuery(
    {
      q: debouncedSearch,
      limit: suggestionLimit,
    },
    { skip: !shouldFetchSuggestions }
  );

  const hasMoreSuggestions = suggestions.length >= suggestionLimit;

  useEffect(() => {
    setSearchInput(currentSearchQuery);
  }, [currentSearchQuery]);

  useEffect(() => {
    setSuggestionLimit(INITIAL_SUGGESTION_LIMIT);
  }, [debouncedSearch]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (searchContainerRef.current && !searchContainerRef.current.contains(event.target as Node)) {
        setShowSearchDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const navigateToSearch = (rawQuery: string) => {
    const query = rawQuery.trim();
    const params = location.pathname === '/' ? new URLSearchParams(location.search) : new URLSearchParams();

    if (query) {
      params.set('q', query);
    } else {
      params.delete('q');
    }
    params.delete('page');

    setShowSearchDropdown(false);
    const search = params.toString();
    navigate({ pathname: '/', search: search ? `?${search}` : '' });
  };

  const handleSearchSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    navigateToSearch(searchInput);
  };

  const handleLogout = async () => {

    if (refreshToken) {
      try {
        await revoke({ refreshToken }).unwrap();
      } catch (error) {
        console.error('Refresh token revoke failed', error);
      }
    }
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

          <div className="hidden md:flex flex-1 items-center justify-center gap-6 px-6">
            {/* Desktop Navigation */}
            <nav className="flex items-center space-x-6">
              <Link to="/" className="text-sm font-medium hover:text-primary transition-colors">
                Ürünler
              </Link>
              {isAuthenticated && (
                <>
                  <Link to="/orders" className="text-sm font-medium hover:text-primary transition-colors">
                    Siparişlerim
                  </Link>
                  <Link to="/returns" className="text-sm font-medium hover:text-primary transition-colors">
                    İadelerim
                  </Link>
                  <Link to="/loyalty" className="text-sm font-medium hover:text-primary transition-colors">
                    Puanlarım
                  </Link>
                  <Link to="/notifications" className="text-sm font-medium hover:text-primary transition-colors">
                    Bildirimler
                  </Link>
                  <Link to="/cart" className="text-sm font-medium hover:text-primary transition-colors">
                    Sepetim
                  </Link>
                </>
              )}
            </nav>

            <div className="relative w-full max-w-sm" ref={searchContainerRef}>
              <form onSubmit={handleSearchSubmit} autoComplete="off">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  name="product-search-desktop"
                  value={searchInput}
                  onChange={(e) => {
                    setSearchInput(e.target.value);
                    setShowSearchDropdown(true);
                  }}
                  onFocus={() => setShowSearchDropdown(true)}
                  autoComplete="off"
                  autoCorrect="off"
                  autoCapitalize="none"
                  spellCheck={false}
                  data-lpignore="true"
                  data-form-type="other"
                  placeholder="Ürün ara..."
                  className="h-9 pl-9 pr-16"
                />
                <Button
                  type="submit"
                  size="sm"
                  className="absolute right-1 top-1/2 h-7 -translate-y-1/2 px-3"
                >
                  Ara
                </Button>
              </form>

              {showSearchDropdown && shouldFetchSuggestions && (
                <div className="absolute left-0 right-0 top-full z-50 mt-2 overflow-hidden rounded-md border bg-background shadow-lg">
                  {isFetchingSuggestions ? (
                    <p className="px-3 py-2 text-sm text-muted-foreground">Aranıyor...</p>
                  ) : suggestions.length === 0 ? (
                    <p className="px-3 py-2 text-sm text-muted-foreground">Ürün bulunamadı</p>
                  ) : (
                    <div className="max-h-72 overflow-y-auto">
                      {suggestions.map((product) => (
                        <Link
                          key={product.id}
                          to={`/products/${product.id}`}
                          onClick={() => setShowSearchDropdown(false)}
                          className="block border-b px-3 py-2 hover:bg-muted/70"
                        >
                          <p className="truncate text-sm font-medium">{product.name}</p>
                          <p className="text-xs text-muted-foreground">
                            {product.categoryName} • {product.price.toLocaleString('tr-TR')} {product.currency}
                          </p>
                        </Link>
                      ))}
                      <button
                        type="button"
                        onClick={() =>
                          setSuggestionLimit((prev) =>
                            hasMoreSuggestions ? prev + SUGGESTION_STEP : prev
                          )
                        }
                        disabled={!hasMoreSuggestions}
                        className="w-full border-b px-3 py-2 text-left text-sm font-medium text-primary hover:bg-muted/70 disabled:text-muted-foreground disabled:hover:bg-transparent"
                      >
                        {hasMoreSuggestions ? 'Daha fazla göster' : 'Tüm sonuçlar listelendi'}
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* Right Side */}
          <div className="flex items-center space-x-4">
            {/* Wishlist */}
            <Button variant="ghost" size="icon" className="relative" asChild>
              <Link to={isAuthenticated ? "/wishlist" : "/login"} state={{ from: location }}>
                <Heart className="h-5 w-5" />
                {wishlistItemCount > 0 && (
                  <Badge
                    variant="destructive"
                    className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 text-xs"
                  >
                    {wishlistItemCount}
                  </Badge>
                )}
              </Link>
            </Button>

            {/* Cart */}
            {isAuthenticated && (
              <Button variant="ghost" size="icon" className="relative" asChild>
                <Link to="/notifications">
                  <Bell className="h-5 w-5" />
                  {unreadNotificationCount > 0 && (
                    <Badge
                      variant="destructive"
                      className="absolute -top-1 -right-1 h-5 min-w-5 px-1 flex items-center justify-center p-0 text-xs"
                    >
                      {unreadNotificationCount > 9 ? '9+' : unreadNotificationCount}
                    </Badge>
                  )}
                </Link>
              </Button>
            )}

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
                  <DropdownMenuItem asChild>
                    <Link to="/notifications" className="flex items-center gap-2">
                      <Bell className="h-4 w-4 text-primary" />
                      Bildirim Merkezi
                      {unreadNotificationCount > 0 && (
                        <Badge variant="secondary" className="ml-auto">
                          {unreadNotificationCount}
                        </Badge>
                      )}
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link to="/returns" className="flex items-center gap-2">
                      <RefreshCw className="h-4 w-4 text-primary" />
                      İade ve İptal Taleplerim
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
                    <Link to="/loyalty" className="flex items-center gap-2">
                      <Gift className="h-4 w-4 text-amber-600" />
                      Puanlarım ve Ödüllerim
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link to="/help" className="flex items-center gap-2">
                      <HelpCircle className="h-4 w-4 text-primary" />
                      Yardım
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link to="/support" className="flex items-center gap-2">
                      <MessageSquare className="h-4 w-4 text-primary" />
                      Canlı Destek
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
                <SheetTitle className="sr-only">Mobil Menü</SheetTitle>
                <SheetDescription className="sr-only">Site navigasyon menüsü</SheetDescription>
                <nav className="flex flex-col space-y-4 mt-8 px-4">
                  <form onSubmit={handleSearchSubmit} autoComplete="off">
                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        name="product-search-mobile"
                        value={searchInput}
                        onChange={(e) => setSearchInput(e.target.value)}
                        autoComplete="off"
                        autoCorrect="off"
                        autoCapitalize="none"
                        spellCheck={false}
                        data-lpignore="true"
                        data-form-type="other"
                        placeholder="Ürün ara..."
                        className="pl-9 pr-16"
                      />
                      <Button
                        type="submit"
                        size="sm"
                        className="absolute right-1 top-1/2 h-7 -translate-y-1/2 px-3"
                      >
                        Ara
                      </Button>
                    </div>
                  </form>
                  <Link to="/" className="text-lg font-medium">
                    Ürünler
                  </Link>
                  {isAuthenticated ? (
                    <>
                      <Link to="/wishlist" className="text-lg font-medium">
                        Favorilerim ({wishlistItemCount})
                      </Link>
                      <Link to="/cart" className="text-lg font-medium">
                        Sepetim ({cartItemCount})
                      </Link>
                      <Link to="/notifications" className="text-lg font-medium">
                        Bildirimler ({unreadNotificationCount})
                      </Link>
                      <div className="border-t pt-4">
                        <p className="text-xs text-muted-foreground uppercase tracking-wider mb-2">Siparişlerim</p>
                        <Link to="/orders" className="text-lg font-medium">
                          Tüm Siparişlerim
                        </Link>
                        <Link to="/returns" className="mt-3 text-lg font-medium">
                          İade ve İptal Taleplerim
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
                          <Link to="/loyalty" className="text-lg font-medium">
                            Puanlarım ve Ödüllerim
                          </Link>
                          <Link to="/help" className="text-lg font-medium">
                            Yardım
                          </Link>
                          <Link to="/support" className="text-lg font-medium">
                            Canlı Destek
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
                      {wishlistItemCount > 0 && (
                        <Link to="/login" state={{ from: location }} className="text-lg font-medium">
                          Bekleyen Favoriler ({wishlistItemCount})
                        </Link>
                      )}
                      <Button asChild>
                        <Link to="/login" state={{ from: location }}>Giriş Yap</Link>
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
