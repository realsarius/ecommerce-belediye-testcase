import { useEffect, useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Bell,
  BellRing,
  ChevronLeft,
  ChevronRight,
  FolderTree,
  Gift,
  HeartPulse,
  LayoutDashboard,
  LogOut,
  Megaphone,
  Menu,
  MessageSquare,
  MessageSquareQuote,
  Package,
  ShieldCheck,
  ShoppingBag,
  Store,
  Ticket,
  User,
  Users,
} from 'lucide-react';
import { Button } from '@/components/common/button';
import { Badge } from '@/components/common/badge';
import { Avatar, AvatarFallback } from '@/components/common/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/common/dropdown-menu';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetTitle,
  SheetTrigger,
} from '@/components/common/sheet';
import { ScrollArea } from '@/components/common/scroll-area';
import { cn } from '@/lib/utils';
import { usePageTitle } from '@/hooks/usePageTitle';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { logout } from '@/features/auth/authSlice';
import { useRevokeMutation } from '@/features/auth/authApi';
import { useGetUnreadNotificationCountQuery } from '@/features/notifications/notificationsApi';
import {
  buildDashboardBreadcrumbs,
  getUserInitials,
  isDashboardItemActive,
  normalizeDashboardPath,
  type DashboardNavGroup,
} from '@/lib/dashboardLayout';

const adminNavigationGroups: DashboardNavGroup[] = [
  {
    label: 'Genel',
    items: [
      { label: 'Dashboard', href: '/admin/dashboard', icon: LayoutDashboard },
    ],
  },
  {
    label: 'Kullanıcılar',
    items: [
      { label: 'Tüm Kullanıcılar', href: '/admin/users', icon: Users },
      { label: 'Roller & İzinler', icon: ShieldCheck, disabled: true, badge: 'Yakında' },
    ],
  },
  {
    label: 'Katalog',
    items: [
      { label: 'Ürünler', href: '/admin/products', icon: Package },
      { label: 'Kategoriler', href: '/admin/categories', icon: FolderTree },
    ],
  },
  {
    label: 'Satış',
    items: [
      { label: 'Siparişler', href: '/admin/orders', icon: ShoppingBag },
      { label: 'İade Talepleri', href: '/admin/returns', icon: ArrowLeft },
    ],
  },
  {
    label: 'Satıcılar',
    items: [
      { label: 'Seller Listesi', href: '/admin/sellers', icon: Store },
      { label: 'Başvurular', icon: Users, disabled: true, badge: 'Yakında' },
    ],
  },
  {
    label: 'Finans',
    items: [
      { label: 'Gelir Raporu', href: '/admin/finance', icon: HeartPulse },
      { label: 'Kupon & Kampanyalar', href: '/admin/coupons', icon: Ticket },
      { label: 'Gift Cardlar', href: '/admin/gift-cards', icon: Gift },
    ],
  },
  {
    label: 'Destek',
    items: [
      { label: 'Destek Talepleri', href: '/admin/support', icon: MessageSquare },
      { label: 'Yorum Moderasyonu', href: '/admin/reviews', icon: MessageSquareQuote },
    ],
  },
  {
    label: 'İletişim',
    items: [
      { label: 'Duyuru Gönder', href: '/admin/announcements', icon: Megaphone },
      { label: 'Bildirim Şablonları', href: '/admin/notifications/templates', icon: BellRing },
    ],
  },
  {
    label: 'Sistem',
    items: [
      { label: 'Sistem Sağlığı', href: '/admin/system', icon: ShieldCheck },
    ],
  },
];

