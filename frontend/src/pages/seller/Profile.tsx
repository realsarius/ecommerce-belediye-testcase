import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useGetSellerProfileQuery, useCreateSellerProfileMutation, useUpdateSellerProfileMutation } from '@/features/seller/sellerApi';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { Badge } from '@/components/common/badge';
import { Loader2, Save, Store, CheckCircle, Clock } from 'lucide-react';
import { toast } from 'sonner';
import { useEffect } from 'react';

const profileSchema = z.object({
  brandName: z.string().min(2, 'Marka adı en az 2 karakter olmalı').max(100, 'Marka adı çok uzun'),
  brandDescription: z.string().max(500, 'Açıklama çok uzun').optional().or(z.literal('')),
  logoUrl: z.string().url('Geçerli bir URL girin').optional().or(z.literal('')),
});

type ProfileFormData = z.infer<typeof profileSchema>;

export default function SellerProfile() {
  const { data: profile, isLoading, refetch } = useGetSellerProfileQuery();
  const [createProfile, { isLoading: isCreating }] = useCreateSellerProfileMutation();
  const [updateProfile, { isLoading: isUpdating }] = useUpdateSellerProfileMutation();

  const isEdit = !!profile;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      brandName: '',
      brandDescription: '',
      logoUrl: '',
    },
  });

  useEffect(() => {
    if (profile) {
      reset({
        brandName: profile.brandName,
        brandDescription: profile.brandDescription || '',
        logoUrl: profile.logoUrl || '',
      });
    }
  }, [profile, reset]);

  const onSubmit = async (data: ProfileFormData) => {
    try {
      if (isEdit) {
        await updateProfile(data).unwrap();
        toast.success('Profil güncellendi');
      } else {
        await createProfile({
          brandName: data.brandName,
          brandDescription: data.brandDescription || undefined,
          logoUrl: data.logoUrl || undefined,
        }).unwrap();
        toast.success('Profil oluşturuldu');
      }
      refetch();
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'İşlem başarısız');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-3xl font-bold mb-8">Marka Profili</h1>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Store className="h-5 w-5 text-amber-600" />
                {isEdit ? 'Profili Düzenle' : 'Yeni Profil Oluştur'}
              </CardTitle>
              <CardDescription>
                {isEdit
                  ? 'Marka bilgilerinizi güncelleyin'
                  : 'Ürün satabilmek için marka profilinizi oluşturun'}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="brandName">Marka Adı *</Label>
                  <Input
                    id="brandName"
                    placeholder="Örn: Teknoloji Dükkanı"
                    {...register('brandName')}
                  />
                  {errors.brandName && (
                    <p className="text-sm text-destructive">{errors.brandName.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="brandDescription">Marka Açıklaması</Label>
                  <Input
                    id="brandDescription"
                    placeholder="Markanızı kısaca tanıtın"
                    {...register('brandDescription')}
                  />
                  {errors.brandDescription && (
                    <p className="text-sm text-destructive">{errors.brandDescription.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="logoUrl">Logo URL (Opsiyonel)</Label>
                  <Input
                    id="logoUrl"
                    placeholder="https://example.com/logo.png"
                    {...register('logoUrl')}
                  />
                  {errors.logoUrl && (
                    <p className="text-sm text-destructive">{errors.logoUrl.message}</p>
                  )}
                  <p className="text-sm text-muted-foreground">
                    Markanızın logosunun URL adresini girin
                  </p>
                </div>

                <Button
                  type="submit"
                  className="w-full bg-amber-600 hover:bg-amber-700"
                  disabled={isCreating || isUpdating}
                >
                  {(isCreating || isUpdating) && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  <Save className="mr-2 h-4 w-4" />
                  {isEdit ? 'Güncelle' : 'Profil Oluştur'}
                </Button>
              </form>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Profil Durumu</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {profile ? (
                <>
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">Durum</span>
                    {profile.isVerified ? (
                      <Badge className="bg-green-500">
                        <CheckCircle className="mr-1 h-3 w-3" />
                        Onaylı
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="border-amber-500 text-amber-600">
                        <Clock className="mr-1 h-3 w-3" />
                        Onay Bekliyor
                      </Badge>
                    )}
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">Oluşturma</span>
                    <span className="text-sm">
                      {new Date(profile.createdAt).toLocaleDateString('tr-TR')}
                    </span>
                  </div>
                </>
              ) : (
                <p className="text-muted-foreground text-sm">
                  Henüz profil oluşturulmadı
                </p>
              )}
            </CardContent>
          </Card>

          {profile && (
            <Card>
              <CardHeader>
                <CardTitle>Önizleme</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-3">
                  {profile.logoUrl ? (
                    <img
                      src={profile.logoUrl}
                      alt={profile.brandName}
                      className="h-12 w-12 rounded-lg object-cover"
                    />
                  ) : (
                    <div className="h-12 w-12 rounded-lg bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center">
                      <Store className="h-6 w-6 text-amber-600" />
                    </div>
                  )}
                  <div>
                    <p className="font-semibold">{profile.brandName}</p>
                    <p className="text-sm text-muted-foreground line-clamp-1">
                      {profile.brandDescription || 'Açıklama yok'}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
