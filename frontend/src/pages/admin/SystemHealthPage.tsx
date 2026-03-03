import {
  AlertTriangle,
  CheckCircle2,
  Clock3,
  Database,
  Activity,
  Rabbit,
  SearchCheck,
  ServerCrash,
} from 'lucide-react';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { DataTable } from '@/components/admin/DataTable';
import { EmptyState } from '@/components/admin/EmptyState';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
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
  useGetAdminErrorLogsQuery,
  useGetAdminSystemHealthQuery,
} from '@/features/admin/adminApi';
import type { AdminServiceHealth, AdminServiceStatus } from '@/features/admin/types';
import { formatDateTime } from '@/lib/format';

function getStatusTone(status: AdminServiceStatus) {
  switch (status) {
    case 'Healthy':
      return 'success' as const;
    case 'Degraded':
      return 'warning' as const;
    case 'Unhealthy':
      return 'danger' as const;
    default:
      return 'neutral' as const;
  }
}

function getStatusLabel(status: AdminServiceStatus) {
  switch (status) {
    case 'Healthy':
      return 'Sağlıklı';
    case 'Degraded':
      return 'Yavaşladı';
    case 'Unhealthy':
      return 'Erişilemiyor';
    default:
      return status;
  }
}

function getServiceIcon(name: string) {
  switch (name) {
    case 'PostgreSQL':
      return Database;
    case 'Redis':
      return Activity;
    case 'RabbitMQ':
      return Rabbit;
    case 'Elasticsearch':
      return SearchCheck;
    default:
      return CheckCircle2;
  }
}

function getServiceSurface(status: AdminServiceStatus) {
  switch (status) {
    case 'Healthy':
      return {
        accentClass: 'text-emerald-600 dark:text-emerald-300',
        surfaceClass: 'bg-emerald-500/10',
      };
    case 'Degraded':
      return {
        accentClass: 'text-amber-600 dark:text-amber-300',
        surfaceClass: 'bg-amber-500/10',
      };
    case 'Unhealthy':
      return {
        accentClass: 'text-rose-600 dark:text-rose-300',
        surfaceClass: 'bg-rose-500/10',
      };
    default:
      return {
        accentClass: 'text-slate-600 dark:text-slate-300',
        surfaceClass: 'bg-slate-500/10',
      };
  }
}

