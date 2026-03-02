import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  BadgeCheck,
  Ban,
  Mail,
  MapPin,
  Phone,
  ShieldAlert,
  ShoppingBag,
  User,
} from 'lucide-react';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/common/select';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { ConfirmModal } from '@/components/admin/ConfirmModal';
import {
  useGetAdminUserDetailQuery,
  useUpdateAdminUserRoleMutation,
  useUpdateAdminUserStatusMutation,
} from '@/features/admin/adminApi';
import type { AdminUserStatus } from '@/features/admin/types';

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

function getStatusTone(status: AdminUserStatus) {
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

function getStatusLabel(status: AdminUserStatus) {
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

export default function UserDetailPage() {
  const { id } = useParams<{ id: string }>();
  const userId = Number(id);

  const { data: user, isLoading, refetch } = useGetAdminUserDetailQuery(userId, {
    skip: !userId,
  });
  const [updateRole, { isLoading: isUpdatingRole }] = useUpdateAdminUserRoleMutation();
  const [updateStatus, { isLoading: isUpdatingStatus }] = useUpdateAdminUserStatusMutation();

  const [selectedRole, setSelectedRole] = useState('Customer');
  const [pendingStatus, setPendingStatus] = useState<AdminUserStatus | null>(null);

  useEffect(() => {
    if (user) {
      setSelectedRole(user.role);
    }
  }, [user]);

  const summary = useMemo(() => {
    if (!user) {
      return null;
    }

    return {
      averageOrderValue: user.averageOrderValue,
      totalSpent: user.totalSpent,
      orderCount: user.orderCount,
    };
  }, [user]);

  const handleRoleUpdate = async () => {
    if (!user || selectedRole === user.role) {
      return;
    }

    await updateRole({ id: user.id, role: selectedRole }).unwrap();
    await refetch();
  };

  const handleStatusUpdate = async () => {
    if (!user || !pendingStatus) {
      return;
    }

    await updateStatus({ id: user.id, status: pendingStatus }).unwrap();
    setPendingStatus(null);
    await refetch();
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-40" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-36 rounded-xl" />
          ))}
        </div>
        <Skeleton className="h-[540px] rounded-xl" />
      </div>
    );
  }

  if (!user || !summary) {
    return (
      <Card className="border-border/70">
        <CardContent className="flex flex-col items-center gap-4 p-12 text-center">
          <User className="h-12 w-12 text-muted-foreground" />
          <div className="space-y-1">
            <p className="text-xl font-semibold">Kullanıcı bulunamadı</p>
            <p className="text-muted-foreground">Bu kullanıcı kaydı mevcut değil veya endpoint yanıt vermedi.</p>
          </div>
          <Button asChild variant="outline">
            <Link to="/admin/users">Listeye dön</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Button variant="ghost" asChild>
        <Link to="/admin/users">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Kullanıcı listesine dön
        </Link>
      </Button>

      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight">{user.fullName}</h1>
            <StatusBadge label={getStatusLabel(user.status)} tone={getStatusTone(user.status)} />
          </div>
          <p className="text-muted-foreground">
            {user.email} • Rol: {user.role}
          </p>
        </div>

        <Card className="w-full max-w-xl border-border/70 bg-muted/20">
          <CardContent className="grid gap-3 p-5 md:grid-cols-2">
            <div className="space-y-2">
              <p className="text-sm font-medium">Rol Değiştir</p>
              <div className="flex gap-2">
                <Select value={selectedRole} onValueChange={setSelectedRole}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Rol seçin" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Admin">Admin</SelectItem>
                    <SelectItem value="Customer">Customer</SelectItem>
                    <SelectItem value="Seller">Seller</SelectItem>
                    <SelectItem value="Support">Support</SelectItem>
                  </SelectContent>
                </Select>
                <Button onClick={() => void handleRoleUpdate()} disabled={isUpdatingRole || selectedRole === user.role}>
                  Kaydet
                </Button>
              </div>
            </div>

            <div className="space-y-2">
              <p className="text-sm font-medium">Hesap Aksiyonları</p>
              <div className="flex flex-wrap gap-2">
                <Button variant="outline" onClick={() => setPendingStatus('Suspended')}>
                  Askıya Al
                </Button>
                <Button variant="destructive" onClick={() => setPendingStatus('Banned')}>
                  Banla
                </Button>
                <Button variant="secondary" onClick={() => setPendingStatus('Active')}>
                  Aktifleştir
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Toplam Harcama"
          value={formatCurrency(summary.totalSpent)}
          helperText="İptal ve bekleyen ödeme hariç siparişler."
          icon={ShoppingBag}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Sipariş Sayısı"
          value={summary.orderCount.toLocaleString('tr-TR')}
          helperText="Tamamlanan sipariş adedi."
          icon={BadgeCheck}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Ortalama Sipariş"
          value={formatCurrency(summary.averageOrderValue)}
          helperText="Ortalama sepet değeri."
          icon={ShieldAlert}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
        <KpiCard
          title="Son Giriş"
          value={user.lastLoginAt ? formatDate(user.lastLoginAt) : '-'}
          helperText="En son token üretilen oturum."
          icon={Ban}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
      </div>

      <Tabs defaultValue="profile" className="space-y-4">
        <TabsList className="h-auto w-full flex-wrap justify-start gap-2 bg-transparent p-0">
          <TabsTrigger value="profile" className="rounded-full border bg-muted/50 px-4 py-2">Profil</TabsTrigger>
          <TabsTrigger value="orders" className="rounded-full border bg-muted/50 px-4 py-2">Siparişler</TabsTrigger>
          <TabsTrigger value="addresses" className="rounded-full border bg-muted/50 px-4 py-2">Adresler</TabsTrigger>
        </TabsList>

        <TabsContent value="profile">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Profil Bilgileri</CardTitle>
              <CardDescription>Kullanıcının hesap ve iletişim özeti.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-6 md:grid-cols-2">
              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <User className="h-5 w-5 text-primary" />
                  <div>
                    <p className="text-sm text-muted-foreground">Ad Soyad</p>
                    <p className="font-medium">{user.fullName}</p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <Mail className="h-5 w-5 text-primary" />
                  <div>
                    <p className="text-sm text-muted-foreground">E-posta</p>
                    <p className="font-medium">{user.email}</p>
                  </div>
                </div>
              </div>
              <div className="space-y-4">
                <div>
                  <p className="text-sm text-muted-foreground">Kayıt Tarihi</p>
                  <p className="font-medium">{formatDate(user.createdAt)}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">E-posta Doğrulama</p>
                  <p className="font-medium">{user.isEmailVerified ? 'Doğrulandı' : 'Doğrulanmadı'}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="orders">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Siparişler</CardTitle>
              <CardDescription>Kullanıcıya ait sipariş geçmişi.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {user.orders.length === 0 ? (
                <p className="py-10 text-center text-muted-foreground">Bu kullanıcıya ait sipariş bulunmuyor.</p>
              ) : (
                user.orders.map((order) => (
                  <div key={order.id} className="rounded-2xl border p-4">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                      <div>
                        <p className="font-medium">{order.orderNumber || `Sipariş #${order.id}`}</p>
                        <p className="text-sm text-muted-foreground">{formatDate(order.createdAt)}</p>
                      </div>
                      <div className="flex items-center gap-3">
                        <StatusBadge label={order.status} tone={order.status === 'Cancelled' ? 'danger' : 'info'} />
                        <span className="font-semibold">{formatCurrency(order.totalAmount)}</span>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="addresses">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Adresler</CardTitle>
              <CardDescription>Kayıtlı teslimat adresleri yalnızca okunur olarak gösterilir.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {user.addresses.length === 0 ? (
                <p className="py-10 text-center text-muted-foreground">Kayıtlı adres bulunmuyor.</p>
              ) : (
                user.addresses.map((address) => (
                  <div key={address.id} className="rounded-2xl border p-4">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="font-medium">{address.title}</p>
                      {address.isDefault ? <StatusBadge label="Varsayılan" tone="success" /> : null}
                    </div>
                    <div className="mt-3 space-y-2 text-sm text-muted-foreground">
                      <p className="flex items-center gap-2">
                        <User className="h-4 w-4" />
                        {address.fullName}
                      </p>
                      <p className="flex items-center gap-2">
                        <Phone className="h-4 w-4" />
                        {address.phone}
                      </p>
                      <p className="flex items-start gap-2">
                        <MapPin className="mt-0.5 h-4 w-4" />
                        <span>
                          {address.addressLine}, {address.district} / {address.city}
                          {address.postalCode ? `, ${address.postalCode}` : ''}
                        </span>
                      </p>
                    </div>
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <ConfirmModal
        open={pendingStatus !== null}
        onOpenChange={(open) => {
          if (!open) {
            setPendingStatus(null);
          }
        }}
        title="Kullanıcı durumu güncellensin mi?"
        description={
          pendingStatus === 'Banned'
            ? 'Bu işlem kullanıcının yeni oturum açmasını engeller ve açık refresh tokenlarını iptal eder.'
            : pendingStatus === 'Suspended'
              ? 'Bu işlem kullanıcıyı geçici olarak askıya alır ve açık refresh tokenlarını iptal eder.'
              : 'Bu işlem kullanıcı hesabını yeniden aktif hale getirir.'
        }
        confirmLabel="Durumu Güncelle"
        confirmVariant={pendingStatus === 'Banned' ? 'destructive' : 'default'}
        isLoading={isUpdatingStatus}
        onConfirm={() => void handleStatusUpdate()}
      />
    </div>
  );
}
