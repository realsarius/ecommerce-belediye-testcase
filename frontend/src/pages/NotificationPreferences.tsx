import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { BellRing, ChevronLeft, Mail, Smartphone } from 'lucide-react';
import { toast } from 'sonner';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import {
  useGetNotificationPreferencesQuery,
  useUpdateNotificationPreferencesMutation,
} from '@/features/notifications/notificationsApi';
import type { NotificationPreference, NotificationTemplate } from '@/features/notifications/types';

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

function ChannelToggle({
  label,
  enabled,
  supported,
  onToggle,
}: {
  label: string;
  enabled: boolean;
  supported: boolean;
  onToggle: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onToggle}
      disabled={!supported}
      className={`rounded-full border px-3 py-1.5 text-sm font-medium transition-colors ${
        supported
          ? enabled
            ? 'border-primary/50 bg-primary/10 text-foreground'
            : 'border-border/70 bg-background/60 text-muted-foreground hover:border-primary/30 hover:text-foreground'
          : 'cursor-not-allowed border-dashed border-border/50 bg-muted/30 text-muted-foreground/70'
      }`}
    >
      {label}
    </button>
  );
}

export default function NotificationPreferences() {
  const { data, isLoading } = useGetNotificationPreferencesQuery();
  const [updatePreferences, { isLoading: isSaving }] = useUpdateNotificationPreferencesMutation();
  const [draft, setDraft] = useState<NotificationPreference[] | null>(null);

  const preferences = useMemo(
    () => draft ?? data?.preferences ?? [],
    [data?.preferences, draft]
  );
  const templates = data?.templates ?? [];

  const updatePreference = (
    type: NotificationPreference['type'],
    patch: Partial<Pick<NotificationPreference, 'inAppEnabled' | 'emailEnabled' | 'pushEnabled'>>
  ) => {
    setDraft((current) => {
      const base = current ?? data?.preferences ?? [];
      return base.map((preference) =>
        preference.type === type
          ? { ...preference, ...patch }
          : preference
      );
    });
  };

  const handleSave = async () => {
    try {
      await updatePreferences({
        preferences: preferences.map((preference) => ({
          type: preference.type,
          inAppEnabled: preference.inAppEnabled,
          emailEnabled: preference.emailEnabled,
          pushEnabled: preference.pushEnabled,
        })),
      }).unwrap();
      setDraft(null);
      toast.success('Bildirim tercihleri güncellendi.');
    } catch (error) {
      toast.error(getErrorMessage(error, 'Tercihler güncellenemedi.'));
    }
  };

  const hasDraftChanges = draft !== null;

  const getTemplate = (type: NotificationPreference['type']) =>
    templates.find((template) => template.type === type);

  return (
    <div className="container mx-auto max-w-6xl px-4 py-10">
      <div className="mb-8 flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div>
          <Button asChild variant="ghost" className="mb-3 -ml-3">
            <Link to="/notifications">
              <ChevronLeft className="mr-2 h-4 w-4" />
              Bildirim merkezine dön
            </Link>
          </Button>
          <div className="mb-3 flex flex-wrap items-center gap-2">
            <Badge variant="outline" className="rounded-full px-3 py-1 text-xs tracking-wide">
              Bildirim Tercihleri
            </Badge>
            <Badge variant="secondary" className="rounded-full px-3 py-1 text-xs">
              Kanal bazlı yönetim
            </Badge>
          </div>
          <h1 className="text-3xl font-semibold tracking-tight">Bildirim tercihleri</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Hangi olaylar için uygulama içi, e-posta veya ileride push bildirimi almak istediğini buradan yönetebilirsin.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            onClick={() => setDraft(data?.preferences ?? [])}
            disabled={!hasDraftChanges || isSaving}
          >
            Değişiklikleri geri al
          </Button>
          <Button onClick={handleSave} disabled={isSaving || !hasDraftChanges}>
            Tercihleri kaydet
          </Button>
        </div>
      </div>

      <Tabs defaultValue="channels" className="gap-6">
        <TabsList>
          <TabsTrigger value="channels">Kanal tercihleri</TabsTrigger>
          <TabsTrigger value="templates">Şablon önizlemeleri</TabsTrigger>
        </TabsList>

        <TabsContent value="channels" className="space-y-4">
          {isLoading ? (
            <div className="grid gap-4 lg:grid-cols-2">
              {Array.from({ length: 4 }).map((_, index) => (
                <Card key={index} className="border-border/60 bg-background/70">
                  <CardHeader>
                    <div className="h-4 w-40 animate-pulse rounded bg-muted" />
                    <div className="h-4 w-full animate-pulse rounded bg-muted" />
                  </CardHeader>
                  <CardContent>
                    <div className="flex gap-2">
                      <div className="h-9 w-24 animate-pulse rounded-full bg-muted" />
                      <div className="h-9 w-24 animate-pulse rounded-full bg-muted" />
                      <div className="h-9 w-24 animate-pulse rounded-full bg-muted" />
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <div className="grid gap-4 lg:grid-cols-2">
              {preferences.map((preference) => {
                const template = getTemplate(preference.type);

                return (
                  <Card key={preference.type} className="border-border/60 bg-background/70">
                    <CardHeader>
                      <div className="flex items-center justify-between gap-3">
                        <div>
                          <CardTitle>{preference.displayName}</CardTitle>
                          <CardDescription className="mt-2 leading-6">
                            {preference.description}
                          </CardDescription>
                        </div>
                        <Badge variant="outline" className="rounded-full">
                          {preference.type}
                        </Badge>
                      </div>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="flex flex-wrap gap-2">
                        <ChannelToggle
                          label="Uygulama içi"
                          enabled={preference.inAppEnabled}
                          supported={preference.supportsInApp}
                          onToggle={() => updatePreference(preference.type, { inAppEnabled: !preference.inAppEnabled })}
                        />
                        <ChannelToggle
                          label="E-posta"
                          enabled={preference.emailEnabled}
                          supported={preference.supportsEmail}
                          onToggle={() => updatePreference(preference.type, { emailEnabled: !preference.emailEnabled })}
                        />
                        <ChannelToggle
                          label="Push"
                          enabled={preference.pushEnabled}
                          supported={preference.supportsPush}
                          onToggle={() => updatePreference(preference.type, { pushEnabled: !preference.pushEnabled })}
                        />
                      </div>

                      {template ? (
                        <div className="rounded-xl border border-dashed border-border/70 bg-muted/20 p-4 text-sm">
                          <p className="font-medium text-foreground">Örnek bildirim</p>
                          <p className="mt-2 text-foreground">{template.titleExample}</p>
                          <p className="mt-1 text-muted-foreground">{template.bodyExample}</p>
                        </div>
                      ) : null}
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          )}
        </TabsContent>

        <TabsContent value="templates" className="space-y-4">
          <div className="grid gap-4 lg:grid-cols-2">
            {templates.map((template: NotificationTemplate) => (
              <Card key={template.type} className="border-border/60 bg-background/70">
                <CardHeader>
                  <div className="flex flex-wrap items-center gap-2">
                    <CardTitle>{template.displayName}</CardTitle>
                    <Badge variant="secondary">{template.type}</Badge>
                  </div>
                  <CardDescription>
                    Tetikleyici olaya göre gönderilen örnek başlık ve içerik yapısı.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="rounded-xl border border-border/70 bg-muted/20 p-4">
                    <p className="text-sm font-medium text-foreground">{template.titleExample}</p>
                    <p className="mt-2 text-sm text-muted-foreground">{template.bodyExample}</p>
                  </div>
                  <div className="flex flex-wrap gap-2 text-sm text-muted-foreground">
                    <Badge variant={template.supportsInApp ? 'default' : 'outline'}>
                      <BellRing className="mr-1 h-3.5 w-3.5" />
                      Uygulama içi
                    </Badge>
                    <Badge variant={template.supportsEmail ? 'default' : 'outline'}>
                      <Mail className="mr-1 h-3.5 w-3.5" />
                      E-posta
                    </Badge>
                    <Badge variant={template.supportsPush ? 'default' : 'outline'}>
                      <Smartphone className="mr-1 h-3.5 w-3.5" />
                      Push
                    </Badge>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
