import { useEffect, useMemo } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useForm, Controller, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  useCreateSellerProductMutation,
  useGetSellerProductQuery,
  useGetSellerProfileQuery,
  useUpdateSellerProductMutation,
} from '@/features/seller/sellerApi';
import { useGetCategoriesQuery } from '@/features/admin/adminApi';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Checkbox } from '@/components/common/checkbox';
import { Skeleton } from '@/components/common/skeleton';
import { Textarea } from '@/components/common/textarea';
import { Badge } from '@/components/common/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { ArrowLeft, Boxes, Hash, Layers3, Loader2, Save, Sparkles, Store, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { ProductImageUploader } from '@/components/media/ProductImageUploader';


const imageSchema = z.object({
  id: z.number().optional(),
  imageUrl: z.string().url('Geçerli bir görsel URL girin'),
  objectKey: z.string().max(1024, 'Object key çok uzun').optional(),
  sortOrder: z.number().int().min(0).default(0),
  isPrimary: z.boolean().default(false),
});

const variantSchema = z.object({
  name: z.string().min(1, 'Varyant adı gereklidir').max(100, 'Varyant adı çok uzun'),
  value: z.string().min(1, 'Varyant değeri gereklidir').max(200, 'Varyant değeri çok uzun'),
});

const productSchema = z.object({
  name: z.string().min(1, 'Ürün adı gereklidir').max(200, 'Ürün adı çok uzun'),
  description: z.string().max(2000, 'Açıklama çok uzun').optional().or(z.literal('')),
  sku: z
    .string()
    .min(1, 'SKU gereklidir')
    .max(50, 'SKU çok uzun')
    .transform((val) => val.toUpperCase()),
  price: z
    .union([z.string(), z.number()])
    .transform((val) => (typeof val === 'string' ? parseFloat(val) : val))
    .refine((val) => !isNaN(val), { message: 'Geçerli bir fiyat girin' })
    .refine((val) => val >= 0, { message: 'Fiyat 0 veya daha büyük olmalı' }),
  categoryId: z.string().min(1, 'Kategori seçiniz'),
  initialStock: z
    .union([z.string(), z.number()])
    .transform((val) => (typeof val === 'string' ? parseInt(val, 10) : val))
    .refine((val) => !isNaN(val) && val >= 0, { message: 'Stok negatif olamaz' })
    .default(0),
  isActive: z.boolean().default(true),
  images: z.array(imageSchema).max(8, 'En fazla 8 görsel ekleyebilirsiniz').default([]),
  variants: z.array(variantSchema).max(20, 'En fazla 20 varyant ekleyebilirsiniz').default([]),
});

type ProductFormInput = z.input<typeof productSchema>;
type ProductFormData = z.output<typeof productSchema>;

function normalizeImagesForRequest(images: ProductFormData['images']) {
  const sanitized = (images || [])
    .filter((image) => image.imageUrl?.trim())
    .map((image, index) => ({
      imageUrl: image.imageUrl.trim(),
      objectKey: image.objectKey?.trim() ? image.objectKey.trim() : undefined,
      isPrimary: !!image.isPrimary,
      sortOrder: index,
    }));

  if (sanitized.length > 0 && sanitized.every((image) => !image.isPrimary)) {
    sanitized[0].isPrimary = true;
  }

  return sanitized;
}

function buildSkuCandidate(name: string) {
  const normalized = name
    .toLocaleUpperCase('tr-TR')
    .replace(/[^A-Z0-9\s]/g, ' ')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 3)
    .map((segment) => segment.slice(0, 4))
    .join('-');

  const suffix = Math.floor(100 + Math.random() * 900);
  return `${normalized || 'URUN'}-${suffix}`;
}

