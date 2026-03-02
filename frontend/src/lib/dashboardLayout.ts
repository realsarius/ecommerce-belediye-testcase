import type { LucideIcon } from 'lucide-react';

export type DashboardNavItem = {
  label: string;
  href?: string;
  icon: LucideIcon;
  badge?: string;
  disabled?: boolean;
};

export type DashboardNavGroup = {
  label: string;
  items: DashboardNavItem[];
};

export function normalizeDashboardPath(pathname: string, rootPath: '/admin' | '/seller') {
  return pathname === rootPath ? `${rootPath}/dashboard` : pathname;
}

export function isDashboardItemActive(pathname: string, href?: string) {
  if (!href) {
    return false;
  }

  if (pathname === href) {
    return true;
  }

  return href !== '/admin/dashboard'
    && href !== '/seller/dashboard'
    && pathname.startsWith(`${href}/`);
}

export function buildDashboardBreadcrumbs(
  pathname: string,
  rootPath: '/admin' | '/seller',
  rootLabel: string,
  groups: DashboardNavGroup[]
) {
  const normalizedPath = normalizeDashboardPath(pathname, rootPath);
  const breadcrumbs = [{ label: rootLabel, href: `${rootPath}/dashboard` }];
  const navItems = groups.flatMap((group) => group.items);
  const navItem = navItems.find((item) => item.href === normalizedPath);

  if (navItem && navItem.href) {
    breadcrumbs.push({ label: navItem.label, href: navItem.href });
    return breadcrumbs;
  }

  const parentNavItem = navItems.find((item) => item.href && normalizedPath.startsWith(`${item.href}/`));

  if (parentNavItem?.href) {
    breadcrumbs.push({ label: parentNavItem.label, href: parentNavItem.href });

    const remainingParts = normalizedPath
      .slice(parentNavItem.href.length)
      .split('/')
      .filter(Boolean);

    let currentPath = parentNavItem.href;
    for (const part of remainingParts) {
      currentPath += `/${part}`;
      breadcrumbs.push({
        label: part
          .split('-')
          .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
          .join(' '),
        href: currentPath,
      });
    }

    return breadcrumbs;
  }

  const parts = normalizedPath
    .split('/')
    .filter(Boolean)
    .slice(1);

  let currentPath = rootPath;
  for (const part of parts) {
    currentPath += `/${part}`;
    breadcrumbs.push({
      label: part
        .split('-')
        .map((segment) => segment.charAt(0).toUpperCase() + segment.slice(1))
        .join(' '),
      href: currentPath,
    });
  }

  return breadcrumbs;
}

export function getUserInitials(
  firstName?: string | null,
  lastName?: string | null,
  email?: string | null
) {
  const first = firstName?.trim()?.charAt(0) ?? '';
  const last = lastName?.trim()?.charAt(0) ?? '';
  const initials = `${first}${last}`.trim();

  if (initials) {
    return initials.toUpperCase();
  }

  return (email?.trim()?.charAt(0) ?? 'U').toUpperCase();
}

export function formatShortDate(value: string) {
  return new Date(value).toLocaleDateString('tr-TR', {
    day: '2-digit',
    month: 'short',
  });
}
