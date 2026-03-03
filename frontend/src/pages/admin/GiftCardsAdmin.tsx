import { useState } from 'react';
import {
  useCreateGiftCardMutation,
  useGetGiftCardsQuery,
  useUpdateGiftCardMutation,
} from '@/features/giftCards/giftCardsApi';
import type { CreateGiftCardRequest, GiftCard, UpdateGiftCardRequest } from '@/features/giftCards/types';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Checkbox } from '@/components/common/checkbox';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { Skeleton } from '@/components/common/skeleton';
import { EmptyState } from '@/components/admin/EmptyState';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Gift, Plus, Pencil } from 'lucide-react';
import { toast } from 'sonner';

export default function GiftCardsAdmin() {
  const [dialog, setDialog] = useState<{
    open: boolean;
    mode: 'create' | 'edit';
    giftCard?: GiftCard;
  }>({ open: false, mode: 'create' });
  const [formData, setFormData] = useState<CreateGiftCardRequest & { isActive?: boolean }>({
    code: '',
    initialBalance: 250,
    validDays: 180,
    description: '',
  });

  const { data: giftCards, isLoading } = useGetGiftCardsQuery();
  const [createGiftCard, { isLoading: isCreating }] = useCreateGiftCardMutation();
  const [updateGiftCard, { isLoading: isUpdating }] = useUpdateGiftCardMutation();

  const resetForm = () => {
    setFormData({
      code: '',
      initialBalance: 250,
      validDays: 180,
      description: '',
      isActive: true,
    });
  };

  const openCreateDialog = () => {
    resetForm();
    setDialog({ open: true, mode: 'create' });
  };

  const openEditDialog = (giftCard: GiftCard) => {
    setFormData({
      code: giftCard.code,
      initialBalance: giftCard.initialBalance,
      expiresAt: giftCard.expiresAt ?? undefined,
      description: giftCard.description ?? '',
      isActive: giftCard.isActive,
    });
    setDialog({ open: true, mode: 'edit', giftCard });
  };

  const handleSubmit = async () => {
    if (dialog.mode === 'create') {
      if ((formData.initialBalance ?? 0) <= 0) {
        toast.error("Gift card bakiyesi 0'dan büyük olmalıdır");
        return;
      }

      try {
        await createGiftCard({
          code: formData.code || undefined,
          initialBalance: Number(formData.initialBalance),
          validDays: formData.validDays,
          description: formData.description,
        }).unwrap();
        toast.success('Gift card oluşturuldu');
        setDialog({ open: false, mode: 'create' });
      } catch {
        toast.error('Gift card oluşturulamadı');
      }

      return;
    }

    if (!dialog.giftCard) {
      return;
    }

    try {
      const payload: UpdateGiftCardRequest = {
        isActive: formData.isActive,
        expiresAt: formData.expiresAt,
        description: formData.description,
      };
      await updateGiftCard({ id: dialog.giftCard.id, data: payload }).unwrap();
      toast.success('Gift card güncellendi');
      setDialog({ open: false, mode: 'create' });
    } catch {
      toast.error('Gift card güncellenemedi');
    }
  };

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-3xl font-bold">Gift Cardlar</h1>
        <Button onClick={openCreateDialog}>
          <Plus className="mr-2 h-4 w-4" />
          Yeni Gift Card
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Kod</TableHead>
                <TableHead>Bakiye</TableHead>
                <TableHead>Sahip</TableHead>
                <TableHead>Son Tarih</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead className="text-right">İşlemler</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {giftCards?.map((giftCard) => (
                <TableRow key={giftCard.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-500/10">
                        <Gift className="h-5 w-5 text-emerald-600" />
                      </div>
                      <div>
                        <p className="font-mono font-bold">{giftCard.code}</p>
                        {giftCard.description ? (
                          <p className="text-xs text-muted-foreground">{giftCard.description}</p>
                        ) : null}
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <p className="font-semibold text-primary">{giftCard.currentBalance.toLocaleString('tr-TR')} ₺</p>
                    <p className="text-xs text-muted-foreground">İlk: {giftCard.initialBalance.toLocaleString('tr-TR')} ₺</p>
                  </TableCell>
                  <TableCell>
                    {giftCard.assignedUserEmail || <span className="text-muted-foreground">Henüz atanmadı</span>}
                  </TableCell>
                  <TableCell>
                    {giftCard.expiresAt ? new Date(giftCard.expiresAt).toLocaleDateString('tr-TR') : 'Süresiz'}
                  </TableCell>
                  <TableCell>
                    <StatusBadge label={giftCard.isActive ? 'Aktif' : 'Pasif'} tone={giftCard.isActive ? 'success' : 'neutral'} />
                  </TableCell>
                  <TableCell className="text-right">
                    <Button variant="ghost" size="icon" onClick={() => openEditDialog(giftCard)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {giftCards?.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="p-0">
                    <EmptyState
                      icon={Gift}
                      title="Henüz gift card oluşturulmadı"
                      description="İlk gift card kaydını oluşturduğunuzda bakiye ve atama bilgileri bu tabloda görünecek."
                      className="border-0 shadow-none"
                    />
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </div>
      )}

      <Dialog open={dialog.open} onOpenChange={(open) => setDialog((current) => ({ ...current, open }))}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{dialog.mode === 'create' ? 'Yeni Gift Card' : 'Gift Card Düzenle'}</DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Kod</Label>
              <Input
                value={formData.code ?? ''}
                onChange={(e) => setFormData((current) => ({ ...current, code: e.target.value.toUpperCase() }))}
                placeholder="Boş bırakırsan otomatik üretilir"
                disabled={dialog.mode === 'edit'}
              />
            </div>

            {dialog.mode === 'create' ? (
              <>
                <div className="space-y-2">
                  <Label>İlk Bakiye</Label>
                  <Input
                    type="number"
                    min={1}
                    value={formData.initialBalance}
                    onChange={(e) =>
                      setFormData((current) => ({ ...current, initialBalance: Number(e.target.value) }))
                    }
                  />
                </div>
                <div className="space-y-2">
                  <Label>Geçerlilik (gün)</Label>
                  <Input
                    type="number"
                    min={1}
                    value={formData.validDays ?? ''}
                    onChange={(e) =>
                      setFormData((current) => ({ ...current, validDays: e.target.value ? Number(e.target.value) : undefined }))
                    }
                  />
                </div>
              </>
            ) : (
              <>
                <div className="space-y-2">
                  <Label>Son Kullanım Tarihi</Label>
                  <Input
                    type="datetime-local"
                    value={formData.expiresAt ? new Date(formData.expiresAt).toISOString().slice(0, 16) : ''}
                    onChange={(e) =>
                      setFormData((current) => ({
                        ...current,
                        expiresAt: e.target.value ? new Date(e.target.value).toISOString() : undefined,
                      }))
                    }
                  />
                </div>
                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="gift-card-active"
                    checked={Boolean(formData.isActive)}
                    onCheckedChange={(checked) =>
                      setFormData((current) => ({ ...current, isActive: Boolean(checked) }))
                    }
                  />
                  <Label htmlFor="gift-card-active">Aktif</Label>
                </div>
              </>
            )}

            <div className="space-y-2">
              <Label>Açıklama</Label>
              <Input
                value={formData.description ?? ''}
                onChange={(e) => setFormData((current) => ({ ...current, description: e.target.value }))}
                placeholder="Kurumsal hediye, kampanya vb."
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setDialog({ open: false, mode: 'create' })}>
              İptal
            </Button>
            <Button onClick={handleSubmit} disabled={isCreating || isUpdating}>
              Kaydet
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