export default function SellerProductForm() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEdit = id && id !== 'new';
  const productId = isEdit ? parseInt(id) : 0;

  const { data: profile, isLoading: profileLoading } = useGetSellerProfileQuery();
  const { data: product, isLoading: isProductLoading } = useGetSellerProductQuery(productId, {
    skip: !isEdit,
  });
  const { data: categories } = useGetCategoriesQuery();
  const [createProduct, { isLoading: isCreating }] = useCreateSellerProductMutation();
  const [updateProduct, { isLoading: isUpdating }] = useUpdateSellerProductMutation();

  const {
    register,
    handleSubmit,
    control,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ProductFormInput, unknown, ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      name: '',
      description: '',
      sku: '',
      price: 0,
      categoryId: '',
      initialStock: 0,
      isActive: true,
      images: [],
      variants: [],
    },
  });
  const { fields: variantFields, append: appendVariant, remove: removeVariant } = useFieldArray({
    control,
    name: 'variants',
  });

  const productName = watch('name');
  const description = watch('description');
  const sku = watch('sku');
  const price = Number(watch('price') || 0);
  const stock = Number(watch('initialStock') || 0);
  const isActive = watch('isActive');
  const selectedCategoryId = watch('categoryId');
  const images = (watch('images') || []).map((image, index) => ({
    id: image.id,
    imageUrl: image.imageUrl,
    objectKey: image.objectKey,
    sortOrder: image.sortOrder ?? index,
    isPrimary: image.isPrimary ?? index === 0,
  }));
  const variants = watch('variants') || [];
  const selectedCategory = categories?.find((category) => category.id.toString() === selectedCategoryId);
  const stockMeta = useMemo(() => {
    if (stock <= 0) {
      return {
        label: 'Stokta yok',
        className: 'bg-rose-500/10 text-rose-700 dark:text-rose-300',
      };
    }

    if (stock <= 5) {
      return {
        label: 'Düşük stok',
        className: 'bg-amber-500/10 text-amber-700 dark:text-amber-300',
      };
    }

    return {
      label: 'Stok yeterli',
      className: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300',
    };
  }, [stock]);

  const handleGenerateSku = () => {
    const nextSku = buildSkuCandidate(productName || '');
    setValue('sku', nextSku, { shouldDirty: true, shouldValidate: true });
    toast.success('SKU otomatik oluşturuldu.');
  };

  useEffect(() => {
    if (product && isEdit && categories) {
      const legacyCategoryId = (product as { CategoryId?: unknown }).CategoryId;
      const rawCatId = product.categoryId ?? (typeof legacyCategoryId === 'number' ? legacyCategoryId : undefined);
      reset({
        name: product.name,
        description: product.description || '',
        sku: product.sku,
        price: product.price,
        categoryId: rawCatId ? rawCatId.toString() : '',
        initialStock: product.stockQuantity,
        isActive: product.isActive,
        images: (product.images || []).map((image) => ({
          id: image.id,
          imageUrl: image.imageUrl,
          objectKey: image.objectKey,
          sortOrder: image.sortOrder,
          isPrimary: image.isPrimary,
        })),
        variants: (product.variants || []).map((variant) => ({
          name: variant.name,
          value: variant.value,
        })),
      });
    }
  }, [product, categories, isEdit, reset]);

  const onSubmit = async (data: ProductFormData) => {
    try {
      if (isEdit) {
        const updatePayload = {
          name: data.name,
          description: data.description || '',
          sku: data.sku,
          price: data.price,
          currency: 'TRY',
          categoryId: parseInt(data.categoryId, 10),
          stockQuantity: data.initialStock, // Form uses initialStock field name for stock input
          isActive: data.isActive,
          images: normalizeImagesForRequest(data.images),
          variants: data.variants.map((variant, index) => ({
            name: variant.name,
            value: variant.value,
            sortOrder: index,
          })),
        };
        await updateProduct({ id: productId, data: updatePayload }).unwrap();
        toast.success('Ürün güncellendi.');
      } else {
        const createPayload = {
          name: data.name,
          description: data.description || '',
          sku: data.sku,
          price: data.price,
          currency: 'TRY',
          categoryId: parseInt(data.categoryId, 10),
          initialStock: data.initialStock,
          isActive: data.isActive,
          images: normalizeImagesForRequest(data.images),
          variants: data.variants.map((variant, index) => ({
            name: variant.name,
            value: variant.value,
            sortOrder: index,
          })),
        };
        await createProduct(createPayload).unwrap();
        toast.success('Ürün oluşturuldu.');
      }
      navigate('/seller/products');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'İşlem başarısız oldu.');
    }
  };


  if (!profileLoading && !profile) {
    return (
      <div>
        <Button variant="ghost" asChild className="mb-6">
          <Link to="/seller/products">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Geri
          </Link>
        </Button>
        <Card className="border-amber-500 bg-amber-50 dark:bg-amber-950/30">
          <CardContent className="p-6 text-center">
            <Store className="h-12 w-12 mx-auto mb-4 text-amber-600" />
            <h2 className="text-xl font-semibold mb-2">Marka Profili Gerekli</h2>
            <p className="text-muted-foreground mb-4">
              Ürün ekleyebilmek için önce marka profilinizi oluşturmanız gerekmektedir.
            </p>
            <Button asChild className="bg-amber-600 hover:bg-amber-700">
              <Link to="/seller/profile">Profil Oluştur</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isEdit && isProductLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-96" />
      </div>
    );
  }

  return (
    <div>
      <Button variant="ghost" asChild className="mb-6">
        <Link to="/seller/products">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Ürünlerime Dön
        </Link>
      </Button>

      <h1 className="text-3xl font-bold mb-8">
        {isEdit ? 'Ürün Düzenle' : 'Yeni Ürün'}
      </h1>

      <form onSubmit={handleSubmit(onSubmit)}>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Temel Bilgiler</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="name">Ürün Adı *</Label>
                  <Input
                    id="name"
                    placeholder="Ürün adını girin"
                    {...register('name')}
                  />
                  {errors.name && (
                    <p className="text-sm text-destructive">{errors.name.message}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="description">Açıklama</Label>
                  <Textarea
                    id="description"
                    rows={5}
                    placeholder="Ürünün öne çıkan özelliklerini, kullanım alanını ve müşterinin bilmesi gereken detayları yazın"
                    {...register('description')}
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <div className="flex items-center justify-between gap-3">
                      <Label htmlFor="sku">SKU *</Label>
                      <Button type="button" variant="ghost" size="sm" onClick={handleGenerateSku}>
                        <Hash className="mr-2 h-4 w-4" />
                        {sku ? 'Yeniden Oluştur' : 'SKU Oluştur'}
                      </Button>
                    </div>
                    <div className="relative">
                      <Input
                        id="sku"
                        placeholder="PROD-001"
                        {...register('sku')}
                      />
                      <Sparkles className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    </div>
                    <p className="text-xs text-muted-foreground">
                      SKU stok ve operasyon takibi için benzersiz olmalı. İsterseniz ürün adına göre otomatik üretebilirsiniz.
                    </p>
                    {errors.sku && (
                      <p className="text-sm text-destructive">{errors.sku.message}</p>
                    )}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="category">Kategori *</Label>
                    <Controller
                      name="categoryId"
                      control={control}
                      render={({ field }) => (
                        <Select
                          key={product?.categoryId ? `cat-${product.categoryId}` : 'cat-empty'}
                          value={field.value ? field.value.toString() : ""}
                          onValueChange={(val) => {
                              if (!val && product?.categoryId) {
                                return;
                              }
                              field.onChange(val);
                          }}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Kategori seçin" />
                          </SelectTrigger>
                          <SelectContent>
                            {categories?.map((cat) => (
                              <SelectItem key={cat.id} value={cat.id.toString()}>
                                {cat.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                    />
                    {errors.categoryId && (
                      <p className="text-sm text-destructive">{errors.categoryId.message}</p>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Fiyat & Stok</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="price">Fiyat (TRY) *</Label>
                    <Input
                      id="price"
                      type="number"
                      step="0.01"
                      placeholder="0.00"
                      {...register('price')}
                    />
                    {errors.price && (
                      <p className="text-sm text-destructive">{errors.price.message}</p>
                    )}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="stock">Stok Miktarı</Label>
                    <Input
                      id="stock"
                      type="number"
                      placeholder="0"
                      {...register('initialStock')}
                    />
                    {errors.initialStock && (
                      <p className="text-sm text-destructive">{errors.initialStock.message}</p>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Görseller</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <ProductImageUploader
                  productId={isEdit ? productId : undefined}
                  canUpload={!!isEdit}
                  images={images}
                  onChange={(nextImages) => {
                    setValue('images', nextImages, { shouldDirty: true, shouldValidate: true });
                  }}
                  maxFiles={8}
                />
                {errors.images && !Array.isArray(errors.images) && 'message' in errors.images ? (
                  <p className="text-sm text-destructive">{errors.images.message as string}</p>
                ) : null}
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0">
                <CardTitle>Varyantlar</CardTitle>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => appendVariant({ name: '', value: '' })}
                >
                  <Layers3 className="mr-2 h-4 w-4" />
                  Varyant Ekle
                </Button>
              </CardHeader>
              <CardContent className="space-y-4">
                {variantFields.length === 0 ? (
                  <div className="rounded-xl border border-dashed border-border/70 p-6 text-sm text-muted-foreground">
                    Beden, renk veya materyal gibi varyant satırlarını buraya ekleyebilirsiniz.
                  </div>
                ) : (
                  variantFields.map((field, index) => (
                    <div key={field.id} className="grid gap-3 rounded-xl border border-border/70 p-4 md:grid-cols-[1fr_1fr_auto]">
                      <div className="space-y-2">
                        <Label htmlFor={`variant-name-${index}`}>Varyant Adı</Label>
                        <Input
                          id={`variant-name-${index}`}
                          placeholder="Örn: Renk"
                          {...register(`variants.${index}.name`)}
                        />
                        {errors.variants?.[index]?.name ? (
                          <p className="text-sm text-destructive">{errors.variants[index]?.name?.message}</p>
                        ) : null}
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor={`variant-value-${index}`}>Varyant Değeri</Label>
                        <Input
                          id={`variant-value-${index}`}
                          placeholder="Örn: Siyah"
                          {...register(`variants.${index}.value`)}
                        />
                        {errors.variants?.[index]?.value ? (
                          <p className="text-sm text-destructive">{errors.variants[index]?.value?.message}</p>
                        ) : null}
                      </div>
                      <div className="flex items-end">
                        <Button
                          type="button"
                          variant="ghost"
                          className="text-destructive hover:text-destructive"
                          onClick={() => removeVariant(index)}
                        >
                          <Trash2 className="mr-2 h-4 w-4" />
                          Kaldır
                        </Button>
                      </div>
                    </div>
                  ))
                )}
              </CardContent>
            </Card>
          </div>

          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Yayın Durumu</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between gap-3">
                  <div className="space-y-1">
                    <p className="font-medium">{isActive ? 'Aktif Yayın' : 'Taslak'}</p>
                    <p className="text-sm text-muted-foreground">
                      {isActive
                        ? 'Ürün müşterilere görünür ve listelerde yer alır.'
                        : 'Ürün kaydedilir ancak vitrinde görünmez.'}
                    </p>
                  </div>
                  <Badge className={isActive ? 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300' : 'bg-slate-500/10 text-slate-700 dark:text-slate-300'}>
                    {isActive ? 'Aktif' : 'Taslak'}
                  </Badge>
                </div>
                <div className="flex items-center space-x-2">
                  <Controller
                    name="isActive"
                    control={control}
                    render={({ field }) => (
                      <Checkbox
                        id="isActive"
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    )}
                  />
                  <Label htmlFor="isActive">Kaydı aktif yayınla</Label>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Hızlı Özet</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="rounded-xl border border-border/70 bg-muted/20 p-4">
                  <div className="flex items-start gap-3">
                    <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-amber-500/10">
                      <Boxes className="h-5 w-5 text-amber-600 dark:text-amber-300" />
                    </div>
                    <div className="min-w-0">
                      <p className="font-medium">{productName || 'Ürün adı bekleniyor'}</p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        {selectedCategory?.name || 'Kategori seçilmedi'}
                      </p>
                      <p className="mt-2 text-sm text-muted-foreground">
                        {description?.trim() || 'Açıklama henüz girilmedi.'}
                      </p>
                    </div>
                  </div>
                </div>

                <div className="space-y-3 text-sm">
                  <div className="flex items-center justify-between gap-3">
                    <span className="text-muted-foreground">Görsel</span>
                    <span className="font-medium">{images.length} adet</span>
                  </div>
                  <div className="flex items-center justify-between gap-3">
                    <span className="text-muted-foreground">Varyant</span>
                    <span className="font-medium">{variants.length} satır</span>
                  </div>
                  <div className="flex items-center justify-between gap-3">
                    <span className="text-muted-foreground">SKU</span>
                    <span className="font-medium">{sku || '-'}</span>
                  </div>
                  <div className="flex items-center justify-between gap-3">
                    <span className="text-muted-foreground">Fiyat</span>
                    <span className="font-medium">{price.toLocaleString('tr-TR')} TL</span>
                  </div>
                  <div className="flex items-center justify-between gap-3">
                    <span className="text-muted-foreground">Stok</span>
                    <Badge className={stockMeta.className}>{stockMeta.label}</Badge>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Not</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-muted-foreground">
                <p>Düzenleme modunda dosyalar R2 storage alanına yüklenir ve sürükle bırak ile sıralanabilir.</p>
                <p>Yeni üründe önce kaydı oluşturup ardından düzenleme ekranında dosya upload yapabilirsiniz.</p>
              </CardContent>
            </Card>

            <Button type="submit" className="w-full bg-amber-600 hover:bg-amber-700" disabled={isCreating || isUpdating}>
              {(isCreating || isUpdating) && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              <Save className="mr-2 h-4 w-4" />
              {isEdit ? 'Güncelle' : 'Oluştur'}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
