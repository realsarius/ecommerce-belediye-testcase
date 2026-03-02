import { useMemo, useState } from 'react';
import { BellRing, Mail, Smartphone } from 'lucide-react';
import { toast } from 'sonner';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Textarea } from '@/components/common/textarea';
import {
  useGetAdminNotificationTemplatesQuery,
  useUpdateAdminNotificationTemplateMutation,
} from '@/features/admin/adminApi';
import type { NotificationTemplate } from '@/features/notifications/types';

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

export default function NotificationTemplatesAdmin() {
  const { data, isLoading } = useGetAdminNotificationTemplatesQuery();
  const [updateTemplate, { isLoading: isSaving }] = useUpdateAdminNotificationTemplateMutation();
  const [drafts, setDrafts] = useState<Record<string, NotificationTemplate>>({});

  const templates = useMemo(
    () => (data ?? []).map((template) => drafts[template.type] ?? template),
    [data, drafts]
  );

  const updateDraft = (type: string, patch: Partial<NotificationTemplate>) => {
    setDrafts((current) => {
      const source = current[type] ?? data?.find((item) => item.type === type);
      if (!source) return current;

      return {
        ...current,
        [type]: { ...source, ...patch },
      };
    });
  };

  const hasChanges = (type: string) => {
    const source = data?.find((item) => item.type === type);
    const draft = drafts[type];
    if (!source || !draft) return false;

    return JSON.stringify(source) !== JSON.stringify(draft);
  };

  const handleSave = async (template: NotificationTemplate) => {
    try {
      await updateTemplate({
        type: template.type,
        data: {
          displayName: template.displayName,
          description: template.description,
          titleExample: template.titleExample,
          bodyExample: template.bodyExample,
        },
      }).unwrap();
      setDrafts((current) => {
        const next = { ...current };
        delete next[template.type];
        return next;
      });
      toast.success('Bildirim şablonu güncellendi.');
    } catch (error) {
      toast.error(getErrorMessage(error, 'Bildirim şablonu güncellenemedi.'));
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Bildirim Şablonları</h1>
        <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
          Kullanıcı tercih ekranında ve ürün içinde görünen bildirim şablonlarını buradan yönetebilirsiniz.
        </p>
      </div>

      {isLoading ? (
        <div className="grid gap-6 xl:grid-cols-2">
          {Array.from({ length: 4 }).map((_, index) => (
            <Card key={index}>
              <CardHeader>
                <div className="h-5 w-40 animate-pulse rounded bg-muted" />
                <div className="h-4 w-full animate-pulse rounded bg-muted" />
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="h-10 animate-pulse rounded bg-muted" />
                <div className="h-20 animate-pulse rounded bg-muted" />
                <div className="h-20 animate-pulse rounded bg-muted" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <div className="grid gap-6 xl:grid-cols-2">
          {templates.map((template) => (
            <Card key={template.type}>
              <CardHeader>
                <div className="flex flex-wrap items-center gap-2">
                  <CardTitle>{template.displayName}</CardTitle>
                  <Badge variant="secondary">{template.type}</Badge>
                </div>
                <CardDescription>
                  Bu şablonun görünen adı, açıklaması ve örnek bildirim metinleri yönetilir.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
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

                <div className="space-y-2">
                  <Label>Görünen ad</Label>
                  <Input
                    value={template.displayName}
                    onChange={(event) => updateDraft(template.type, { displayName: event.target.value })}
                  />
                </div>

                <div className="space-y-2">
                  <Label>Açıklama</Label>
                  <Textarea
                    value={template.description}
                    onChange={(event) => updateDraft(template.type, { description: event.target.value })}
                    rows={3}
                  />
                </div>

                <div className="space-y-2">
                  <Label>Örnek başlık</Label>
                  <Input
                    value={template.titleExample}
                    onChange={(event) => updateDraft(template.type, { titleExample: event.target.value })}
                  />
                </div>

                <div className="space-y-2">
                  <Label>Örnek içerik</Label>
                  <Textarea
                    value={template.bodyExample}
                    onChange={(event) => updateDraft(template.type, { bodyExample: event.target.value })}
                    rows={4}
                  />
                </div>

                <div className="flex items-center justify-between rounded-xl border border-dashed border-border/70 bg-muted/20 p-4">
                  <div>
                    <p className="text-sm font-medium text-foreground">Canlı önizleme</p>
                    <p className="mt-2 text-sm font-medium text-foreground">{template.titleExample}</p>
                    <p className="mt-1 text-sm text-muted-foreground">{template.bodyExample}</p>
                  </div>
                  <Button
                    onClick={() => handleSave(template)}
                    disabled={!hasChanges(template.type) || isSaving}
                  >
                    Kaydet
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
