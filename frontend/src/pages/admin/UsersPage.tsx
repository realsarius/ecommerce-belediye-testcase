import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Search, ShieldCheck, UserCheck, UserX, Users } from 'lucide-react';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { useGetAdminUsersQuery } from '@/features/admin/adminApi';
import type { AdminUserListItem, AdminUserStatus } from '@/features/admin/types';
import { useDebounce } from '@/hooks/useDebounce';

function formatDate(value?: string | null) {
  if (!value) {
    return '-';
  }

  return new Date(value).toLocaleString('tr-TR', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    maximumFractionDigits: 0,
  }).format(value);
}

function getStatusTone(status: string) {
  switch (status) {
    case 'Active':
      return 'success' as const;
    case 'Suspended':
      return 'warning' as const;
    case 'Banned':
      return 'danger' as const;
    default:
      return 'neutral' as const;
  }
}

function getStatusLabel(status: string) {
  switch (status) {
    case 'Active':
      return 'Aktif';
    case 'Suspended':
      return 'Askıda';
    case 'Banned':
      return 'Banlı';
    default:
      return status;
  }
}

export default function UsersPage() {
  const [search, setSearch] = useState('');
  const [role, setRole] = useState<string>('all');
  const [status, setStatus] = useState<'all' | AdminUserStatus>('all');
  const [registeredFrom, setRegisteredFrom] = useState('');
  const [registeredTo, setRegisteredTo] = useState('');
  const [page, setPage] = useState(1);

  const debouncedSearch = useDebounce(search.trim(), 300);

  const queryParams = useMemo(
    () => ({
      search: debouncedSearch || undefined,
      role: role === 'all' ? undefined : role,
      status: status === 'all' ? undefined : status,
      registeredFrom: registeredFrom || undefined,
      registeredTo: registeredTo || undefined,
      page,
      pageSize: 12,
    }),
    [debouncedSearch, page, registeredFrom, registeredTo, role, status]
  );

  const { data, isLoading, isFetching } = useGetAdminUsersQuery(queryParams);
  const users: AdminUserListItem[] = data?.items ?? [];

  const stats = useMemo(() => {
    return {
      total: data?.totalCount ?? 0,
      active: users.filter((user) => user.status === 'Active').length,
      suspended: users.filter((user) => user.status === 'Suspended').length,
      banned: users.filter((user) => user.status === 'Banned').length,
    };
  }, [data?.totalCount, users]);

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Kullanıcı Yönetimi</h1>
        <p className="max-w-3xl text-muted-foreground">
          Kullanıcıları arayın, filtreleyin ve detay ekranından rol veya hesap durumlarını yönetin.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Toplam Kullanıcı"
          value={stats.total.toLocaleString('tr-TR')}
          helperText="Mevcut filtrelerle eşleşen kayıtlar."
          icon={Users}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Hesap"
          value={stats.active.toLocaleString('tr-TR')}
          helperText="Girişe açık kullanıcılar."
          icon={UserCheck}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Askıdaki Hesap"
          value={stats.suspended.toLocaleString('tr-TR')}
          helperText="Geçici olarak durdurulan hesaplar."
          icon={ShieldCheck}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="Banlı Hesap"
          value={stats.banned.toLocaleString('tr-TR')}
          helperText="Kalıcı olarak kapatılan hesaplar."
          icon={UserX}
          accentClass="text-rose-600 dark:text-rose-300"
          surfaceClass="bg-rose-500/10"
        />
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Kullanıcı Listesi</CardTitle>
          <CardDescription>Arama ve filtreler 300ms debounce ile çalışır.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 xl:grid-cols-[1.6fr_0.8fr_0.8fr_0.8fr_0.8fr]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
                placeholder="Ad, e-posta veya rol ara..."
                className="pl-10"
              />
            </div>

            <Select
              value={role}
              onValueChange={(value) => {
                setRole(value);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Rol" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tüm Roller</SelectItem>
                <SelectItem value="Admin">Admin</SelectItem>
                <SelectItem value="Customer">Customer</SelectItem>
                <SelectItem value="Seller">Seller</SelectItem>
                <SelectItem value="Support">Support</SelectItem>
              </SelectContent>
            </Select>

            <Select
              value={status}
              onValueChange={(value) => {
                setStatus(value as 'all' | AdminUserStatus);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Durum" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tüm Durumlar</SelectItem>
                <SelectItem value="Active">Aktif</SelectItem>
                <SelectItem value="Suspended">Askıya Alınmış</SelectItem>
                <SelectItem value="Banned">Banlı</SelectItem>
              </SelectContent>
            </Select>

            <Input
              type="date"
              value={registeredFrom}
              onChange={(event) => {
                setRegisteredFrom(event.target.value);
                setPage(1);
              }}
            />
            <Input
              type="date"
              value={registeredTo}
              onChange={(event) => {
                setRegisteredTo(event.target.value);
                setPage(1);
              }}
            />
          </div>

          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 8 }).map((_, index) => (
                <Skeleton key={index} className="h-14 rounded-xl" />
              ))}
            </div>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Kullanıcı</TableHead>
                    <TableHead>Rol</TableHead>
                    <TableHead>Kayıt Tarihi</TableHead>
                    <TableHead>Son Giriş</TableHead>
                    <TableHead>Toplam Harcama</TableHead>
                    <TableHead>Durum</TableHead>
                    <TableHead className="text-right">İşlem</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell>
                        <div>
                          <p className="font-medium">{user.fullName}</p>
                          <p className="text-sm text-muted-foreground">{user.email}</p>
                        </div>
                      </TableCell>
                      <TableCell>{user.role}</TableCell>
                      <TableCell>{formatDate(user.createdAt)}</TableCell>
                      <TableCell>{formatDate(user.lastLoginAt)}</TableCell>
                      <TableCell>{formatCurrency(user.totalSpent)}</TableCell>
                      <TableCell>
                        <StatusBadge
                          label={getStatusLabel(user.status)}
                          tone={getStatusTone(user.status)}
                        />
                      </TableCell>
                      <TableCell className="text-right">
                        <Button asChild variant="outline" size="sm">
                          <Link to={`/admin/users/${user.id}`}>Detay</Link>
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {users.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={7} className="p-4">
                        <EmptyState
                          icon={Users}
                          title="Kullanıcı bulunamadı"
                          description="Seçili filtrelere uyan kullanıcı kaydı bulunamadı. Arama ifadesini veya tarih aralığını güncelleyerek tekrar deneyin."
                          className="border-none bg-transparent shadow-none"
                        />
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>

              <div className="flex flex-col gap-3 border-t pt-4 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-sm text-muted-foreground">
                  Sayfa {data?.page ?? 1} / {Math.max(data?.totalPages ?? 1, 1)}
                  {isFetching ? ' • Güncelleniyor...' : ''}
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    onClick={() => setPage((current) => Math.max(current - 1, 1))}
                    disabled={!data?.hasPreviousPage}
                  >
                    Önceki
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() => setPage((current) => current + 1)}
                    disabled={!data?.hasNextPage}
                  >
                    Sonraki
                  </Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
