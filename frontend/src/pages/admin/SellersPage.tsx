import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import {
  Search,
  ShieldCheck,
  Store,
  Users,
  Wallet,
} from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/common/avatar';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import { Input } from '@/components/common/input';
import { Skeleton } from '@/components/common/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/common/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import { Textarea } from '@/components/common/textarea';
import { KpiCard } from '@/components/admin/KpiCard';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { EmptyState } from '@/components/admin/EmptyState';
import { TableLoadingState } from '@/components/admin/TableLoadingState';
import {
  useApproveAdminSellerApplicationMutation,
  useGetAdminSellersQuery,
  useRejectAdminSellerApplicationMutation,
  useUpdateAdminSellerCommissionMutation,
  useUpdateAdminSellerStatusMutation,
} from '@/features/admin/adminApi';
import type { AdminSellerListItem, AdminSellerStatus } from '@/features/admin/types';

function getInitials(value: string) {
  return value
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('tr-TR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function formatCurrency(value: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
}

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

function getStatusTone(status: AdminSellerStatus) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Pending':
      return 'warning';
    case 'Suspended':
      return 'danger';
    case 'Closed':
      return 'neutral';
    default:
      return 'neutral';
  }
}

function getStatusLabel(status: AdminSellerStatus) {
  switch (status) {
    case 'Active':
      return 'Aktif';
    case 'Pending':
      return 'Başvuru';
    case 'Suspended':
      return 'Askıda';
    case 'Closed':
      return 'Kapalı';
    default:
      return status;
  }
}

