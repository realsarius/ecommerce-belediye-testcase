import { useState } from 'react';
import {
  useGetCouponsQuery,
  useCreateCouponMutation,
  useUpdateCouponMutation,
  useDeleteCouponMutation,
} from '@/features/coupons/couponsApi';
import { CouponType, type Coupon, type CreateCouponRequest, type UpdateCouponRequest } from '@/features/coupons/types';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Badge } from '@/components/common/badge';
import { Checkbox } from '@/components/common/checkbox';
import { Label } from '@/components/common/label';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { Skeleton } from '@/components/common/skeleton';
import { Plus, Pencil, Trash2, Ticket, Percent, DollarSign, Wand2 } from 'lucide-react';
import { toast } from 'sonner';
import { useDevTools } from '@/components/common/DevToolsProvider';

export default function CouponsAdmin() {
  const [dialog, setDialog] = useState<{
    open: boolean;
    mode: 'create' | 'edit';
    coupon?: Coupon;
  }>({ open: false, mode: 'create' });
  const [formData, setFormData] = useState<CreateCouponRequest & { isActive?: boolean; expiresAt?: string }>({
    code: '',
    type: CouponType.Percentage,
    value: 10,
    minOrderAmount: undefined,
    usageLimit: 0,
    validDays: 7,
    description: '',
  });

  const { data: coupons, isLoading } = useGetCouponsQuery();
  const [createCoupon, { isLoading: isCreating }] = useCreateCouponMutation();
  const [updateCoupon, { isLoading: isUpdating }] = useUpdateCouponMutation();
  const [deleteCoupon, { isLoading: isDeleting }] = useDeleteCouponMutation();
  const { isDevToolsEnabled } = useDevTools();

  const openCreateDialog = () => {
    setFormData({
      code: '',
      type: CouponType.Percentage,
      value: 10,
      minOrderAmount: undefined,
      usageLimit: 0,
      validDays: 7,
      description: '',
    });
    setDialog({ open: true, mode: 'create' });
  };

  const openEditDialog = (coupon: Coupon) => {
    setFormData({
      code: coupon.code,
      type: coupon.type,
      value: coupon.value,
      minOrderAmount: coupon.minOrderAmount,
      usageLimit: coupon.usageLimit,
      validDays: 7,
      description: coupon.description || '',
      isActive: coupon.isActive,
      expiresAt: coupon.expiresAt,
    });
    setDialog({ open: true, mode: 'edit', coupon });
  };

  const handleSubmit = async () => {
    if (!formData.code.trim()) {
      toast.error('Kupon kodu gereklidir');
      return;
    }
    if (formData.value <= 0) {
      toast.error('İndirim değeri 0\'dan büyük olmalıdır');
      return;
    }

    try {
      if (dialog.mode === 'create') {
        await createCoupon(formData).unwrap();
        toast.success('Kupon oluşturuldu');
      } else if (dialog.coupon) {
        const updateData: UpdateCouponRequest = {
          code: formData.code,
          type: formData.type,
          value: formData.value,
          minOrderAmount: formData.minOrderAmount,
          usageLimit: formData.usageLimit,
          isActive: formData.isActive,
          description: formData.description,
        };
        await updateCoupon({ id: dialog.coupon.id, data: updateData }).unwrap();
        toast.success('Kupon güncellendi');
      }
      setDialog({ open: false, mode: 'create' });
    } catch {
      toast.error(dialog.mode === 'create' ? 'Kupon oluşturulamadı' : 'Kupon güncellenemedi');
    }
  };

  const handleDelete = async (id: number, code: string) => {
    if (!confirm(`"${code}" kuponunu silmek istediğinize emin misiniz?`)) return;
    try {
      await deleteCoupon(id).unwrap();
      toast.success('Kupon silindi');
    } catch {
      toast.error('Kupon silinemedi');
    }
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('tr-TR');
  };

  const isExpired = (expiresAt: string) => {
    return new Date(expiresAt) < new Date();
  };

  const fillSampleData = () => {
    setFormData({
      code: 'YILBASI20',
      type: CouponType.Percentage,
      value: 20,
      minOrderAmount: 500,
      usageLimit: 1,
      validDays: 7,
      description: 'Yılbaşı Kampanyası',
      isActive: true,
    });
    toast.success('Örnek veriler dolduruldu ✨');
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold">Kuponlar</h1>
        <Button onClick={openCreateDialog}>
          <Plus className="mr-2 h-4 w-4" />
          Yeni Kupon
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      ) : (
        <div className="border rounded-lg overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Kupon Kodu</TableHead>
                <TableHead>Tür</TableHead>
                <TableHead>Değer</TableHead>
                <TableHead>Kullanım</TableHead>
                <TableHead>Son Tarih</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead className="text-right">İşlemler</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {coupons?.map((coupon) => (
                <TableRow key={coupon.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <div className="h-10 w-10 bg-primary/10 rounded-lg flex items-center justify-center">
                        <Ticket className="h-5 w-5 text-primary" />
                      </div>
                      <div>
                        <span className="font-mono font-bold">{coupon.code}</span>
                        {coupon.description && (
                          <p className="text-xs text-muted-foreground">{coupon.description}</p>
                        )}
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      {coupon.type === CouponType.Percentage ? (
                        <Percent className="h-4 w-4 text-muted-foreground" />
                      ) : (
                        <DollarSign className="h-4 w-4 text-muted-foreground" />
                      )}
                      <span>{coupon.type === CouponType.Percentage ? 'Yüzde' : 'Sabit'}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="font-semibold text-primary">
                      {coupon.type === CouponType.Percentage ? `%${coupon.value}` : `${coupon.value} TL`}
                    </span>
                    {coupon.minOrderAmount && (
                      <p className="text-xs text-muted-foreground">Min: {coupon.minOrderAmount} TL</p>
                    )}
                  </TableCell>
                  <TableCell>
                    <span>
                      {coupon.usedCount}
                      {coupon.usageLimit > 0 && ` / ${coupon.usageLimit}`}
                    </span>
                  </TableCell>
                  <TableCell>
                    <span className={isExpired(coupon.expiresAt) ? 'text-destructive' : ''}>
                      {formatDate(coupon.expiresAt)}
                    </span>
                  </TableCell>
                  <TableCell>
                    {!coupon.isActive ? (
                      <Badge variant="secondary">Pasif</Badge>
                    ) : isExpired(coupon.expiresAt) ? (
                      <Badge variant="destructive">Süresi Dolmuş</Badge>
                    ) : coupon.usageLimit > 0 && coupon.usedCount >= coupon.usageLimit ? (
                      <Badge variant="outline">Limit Dolmuş</Badge>
                    ) : (
                      <Badge variant="default">Aktif</Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="ghost" size="icon" onClick={() => openEditDialog(coupon)}>
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(coupon.id, coupon.code)}
                        disabled={isDeleting}
                        className="text-destructive hover:text-destructive"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
              {coupons?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="text-center py-8 text-muted-foreground">
                    Henüz kupon eklenmemiş
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={dialog.open} onOpenChange={(open) => setDialog({ ...dialog, open })}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center justify-between w-full">
              <span>{dialog.mode === 'create' ? 'Yeni Kupon' : 'Kupon Düzenle'}</span>
              {isDevToolsEnabled && dialog.mode === 'create' && (
                <Button 
                  variant="ghost" 
                  size="sm" 
                  onClick={fillSampleData}
                  className="h-8 text-muted-foreground hover:text-primary"
                  title="Örnek Veri Doldur (Dev Tools)"
                >
                  <Wand2 className="mr-2 h-4 w-4" />
                  Örnek Doldur
                </Button>
              )}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Kupon Kodu</Label>
              <Input
                placeholder="YILBASI20"
                value={formData.code}
                onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                className="font-mono"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>İndirim Türü</Label>
                <Select
                  value={String(formData.type)}
                  onValueChange={(value) => setFormData({ ...formData, type: Number(value) as CouponType })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(CouponType.Percentage)}>Yüzde (%)</SelectItem>
                    <SelectItem value={String(CouponType.FixedAmount)}>Sabit Tutar (TL)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Değer</Label>
                <Input
                  type="number"
                  min="0"
                  placeholder={formData.type === CouponType.Percentage ? '20' : '100'}
                  value={formData.value}
                  onChange={(e) => setFormData({ ...formData, value: Number(e.target.value) })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Min. Sipariş Tutarı (TL)</Label>
                <Input
                  type="number"
                  min="0"
                  placeholder="Opsiyonel"
                  value={formData.minOrderAmount || ''}
                  onChange={(e) => setFormData({ ...formData, minOrderAmount: e.target.value ? Number(e.target.value) : undefined })}
                />
              </div>
              <div className="space-y-2">
                <Label>Kullanım Limiti</Label>
                <Input
                  type="number"
                  min="0"
                  placeholder="0 = Sınırsız"
                  value={formData.usageLimit}
                  onChange={(e) => setFormData({ ...formData, usageLimit: Number(e.target.value) })}
                />
              </div>
            </div>

            {dialog.mode === 'create' && (
              <div className="space-y-2">
                <Label>Geçerlilik Süresi (Gün)</Label>
                <Input
                  type="number"
                  min="1"
                  value={formData.validDays}
                  onChange={(e) => setFormData({ ...formData, validDays: Number(e.target.value) })}
                />
              </div>
            )}

            <div className="space-y-2">
              <Label>Açıklama</Label>
              <Input
                placeholder="Yılbaşı kampanyası"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              />
            </div>

            {dialog.mode === 'edit' && (
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="isActive"
                  checked={formData.isActive}
                  onCheckedChange={(checked) => setFormData({ ...formData, isActive: !!checked })}
                />
                <Label htmlFor="isActive">Aktif</Label>
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialog({ open: false, mode: 'create' })}>
              İptal
            </Button>
            <Button onClick={handleSubmit} disabled={isCreating || isUpdating}>
              {dialog.mode === 'create' ? 'Oluştur' : 'Güncelle'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
