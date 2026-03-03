import { useMemo, useState } from 'react';
import { BellRing, Mail, Megaphone, Send, Users } from 'lucide-react';
import { toast } from 'sonner';
import { EmptyState } from '@/components/admin/EmptyState';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Checkbox } from '@/components/common/checkbox';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { ScrollArea } from '@/components/common/scroll-area';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { Textarea } from '@/components/common/textarea';
import {
  useCreateAdminAnnouncementMutation,
  useGetAdminAnnouncementsQuery,
  useGetAdminUsersQuery,
} from '@/features/admin/adminApi';
import type {
  AdminAnnouncement,
  AdminAnnouncementAudienceType,
  AdminAnnouncementChannel,
  AdminAnnouncementStatus,
} from '@/features/admin/types';
import { useDebounce } from '@/hooks/useDebounce';

const audienceOptions: Array<{ value: AdminAnnouncementAudienceType; label: string }> = [
  { value: 'AllUsers', label: 'Tüm Kullanıcılar' },
  { value: 'AllSellers', label: 'Tüm Sellerlar' },
  { value: 'Role', label: 'Belirli Rol' },
  { value: 'SpecificUsers', label: 'Belirli Kullanıcılar' },
];

const roleOptions = ['Admin', 'Customer', 'Seller', 'Support'] as const;
const channelOptions: Array<{ value: AdminAnnouncementChannel; label: string }> = [
  { value: 'InApp', label: 'In-App Bildirim' },
  { value: 'Email', label: 'E-posta' },
];

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

function getStatusTone(status: AdminAnnouncementStatus) {
  switch (status) {
    case 'Sent':
      return 'success' as const;
    case 'PartiallySent':
      return 'warning' as const;
    case 'Failed':
      return 'danger' as const;
    case 'Scheduled':
      return 'info' as const;
    default:
      return 'neutral' as const;
  }
}

function getStatusLabel(status: AdminAnnouncementStatus) {
  switch (status) {
    case 'Scheduled':
      return 'Zamanlandı';
    case 'Processing':
      return 'İşleniyor';
    case 'Sent':
      return 'Gönderildi';
    case 'PartiallySent':
      return 'Kısmen Gönderildi';
    case 'Failed':
      return 'Başarısız';
    default:
      return status;
  }
}

function getAudienceLabel(item: AdminAnnouncement) {
  switch (item.audienceType) {
    case 'AllUsers':
      return 'Tüm Kullanıcılar';
    case 'AllSellers':
      return 'Tüm Sellerlar';
    case 'Role':
      return item.targetRole ? `${item.targetRole} rolü` : 'Belirli Rol';
    case 'SpecificUsers':
      return `${item.targetUserIds.length} seçili kullanıcı`;
    default:
      return item.audienceType;
  }
}