function AdminNav({
  collapsed,
  onNavigate,
  pathname,
}: {
  collapsed: boolean;
  onNavigate?: () => void;
  pathname: string;
}) {
  return (
    <div className="space-y-6 p-3">
      {adminNavigationGroups.map((group) => (
        <div key={group.label} className="space-y-2">
          {!collapsed ? (
            <p className="px-2 text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
              {group.label}
            </p>
          ) : null}
          <div className="space-y-1">
            {group.items.map((item) => {
              const Icon = item.icon;
              const isActive = isDashboardItemActive(pathname, item.href);
              const itemClasses = cn(
                'group flex items-center gap-3 rounded-2xl px-3 py-2.5 text-sm transition-colors',
                collapsed ? 'justify-center px-2' : '',
                item.disabled
                  ? 'cursor-not-allowed text-muted-foreground/70'
                  : isActive
                    ? 'bg-primary text-primary-foreground shadow-sm'
                    : 'text-muted-foreground hover:bg-muted hover:text-foreground'
              );

              const itemContent = (
                <>
                  <Icon className="h-4.5 w-4.5 shrink-0" />
                  {!collapsed ? (
                    <>
                      <span className="min-w-0 flex-1 truncate">{item.label}</span>
                      {item.badge ? (
                        <Badge
                          variant={item.disabled ? 'outline' : 'secondary'}
                          className={cn(
                            'rounded-full px-2 py-0 text-[10px] uppercase tracking-wide',
                            isActive ? 'border-primary-foreground/30 text-primary-foreground' : ''
                          )}
                        >
                          {item.badge}
                        </Badge>
                      ) : null}
                    </>
                  ) : null}
                </>
              );

              if (item.href && !item.disabled) {
                return (
                  <Link
                    key={item.label}
                    to={item.href}
                    title={collapsed ? item.label : undefined}
                    onClick={onNavigate}
                    className={itemClasses}
                  >
                    {itemContent}
                  </Link>
                );
              }

              return (
                <div
                  key={item.label}
                  title={collapsed ? item.label : undefined}
                  className={itemClasses}
                >
                  {itemContent}
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}

export function AdminLayout() {
  usePageTitle();

  const location = useLocation();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { user, refreshToken } = useAppSelector((state) => state.auth);
  const { data: notificationSummary } = useGetUnreadNotificationCountQuery();
  const [revoke] = useRevokeMutation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem('adminSidebarCollapsed') === 'true');

  useEffect(() => {
    localStorage.setItem('adminSidebarCollapsed', String(collapsed));
  }, [collapsed]);

  const normalizedPath = normalizeDashboardPath(location.pathname, '/admin');
  const breadcrumbs = buildDashboardBreadcrumbs(normalizedPath, '/admin', 'Yönetim Paneli', adminNavigationGroups);
  const unreadNotificationCount = notificationSummary?.unreadCount ?? 0;

  const handleLogout = async () => {
    if (refreshToken) {
      try {
        await revoke({ refreshToken }).unwrap();
      } catch {
        // no-op
      }
    }

    dispatch(logout());
    navigate('/');
  };

  return (
    <div className="min-h-screen bg-muted/20 md:flex">
      <aside
        className={cn(
          'hidden border-r bg-background/95 transition-all duration-200 md:flex md:flex-col',
          collapsed ? 'md:w-16' : 'md:w-64'
        )}
      >
        <div className={cn('flex items-center border-b px-3 py-3', collapsed ? 'justify-center' : 'justify-between')}>
          {!collapsed ? (
            <div>
              <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Admin</p>
              <h2 className="mt-1 text-lg font-semibold">Operasyon Merkezi</h2>
            </div>
          ) : (
            <div className="rounded-2xl bg-primary/10 p-2 text-primary">
              <ShieldCheck className="h-5 w-5" />
            </div>
          )}
          <Button
            variant="ghost"
            size="icon"
            className="shrink-0"
            onClick={() => setCollapsed((value) => !value)}
          >
            {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
          </Button>
        </div>

        <ScrollArea className="flex-1">
          <AdminNav collapsed={collapsed} pathname={normalizedPath} />
        </ScrollArea>

        <div className="border-t p-3">
          <Button variant="ghost" asChild className={cn('w-full', collapsed ? 'justify-center px-0' : 'justify-start')}>
            <Link to="/" title={collapsed ? 'Mağazaya Dön' : undefined}>
              <ArrowLeft className="mr-2 h-4 w-4 shrink-0" />
              {!collapsed ? <span>Mağazaya Dön</span> : null}
            </Link>
          </Button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-40 border-b bg-background/90 backdrop-blur">
          <div className="flex h-16 items-center justify-between gap-4 px-4 md:hidden">
            <div className="flex items-center gap-3">
              <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
                <SheetTrigger asChild>
                  <Button variant="ghost" size="icon">
                    <Menu className="h-5 w-5" />
                  </Button>
                </SheetTrigger>
                <SheetContent side="left" className="w-80 p-0">
                  <SheetTitle className="sr-only">Admin menüsü</SheetTitle>
                  <SheetDescription className="sr-only">Admin panel navigasyonu</SheetDescription>
                  <div className="border-b px-4 py-4">
                    <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Admin</p>
                    <h2 className="mt-1 text-lg font-semibold">Operasyon Merkezi</h2>
                  </div>
                  <ScrollArea className="h-[calc(100vh-8rem)]">
                    <AdminNav collapsed={false} onNavigate={() => setMobileMenuOpen(false)} pathname={normalizedPath} />
                  </ScrollArea>
                </SheetContent>
              </Sheet>
              <div>
                <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Admin</p>
                <p className="font-semibold">Operasyon Merkezi</p>
              </div>
            </div>

            <Button variant="ghost" size="icon" asChild>
              <Link to="/notifications">
                <Bell className="h-5 w-5" />
              </Link>
            </Button>
          </div>

          <div className="hidden h-16 items-center justify-between gap-6 px-6 md:flex">
            <div className="min-w-0">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                {breadcrumbs.map((crumb, index) => (
                  <div key={`${crumb.href}-${index}`} className="flex items-center gap-2">
                    {index > 0 ? <ChevronRight className="h-3.5 w-3.5" /> : null}
                    {index === breadcrumbs.length - 1 ? (
                      <span className="font-medium text-foreground">{crumb.label}</span>
                    ) : (
                      <Link to={crumb.href} className="hover:text-foreground">
                        {crumb.label}
                      </Link>
                    )}
                  </div>
                ))}
              </div>
              <p className="mt-1 text-xs text-muted-foreground">
                Operasyon, katalog ve sipariş süreçlerini tek panelden yönetin.
              </p>
            </div>

            <div className="flex items-center gap-2">
              <Button variant="ghost" size="icon" className="relative" asChild>
                <Link to="/notifications">
                  <Bell className="h-5 w-5" />
                  {unreadNotificationCount > 0 ? (
                    <Badge
                      variant="destructive"
                      className="absolute -right-1 -top-1 h-5 min-w-5 rounded-full px-1 text-[10px]"
                    >
                      {unreadNotificationCount > 9 ? '9+' : unreadNotificationCount}
                    </Badge>
                  ) : null}
                </Link>
              </Button>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" className="flex h-auto items-center gap-3 rounded-2xl px-2 py-1.5">
                    <Avatar className="h-9 w-9 border">
                      <AvatarFallback className="bg-primary/10 font-semibold text-primary">
                        {getUserInitials(user?.firstName, user?.lastName, user?.email)}
                      </AvatarFallback>
                    </Avatar>
                    <div className="text-left">
                      <p className="text-sm font-medium">{user?.firstName} {user?.lastName}</p>
                      <p className="text-xs text-muted-foreground">{user?.email}</p>
                    </div>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-64">
                  <DropdownMenuLabel>Yönetici Hesabı</DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem asChild>
                    <Link to="/account">
                      <User className="h-4 w-4" />
                      Profilim
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link to="/">
                      <ArrowLeft className="h-4 w-4" />
                      Mağazaya Dön
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem variant="destructive" onClick={handleLogout}>
                    <LogOut className="h-4 w-4" />
                    Çıkış Yap
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </header>

        <main className="min-w-0 flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
