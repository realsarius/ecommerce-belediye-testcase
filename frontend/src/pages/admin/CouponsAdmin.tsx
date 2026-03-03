import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import {
  CalendarRange,
  DollarSign,
  Info,
  Pencil,
  Percent,
  Plus,
  Sparkles,
  Tag,
  Ticket,
  Trash2,
  Wand2,
  CopyPlus,
} from 'lucide-react';
import {
  useCreateCouponMutation,
  useDeleteCouponMutation,
  useGetCouponsQuery,
  useUpdateCouponMutation,
} from '@/features/coupons/couponsApi';
import {
  CouponType,
  type Coupon,
  type CreateCouponRequest,
  type UpdateCouponRequest,
} from '@/features/coupons/types';
import {
  CampaignStatus,
  CampaignType,
  type Campaign,
  type CreateCampaignRequest,
  type UpdateCampaignRequest,
} from '@/features/campaigns/types';
import {
  useCreateAdminCampaignMutation,
  useDeleteAdminCampaignMutation,
  useGetAdminCampaignsQuery,
  useUpdateAdminCampaignMutation,
} from '@/features/campaigns/campaignsApi';
import { useGetProductsQuery } from '@/features/products/productsApi';
import { useDevTools } from '@/components/common/DevToolsProvider';
import { ConfirmModal } from '@/components/admin/ConfirmModal';
import { EmptyState } from '@/components/admin/EmptyState';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Checkbox } from '@/components/common/checkbox';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import { Textarea } from '@/components/common/textarea';

type CouponsTab = 'coupons' | 'campaigns';

type CouponDialogState = {
  open: boolean;
  mode: 'create' | 'edit';
  coupon?: Coupon;
};

type CampaignDialogState = {
  open: boolean;
  mode: 'create' | 'edit';
  campaign?: Campaign;
};

type CampaignProductForm = {
  productId: number;
  campaignPrice: string;
  isFeatured: boolean;
};

type CampaignFormData = {
  name: string;
  description: string;
  badgeText: string;
  type: CampaignType;
  isEnabled: boolean;
  startsAt: string;
  endsAt: string;
  products: CampaignProductForm[];
};

type CouponFormData = CreateCouponRequest & {
  isActive?: boolean;
  expiresAt: string;
};

type PendingDelete =
  | { kind: 'coupon'; id: number; label: string }
  | { kind: 'campaign'; id: number; label: string };

const campaignTypeLabels: Record<CampaignType, string> = {
  [CampaignType.FlashSale]: 'Flash Sale',
  [CampaignType.Seasonal]: 'Sezonluk',
  [CampaignType.Highlight]: 'Öne Çıkan',
};

const campaignStatusLabels: Record<CampaignStatus, string> = {
  [CampaignStatus.Draft]: 'Taslak',
  [CampaignStatus.Scheduled]: 'Planlandı',
  [CampaignStatus.Active]: 'Aktif',
  [CampaignStatus.Ended]: 'Bitti',
};

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('tr-TR');
}

function formatDateTime(dateStr: string) {
  return new Date(dateStr).toLocaleString('tr-TR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatCurrency(value?: number | null) {
  if (typeof value !== 'number') {
    return 'Yok';
  }

  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    maximumFractionDigits: 0,
  }).format(value);
}

