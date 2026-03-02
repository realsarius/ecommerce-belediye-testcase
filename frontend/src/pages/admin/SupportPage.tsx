import { MessageSquareText, TimerReset, UserCheck, Users } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { KpiCard } from '@/components/admin/KpiCard';
import Support from '@/pages/Support';
import type { SupportConversationStatus } from '@/features/support/types';
import { useGetSupportQueueQuery } from '@/features/support/supportApi';

function formatRelativeDate(value?: string | null) {
  if (!value) {
    return 'Henüz aktivite yok';
  }

  return new Date(value).toLocaleString('tr-TR', {
    day: '2-digit',
    month: 'long',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export default function AdminSupportPage() {
  const [conversationFilter, setConversationFilter] = useState<'All' | SupportConversationStatus>('All');
  const { data: queueData, isLoading } = useGetSupportQueueQuery({ page: 1, pageSize: 50 });
  const conversations = queueData?.items ?? [];
  const openCount = conversations.filter((conversation) => conversation.status === 'Open').length;
  const assignedCount = conversations.filter((conversation) => conversation.status === 'Assigned').length;
  const unassignedCount = conversations.filter((conversation) => !conversation.supportUserId).length;
  const lastActivity = conversations
    .map((conversation) => conversation.lastMessageAt || conversation.createdAt)
    .filter(Boolean)
    .sort((left, right) => new Date(right!).getTime() - new Date(left!).getTime())[0];

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Destek Talepleri</h1>
        <p className="max-w-3xl text-muted-foreground">
          Canlı destek kuyruğunu admin panelinden izleyin, görüşmeleri üstlenin ve SignalR üzerinden anlık yanıt verin.
          Mevcut backend bu aşamada temsilci atama için yalnızca doğrudan kullanıcı kimliği ile çalışma sunuyor.
        </p>
        <p className="text-sm text-muted-foreground">
          Durum sekmeleri gömülü canlı destek alanını filtreler; böylece açık, atanmış ve kapanmış görüşmeleri ayrı ayrı takip edebilirsiniz.
        </p>
      </div>

      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-36 rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <KpiCard
            title="Açık Kuyruk"
            value={openCount.toLocaleString('tr-TR')}
            helperText="Henüz çözülmemiş ve işlem bekleyen görüşmeler."
            icon={MessageSquareText}
            accentClass="text-sky-600 dark:text-sky-300"
            surfaceClass="bg-sky-500/10"
          />
          <KpiCard
            title="Üstlenilen"
            value={assignedCount.toLocaleString('tr-TR')}
            helperText="Bir support kullanıcısına atanmış aktif görüşmeler."
            icon={UserCheck}
            accentClass="text-emerald-600 dark:text-emerald-300"
            surfaceClass="bg-emerald-500/10"
          />
          <KpiCard
            title="Atama Bekleyen"
            value={unassignedCount.toLocaleString('tr-TR')}
            helperText="Henüz kimse tarafından sahiplenilmemiş talepler."
            icon={Users}
            accentClass="text-amber-600 dark:text-amber-300"
            surfaceClass="bg-amber-500/10"
          />
          <KpiCard
            title="Son Aktivite"
            value={formatRelativeDate(lastActivity)}
            helperText="Kuyruktaki en güncel mesaj veya görüşme açılışı."
            icon={TimerReset}
            accentClass="text-violet-600 dark:text-violet-300"
            surfaceClass="bg-violet-500/10"
          />
        </div>
      )}

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Canlı Destek Çalışma Alanı</CardTitle>
          <CardDescription>
            Var olan destek kuyruğu, mesaj akışı ve görüşme üstlenme davranışı admin paneline gömülü olarak çalışır.
          </CardDescription>
        </CardHeader>
        <CardContent className="p-4 md:p-6">
          <Support
            embedded
            showRoleBadge={false}
            conversationFilter={conversationFilter}
            onConversationFilterChange={setConversationFilter}
          />
        </CardContent>
      </Card>
    </div>
  );
}