export default function AnnouncementsPage() {
  const [title, setTitle] = useState('');
  const [message, setMessage] = useState('');
  const [audienceType, setAudienceType] = useState<AdminAnnouncementAudienceType>('AllUsers');
  const [targetRole, setTargetRole] = useState<string>('Customer');
  const [channels, setChannels] = useState<AdminAnnouncementChannel[]>(['InApp']);
  const [scheduleEnabled, setScheduleEnabled] = useState(false);
  const [scheduledAt, setScheduledAt] = useState('');
  const [userSearch, setUserSearch] = useState('');
  const [selectedUserIds, setSelectedUserIds] = useState<number[]>([]);

  const debouncedUserSearch = useDebounce(userSearch.trim(), 300);
  const { data: announcements = [], isLoading } = useGetAdminAnnouncementsQuery(25);
  const { data: userOptions } = useGetAdminUsersQuery(
    {
      search: debouncedUserSearch || undefined,
      page: 1,
      pageSize: 12,
      status: 'Active',
    },
    { skip: audienceType !== 'SpecificUsers' }
  );
  const [createAnnouncement, { isLoading: isCreating }] = useCreateAdminAnnouncementMutation();

  const selectableUsers = userOptions?.items ?? [];

  const stats = useMemo(() => ({
    total: announcements.length,
    scheduled: announcements.filter((item) => item.status === 'Scheduled').length,
    sent: announcements.filter((item) => item.status === 'Sent' || item.status === 'PartiallySent').length,
    reached: announcements.reduce((sum, item) => sum + item.deliveredCount, 0),
  }), [announcements]);

  const toggleChannel = (channel: AdminAnnouncementChannel) => {
    setChannels((current) =>
      current.includes(channel)
        ? current.filter((item) => item !== channel)
        : [...current, channel]
    );
  };

  const toggleUser = (userId: number) => {
    setSelectedUserIds((current) =>
      current.includes(userId)
        ? current.filter((item) => item !== userId)
        : [...current, userId]
    );
  };

  const resetForm = () => {
    setTitle('');
    setMessage('');
    setAudienceType('AllUsers');
    setTargetRole('Customer');
    setChannels(['InApp']);
    setScheduleEnabled(false);
    setScheduledAt('');
    setUserSearch('');
    setSelectedUserIds([]);
  };

  const handleSubmit = async () => {
    if (!title.trim() || !message.trim()) {
      toast.error('Başlık ve mesaj zorunludur.');
      return;
    }

    if (channels.length === 0) {
      toast.error('En az bir kanal seçmelisiniz.');
      return;
    }

    if (audienceType === 'SpecificUsers' && selectedUserIds.length === 0) {
      toast.error('Belirli kullanıcılar için en az bir kullanıcı seçmelisiniz.');
      return;
    }

    if (audienceType === 'Role' && !targetRole) {
      toast.error('Rol seçimi zorunludur.');
      return;
    }

    try {
      await createAnnouncement({
        title: title.trim(),
        message: message.trim(),
        audienceType,
        targetRole: audienceType === 'Role' ? targetRole : undefined,
        targetUserIds: audienceType === 'SpecificUsers' ? selectedUserIds : [],
        channels,
        scheduledAt: scheduleEnabled && scheduledAt ? new Date(scheduledAt).toISOString() : null,
      }).unwrap();

      toast.success(scheduleEnabled ? 'Duyuru zamanlandı.' : 'Duyuru gönderildi.');
      resetForm();
    } catch (error) {
      toast.error(getErrorMessage(error, 'Duyuru gönderilemedi.'));
    }
  };

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Duyuru Gönder</h1>
        <p className="max-w-3xl text-muted-foreground">
          Yönetim panelinden hedef kitle seçerek in-app veya e-posta duyuruları gönderin, zamanlayın ve geçmiş gönderimleri izleyin.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Toplam Duyuru"
          value={stats.total.toLocaleString('tr-TR')}
          helperText="Listelenen son gönderimler."
          icon={Megaphone}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Zamanlanan"
          value={stats.scheduled.toLocaleString('tr-TR')}
          helperText="İleri tarihe planlanan duyurular."
          icon={BellRing}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="Gönderilen"
          value={stats.sent.toLocaleString('tr-TR')}
          helperText="Tamamlanan veya kısmen tamamlanan gönderimler."
          icon={Send}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Ulaşılan Kişi"
          value={stats.reached.toLocaleString('tr-TR')}
          helperText="En az bir kanaldan teslim edilen toplam alıcı."
          icon={Users}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Yeni Duyuru</CardTitle>
            <CardDescription>Hedef kitleyi, kanalları ve isterseniz gönderim zamanını belirleyin.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="announcement-title">Başlık</Label>
              <Input
                id="announcement-title"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                placeholder="Örn. Bayram kargo planı güncellendi"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="announcement-message">Mesaj</Label>
              <Textarea
                id="announcement-message"
                value={message}
                onChange={(event) => setMessage(event.target.value)}
                rows={6}
                placeholder="Duyuru içeriğini yazın..."
              />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>Hedef Kitle</Label>
                <Select value={audienceType} onValueChange={(value) => setAudienceType(value as AdminAnnouncementAudienceType)}>
                  <SelectTrigger>
                    <SelectValue placeholder="Kitle seçin" />
                  </SelectTrigger>
                  <SelectContent>
                    {audienceOptions.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {audienceType === 'Role' ? (
                <div className="space-y-2">
                  <Label>Rol</Label>
                  <Select value={targetRole} onValueChange={setTargetRole}>
                    <SelectTrigger>
                      <SelectValue placeholder="Rol seçin" />
                    </SelectTrigger>
                    <SelectContent>
                      {roleOptions.map((role) => (
                        <SelectItem key={role} value={role}>
                          {role}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ) : null}
            </div>

            <div className="space-y-3">
              <Label>Gönderim Kanalları</Label>
              <div className="grid gap-3 md:grid-cols-2">
                {channelOptions.map((channel) => (
                  <label
                    key={channel.value}
                    className="flex items-center gap-3 rounded-xl border border-border/70 px-4 py-3"
                  >
                    <Checkbox
                      checked={channels.includes(channel.value)}
                      onCheckedChange={() => toggleChannel(channel.value)}
                    />
                    <span className="text-sm font-medium">{channel.label}</span>
                  </label>
                ))}
              </div>
            </div>

            {audienceType === 'SpecificUsers' ? (
              <div className="space-y-3">
                <div className="space-y-2">
                  <Label>Kullanıcı Ara</Label>
                  <Input
                    value={userSearch}
                    onChange={(event) => setUserSearch(event.target.value)}
                    placeholder="Ad veya e-posta ara..."
                  />
                </div>
                <ScrollArea className="h-52 rounded-xl border border-border/70">
                  <div className="space-y-2 p-3">
                    {selectableUsers.map((user) => (
                      <label
                        key={user.id}
                        className="flex items-start gap-3 rounded-lg border border-border/60 px-3 py-2"
                      >
                        <Checkbox
                          checked={selectedUserIds.includes(user.id)}
                          onCheckedChange={() => toggleUser(user.id)}
                        />
                        <div className="space-y-0.5">
                          <p className="text-sm font-medium">{user.fullName}</p>
                          <p className="text-xs text-muted-foreground">{user.email}</p>
                        </div>
                      </label>
                    ))}
                    {selectableUsers.length === 0 ? (
                      <p className="px-1 py-4 text-sm text-muted-foreground">
                        Aramaya uygun kullanıcı bulunamadı.
                      </p>
                    ) : null}
                  </div>
                </ScrollArea>
                <p className="text-sm text-muted-foreground">
                  Seçili kullanıcı sayısı: {selectedUserIds.length}
                </p>
              </div>
            ) : null}

            <div className="space-y-3 rounded-xl border border-border/70 p-4">
              <label className="flex items-center gap-3">
                <Checkbox checked={scheduleEnabled} onCheckedChange={(checked) => setScheduleEnabled(Boolean(checked))} />
                <span className="text-sm font-medium">Hemen gönderme, zamanla</span>
              </label>

              {scheduleEnabled ? (
                <div className="space-y-2">
                  <Label htmlFor="announcement-schedule">Gönderim Zamanı</Label>
                  <Input
                    id="announcement-schedule"
                    type="datetime-local"
                    value={scheduledAt}
                    onChange={(event) => setScheduledAt(event.target.value)}
                  />
                </div>
              ) : null}
            </div>

            <div className="flex justify-end">
              <Button onClick={handleSubmit} disabled={isCreating}>
                <Megaphone className="mr-2 h-4 w-4" />
                {scheduleEnabled ? 'Duyuruyu Zamanla' : 'Duyuruyu Gönder'}
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Son Gönderimler</CardTitle>
            <CardDescription>Durum, kanal ve erişim özetlerini tek ekranda takip edin.</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 5 }).map((_, index) => (
                  <Skeleton key={index} className="h-16 rounded-xl" />
                ))}
              </div>
            ) : announcements.length === 0 ? (
              <EmptyState
                icon={Megaphone}
                title="Henüz duyuru geçmişi yok"
                description="İlk gönderimi yaptığınızda duyuru geçmişi, durum ve erişim istatistikleri burada görünecek."
                className="border-dashed"
              />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Başlık</TableHead>
                    <TableHead>Hedef</TableHead>
                    <TableHead>Kanallar</TableHead>
                    <TableHead>Durum</TableHead>
                    <TableHead>Ulaşım</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {announcements.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>
                        <div className="space-y-1">
                          <p className="font-medium">{item.title}</p>
                          <p className="line-clamp-2 text-xs text-muted-foreground">{item.message}</p>
                          <p className="text-xs text-muted-foreground">
                            {item.createdByName} • {formatDate(item.createdAt)}
                          </p>
                        </div>
                      </TableCell>
                      <TableCell className="text-sm">{getAudienceLabel(item)}</TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-2">
                          {item.channels.map((channel) => (
                            <Badge key={channel} variant="outline">
                              {channel === 'Email' ? (
                                <Mail className="mr-1 h-3.5 w-3.5" />
                              ) : (
                                <BellRing className="mr-1 h-3.5 w-3.5" />
                              )}
                              {channel}
                            </Badge>
                          ))}
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="space-y-1">
                          <StatusBadge label={getStatusLabel(item.status)} tone={getStatusTone(item.status)} />
                          <p className="text-xs text-muted-foreground">
                            {item.scheduledAt ? `Plan: ${formatDate(item.scheduledAt)}` : `Gönderim: ${formatDate(item.sentAt)}`}
                          </p>
                        </div>
                      </TableCell>
                      <TableCell className="text-sm">
                        <div className="space-y-1">
                          <p>{item.deliveredCount} / {item.recipientCount} başarılı</p>
                          {item.failedCount > 0 ? (
                            <p className="text-xs text-rose-600 dark:text-rose-300">{item.failedCount} başarısız</p>
                          ) : null}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
