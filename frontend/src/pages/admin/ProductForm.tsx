import { useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useGetProductQuery, useCreateProductMutation, useUpdateProductMutation } from '@/features/products/productsApi';
import { useGetCategoriesQuery } from '@/features/admin/adminApi';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Checkbox } from '@/components/common/checkbox';
import { Skeleton } from '@/components/common/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { ArrowLeft, Loader2, Save } from 'lucide-react';
import { toast } from 'sonner';


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
  currency: z.string().default('TRY'),
  // Form içinde string olarak tutuyoruz, API'ye gönderirken number'a çevireceğiz
  categoryId: z.string().min(1, 'Kategori seçiniz'),
  stockQuantity: z
    .union([z.string(), z.number()])
    .transform((val) => (typeof val === 'string' ? parseInt(val, 10) : val))
    .refine((val) => !isNaN(val) && val >= 0, { message: 'Stok negatif olamaz' })
    .default(0),
  isActive: z.boolean().default(true),
});

type ProductFormInput = z.input<typeof productSchema>;
type ProductFormData = z.output<typeof productSchema>;

export default function ProductForm() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEdit = id && id !== 'new';
  const productId = isEdit ? parseInt(id) : 0;

  const { data: product, isLoading: isProductLoading } = useGetProductQuery(productId, {
    skip: !isEdit,
  });
  const { data: categories } = useGetCategoriesQuery();
  const [createProduct, { isLoading: isCreating }] = useCreateProductMutation();
  const [updateProduct, { isLoading: isUpdating }] = useUpdateProductMutation();

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<ProductFormInput, unknown, ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      name: '',
      description: '',
      sku: '',
      price: 0,
      currency: 'TRY',
      categoryId: '',
      stockQuantity: 0,
      isActive: true,
    },
  });

  useEffect(() => {
    // Kategoriler ve ürün yüklendiğinde formu doldur
    if (product && isEdit && categories) {
      const legacyCategoryId = (product as { CategoryId?: unknown }).CategoryId;
      const rawCatId = product.categoryId ?? (typeof legacyCategoryId === 'number' ? legacyCategoryId : undefined);

      reset({
        name: product.name,
        description: product.description || '',
        sku: product.sku,
        price: product.price,
        currency: product.currency,
        categoryId: rawCatId ? rawCatId.toString() : '',
        stockQuantity: product.stockQuantity,
        isActive: product.isActive,
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
          currency: data.currency,
          categoryId: parseInt(data.categoryId, 10),
          stockQuantity: data.stockQuantity,
          isActive: data.isActive,
        };
        await updateProduct({ id: productId, data: updatePayload }).unwrap();
        toast.success('Ürün güncellendi');
      } else {
        const createPayload = {
          name: data.name,
          description: data.description || '',
          sku: data.sku,
          price: data.price,
          currency: data.currency,
          categoryId: parseInt(data.categoryId, 10),
          initialStock: data.stockQuantity,
          isActive: data.isActive,
        };
        await createProduct(createPayload).unwrap();
        toast.success('Ürün oluşturuldu');
      }
      navigate('/admin/products');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'İşlem başarısız');
    }
  };

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
        <Link to="/admin/products">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Ürünlere Dön
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
                  <Input
                    id="description"
                    placeholder="Ürün açıklaması"
                    {...register('description')}
                  />
                  {errors.description && (
                    <p className="text-sm text-destructive">{errors.description.message}</p>
                  )}
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="sku">SKU *</Label>
                    <Input
                      id="sku"
                      placeholder="PROD-001"
                      {...register('sku')}
                    />
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
                    <Label htmlFor="price">Fiyat *</Label>
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
                    <Label htmlFor="currency">Para Birimi</Label>
                    <Controller
                      name="currency"
                      control={control}
                      render={({ field }) => (
                        <Select
                          value={field.value}
                          onValueChange={field.onChange}
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="TRY">TRY (₺)</SelectItem>
                            <SelectItem value="USD">USD ($)</SelectItem>
                            <SelectItem value="EUR">EUR (€)</SelectItem>
                          </SelectContent>
                        </Select>
                      )}
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="stock">Stok Miktarı</Label>
                  <Input
                    id="stock"
                    type="number"
                    placeholder="0"
                    {...register('stockQuantity')}
                  />
                  {errors.stockQuantity && (
                    <p className="text-sm text-destructive">{errors.stockQuantity.message}</p>
                  )}
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Durum</CardTitle>
              </CardHeader>
              <CardContent>
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
                  <Label htmlFor="isActive">Aktif</Label>
                </div>
                <p className="text-sm text-muted-foreground mt-2">
                  Aktif ürünler müşterilere görünür
                </p>
              </CardContent>
            </Card>

            <Button type="submit" className="w-full" disabled={isCreating || isUpdating}>
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