function formatDateInput(value: Date) {
  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function buildCouponCodeCandidate() {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  const parts = Array.from({ length: 3 }, () =>
    Array.from({ length: 4 }, () => alphabet[Math.floor(Math.random() * alphabet.length)]).join('')
  );

  return parts.join('-');
}

function getValidDaysFromDate(dateValue: string) {
  const selected = new Date(`${dateValue}T23:59:59`);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const diff = selected.getTime() - today.getTime();
  return Math.ceil(diff / (1000 * 60 * 60 * 24));
}

function formatDateTimeLocal(dateStr: string) {
  const date = new Date(dateStr);
  const pad = (value: number) => String(value).padStart(2, '0');

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function isExpired(expiresAt: string) {
  return new Date(expiresAt) < new Date();
}

function normalizeCampaignType(value: Campaign['type']): CampaignType {
  if (typeof value === 'number') {
    return value as CampaignType;
  }

  if (value === 'Seasonal') {
    return CampaignType.Seasonal;
  }

  if (value === 'Highlight') {
    return CampaignType.Highlight;
  }

  return CampaignType.FlashSale;
}

function normalizeCampaignStatus(value: Campaign['status']): CampaignStatus {
  if (typeof value === 'number') {
    return value as CampaignStatus;
  }

  if (value === 'Scheduled') {
    return CampaignStatus.Scheduled;
  }

  if (value === 'Active') {
    return CampaignStatus.Active;
  }

  if (value === 'Ended') {
    return CampaignStatus.Ended;
  }

  return CampaignStatus.Draft;
}

function getCampaignStatusTone(status: CampaignStatus) {
  switch (status) {
    case CampaignStatus.Active:
      return 'success' as const;
    case CampaignStatus.Scheduled:
      return 'warning' as const;
    case CampaignStatus.Ended:
      return 'danger' as const;
    default:
      return 'neutral' as const;
  }
}

function createEmptyCampaignForm(): CampaignFormData {
  return {
    name: '',
    description: '',
    badgeText: '',
    type: CampaignType.FlashSale,
    isEnabled: true,
    startsAt: '',
    endsAt: '',
    products: [],
  };
}

export default function CouponsAdmin() {
  const [activeTab, setActiveTab] = useState<CouponsTab>('coupons');
  const [couponDialog, setCouponDialog] = useState<CouponDialogState>({ open: false, mode: 'create' });
  const [campaignDialog, setCampaignDialog] = useState<CampaignDialogState>({ open: false, mode: 'create' });
  const [pendingDelete, setPendingDelete] = useState<PendingDelete | null>(null);
  const [couponFormData, setCouponFormData] = useState<CouponFormData>({
    code: '',
    type: CouponType.Percentage,
    value: 10,
    minOrderAmount: undefined,
    usageLimit: 0,
    validDays: 7,
    expiresAt: formatDateInput(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)),
    description: '',
  });
  const [campaignFormData, setCampaignFormData] = useState<CampaignFormData>(createEmptyCampaignForm);

  const { data: coupons, isLoading: isCouponsLoading } = useGetCouponsQuery();
  const { data: campaigns, isLoading: isCampaignsLoading } = useGetAdminCampaignsQuery();
  const { data: productPage, isLoading: isProductsLoading } = useGetProductsQuery({ page: 1, pageSize: 100 });
  const [createCoupon, { isLoading: isCreatingCoupon }] = useCreateCouponMutation();
  const [updateCoupon, { isLoading: isUpdatingCoupon }] = useUpdateCouponMutation();
  const [deleteCoupon, { isLoading: isDeletingCoupon }] = useDeleteCouponMutation();
  const [createCampaign, { isLoading: isCreatingCampaign }] = useCreateAdminCampaignMutation();
  const [updateCampaign, { isLoading: isUpdatingCampaign }] = useUpdateAdminCampaignMutation();
  const [deleteCampaign, { isLoading: isDeletingCampaign }] = useDeleteAdminCampaignMutation();
  const { isDevToolsEnabled } = useDevTools();

  const products = productPage?.items ?? [];
  const selectedCampaignProductIds = useMemo(
    () => new Set(campaignFormData.products.map((item) => item.productId)),
    [campaignFormData.products]
  );

  const openCreateCouponDialog = () => {
    setCouponFormData({
      code: '',
      type: CouponType.Percentage,
      value: 10,
      minOrderAmount: undefined,
      usageLimit: 0,
      validDays: 7,
      expiresAt: formatDateInput(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)),
      description: '',
    });
    setCouponDialog({ open: true, mode: 'create' });
  };

  const openEditCouponDialog = (coupon: Coupon) => {
    setCouponFormData({
      code: coupon.code,
      type: coupon.type,
      value: coupon.value,
      minOrderAmount: coupon.minOrderAmount,
      usageLimit: coupon.usageLimit,
      validDays: 7,
      expiresAt: formatDateInput(new Date(coupon.expiresAt)),
      description: coupon.description || '',
      isActive: coupon.isActive,
    });
    setCouponDialog({ open: true, mode: 'edit', coupon });
  };

  const openCreateCampaignDialog = () => {
    setCampaignFormData(createEmptyCampaignForm());
    setCampaignDialog({ open: true, mode: 'create' });
  };

  const openEditCampaignDialog = (campaign: Campaign) => {
    setCampaignFormData({
      name: campaign.name,
      description: campaign.description ?? '',
      badgeText: campaign.badgeText ?? '',
      type: normalizeCampaignType(campaign.type),
      isEnabled: campaign.isEnabled,
      startsAt: formatDateTimeLocal(campaign.startsAt),
      endsAt: formatDateTimeLocal(campaign.endsAt),
      products: campaign.products.map((product) => ({
        productId: product.productId,
        campaignPrice: String(product.campaignPrice),
        isFeatured: product.isFeatured,
      })),
    });
    setCampaignDialog({ open: true, mode: 'edit', campaign });
  };

  const handleCouponSubmit = async () => {
    if (!couponFormData.code.trim()) {
      toast.error('Kupon kodu gereklidir');
      return;
    }

    if (couponFormData.value <= 0) {
      toast.error('İndirim değeri 0’dan büyük olmalıdır');
      return;
    }

    const calculatedValidDays = getValidDaysFromDate(couponFormData.expiresAt);
    if (calculatedValidDays <= 0) {
      toast.error('Kuponun son kullanma tarihi bugünden sonra olmalıdır');
      return;
    }

    try {
      if (couponDialog.mode === 'create') {
        const createPayload: CreateCouponRequest = {
          code: couponFormData.code,
          type: couponFormData.type,
          value: couponFormData.value,
          minOrderAmount: couponFormData.minOrderAmount,
          usageLimit: couponFormData.usageLimit,
          validDays: calculatedValidDays,
          description: couponFormData.description,
        };

        await createCoupon(createPayload).unwrap();
        toast.success('Kupon oluşturuldu');
      } else if (couponDialog.coupon) {
        const updateData: UpdateCouponRequest = {
          code: couponFormData.code,
          type: couponFormData.type,
          value: couponFormData.value,
          minOrderAmount: couponFormData.minOrderAmount,
          usageLimit: couponFormData.usageLimit,
          expiresAt: new Date(`${couponFormData.expiresAt}T23:59:59`).toISOString(),
          isActive: couponFormData.isActive,
          description: couponFormData.description,
        };
        await updateCoupon({ id: couponDialog.coupon.id, data: updateData }).unwrap();
        toast.success('Kupon güncellendi');
      }
      setCouponDialog({ open: false, mode: 'create' });
    } catch {
      toast.error(couponDialog.mode === 'create' ? 'Kupon oluşturulamadı' : 'Kupon güncellenemedi');
    }
  };

  const handleCampaignSubmit = async () => {
    if (!campaignFormData.name.trim()) {
      toast.error('Kampanya adı gereklidir');
      return;
    }

    if (!campaignFormData.startsAt || !campaignFormData.endsAt) {
      toast.error('Başlangıç ve bitiş tarihleri gereklidir');
      return;
    }

    if (campaignFormData.products.length === 0) {
      toast.error('En az bir ürün seçmelisiniz');
      return;
    }

    const productsPayload = campaignFormData.products.map((product) => ({
      productId: product.productId,
      campaignPrice: Number(product.campaignPrice),
      isFeatured: product.isFeatured,
    }));

    if (productsPayload.some((product) => Number.isNaN(product.campaignPrice) || product.campaignPrice <= 0)) {
      toast.error('Tüm kampanya fiyatları 0’dan büyük olmalıdır');
      return;
    }

    const payload: CreateCampaignRequest = {
      name: campaignFormData.name.trim(),
      description: campaignFormData.description.trim() || undefined,
      badgeText: campaignFormData.badgeText.trim() || undefined,
      type: campaignFormData.type,
      isEnabled: campaignFormData.isEnabled,
      startsAt: new Date(campaignFormData.startsAt).toISOString(),
      endsAt: new Date(campaignFormData.endsAt).toISOString(),
      products: productsPayload,
    };

    try {
      if (campaignDialog.mode === 'create') {
        await createCampaign(payload).unwrap();
        toast.success('Kampanya oluşturuldu');
      } else if (campaignDialog.campaign) {
        const updateData: UpdateCampaignRequest = payload;
        await updateCampaign({ id: campaignDialog.campaign.id, data: updateData }).unwrap();
        toast.success('Kampanya güncellendi');
      }

      setCampaignDialog({ open: false, mode: 'create' });
      setCampaignFormData(createEmptyCampaignForm());
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Kampanya kaydedilemedi');
    }
  };

  const handleDeleteCoupon = async (id: number) => {
    try {
      await deleteCoupon(id).unwrap();
      setPendingDelete(null);
      toast.success('Kupon silindi');
    } catch {
      toast.error('Kupon silinemedi');
    }
  };

  const handleDeleteCampaign = async (campaignId: number) => {
    try {
      await deleteCampaign(campaignId).unwrap();
      setPendingDelete(null);
      toast.success('Kampanya silindi');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Kampanya silinemedi');
    }
  };

  const handleConfirmDelete = async () => {
    if (!pendingDelete) {
      return;
    }

    if (pendingDelete.kind === 'coupon') {
      await handleDeleteCoupon(pendingDelete.id);
      return;
    }

    await handleDeleteCampaign(pendingDelete.id);
  };

  const toggleCampaignProduct = (productId: number, checked: boolean) => {
    if (checked) {
      const product = products.find((item) => item.id === productId);
      if (!product) {
        return;
      }

      setCampaignFormData((current) => ({
        ...current,
        products: [
          ...current.products,
          {
            productId,
            campaignPrice: String(product.campaignPrice ?? product.price),
            isFeatured: false,
          },
        ],
      }));
      return;
    }

    setCampaignFormData((current) => ({
      ...current,
      products: current.products.filter((item) => item.productId !== productId),
    }));
  };

  const updateCampaignProduct = (
    productId: number,
    field: keyof CampaignProductForm,
    value: string | boolean
  ) => {
    setCampaignFormData((current) => ({
      ...current,
      products: current.products.map((item) => (
        item.productId === productId
          ? {
              ...item,
              [field]: value,
            }
          : item
      )),
    }));
  };

  const fillSampleCouponData = () => {
    setCouponFormData({
      code: 'YILBASI20',
      type: CouponType.Percentage,
      value: 20,
      minOrderAmount: 500,
      usageLimit: 1,
      validDays: 7,
      expiresAt: formatDateInput(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)),
      description: 'Yılbaşı Kampanyası',
      isActive: true,
    });
    toast.success('Örnek kupon verileri dolduruldu');
  };

  const generateCouponCode = () => {
    setCouponFormData((current) => ({
      ...current,
      code: buildCouponCodeCandidate(),
    }));
    toast.success('Kupon kodu oluşturuldu');
  };

  const fillSampleCampaignData = () => {
    const [firstProduct, secondProduct] = products;

    setCampaignFormData({
      name: 'Hafta Sonu Fırsatları',
      description: 'Seçili ürünlerde kısa süreli fiyat avantajı.',
      badgeText: 'Hafta Sonu',
      type: CampaignType.FlashSale,
      isEnabled: true,
      startsAt: formatDateTimeLocal(new Date().toISOString()),
      endsAt: formatDateTimeLocal(new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString()),
      products: [firstProduct, secondProduct].filter(Boolean).map((product, index) => ({
        productId: product.id,
        campaignPrice: String(Math.max(1, Math.round(product.price * (index === 0 ? 0.8 : 0.9)))),
        isFeatured: index === 0,
      })),
    });

    toast.success('Örnek kampanya verileri dolduruldu');
  };

  const campaignSummary = useMemo(() => {
    const items = campaigns ?? [];

    return {
      active: items.filter((item) => normalizeCampaignStatus(item.status) === CampaignStatus.Active).length,
      scheduled: items.filter((item) => normalizeCampaignStatus(item.status) === CampaignStatus.Scheduled).length,
      draft: items.filter((item) => normalizeCampaignStatus(item.status) === CampaignStatus.Draft).length,
      ended: items.filter((item) => normalizeCampaignStatus(item.status) === CampaignStatus.Ended).length,
    };
  }, [campaigns]);

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Kupon ve Kampanyalar</h1>
        <p className="max-w-3xl text-muted-foreground">
          Kuponları ve katalog kampanyalarını tek ekrandan yönetin. Kampanya akışı mevcut admin endpoint’leriyle
          çalışır; ürün seçimi ise mevcut katalog verisi üzerinden yapılır.
        </p>
      </div>

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as CouponsTab)} className="space-y-4">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <TabsList>
            <TabsTrigger value="coupons">Kuponlar</TabsTrigger>
            <TabsTrigger value="campaigns">Kampanyalar</TabsTrigger>
          </TabsList>

          <Button onClick={activeTab === 'coupons' ? openCreateCouponDialog : openCreateCampaignDialog}>
            <Plus className="mr-2 h-4 w-4" />
            {activeTab === 'coupons' ? 'Yeni Kupon' : 'Yeni Kampanya'}
          </Button>
        </div>

        <TabsContent value="coupons" className="space-y-4">
          {isCouponsLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, index) => (
                <Skeleton key={index} className="h-16" />
              ))}
            </div>
          ) : (
            <div className="overflow-x-auto rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Kupon Kodu</TableHead>
                    <TableHead>Tür</TableHead>
                    <TableHead>Değer</TableHead>
                    <TableHead>Min. Tutar</TableHead>
                    <TableHead>Kullanım</TableHead>
                    <TableHead>Oluşturulma</TableHead>
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
                          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                            <Ticket className="h-5 w-5 text-primary" />
                          </div>
                          <div>
                            <span className="font-mono font-bold">{coupon.code}</span>
                            {coupon.description ? (
                              <p className="text-xs text-muted-foreground">{coupon.description}</p>
                            ) : null}
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
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatCurrency(coupon.minOrderAmount)}
                      </TableCell>
                      <TableCell>
                        <div className="space-y-1 text-sm">
                          <p className="font-medium">
                            {coupon.usedCount}
                            {coupon.usageLimit > 0 ? ` / ${coupon.usageLimit}` : ' / limitsiz'}
                          </p>
                          <p className="text-muted-foreground">
                            {coupon.usageLimit > 0
                              ? `${Math.max(0, coupon.usageLimit - coupon.usedCount)} kullanım kaldı`
                              : 'Sınırsız kullanım'}
                          </p>
                        </div>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatDateTime(coupon.createdAt)}
                      </TableCell>
                      <TableCell>
                        <span className={isExpired(coupon.expiresAt) ? 'text-destructive' : ''}>
                          {formatDateTime(coupon.expiresAt)}
                        </span>
                      </TableCell>
                      <TableCell>
                        {!coupon.isActive ? (
                          <StatusBadge label="Pasif" tone="neutral" />
                        ) : isExpired(coupon.expiresAt) ? (
                          <StatusBadge label="Süresi Dolmuş" tone="danger" />
                        ) : coupon.usageLimit > 0 && coupon.usedCount >= coupon.usageLimit ? (
                          <StatusBadge label="Limit Dolmuş" tone="warning" />
                        ) : (
                          <StatusBadge label="Aktif" tone="success" />
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button variant="ghost" size="icon" onClick={() => openEditCouponDialog(coupon)}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => setPendingDelete({ kind: 'coupon', id: coupon.id, label: coupon.code })}
                            disabled={isDeletingCoupon}
                            className="text-destructive hover:text-destructive"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                  {coupons?.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={9} className="p-0">
                        <EmptyState
                          icon={Ticket}
                          title="Henüz kupon eklenmemiş"
                          description="İlk kuponu oluşturarak bu ekrandan kullanım, limit ve son tarih takibini başlatabilirsiniz."
                          className="border-0 shadow-none"
                        />
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
            </div>
          )}
        </TabsContent>

        <TabsContent value="campaigns" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <Card className="border-border/70">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Aktif</CardTitle>
              </CardHeader>
              <CardContent className="flex items-center justify-between">
                <span className="text-3xl font-semibold">{campaignSummary.active.toLocaleString('tr-TR')}</span>
                <Sparkles className="h-5 w-5 text-emerald-500" />
              </CardContent>
            </Card>
            <Card className="border-border/70">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Planlandı</CardTitle>
              </CardHeader>
              <CardContent className="flex items-center justify-between">
                <span className="text-3xl font-semibold">{campaignSummary.scheduled.toLocaleString('tr-TR')}</span>
                <CalendarRange className="h-5 w-5 text-amber-500" />
              </CardContent>
            </Card>
            <Card className="border-border/70">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Taslak</CardTitle>
              </CardHeader>
              <CardContent className="flex items-center justify-between">
                <span className="text-3xl font-semibold">{campaignSummary.draft.toLocaleString('tr-TR')}</span>
                <Tag className="h-5 w-5 text-slate-500" />
              </CardContent>
            </Card>
            <Card className="border-border/70">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Tamamlandı</CardTitle>
              </CardHeader>
              <CardContent className="flex items-center justify-between">
                <span className="text-3xl font-semibold">{campaignSummary.ended.toLocaleString('tr-TR')}</span>
                <Ticket className="h-5 w-5 text-rose-500" />
              </CardContent>
            </Card>
          </div>

          {isCampaignsLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 4 }).map((_, index) => (
                <Skeleton key={index} className="h-16" />
              ))}
            </div>
          ) : (
            <div className="overflow-x-auto rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Kampanya</TableHead>
                    <TableHead>Tür</TableHead>
                    <TableHead>Ürün</TableHead>
                    <TableHead>Dönem</TableHead>
                    <TableHead>Durum</TableHead>
                    <TableHead className="text-right">İşlemler</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {campaigns?.map((campaign) => {
                    const normalizedType = normalizeCampaignType(campaign.type);
                    const normalizedStatus = normalizeCampaignStatus(campaign.status);

                    return (
                      <TableRow key={campaign.id}>
                        <TableCell>
                          <div>
                            <p className="font-medium">{campaign.name}</p>
                            <p className="text-xs text-muted-foreground">
                              {campaign.badgeText || campaign.description || 'Kısa açıklama yok'}
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>{campaignTypeLabels[normalizedType]}</TableCell>
                        <TableCell>
                          <div className="space-y-1">
                            <p className="font-medium">{campaign.products.length} ürün</p>
                            <p className="text-xs text-muted-foreground">
                              {campaign.products.filter((product) => product.isFeatured).length} öne çıkan
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="space-y-1 text-sm">
                            <p>{formatDate(campaign.startsAt)}</p>
                            <p className="text-muted-foreground">Bitiş: {formatDate(campaign.endsAt)}</p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <StatusBadge
                            label={campaignStatusLabels[normalizedStatus]}
                            tone={getCampaignStatusTone(normalizedStatus)}
                          />
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex justify-end gap-2">
                            <Button variant="ghost" size="icon" onClick={() => openEditCampaignDialog(campaign)}>
                              <Pencil className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => setPendingDelete({ kind: 'campaign', id: campaign.id, label: campaign.name })}
                              disabled={isDeletingCampaign}
                              className="text-destructive hover:text-destructive"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                  {campaigns?.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="p-0">
                        <EmptyState
                          icon={Sparkles}
                          title="Henüz kampanya eklenmemiş"
                          description="Katalog ürünlerini seçerek zamanlı fiyat kampanyaları oluşturduğunuzda liste burada görünecek."
                          className="border-0 shadow-none"
                        />
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
            </div>
          )}
        </TabsContent>
      </Tabs>

      <Dialog open={couponDialog.open} onOpenChange={(open) => setCouponDialog({ ...couponDialog, open })}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex w-full items-center justify-between">
              <span>{couponDialog.mode === 'create' ? 'Yeni Kupon' : 'Kupon Düzenle'}</span>
              {isDevToolsEnabled && couponDialog.mode === 'create' ? (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={fillSampleCouponData}
                  className="h-8 text-muted-foreground hover:text-primary"
                  title="Örnek Veri Doldur"
                >
                  <Wand2 className="mr-2 h-4 w-4" />
                  Örnek Doldur
                </Button>
              ) : null}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <div className="flex items-center justify-between gap-3">
                <Label>Kupon Kodu</Label>
                <Button type="button" variant="ghost" size="sm" onClick={generateCouponCode}>
                  <CopyPlus className="mr-2 h-4 w-4" />
                  Kod Üret
                </Button>
              </div>
              <Input
                placeholder="YILBASI20"
                value={couponFormData.code}
                onChange={(event) => setCouponFormData({ ...couponFormData, code: event.target.value.toUpperCase() })}
                className="font-mono"
              />
              <p className="text-xs text-muted-foreground">
                Kod büyük harfe çevrilir. İsterseniz sistemden benzersiz bir kampanya kodu üretebilirsiniz.
              </p>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>İndirim Türü</Label>
                <Select
                  value={String(couponFormData.type)}
                  onValueChange={(value) => setCouponFormData({ ...couponFormData, type: Number(value) as CouponType })}
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
                  placeholder={couponFormData.type === CouponType.Percentage ? '20' : '100'}
                  value={couponFormData.value}
                  onChange={(event) => setCouponFormData({ ...couponFormData, value: Number(event.target.value) })}
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
                  value={couponFormData.minOrderAmount || ''}
                  onChange={(event) => setCouponFormData({
                    ...couponFormData,
                    minOrderAmount: event.target.value ? Number(event.target.value) : undefined,
                  })}
                />
              </div>
              <div className="space-y-2">
                <Label>Kullanım Limiti</Label>
                <Input
                  type="number"
                  min="0"
                  placeholder="0 = Sınırsız"
                  value={couponFormData.usageLimit}
                  onChange={(event) => setCouponFormData({ ...couponFormData, usageLimit: Number(event.target.value) })}
                />
              </div>
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>Son Kullanma Tarihi</Label>
                <Input
                  type="date"
                  min={formatDateInput(new Date())}
                  value={couponFormData.expiresAt}
                  onChange={(event) => setCouponFormData({ ...couponFormData, expiresAt: event.target.value })}
                />
              </div>
              <div className="rounded-lg border border-dashed border-border/70 bg-muted/20 p-3 text-sm">
                <div className="flex items-start gap-2">
                  <Info className="mt-0.5 h-4 w-4 text-muted-foreground" />
                  <div className="space-y-1 text-muted-foreground">
                    <p className="font-medium text-foreground">Geçerlilik Özeti</p>
                    <p>
                      Kupon {couponFormData.expiresAt
                        ? `${couponFormData.expiresAt} tarihine kadar aktif olur.`
                        : 'bir bitiş tarihi seçilene kadar kaydedilemez.'}
                    </p>
                    <p>
                      {getValidDaysFromDate(couponFormData.expiresAt) > 0
                        ? `Bugünden itibaren ${getValidDaysFromDate(couponFormData.expiresAt)} gün geçerli.`
                        : 'Bitiş tarihi bugünden sonra olmalı.'}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Açıklama</Label>
              <Textarea
                placeholder="Yılbaşı kampanyası"
                value={couponFormData.description}
                onChange={(event) => setCouponFormData({ ...couponFormData, description: event.target.value })}
                rows={3}
              />
            </div>

            {couponDialog.mode === 'edit' ? (
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="coupon-is-active"
                  checked={couponFormData.isActive}
                  onCheckedChange={(checked) => setCouponFormData({ ...couponFormData, isActive: !!checked })}
                />
                <Label htmlFor="coupon-is-active">Aktif</Label>
              </div>
            ) : null}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCouponDialog({ open: false, mode: 'create' })}>
              İptal
            </Button>
            <Button onClick={handleCouponSubmit} disabled={isCreatingCoupon || isUpdatingCoupon}>
              {couponDialog.mode === 'create' ? 'Oluştur' : 'Güncelle'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={campaignDialog.open} onOpenChange={(open) => setCampaignDialog({ ...campaignDialog, open })}>
        <DialogContent className="max-h-[90vh] max-w-4xl overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex w-full items-center justify-between">
              <span>{campaignDialog.mode === 'create' ? 'Yeni Kampanya' : 'Kampanya Düzenle'}</span>
              {isDevToolsEnabled ? (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={fillSampleCampaignData}
                  className="h-8 text-muted-foreground hover:text-primary"
                  title="Örnek Veri Doldur"
                >
                  <Wand2 className="mr-2 h-4 w-4" />
                  Örnek Doldur
                </Button>
              ) : null}
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-5">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>Kampanya Adı</Label>
                <Input
                  value={campaignFormData.name}
                  onChange={(event) => setCampaignFormData({ ...campaignFormData, name: event.target.value })}
                  placeholder="Hafta Sonu Fırsatları"
                />
              </div>
              <div className="space-y-2">
                <Label>Badge Metni</Label>
                <Input
                  value={campaignFormData.badgeText}
                  onChange={(event) => setCampaignFormData({ ...campaignFormData, badgeText: event.target.value })}
                  placeholder="Flash Sale"
                />
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>Kampanya Türü</Label>
                <Select
                  value={String(campaignFormData.type)}
                  onValueChange={(value) => setCampaignFormData({
                    ...campaignFormData,
                    type: Number(value) as CampaignType,
                  })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(CampaignType.FlashSale)}>Flash Sale</SelectItem>
                    <SelectItem value={String(CampaignType.Seasonal)}>Sezonluk</SelectItem>
                    <SelectItem value={String(CampaignType.Highlight)}>Öne Çıkan</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-end">
                <div className="flex items-center space-x-2 rounded-lg border px-3 py-2">
                  <Checkbox
                    id="campaign-enabled"
                    checked={campaignFormData.isEnabled}
                    onCheckedChange={(checked) => setCampaignFormData({
                      ...campaignFormData,
                      isEnabled: !!checked,
                    })}
                  />
                  <Label htmlFor="campaign-enabled">Kampanya aktif olsun</Label>
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Açıklama</Label>
              <Textarea
                value={campaignFormData.description}
                onChange={(event) => setCampaignFormData({ ...campaignFormData, description: event.target.value })}
                placeholder="Seçili ürünlerde kısa süreli fiyat avantajı."
                rows={3}
              />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>Başlangıç</Label>
                <Input
                  type="datetime-local"
                  value={campaignFormData.startsAt}
                  onChange={(event) => setCampaignFormData({ ...campaignFormData, startsAt: event.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Bitiş</Label>
                <Input
                  type="datetime-local"
                  value={campaignFormData.endsAt}
                  onChange={(event) => setCampaignFormData({ ...campaignFormData, endsAt: event.target.value })}
                />
              </div>
            </div>

            <div className="space-y-3">
              <div>
                <p className="font-medium">Kampanyaya Dahil Ürünler</p>
                <p className="text-sm text-muted-foreground">
                  Ürün seçin, kampanya fiyatını belirleyin ve isterseniz öne çıkarın.
                </p>
              </div>

              {isProductsLoading ? (
                <div className="space-y-2">
                  {Array.from({ length: 4 }).map((_, index) => (
                    <Skeleton key={index} className="h-14 rounded-lg" />
                  ))}
                </div>
              ) : (
                <div className="space-y-3 rounded-xl border p-3">
                  {products.map((product) => {
                    const selectedItem = campaignFormData.products.find((item) => item.productId === product.id);
                    const isSelected = selectedCampaignProductIds.has(product.id);

                    return (
                      <div key={product.id} className="rounded-xl border border-border/70 p-4">
                        <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                          <div className="flex items-start gap-3">
                            <Checkbox
                              id={`campaign-product-${product.id}`}
                              checked={isSelected}
                              onCheckedChange={(checked) => toggleCampaignProduct(product.id, !!checked)}
                            />
                            <div>
                              <Label htmlFor={`campaign-product-${product.id}`} className="cursor-pointer font-medium">
                                {product.name}
                              </Label>
                              <p className="text-sm text-muted-foreground">
                                {product.categoryName} • {product.price.toLocaleString('tr-TR')} TL • SKU: {product.sku}
                              </p>
                            </div>
                          </div>

                          {isSelected && selectedItem ? (
                            <div className="grid gap-3 md:grid-cols-[180px_auto]">
                              <div className="space-y-2">
                                <Label>Kampanya Fiyatı</Label>
                                <Input
                                  type="number"
                                  min="1"
                                  value={selectedItem.campaignPrice}
                                  onChange={(event) => updateCampaignProduct(product.id, 'campaignPrice', event.target.value)}
                                />
                              </div>
                              <div className="flex items-end">
                                <div className="flex items-center space-x-2 rounded-lg border px-3 py-2">
                                  <Checkbox
                                    id={`campaign-featured-${product.id}`}
                                    checked={selectedItem.isFeatured}
                                    onCheckedChange={(checked) => updateCampaignProduct(product.id, 'isFeatured', !!checked)}
                                  />
                                  <Label htmlFor={`campaign-featured-${product.id}`}>Öne çıkar</Label>
                                </div>
                              </div>
                            </div>
                          ) : null}
                        </div>
                      </div>
                    );
                  })}

                  {products.length === 0 ? (
                    <EmptyState
                      icon={Tag}
                      title="Kampanya için seçilebilir ürün bulunamadı"
                      description="Önce katalogda aktif ürün oluşturun, ardından kampanya ürün listesine ekleyin."
                      className="border-dashed shadow-none"
                    />
                  ) : null}
                </div>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setCampaignDialog({ open: false, mode: 'create' })}>
              İptal
            </Button>
            <Button onClick={handleCampaignSubmit} disabled={isCreatingCampaign || isUpdatingCampaign}>
              {campaignDialog.mode === 'create' ? 'Oluştur' : 'Güncelle'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmModal
        open={Boolean(pendingDelete)}
        onOpenChange={(open) => {
          if (!open) {
            setPendingDelete(null);
          }
        }}
        title={pendingDelete?.kind === 'coupon' ? 'Kuponu sil' : 'Kampanyayı sil'}
        description={
          pendingDelete
            ? `"${pendingDelete.label}" kaydını kalıcı olarak silmek istediğinize emin misiniz?`
            : ''
        }
        confirmLabel={pendingDelete?.kind === 'coupon' ? 'Kuponu Sil' : 'Kampanyayı Sil'}
        confirmVariant="destructive"
        isLoading={isDeletingCoupon || isDeletingCampaign}
        onConfirm={handleConfirmDelete}
      />
    </div>
  );
}
