import { useState } from 'react';
import { useAppSelector } from '@/app/hooks';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import { useGetLoyaltySummaryQuery } from '@/features/loyalty/loyaltyApi';
import { User, Mail, Phone, Calendar, Shield, Save, Loader2 } from 'lucide-react';
import { toast } from 'sonner';

export default function Account() {
  const { user } = useAppSelector((state) => state.auth);
  const { data: loyaltySummary, isLoading: isLoyaltyLoading } = useGetLoyaltySummaryQuery();
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const getUserFormData = () => ({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phone: '',
  });
  const [formData, setFormData] = useState({
    ...getUserFormData(),
  });

  const handleToggleEdit = () => {
    if (isEditing) {
      setIsEditing(false);
      return;
    }
    setFormData(getUserFormData());
    setIsEditing(true);
  };

  const handleSave = async () => {
    setIsSaving(true);

    await new Promise(resolve => setTimeout(resolve, 1000));
    setIsSaving(false);
    setIsEditing(false);
    toast.success('Bilgileriniz güncellendi');
  };

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">Kullanıcı Bilgilerim</h1>
        <p className="text-muted-foreground mt-2">Hesap bilgilerinizi görüntüleyin ve düzenleyin</p>
      </div>

      <div className="space-y-6">
        {/* Profile Card */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                  <User className="h-6 w-6 text-primary" />
                </div>
                <div>
                  <CardTitle>{user?.firstName} {user?.lastName}</CardTitle>
                  <CardDescription>{user?.email}</CardDescription>
                </div>
              </div>
              <Button 
                variant={isEditing ? "outline" : "default"}
                onClick={handleToggleEdit}
              >
                {isEditing ? 'İptal' : 'Düzenle'}
              </Button>
            </div>
          </CardHeader>
        </Card>

        {/* Personal Info */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <User className="h-5 w-5" />
              Kişisel Bilgiler
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">Ad</Label>
                <Input
                  id="firstName"
                  value={isEditing ? formData.firstName : user?.firstName || ''}
                  onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                  disabled={!isEditing}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Soyad</Label>
                <Input
                  id="lastName"
                  value={isEditing ? formData.lastName : user?.lastName || ''}
                  onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                  disabled={!isEditing}
                />
              </div>
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="email" className="flex items-center gap-2">
                <Mail className="h-4 w-4" />
                E-posta
              </Label>
              <Input
                id="email"
                type="email"
                value={isEditing ? formData.email : user?.email || ''}
                disabled
                className="bg-muted"
              />
              <p className="text-xs text-muted-foreground">E-posta adresi değiştirilemez</p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="phone" className="flex items-center gap-2">
                <Phone className="h-4 w-4" />
                Telefon
              </Label>
              <Input
                id="phone"
                placeholder="05XX XXX XX XX"
                value={formData.phone}
                onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                disabled={!isEditing}
              />
            </div>

            {isEditing && (
              <Button onClick={handleSave} disabled={isSaving} className="w-full">
                {isSaving ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Save className="mr-2 h-4 w-4" />
                )}
                Kaydet
              </Button>
            )}
          </CardContent>
        </Card>

        {/* Account Info */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Shield className="h-5 w-5" />
              Hesap Bilgileri
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between py-2">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">Üyelik Tarihi</span>
              </div>
              <span className="text-sm text-muted-foreground">
                {new Date().toLocaleDateString('tr-TR', { year: 'numeric', month: 'long', day: 'numeric' })}
              </span>
            </div>
            <Separator />
            <div className="flex items-center justify-between py-2">
              <div className="flex items-center gap-2">
                <Shield className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">Hesap Türü</span>
              </div>
              <span className="text-sm font-medium text-primary">{user?.role || 'Müşteri'}</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Sadakat Puanlarım</CardTitle>
            <CardDescription>
              100 puan = 1 TL indirim. Puanlarını checkout ekranında kullanabilirsin.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            {isLoyaltyLoading ? (
              <>
                <Skeleton className="h-16 w-full" />
                <Skeleton className="h-32 w-full" />
              </>
            ) : (
              <>
                <div className="grid gap-4 md:grid-cols-3">
                  <div className="rounded-xl border border-amber-400/20 bg-amber-500/10 p-4">
                    <p className="text-sm text-muted-foreground">Kullanılabilir Puan</p>
                    <p className="mt-2 text-2xl font-bold">{loyaltySummary?.availablePoints?.toLocaleString('tr-TR') ?? 0}</p>
                  </div>
                  <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4">
                    <p className="text-sm text-muted-foreground">İndirime Dönüşen Tutar</p>
                    <p className="mt-2 text-2xl font-bold">
                      {(loyaltySummary?.availableDiscountAmount ?? 0).toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                  <div className="rounded-xl border border-sky-400/20 bg-sky-500/10 p-4">
                    <p className="text-sm text-muted-foreground">Toplam Kazanılan</p>
                    <p className="mt-2 text-2xl font-bold">{loyaltySummary?.totalEarnedPoints?.toLocaleString('tr-TR') ?? 0}</p>
                  </div>
                </div>

                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <h3 className="font-medium">Son Hareketler</h3>
                    <span className="text-xs text-muted-foreground">En yeni 10 işlem</span>
                  </div>

                  {loyaltySummary?.recentTransactions?.length ? (
                    <div className="space-y-3">
                      {loyaltySummary.recentTransactions.map((transaction) => (
                        <div key={transaction.id} className="flex items-start justify-between gap-4 rounded-xl border p-3">
                          <div>
                            <p className="font-medium">{transaction.description}</p>
                            <p className="mt-1 text-xs text-muted-foreground">
                              {new Date(transaction.createdAt).toLocaleString('tr-TR')}
                              {transaction.orderNumber ? ` • ${transaction.orderNumber}` : ''}
                            </p>
                          </div>
                          <div className="text-right">
                            <p className={`font-semibold ${transaction.points >= 0 ? 'text-emerald-600' : 'text-rose-600'}`}>
                              {transaction.points >= 0 ? '+' : ''}{transaction.points.toLocaleString('tr-TR')} puan
                            </p>
                            <p className="mt-1 text-xs text-muted-foreground">
                              Bakiye: {transaction.balanceAfter.toLocaleString('tr-TR')}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="rounded-xl border border-dashed p-4 text-sm text-muted-foreground">
                      Henüz sadakat puanı hareketin bulunmuyor.
                    </div>
                  )}
                </div>
              </>
            )}
          </CardContent>
        </Card>

        {/* Danger Zone */}
        <Card className="border-destructive/50">
          <CardHeader>
            <CardTitle className="text-destructive">Tehlikeli Bölge</CardTitle>
            <CardDescription>Bu işlemler geri alınamaz</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium">Şifre Değiştir</p>
                <p className="text-sm text-muted-foreground">Hesap şifrenizi güncelleyin</p>
              </div>
              <Button variant="outline">Şifre Değiştir</Button>
            </div>
            <Separator />
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium">Hesabı Sil</p>
                <p className="text-sm text-muted-foreground">Hesabınızı kalıcı olarak silin</p>
              </div>
              <Button variant="destructive">Hesabı Sil</Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