export default function SellersPage() {
  const [search, setSearch] = useState('');
  const [activeTab, setActiveTab] = useState<'active' | 'applications'>('active');
  const [applicationDialogSeller, setApplicationDialogSeller] = useState<AdminSellerListItem | null>(null);
  const [applicationNote, setApplicationNote] = useState('');
  const [commissionDialogSeller, setCommissionDialogSeller] = useState<AdminSellerListItem | null>(null);
  const [commissionRateInput, setCommissionRateInput] = useState('');

  const { data: sellers = [], isLoading } = useGetAdminSellersQuery();
  const [approveApplication, { isLoading: approving }] = useApproveAdminSellerApplicationMutation();
  const [rejectApplication, { isLoading: rejecting }] = useRejectAdminSellerApplicationMutation();
  const [updateStatus, { isLoading: updatingStatus }] = useUpdateAdminSellerStatusMutation();
  const [updateCommission, { isLoading: updatingCommission }] = useUpdateAdminSellerCommissionMutation();

  const filteredSellers = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('tr-TR');

    return sellers.filter((seller) => {
      const matchesTab = activeTab === 'applications'
        ? seller.status === 'Pending'
        : seller.status !== 'Pending';

      if (!matchesTab) {
        return false;
      }

      if (!term) {
        return true;
      }

      return seller.brandName.toLocaleLowerCase('tr-TR').includes(term)
        || seller.ownerEmail.toLocaleLowerCase('tr-TR').includes(term)
        || `${seller.sellerFirstName} ${seller.sellerLastName}`.toLocaleLowerCase('tr-TR').includes(term)
        || String(seller.id).includes(term);
    });
  }, [activeTab, search, sellers]);

  const summary = useMemo(() => ({
    total: sellers.length,
    active: sellers.filter((seller) => seller.status === 'Active').length,
    pending: sellers.filter((seller) => seller.status === 'Pending').length,
    overriddenCommission: sellers.filter((seller) => seller.hasCommissionOverride).length,
  }), [sellers]);

  const handleStatusToggle = async (seller: AdminSellerListItem, nextStatus: 'Active' | 'Suspended') => {
    try {
      await updateStatus({ id: seller.id, status: nextStatus }).unwrap();
      toast.success(nextStatus === 'Active' ? 'Seller yeniden aktifleştirildi.' : 'Seller askıya alındı.');
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Seller durumu güncellenemedi.'));
    }
  };

  const handleApproveApplication = async () => {
    if (!applicationDialogSeller) {
      return;
    }

    try {
      await approveApplication({
        id: applicationDialogSeller.id,
        reviewNote: applicationNote.trim() || undefined,
      }).unwrap();
      toast.success('Seller başvurusu onaylandı.');
      setApplicationDialogSeller(null);
      setApplicationNote('');
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Başvuru onaylanamadı.'));
    }
  };

  const handleRejectApplication = async () => {
    if (!applicationDialogSeller) {
      return;
    }

    try {
      await rejectApplication({
        id: applicationDialogSeller.id,
        reviewNote: applicationNote.trim() || undefined,
      }).unwrap();
      toast.success('Seller başvurusu reddedildi.');
      setApplicationDialogSeller(null);
      setApplicationNote('');
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Başvuru reddedilemedi.'));
    }
  };

  const handleCommissionSubmit = async () => {
    if (!commissionDialogSeller) {
      return;
    }

    const trimmed = commissionRateInput.trim();
    const parsed = trimmed === '' ? null : Number(trimmed.replace(',', '.'));

    if (trimmed && (Number.isNaN(parsed) || parsed === null || parsed < 0 || parsed > 100)) {
      toast.error('Komisyon oranı 0 ile 100 arasında olmalıdır.');
      return;
    }

    try {
      await updateCommission({ id: commissionDialogSeller.id, rate: parsed }).unwrap();
      toast.success(parsed === null ? 'Komisyon override kaldırıldı.' : 'Komisyon oranı güncellendi.');
      setCommissionDialogSeller(null);
      setCommissionRateInput('');
    } catch (error: unknown) {
      toast.error(getErrorMessage(error, 'Komisyon oranı güncellenemedi.'));
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-36 rounded-xl" />
          ))}
        </div>
        <TableLoadingState rowCount={8} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Seller Yönetimi</h1>
        <p className="max-w-3xl text-muted-foreground">
          Aktif seller hesaplarını, başvuru kuyruğunu ve komisyon override akışını tek yerden yönetin.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          title="Toplam Seller"
          value={summary.total.toLocaleString('tr-TR')}
          helperText="Sistemde seller profili olan mağazalar."
          icon={Users}
          accentClass="text-sky-600 dark:text-sky-300"
          surfaceClass="bg-sky-500/10"
        />
        <KpiCard
          title="Aktif Seller"
          value={summary.active.toLocaleString('tr-TR')}
          helperText="Doğrulanmış ve aktif hesabı bulunan seller sayısı."
          icon={ShieldCheck}
          accentClass="text-emerald-600 dark:text-emerald-300"
          surfaceClass="bg-emerald-500/10"
        />
        <KpiCard
          title="Bekleyen Başvuru"
          value={summary.pending.toLocaleString('tr-TR')}
          helperText="Onay veya red bekleyen seller profilleri."
          icon={Store}
          accentClass="text-amber-600 dark:text-amber-300"
          surfaceClass="bg-amber-500/10"
        />
        <KpiCard
          title="Komisyon Override"
          value={summary.overriddenCommission.toLocaleString('tr-TR')}
          helperText="Varsayılan oranın dışında komisyonu olan seller sayısı."
          icon={Wallet}
          accentClass="text-violet-600 dark:text-violet-300"
          surfaceClass="bg-violet-500/10"
        />
      </div>

      <Card className="border-border/70">
        <CardHeader>
          <CardTitle>Arama</CardTitle>
          <CardDescription>Mağaza adı, owner e-postası veya seller profil numarasına göre filtreleyin.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="relative max-w-md">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Mağaza adı, e-posta veya seller ID ara..."
              className="pl-10"
            />
          </div>
        </CardContent>
      </Card>

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'active' | 'applications')}>
        <TabsList>
          <TabsTrigger value="active">
            Aktif Seller&apos;lar
          </TabsTrigger>
          <TabsTrigger value="applications">
            Başvurular
          </TabsTrigger>
        </TabsList>

        <TabsContent value="active">
          <div className="overflow-hidden rounded-xl border border-border/70 bg-card">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Mağaza</TableHead>
                  <TableHead>Owner</TableHead>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Toplam Satış</TableHead>
                  <TableHead>Ort. Puan</TableHead>
                  <TableHead>Komisyon</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead className="text-right">İşlemler</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredSellers.map((seller) => (
                  <TableRow key={seller.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <Avatar className="h-10 w-10">
                          <AvatarFallback>{getInitials(seller.brandName)}</AvatarFallback>
                        </Avatar>
                        <div>
                          <p className="font-medium">{seller.brandName}</p>
                          <p className="text-sm text-muted-foreground">Seller ID #{seller.id}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div>
                        <p className="font-medium">{seller.sellerFirstName} {seller.sellerLastName}</p>
                        <p className="text-sm text-muted-foreground">{seller.ownerEmail}</p>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div>
                        <p>{seller.productCount.toLocaleString('tr-TR')}</p>
                        <p className="text-sm text-muted-foreground">{seller.activeProductCount} aktif</p>
                      </div>
                    </TableCell>
                    <TableCell>{formatCurrency(seller.totalSales)}</TableCell>
                    <TableCell>{seller.averageRating.toFixed(1)} / 5</TableCell>
                    <TableCell>
                      <div>
                        <p className="font-medium">%{seller.commissionRate.toLocaleString('tr-TR')}</p>
                        <p className="text-sm text-muted-foreground">
                          {seller.hasCommissionOverride ? 'Override aktif' : 'Varsayılan oran'}
                        </p>
                      </div>
                    </TableCell>
                    <TableCell>
                      <StatusBadge label={getStatusLabel(seller.status)} tone={getStatusTone(seller.status)} />
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex flex-wrap justify-end gap-2">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setCommissionDialogSeller(seller);
                            setCommissionRateInput(String(seller.hasCommissionOverride ? seller.commissionRate : ''));
                          }}
                        >
                          Komisyon
                        </Button>
                        {seller.status === 'Active' ? (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={updatingStatus}
                            onClick={() => handleStatusToggle(seller, 'Suspended')}
                          >
                            Askıya Al
                          </Button>
                        ) : null}
                        {(seller.status === 'Suspended' || seller.status === 'Closed') ? (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={updatingStatus}
                            onClick={() => handleStatusToggle(seller, 'Active')}
                          >
                            Aktifleştir
                          </Button>
                        ) : null}
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={`/admin/sellers/${seller.id}`}>Detay</Link>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {filteredSellers.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} className="p-4">
                      <EmptyState
                        icon={Store}
                        title="Seller bulunamadı"
                        description="Arama kriterlerine uyan aktif seller kaydı bulunamadı. Mağaza adı veya owner bilgisini değiştirerek tekrar deneyin."
                        className="border-none bg-transparent shadow-none"
                      />
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </div>
        </TabsContent>

        <TabsContent value="applications">
          <div className="overflow-hidden rounded-xl border border-border/70 bg-card">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Başvuru Sahibi</TableHead>
                  <TableHead>Mağaza</TableHead>
                  <TableHead>Ürün</TableHead>
                  <TableHead>Başvuru Tarihi</TableHead>
                  <TableHead>Komisyon</TableHead>
                  <TableHead className="text-right">İşlem</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredSellers.map((seller) => (
                  <TableRow key={seller.id}>
                    <TableCell>
                      <div>
                        <p className="font-medium">{seller.sellerFirstName} {seller.sellerLastName}</p>
                        <p className="text-sm text-muted-foreground">{seller.ownerEmail}</p>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div>
                        <p className="font-medium">{seller.brandName}</p>
                        <p className="text-sm text-muted-foreground">Seller ID #{seller.id}</p>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div>
                        <p>{seller.productCount.toLocaleString('tr-TR')} ürün</p>
                        <p className="text-sm text-muted-foreground">{seller.totalStock} toplam stok</p>
                      </div>
                    </TableCell>
                    <TableCell>{formatDate(seller.createdAt)}</TableCell>
                    <TableCell>%{seller.commissionRate.toLocaleString('tr-TR')}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setApplicationDialogSeller(seller);
                            setApplicationNote('');
                          }}
                        >
                          İncele
                        </Button>
                        <Button variant="ghost" size="sm" asChild>
                          <Link to={`/admin/sellers/${seller.id}`}>Detay</Link>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {filteredSellers.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="p-4">
                      <EmptyState
                        icon={Store}
                        title="Bekleyen başvuru yok"
                        description="İncelenmesi gereken seller başvurusu şu an görünmüyor. Yeni başvurular geldiğinde bu tablo otomatik dolacak."
                        className="border-none bg-transparent shadow-none"
                      />
                    </TableCell>
                  </TableRow>
                ) : null}
              </TableBody>
            </Table>
          </div>
        </TabsContent>
      </Tabs>

      <Dialog open={!!applicationDialogSeller} onOpenChange={(open) => (!open ? setApplicationDialogSeller(null) : undefined)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Seller Başvurusunu İncele</DialogTitle>
            <DialogDescription>
              Başvuruyu onaylayabilir veya reddedebilirsiniz. Not alanı opsiyoneldir.
            </DialogDescription>
          </DialogHeader>

          {applicationDialogSeller ? (
            <div className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="rounded-xl border bg-muted/20 p-4">
                  <p className="text-sm text-muted-foreground">Mağaza</p>
                  <p className="mt-1 font-semibold">{applicationDialogSeller.brandName}</p>
                </div>
                <div className="rounded-xl border bg-muted/20 p-4">
                  <p className="text-sm text-muted-foreground">Başvuru Sahibi</p>
                  <p className="mt-1 font-semibold">
                    {applicationDialogSeller.sellerFirstName} {applicationDialogSeller.sellerLastName}
                  </p>
                  <p className="text-sm text-muted-foreground">{applicationDialogSeller.ownerEmail}</p>
                </div>
              </div>
              <div className="grid gap-4 md:grid-cols-3">
                <div className="rounded-xl border bg-muted/20 p-4">
                  <p className="text-sm text-muted-foreground">Ürün Sayısı</p>
                  <p className="mt-1 font-semibold">{applicationDialogSeller.productCount}</p>
                </div>
                <div className="rounded-xl border bg-muted/20 p-4">
                  <p className="text-sm text-muted-foreground">Ortalama Puan</p>
                  <p className="mt-1 font-semibold">{applicationDialogSeller.averageRating.toFixed(1)} / 5</p>
                </div>
                <div className="rounded-xl border bg-muted/20 p-4">
                  <p className="text-sm text-muted-foreground">Varsayılan Komisyon</p>
                  <p className="mt-1 font-semibold">%{applicationDialogSeller.commissionRate.toLocaleString('tr-TR')}</p>
                </div>
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">İnceleme Notu</label>
                <Textarea
                  value={applicationNote}
                  onChange={(event) => setApplicationNote(event.target.value)}
                  placeholder="Red nedeni veya onay notu ekleyebilirsiniz..."
                />
              </div>
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setApplicationDialogSeller(null)}>
              Vazgeç
            </Button>
            <Button
              type="button"
              variant="outline"
              className="border-rose-200 text-rose-700 hover:bg-rose-50"
              disabled={rejecting}
              onClick={handleRejectApplication}
            >
              Reddet
            </Button>
            <Button type="button" disabled={approving} onClick={handleApproveApplication}>
              Onayla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!commissionDialogSeller} onOpenChange={(open) => (!open ? setCommissionDialogSeller(null) : undefined)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Komisyon Override</DialogTitle>
            <DialogDescription>
              Boş bırakırsanız seller varsayılan platform komisyon oranına döner.
            </DialogDescription>
          </DialogHeader>

          {commissionDialogSeller ? (
            <div className="space-y-4">
              <div className="rounded-xl border bg-muted/20 p-4">
                <p className="font-medium">{commissionDialogSeller.brandName}</p>
                <p className="text-sm text-muted-foreground">
                  Mevcut oran: %{commissionDialogSeller.commissionRate.toLocaleString('tr-TR')}
                </p>
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Yeni Komisyon Oranı (%)</label>
                <Input
                  inputMode="decimal"
                  value={commissionRateInput}
                  onChange={(event) => setCommissionRateInput(event.target.value)}
                  placeholder="Örn. 12.5"
                />
              </div>
            </div>
          ) : null}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setCommissionDialogSeller(null)}>
              Vazgeç
            </Button>
            <Button type="button" disabled={updatingCommission} onClick={handleCommissionSubmit}>
              Kaydet
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
