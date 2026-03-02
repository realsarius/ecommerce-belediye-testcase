import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  AtSign,
  CheckCircle,
  Clock,
  Facebook,
  Globe,
  ImageIcon,
  Instagram,
  Loader2,
  Mail,
  Phone,
  Save,
  Store,
} from 'lucide-react';
import {
  useCreateSellerProfileMutation,
  useGetSellerProfileQuery,
  useUpdateSellerProfileMutation,
} from '@/features/seller/sellerApi';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Skeleton } from '@/components/common/skeleton';
import { Textarea } from '@/components/common/textarea';
import { toast } from 'sonner';

const optionalUrlSchema = z.union([z.string().url('Geçerli bir URL girin'), z.literal('')]);
const optionalEmailSchema = z.union([z.string().email('Geçerli bir e-posta girin'), z.literal('')]);

const profileSchema = z.object({
  brandName: z.string().trim().min(2, 'Marka adı en az 2 karakter olmalı').max(100, 'Marka adı çok uzun'),
  brandDescription: z.string().max(1200, 'Açıklama çok uzun').optional().or(z.literal('')),
  logoUrl: optionalUrlSchema.optional(),
  bannerImageUrl: optionalUrlSchema.optional(),
  contactEmail: optionalEmailSchema.optional(),
  contactPhone: z.string().max(50, 'Telefon alanı çok uzun').optional().or(z.literal('')),
  websiteUrl: optionalUrlSchema.optional(),
  instagramUrl: optionalUrlSchema.optional(),
  facebookUrl: optionalUrlSchema.optional(),
  xUrl: optionalUrlSchema.optional(),
});

type ProfileFormData = z.infer<typeof profileSchema>;

const emptyToUndefined = (value?: string) => {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
};

const emptyToString = (value?: string) => value?.trim() ?? '';

