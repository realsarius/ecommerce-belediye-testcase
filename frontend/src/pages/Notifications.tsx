import { Bell, BellRing, CheckCheck, ExternalLink } from 'lucide-react';
import { useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import {
  useGetNotificationsQuery,
  useGetUnreadNotificationCountQuery,
  useMarkAllNotificationsAsReadMutation,
  useMarkNotificationAsReadMutation,
} from '@/features/notifications/notificationsApi';
import type { NotificationItem } from '@/features/notifications/types';

function getErrorMessage(error: unknown, fallback: string) {
  if (
    typeof error === 'object' &&
    error !== null &&
    'data' in error &&
    typeof (error as { data?: { message?: string } }).data?.message === 'string'
  ) {
    return (error as { data?: { message?: string } }).data!.message!;
  }

  return fallback;
}

function formatNotificationDate(value: string) {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function getNotificationBadge(type: NotificationItem['type']) {
  switch (type) {
    case 'Refund':
      return 'İade';
    case 'Wishlist':
      return 'Wishlist';
    case 'Support':
      return 'Destek';
    case 'Campaign':
      return 'Kampanya';
    case 'Order':
      return 'Sipariş';
    default:
      return 'Bildirim';
  }
}

export default function Notifications() {
  const navigate = useNavigate();
  const { data: notifications = [], isLoading, isFetching } = useGetNotificationsQuery({ take: 50 });
  const { data: unreadInfo } = useGetUnreadNotificationCountQuery();
  const [markAsRead, { isLoading: isMarkingSingle }] = useMarkNotificationAsReadMutation();
  const [markAllAsRead, { isLoading: isMarkingAll }] = useMarkAllNotificationsAsReadMutation();

  const unreadCount = unreadInfo?.unreadCount ?? 0;
  const unreadNotifications = useMemo(
    () => notifications.filter((notification) => !notification.isRead).length,
    [notifications]
  );

  const handleOpenNotification = async (notification: NotificationItem) => {
    try {
      if (!notification.isRead) {
        await markAsRead(notification.id).unwrap();
      }

      if (notification.deepLink) {
        navigate(notification.deepLink);
      }
    } catch (error) {
      toast.error(getErrorMessage(error, 'Bildirim açılamadı.'));
    }
  };

  const handleMarkAll = async () => {
    if (unreadCount === 0) {
      toast.info('Okunmamış bildirim bulunmuyor.');
      return;
    }

    try {
      await markAllAsRead().unwrap();
      toast.success('Tüm bildirimler okundu olarak işaretlendi.');
    } catch (error) {
      toast.error(getErrorMessage(error, 'Bildirimler güncellenemedi.'));
    }
  };

  return (
    <div className="container mx-auto px-4 py-10">
      <div className="mb-8 flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div>
          <div className="mb-3 flex items-center gap-3">
            <Badge variant="outline" className="rounded-full px-3 py-1 text-xs tracking-wide">
              Bildirim Merkezi
            </Badge>
            <span className="text-sm text-muted-foreground">
              {unreadCount} okunmamış bildirim
            </span>
          </div>
          <h1 className="text-3xl font-semibold tracking-tight">Bildirimler</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Wishlist, iade ve diğer kritik akışlardaki gelişmeleri tek yerde takip edin.
          </p>
        </div>

        <Button onClick={handleMarkAll} disabled={isMarkingAll || unreadCount === 0}>
          <CheckCheck className="mr-2 h-4 w-4" />
          Tümünü okundu işaretle
        </Button>
      </div>

      <div className="mb-6 grid gap-4 md:grid-cols-3">
        <Card className="border-border/60 bg-background/70">
          <CardHeader className="pb-3">
            <CardDescription>Toplam</CardDescription>
            <CardTitle className="text-2xl">{notifications.length}</CardTitle>
          </CardHeader>
        </Card>
        <Card className="border-border/60 bg-background/70">
          <CardHeader className="pb-3">
            <CardDescription>Okunmamış</CardDescription>
            <CardTitle className="text-2xl">{unreadCount}</CardTitle>
          </CardHeader>
        </Card>
        <Card className="border-border/60 bg-background/70">
          <CardHeader className="pb-3">
            <CardDescription>Bu ekranda açık</CardDescription>
            <CardTitle className="text-2xl">{unreadNotifications}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {isLoading || isFetching ? (
        <div className="grid gap-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Card key={index} className="border-border/60 bg-background/60">
              <CardHeader>
                <div className="h-4 w-32 animate-pulse rounded bg-muted" />
                <div className="h-4 w-48 animate-pulse rounded bg-muted" />
              </CardHeader>
              <CardContent>
                <div className="h-4 w-full animate-pulse rounded bg-muted" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <Card className="border-dashed border-border/60 bg-background/60">
          <CardContent className="flex flex-col items-center justify-center px-6 py-16 text-center">
            <Bell className="mb-4 h-10 w-10 text-muted-foreground" />
            <h2 className="text-xl font-semibold">Henüz bildiriminiz yok</h2>
            <p className="mt-2 max-w-md text-sm text-muted-foreground">
              Fiyat alarmları, stok düşüşleri ve iade güncellemeleri burada görünecek.
            </p>
            <Button asChild variant="outline" className="mt-6">
              <Link to="/wishlist">Wishlist&apos;e dön</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4">
          {notifications.map((notification) => (
            <Card
              key={notification.id}
              className={notification.isRead
                ? 'border-border/60 bg-background/60'
                : 'border-primary/40 bg-primary/5'}
            >
              <CardHeader className="gap-3 md:flex-row md:items-start md:justify-between">
                <div className="space-y-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant={notification.isRead ? 'outline' : 'default'}>
                      {getNotificationBadge(notification.type)}
                    </Badge>
                    {!notification.isRead && (
                      <Badge variant="secondary" className="gap-1">
                        <BellRing className="h-3.5 w-3.5" />
                        Yeni
                      </Badge>
                    )}
                    <span className="text-xs text-muted-foreground">
                      {formatNotificationDate(notification.createdAt)}
                    </span>
                  </div>
                  <div>
                    <CardTitle className="text-lg">{notification.title}</CardTitle>
                    <CardDescription className="mt-2 text-sm leading-6">
                      {notification.body}
                    </CardDescription>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  {!notification.isRead && (
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={isMarkingSingle}
                      onClick={() => markAsRead(notification.id).unwrap().catch((error) => {
                        toast.error(getErrorMessage(error, 'Bildirim güncellenemedi.'));
                      })}
                    >
                      Okundu
                    </Button>
                  )}
                  {notification.deepLink && (
                    <Button size="sm" onClick={() => handleOpenNotification(notification)}>
                      <ExternalLink className="mr-2 h-4 w-4" />
                      Aç
                    </Button>
                  )}
                </div>
              </CardHeader>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