export default function SystemHealthPage() {
  const { data: health, isLoading: healthLoading } = useGetAdminSystemHealthQuery(undefined, {
    pollingInterval: 60000,
  });
  const { data: errorLogs = [], isLoading: logsLoading } = useGetAdminErrorLogsQuery(20, {
    pollingInterval: 60000,
  });

  const grafanaUrl = (import.meta as ImportMeta & { env: Record<string, string | undefined> }).env.VITE_GRAFANA_ADMIN_URL;
  const isLoading = healthLoading || logsLoading;
  const services = health?.services ?? [];
  const hangfire = health?.hangfire;

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-36 rounded-xl" />
          ))}
        </div>
        <Skeleton className="h-[320px] rounded-xl" />
        <Skeleton className="h-[320px] rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Sistem Sağlığı</h1>
        <p className="max-w-3xl text-muted-foreground">
          PostgreSQL, Redis, RabbitMQ, Elasticsearch ve Hangfire özetini tek ekranda izleyin.
          Veriler 60 saniyede bir otomatik yenilenir.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {services.map((service) => {
          const Icon = getServiceIcon(service.name);
          const surface = getServiceSurface(service.status);

          return (
            <KpiCard
              key={service.name}
              title={service.name}
              value={getStatusLabel(service.status)}
              helperText={service.responseTimeMs != null
                ? `${service.description} • ${service.responseTimeMs} ms`
                : service.description}
              icon={Icon}
              accentClass={surface.accentClass}
              surfaceClass={surface.surfaceClass}
            />
          );
        })}
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Servis Durumları</CardTitle>
            <CardDescription>
              Son kontrol zamanı: {formatDateTime(health?.generatedAt)}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {services.map((service: AdminServiceHealth) => (
              <div key={service.name} className="flex flex-col gap-3 rounded-2xl border border-border/70 p-4 md:flex-row md:items-center md:justify-between">
                <div className="space-y-1">
                  <div className="flex items-center gap-3">
                    <p className="font-medium">{service.name}</p>
                    <StatusBadge
                      label={getStatusLabel(service.status)}
                      tone={getStatusTone(service.status)}
                    />
                  </div>
                  <p className="text-sm text-muted-foreground">{service.description}</p>
                </div>
                <div className="text-sm text-muted-foreground">
                  {service.responseTimeMs != null ? `${service.responseTimeMs} ms` : '-'}
                </div>
              </div>
            ))}
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Hangfire Özeti</CardTitle>
            <CardDescription>
              {hangfire?.enabled
                ? 'İş kuyruğu ve son başarısız job kayıtları.'
                : 'Hangfire bu ortamda kapalı veya izlenemiyor.'}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <KpiCard
                title="İşlenen"
                value={String(hangfire?.processingCount ?? 0)}
                helperText="Şu anda çalışan job sayısı."
                icon={Clock3}
                accentClass="text-sky-600 dark:text-sky-300"
                surfaceClass="bg-sky-500/10"
              />
              <KpiCard
                title="Başarısız"
                value={String(hangfire?.failedCount ?? 0)}
                helperText="Monitoring API üzerinden gelen toplam failed job."
                icon={ServerCrash}
                accentClass="text-rose-600 dark:text-rose-300"
                surfaceClass="bg-rose-500/10"
              />
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <KpiCard
                title="Sıradaki"
                value={String(hangfire?.enqueuedCount ?? 0)}
                helperText="Kuyrukta bekleyen işler."
                icon={Activity}
                accentClass="text-amber-600 dark:text-amber-300"
                surfaceClass="bg-amber-500/10"
              />
              <KpiCard
                title="Planlanan"
                value={String(hangfire?.scheduledCount ?? 0)}
                helperText="İleri tarihe planlanan işler."
                icon={Clock3}
                accentClass="text-violet-600 dark:text-violet-300"
                surfaceClass="bg-violet-500/10"
              />
            </div>
          </CardContent>
        </Card>
      </div>

      <DataTable
        title="Son Başarısız Job Kayıtları"
        description="Hangfire monitoring verisinden gelen son başarısız işler."
      >
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Job ID</TableHead>
              <TableHead>Hata Tipi</TableHead>
              <TableHead>Neden</TableHead>
              <TableHead>Zaman</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {hangfire?.failedJobs.map((job) => (
              <TableRow key={job.id}>
                <TableCell className="font-medium">{job.id}</TableCell>
                <TableCell>{job.exceptionType ?? '-'}</TableCell>
                <TableCell className="max-w-[28rem] truncate">{job.reason ?? job.exceptionMessage ?? '-'}</TableCell>
                <TableCell>{formatDateTime(job.failedAt)}</TableCell>
              </TableRow>
            ))}
            {(!hangfire || hangfire.failedJobs.length === 0) ? (
              <TableRow>
                <TableCell colSpan={4} className="p-0">
                  <EmptyState
                    icon={CheckCircle2}
                    title="Başarısız job bulunmuyor"
                    description="İzlenen aralıkta Hangfire tarafında hatalı iş kaydı görünmüyor."
                    className="border-0 shadow-none"
                  />
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </DataTable>

      <DataTable
        title="Son Hata Logları"
        description="Elasticsearch üzerinden çekilen son error ve fatal log kayıtları."
      >
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Zaman</TableHead>
              <TableHead>Seviye</TableHead>
              <TableHead>Mesaj</TableHead>
              <TableHead>Korelasyon</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {errorLogs.map((log, index) => (
              <TableRow key={`${log.timestamp ?? 'no-ts'}-${index}`}>
                <TableCell>{formatDateTime(log.timestamp)}</TableCell>
                <TableCell>
                  <StatusBadge
                    label={log.level}
                    tone={log.level === 'Fatal' ? 'danger' : 'warning'}
                  />
                </TableCell>
                <TableCell className="max-w-[36rem]">
                  <div className="space-y-1">
                    <p className="line-clamp-2">{log.message}</p>
                    {log.exception ? (
                      <p className="line-clamp-2 text-xs text-muted-foreground">{log.exception}</p>
                    ) : null}
                  </div>
                </TableCell>
                <TableCell>{log.correlationId ?? '-'}</TableCell>
              </TableRow>
            ))}
            {errorLogs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="p-0">
                  <EmptyState
                    icon={AlertTriangle}
                    title="Hata logu bulunamadı"
                    description="Elasticsearch erişimi yoksa veya son kayıt yoksa bu alan boş görünür."
                    className="border-0 shadow-none"
                  />
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </DataTable>

      {grafanaUrl ? (
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>Grafana Görünümü</CardTitle>
            <CardDescription>Opsiyonel sistem paneli gömüsü.</CardDescription>
          </CardHeader>
          <CardContent>
            <iframe
              src={grafanaUrl}
              title="Grafana Sistem Paneli"
              className="h-[520px] w-full rounded-2xl border"
            />
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}