export default function SellerProfile() {
  const { data: profile, isLoading, refetch } = useGetSellerProfileQuery();
  const [createProfile, { isLoading: isCreating }] = useCreateSellerProfileMutation();
  const [updateProfile, { isLoading: isUpdating }] = useUpdateSellerProfileMutation();

  const isEdit = !!profile;

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      brandName: '',
      brandDescription: '',
      logoUrl: '',
      bannerImageUrl: '',
      contactEmail: '',
      contactPhone: '',
      websiteUrl: '',
      instagramUrl: '',
      facebookUrl: '',
      xUrl: '',
    },
  });

  useEffect(() => {
    if (!profile) {
      return;
    }

    reset({
      brandName: profile.brandName,
      brandDescription: profile.brandDescription || '',
      logoUrl: profile.logoUrl || '',
      bannerImageUrl: profile.bannerImageUrl || '',
      contactEmail: profile.contactEmail || '',
      contactPhone: profile.contactPhone || '',
      websiteUrl: profile.websiteUrl || '',
      instagramUrl: profile.instagramUrl || '',
      facebookUrl: profile.facebookUrl || '',
      xUrl: profile.xUrl || '',
    });
  }, [profile, reset]);

  const brandName = watch('brandName');
  const brandDescription = watch('brandDescription');
  const logoUrl = watch('logoUrl');
  const bannerImageUrl = watch('bannerImageUrl');
  const contactEmail = watch('contactEmail');
  const contactPhone = watch('contactPhone');
  const websiteUrl = watch('websiteUrl');
  const instagramUrl = watch('instagramUrl');
  const facebookUrl = watch('facebookUrl');
  const xUrl = watch('xUrl');

  const onSubmit = async (data: ProfileFormData) => {
    try {
      if (isEdit) {
        await updateProfile({
          brandName: data.brandName.trim(),
          brandDescription: emptyToString(data.brandDescription),
          logoUrl: emptyToString(data.logoUrl),
          bannerImageUrl: emptyToString(data.bannerImageUrl),
          contactEmail: emptyToString(data.contactEmail),
          contactPhone: emptyToString(data.contactPhone),
          websiteUrl: emptyToString(data.websiteUrl),
          instagramUrl: emptyToString(data.instagramUrl),
          facebookUrl: emptyToString(data.facebookUrl),
          xUrl: emptyToString(data.xUrl),
        }).unwrap();
        toast.success('Profil güncellendi');
      } else {
        await createProfile({
          brandName: data.brandName.trim(),
          brandDescription: emptyToUndefined(data.brandDescription),
          logoUrl: emptyToUndefined(data.logoUrl),
          bannerImageUrl: emptyToUndefined(data.bannerImageUrl),
          contactEmail: emptyToUndefined(data.contactEmail),
          contactPhone: emptyToUndefined(data.contactPhone),
          websiteUrl: emptyToUndefined(data.websiteUrl),
          instagramUrl: emptyToUndefined(data.instagramUrl),
          facebookUrl: emptyToUndefined(data.facebookUrl),
          xUrl: emptyToUndefined(data.xUrl),
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
        <Skeleton className="h-8 w-56" />
        <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
          <Skeleton className="h-[760px] rounded-2xl" />
          <div className="space-y-6">
            <Skeleton className="h-48 rounded-2xl" />
            <Skeleton className="h-72 rounded-2xl" />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Mağaza Profili</h1>
        <p className="max-w-3xl text-muted-foreground">
          Mağazanızın vitrin görselini, iletişim bilgilerini ve sosyal medya bağlantılarını tek yerden yönetin.
        </p>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Store className="h-5 w-5 text-amber-600" />
              {isEdit ? 'Profili Düzenle' : 'Yeni Profil Oluştur'}
            </CardTitle>
            <CardDescription>
              Logo, banner, iletişim ve sosyal medya alanlarını doldurarak mağazanızı daha güven verici hale getirin.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-8">
              <section className="space-y-4">
                <div className="space-y-1">
                  <h2 className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Marka Bilgileri
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    Mağaza vitrininde ve seller panel başlığında görünen bilgiler.
                  </p>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="brandName">Marka Adı *</Label>
                    <Input id="brandName" placeholder="Örn: Kuzey Teknoloji" {...register('brandName')} />
                    {errors.brandName ? (
                      <p className="text-sm text-destructive">{errors.brandName.message}</p>
                    ) : null}
                  </div>

                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="brandDescription">Marka Açıklaması</Label>
                    <Textarea
                      id="brandDescription"
                      rows={5}
                      placeholder="Markanızı, ürün yaklaşımınızı ve müşterilere sunduğunuz deneyimi anlatın."
                      {...register('brandDescription')}
                    />
                    {errors.brandDescription ? (
                      <p className="text-sm text-destructive">{errors.brandDescription.message}</p>
                    ) : null}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="logoUrl">Logo URL</Label>
                    <Input id="logoUrl" placeholder="https://example.com/logo.png" {...register('logoUrl')} />
                    {errors.logoUrl ? (
                      <p className="text-sm text-destructive">{errors.logoUrl.message}</p>
                    ) : (
                      <p className="text-xs text-muted-foreground">Kare oranlı logo önerilir.</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="bannerImageUrl">Banner Görsel URL</Label>
                    <Input
                      id="bannerImageUrl"
                      placeholder="https://example.com/banner.jpg"
                      {...register('bannerImageUrl')}
                    />
                    {errors.bannerImageUrl ? (
                      <p className="text-sm text-destructive">{errors.bannerImageUrl.message}</p>
                    ) : (
                      <p className="text-xs text-muted-foreground">Geniş yatay görsel kullanmanız önerilir.</p>
                    )}
                  </div>
                </div>
              </section>

              <section className="space-y-4">
                <div className="space-y-1">
                  <h2 className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    İletişim Bilgileri
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    Müşterilerin sizi daha rahat tanıyabilmesi için görünen mağaza bilgileri.
                  </p>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="contactEmail">İletişim E-postası</Label>
                    <Input id="contactEmail" placeholder="magaza@example.com" {...register('contactEmail')} />
                    {errors.contactEmail ? (
                      <p className="text-sm text-destructive">{errors.contactEmail.message}</p>
                    ) : null}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="contactPhone">İletişim Telefonu</Label>
                    <Input id="contactPhone" placeholder="+90 555 000 00 00" {...register('contactPhone')} />
                    {errors.contactPhone ? (
                      <p className="text-sm text-destructive">{errors.contactPhone.message}</p>
                    ) : null}
                  </div>

                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="websiteUrl">Web Sitesi</Label>
                    <Input id="websiteUrl" placeholder="https://markaniz.com" {...register('websiteUrl')} />
                    {errors.websiteUrl ? (
                      <p className="text-sm text-destructive">{errors.websiteUrl.message}</p>
                    ) : null}
                  </div>
                </div>
              </section>

              <section className="space-y-4">
                <div className="space-y-1">
                  <h2 className="text-sm font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    Sosyal Medya
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    Aktif kanallarınızı ekleyerek mağaza güvenini ve keşfedilebilirliği artırın.
                  </p>
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="instagramUrl">Instagram</Label>
                    <Input id="instagramUrl" placeholder="https://instagram.com/markaniz" {...register('instagramUrl')} />
                    {errors.instagramUrl ? (
                      <p className="text-sm text-destructive">{errors.instagramUrl.message}</p>
                    ) : null}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="facebookUrl">Facebook</Label>
                    <Input id="facebookUrl" placeholder="https://facebook.com/markaniz" {...register('facebookUrl')} />
                    {errors.facebookUrl ? (
                      <p className="text-sm text-destructive">{errors.facebookUrl.message}</p>
                    ) : null}
                  </div>

                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="xUrl">X / Twitter</Label>
                    <Input id="xUrl" placeholder="https://x.com/markaniz" {...register('xUrl')} />
                    {errors.xUrl ? (
                      <p className="text-sm text-destructive">{errors.xUrl.message}</p>
                    ) : null}
                  </div>
                </div>
              </section>

              <Button
                type="submit"
                className="w-full bg-amber-600 hover:bg-amber-700"
                disabled={isCreating || isUpdating}
              >
                {isCreating || isUpdating ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                <Save className="mr-2 h-4 w-4" />
                {isEdit ? 'Profili Güncelle' : 'Profil Oluştur'}
              </Button>
            </form>
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card className="border-border/70">
            <CardHeader>
              <CardTitle>Profil Durumu</CardTitle>
              <CardDescription>Satıcı profilinizin onay ve görünürlük özeti.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4 text-sm">
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Durum</span>
                {profile ? (
                  profile.isVerified ? (
                    <Badge className="bg-green-500">
                      <CheckCircle className="mr-1 h-3 w-3" />
                      Onaylı
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="border-amber-500 text-amber-600">
                      <Clock className="mr-1 h-3 w-3" />
                      Onay Bekliyor
                    </Badge>
                  )
                ) : (
                  <Badge variant="secondary">Henüz oluşturulmadı</Badge>
                )}
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Profil Tipi</span>
                <span className="font-medium">Seller Mağazası</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span className="text-muted-foreground">Oluşturma</span>
                <span className="font-medium">
                  {profile ? new Date(profile.createdAt).toLocaleDateString('tr-TR') : '-'}
                </span>
              </div>
              <div className="rounded-xl border border-dashed bg-muted/30 p-4 text-muted-foreground">
                Logo, banner ve iletişim alanlarını tamamlamanız mağaza görünürlüğünü ve güven algısını güçlendirir.
              </div>
            </CardContent>
          </Card>

          <Card className="overflow-hidden border-border/70">
            <div
              className="relative h-40 border-b bg-gradient-to-br from-amber-500/20 via-orange-500/15 to-rose-500/10"
              style={bannerImageUrl ? { backgroundImage: `url(${bannerImageUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' } : undefined}
            >
              <div className="absolute inset-0 bg-black/20" />
              <div className="absolute bottom-4 left-4 flex items-end gap-3">
                {logoUrl ? (
                  <img
                    src={logoUrl}
                    alt={brandName || 'Mağaza logosu'}
                    className="h-16 w-16 rounded-2xl border-2 border-white/80 object-cover shadow-lg"
                  />
                ) : (
                  <div className="flex h-16 w-16 items-center justify-center rounded-2xl border-2 border-white/80 bg-white/85 shadow-lg">
                    <ImageIcon className="h-7 w-7 text-amber-600" />
                  </div>
                )}
                <div className="pb-1 text-white">
                  <p className="text-xl font-semibold">{brandName || 'Marka adınız burada görünecek'}</p>
                  <p className="text-sm text-white/80">
                    {profile?.sellerFirstName || 'Seller'} {profile?.sellerLastName || 'hesabı'}
                  </p>
                </div>
              </div>
            </div>
            <CardContent className="space-y-4 p-5">
              <div className="space-y-2">
                <p className="text-sm font-medium text-muted-foreground">Mağaza Önizlemesi</p>
                <p className="text-sm leading-6 text-foreground/90">
                  {brandDescription || 'Marka açıklamanız burada görünecek. Müşterilerinize ne sattığınızı ve sizi neden tercih etmeleri gerektiğini anlatabilirsiniz.'}
                </p>
              </div>

              <div className="grid gap-3 text-sm">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Mail className="h-4 w-4 text-amber-600" />
                  <span>{contactEmail || 'İletişim e-postası eklenmedi'}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Phone className="h-4 w-4 text-amber-600" />
                  <span>{contactPhone || 'Telefon bilgisi eklenmedi'}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Globe className="h-4 w-4 text-amber-600" />
                  <span>{websiteUrl || 'Web sitesi eklenmedi'}</span>
                </div>
              </div>

              <div className="grid gap-2 text-sm sm:grid-cols-3">
                <div className="rounded-xl border bg-muted/20 p-3">
                  <div className="mb-2 flex items-center gap-2 font-medium">
                    <Instagram className="h-4 w-4 text-pink-500" />
                    Instagram
                  </div>
                  <p className="text-muted-foreground">{instagramUrl || 'Yok'}</p>
                </div>
                <div className="rounded-xl border bg-muted/20 p-3">
                  <div className="mb-2 flex items-center gap-2 font-medium">
                    <Facebook className="h-4 w-4 text-blue-600" />
                    Facebook
                  </div>
                  <p className="text-muted-foreground">{facebookUrl || 'Yok'}</p>
                </div>
                <div className="rounded-xl border bg-muted/20 p-3">
                  <div className="mb-2 flex items-center gap-2 font-medium">
                    <AtSign className="h-4 w-4 text-slate-700 dark:text-slate-200" />
                    X
                  </div>
                  <p className="text-muted-foreground">{xUrl || 'Yok'}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
