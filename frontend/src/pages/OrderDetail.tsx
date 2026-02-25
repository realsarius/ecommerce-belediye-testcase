import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useGetOrderQuery, useCancelOrderMutation, useProcessPaymentMutation, useUpdateOrderItemsMutation } from '@/features/orders/ordersApi';
import { useSearchProductsQuery } from '@/features/products/productsApi';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Badge } from '@/components/common/badge';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '@/components/common/dialog';
import { ArrowLeft, Package, MapPin, CreditCard, XCircle, RefreshCw, Loader2, Edit, Plus, Minus, Trash2, Search } from 'lucide-react';
import { toast } from 'sonner';
import type { OrderStatus, OrderItem } from '@/features/orders/types';
import type { Product } from '@/features/products/types';

const statusColors: Record<OrderStatus, string> = {
  PendingPayment: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
  Paid: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
  Processing: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
  Shipped: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200',
  Delivered: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
  Cancelled: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
  Refunded: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
};

const statusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoya Verildi',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal Edildi',
  Refunded: 'İade Edildi',
};

interface EditableOrderItem {
  productId: number;
  productName: string;
  quantity: number;
  priceSnapshot: number;
}

const mapOrderItemsToEditable = (items: OrderItem[]): EditableOrderItem[] =>
  items.map((item) => ({
    productId: item.productId,
    productName: item.productName,
    quantity: item.quantity,
    priceSnapshot: item.priceSnapshot,
  }));

export default function OrderDetail() {
  const { id } = useParams<{ id: string }>();
  const orderId = parseInt(id || '0');

  const { data: order, isLoading, error } = useGetOrderQuery(orderId);
  const [cancelOrder, { isLoading: isCancelling }] = useCancelOrderMutation();
  const [processPayment, { isLoading: isProcessingPayment }] = useProcessPaymentMutation();
  const [updateOrderItems, { isLoading: isUpdatingOrder }] = useUpdateOrderItemsMutation();


  const [showRetryDialog, setShowRetryDialog] = useState(false);
  const [paymentForm, setPaymentForm] = useState({
    cardHolderName: '',
    cardNumber: '',
    expireMonth: '',
    expireYear: '',
    cvc: '',
  });


  const [showEditDialog, setShowEditDialog] = useState(false);
  const [editableItems, setEditableItems] = useState<EditableOrderItem[]>([]);
  const [productSearch, setProductSearch] = useState('');
  const [showProductSearch, setShowProductSearch] = useState(false);


  const { data: productsData } = useSearchProductsQuery(
    { search: productSearch, pageSize: 10, inStock: true },
    { skip: !showProductSearch || productSearch.length < 2 }
  );
  const handleEditDialogOpenChange = (open: boolean) => {
    if (open && order) {
      setEditableItems(mapOrderItemsToEditable(order.items));
    }
    setShowEditDialog(open);
  };


  const validateCardNumber = (cardNumber: string): boolean => {
    const digits = cardNumber.replace(/\s/g, '');
    if (digits.length < 13 || digits.length > 19) return false;

    let sum = 0;
    let isEven = false;

    for (let i = digits.length - 1; i >= 0; i--) {
      let digit = parseInt(digits[i], 10);

      if (isEven) {
        digit *= 2;
        if (digit > 9) digit -= 9;
      }

      sum += digit;
      isEven = !isEven;
    }

    return sum % 10 === 0;
  };

  const handleRetryPayment = async () => {
    if (!paymentForm.cardNumber || !paymentForm.expireMonth || !paymentForm.expireYear || !paymentForm.cvc) {
      toast.error('Lütfen kart bilgilerini doldurun');
      return;
    }

    if (!validateCardNumber(paymentForm.cardNumber)) {
      toast.error('Kart numarası geçersizdir');
      return;
    }

    if (paymentForm.cvc.length < 3) {
      toast.error('CVC en az 3 haneli olmalıdır');
      return;
    }

    try {
      const paymentResult = await processPayment({
        orderId: orderId,
        cardHolderName: paymentForm.cardHolderName,
        cardNumber: paymentForm.cardNumber.replace(/\s/g, ''),
        expiryDate: `${paymentForm.expireMonth}/${paymentForm.expireYear.slice(-2)}`,
        cvv: paymentForm.cvc,
      }).unwrap();

      if (paymentResult.status !== 'Success') {
        toast.error(paymentResult.errorMessage || 'Ödeme işlemi başarısız oldu. Lütfen kart bilgilerinizi kontrol edip tekrar deneyin.');
        return;
      }

      setShowRetryDialog(false);
      setPaymentForm({
        cardHolderName: '',
        cardNumber: '',
        expireMonth: '',
        expireYear: '',
        cvc: '',
      });
      toast.success('Ödeme başarıyla tamamlandı!');
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Ödeme işlemi başarısız oldu');
    }
  };

  const handleCancel = async () => {
    if (!confirm('Siparişi iptal etmek istediğinize emin misiniz?')) return;
    try {
      await cancelOrder(orderId).unwrap();
      toast.success('Sipariş iptal edildi');
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Sipariş iptal edilemedi');
    }
  };


  const handleQuantityChange = (productId: number, delta: number) => {
    setEditableItems(items =>
      items.map(item =>
        item.productId === productId
          ? { ...item, quantity: Math.max(1, item.quantity + delta) }
          : item
      )
    );
  };

  const handleRemoveItem = (productId: number) => {
    if (editableItems.length <= 1) {
      toast.error('Sipariş en az bir ürün içermelidir');
      return;
    }
    setEditableItems(items => items.filter(item => item.productId !== productId));
  };

  const handleAddProduct = (product: Product) => {
    const existingItem = editableItems.find(item => item.productId === product.id);
    if (existingItem) {
      handleQuantityChange(product.id, 1);
    } else {
      setEditableItems([
        ...editableItems,
        {
          productId: product.id,
          productName: product.name,
          quantity: 1,
          priceSnapshot: product.price,
        },
      ]);
    }
    setProductSearch('');
    setShowProductSearch(false);
    toast.success(`${product.name} eklendi`);
  };

  const handleSaveOrderItems = async () => {
    if (editableItems.length === 0) {
      toast.error('Sipariş en az bir ürün içermelidir');
      return;
    }

    try {
      await updateOrderItems({
        orderId,
        items: editableItems.map(item => ({
          productId: item.productId,
          quantity: item.quantity,
        })),
      }).unwrap();
      setShowEditDialog(false);
      toast.success('Sipariş güncellendi');
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Sipariş güncellenemedi');
    }
  };

  const calculateEditTotal = () => {
    const subtotal = editableItems.reduce((sum, item) => sum + item.priceSnapshot * item.quantity, 0);
    const shipping = subtotal >= 1000 ? 0 : 29.90;
    return { subtotal, shipping, total: subtotal + shipping };
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-32 mb-8" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-48" />
            <Skeleton className="h-64" />
          </div>
          <Skeleton className="h-48" />
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <Package className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
        <h2 className="text-2xl font-semibold mb-2">Sipariş Bulunamadı</h2>
        <p className="text-muted-foreground mb-6">
          Aradığınız sipariş mevcut değil.
        </p>
        <Button asChild>
          <Link to="/orders">Siparişlere Dön</Link>
        </Button>
      </div>
    );
  }

  const canCancel = ['PendingPayment', 'Paid', 'Processing'].includes(order.status);
  const canEdit = order.status === 'PendingPayment';

  return (
    <div className="container mx-auto px-4 py-8">
      <Button variant="ghost" asChild className="mb-8">
        <Link to="/orders">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Siparişlere Dön
        </Link>
      </Button>

      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold">Sipariş #{order.id}</h1>
          <p className="text-muted-foreground">
            {new Date(order.createdAt).toLocaleDateString('tr-TR', {
              year: 'numeric',
              month: 'long',
              day: 'numeric',
              hour: '2-digit',
              minute: '2-digit',
            })}
          </p>
        </div>
        <Badge className={statusColors[order.status]} variant="secondary">
          {statusLabels[order.status]}
        </Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-6">
          {/* Order Items */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Ürünler</CardTitle>
                {canEdit && (
                  <Button variant="outline" size="sm" onClick={() => handleEditDialogOpenChange(true)}>
                    <Edit className="mr-2 h-4 w-4" />
                    Düzenle
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {order.items.map((item, index) => (
                <div key={index} className="flex items-center gap-4">
                  <div className="h-16 w-16 bg-muted rounded-lg flex items-center justify-center flex-shrink-0">
                    <Package className="h-8 w-8 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-semibold truncate">{item.productName}</p>
                    <p className="text-sm text-muted-foreground">
                      {item.quantity} x {item.priceSnapshot.toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                  <p className="font-bold">
                    {item.lineTotal.toLocaleString('tr-TR')} ₺
                  </p>
                </div>
              ))}
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>Toplam</span>
                <span>{order.totalAmount.toLocaleString('tr-TR')} ₺</span>
              </div>
            </CardContent>
          </Card>

          {/* Action Buttons */}
          <div className="flex gap-4">
            {canCancel && (
              <Button
                variant="destructive"
                onClick={handleCancel}
                disabled={isCancelling}
              >
                <XCircle className="mr-2 h-4 w-4" />
                Siparişi İptal Et
              </Button>
            )}
          </div>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Shipping Address */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                <CardTitle className="text-base">Teslimat Adresi</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground text-sm">
                {order.shippingAddress}
              </p>
            </CardContent>
          </Card>

          {/* Payment Info */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                <CardTitle className="text-base">Ödeme Bilgisi</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              <p className="text-muted-foreground text-sm">
                Durum: {order.status === 'Paid' ? 'Ödeme alındı' : statusLabels[order.status]}
              </p>
              {order.payment?.status === 'Failed' && (
                <p className="text-sm text-destructive">
                  Hata: {order.payment.errorMessage || 'Ödeme başarısız'}
                </p>
              )}
              {/* Tekrar Öde butonu - sadece PendingPayment ve Failed durumunda */}
              {order.status === 'PendingPayment' && (
                <Button
                  className="w-full"
                  onClick={() => setShowRetryDialog(true)}
                >
                  <RefreshCw className="mr-2 h-4 w-4" />
                  Tekrar Öde
                </Button>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Retry Payment Dialog */}
      <Dialog open={showRetryDialog} onOpenChange={setShowRetryDialog}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Tekrar Ödeme Yap</DialogTitle>
            <DialogDescription>
              Sipariş #{order.id} için ödeme bilgilerini girin.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Kart Üzerindeki İsim</Label>
              <Input
                placeholder="AD SOYAD"
                value={paymentForm.cardHolderName}
                onChange={(e) =>
                  setPaymentForm({ ...paymentForm, cardHolderName: e.target.value.toUpperCase() })
                }
              />
            </div>
            <div className="space-y-2">
              <Label>Kart Numarası</Label>
              <Input
                placeholder="4111 1111 1111 1111"
                value={paymentForm.cardNumber}
                onChange={(e) => {
                  const value = e.target.value.replace(/\D/g, '').slice(0, 16);
                  const formatted = value.replace(/(\d{4})/g, '$1 ').trim();
                  setPaymentForm({ ...paymentForm, cardNumber: formatted });
                }}
              />
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label>Ay</Label>
                <Select
                  value={paymentForm.expireMonth}
                  onValueChange={(v) => setPaymentForm({ ...paymentForm, expireMonth: v })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Ay" />
                  </SelectTrigger>
                  <SelectContent>
                    {Array.from({ length: 12 }, (_, i) => (
                      <SelectItem key={i + 1} value={String(i + 1).padStart(2, '0')}>
                        {String(i + 1).padStart(2, '0')}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Yıl</Label>
                <Select
                  value={paymentForm.expireYear}
                  onValueChange={(v) => setPaymentForm({ ...paymentForm, expireYear: v })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Yıl" />
                  </SelectTrigger>
                  <SelectContent>
                    {Array.from({ length: 10 }, (_, i) => {
                      const year = new Date().getFullYear() + i;
                      return (
                        <SelectItem key={year} value={String(year)}>
                          {year}
                        </SelectItem>
                      );
                    })}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>CVC</Label>
                <Input
                  placeholder="123"
                  maxLength={4}
                  value={paymentForm.cvc}
                  onChange={(e) =>
                    setPaymentForm({ ...paymentForm, cvc: e.target.value.replace(/\D/g, '') })
                  }
                />
              </div>
            </div>
            <p className="text-xs text-muted-foreground">
              Test kartı: 4111 1111 1111 1111, herhangi bir tarih ve CVC
            </p>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowRetryDialog(false)}>
              İptal
            </Button>
            <Button onClick={handleRetryPayment} disabled={isProcessingPayment}>
              {isProcessingPayment && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Ödemeyi Tamamla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Order Dialog */}
      <Dialog open={showEditDialog} onOpenChange={handleEditDialogOpenChange}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Siparişi Düzenle</DialogTitle>
            <DialogDescription>
              Sipariş #{order.id} ürünlerini düzenleyin. Ürün ekleyebilir, çıkarabilir veya miktarları değiştirebilirsiniz.
            </DialogDescription>
          </DialogHeader>

          {/* Add Product Section */}
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Yeni Ürün Ekle</Label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Ürün ara..."
                  className="pl-9"
                  value={productSearch}
                  onChange={(e) => {
                    setProductSearch(e.target.value);
                    setShowProductSearch(e.target.value.length >= 2);
                  }}
                  onFocus={() => setShowProductSearch(productSearch.length >= 2)}
                />
              </div>
              {showProductSearch && productsData && productsData.items.length > 0 && (
                <div className="border rounded-lg mt-2 max-h-48 overflow-y-auto">
                  {productsData.items.map((product) => (
                    <button
                      key={product.id}
                      className="w-full px-4 py-2 text-left hover:bg-muted flex items-center justify-between"
                      onClick={() => handleAddProduct(product)}
                    >
                      <div>
                        <p className="font-medium">{product.name}</p>
                        <p className="text-sm text-muted-foreground">
                          {product.price.toLocaleString('tr-TR')} ₺ • Stok: {product.stockQuantity}
                        </p>
                      </div>
                      <Plus className="h-4 w-4" />
                    </button>
                  ))}
                </div>
              )}
            </div>

            <Separator />

            {/* Current Items */}
            <div className="space-y-3">
              <Label>Sipariş Ürünleri</Label>
              {editableItems.map((item) => (
                <div key={item.productId} className="flex items-center gap-4 p-3 border rounded-lg">
                  <div className="h-12 w-12 bg-muted rounded flex items-center justify-center flex-shrink-0">
                    <Package className="h-6 w-6 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">{item.productName}</p>
                    <p className="text-sm text-muted-foreground">
                      {item.priceSnapshot.toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => handleQuantityChange(item.productId, -1)}
                      disabled={item.quantity <= 1}
                    >
                      <Minus className="h-4 w-4" />
                    </Button>
                    <span className="w-8 text-center font-medium">{item.quantity}</span>
                    <Button
                      variant="outline"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => handleQuantityChange(item.productId, 1)}
                    >
                      <Plus className="h-4 w-4" />
                    </Button>
                  </div>
                  <p className="font-bold w-24 text-right">
                    {(item.priceSnapshot * item.quantity).toLocaleString('tr-TR')} ₺
                  </p>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-destructive hover:text-destructive"
                    onClick={() => handleRemoveItem(item.productId)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>

            <Separator />

            {/* Totals */}
            <div className="space-y-2">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Ara Toplam</span>
                <span>{calculateEditTotal().subtotal.toLocaleString('tr-TR')} ₺</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Kargo</span>
                <span>
                  {calculateEditTotal().shipping === 0 ? (
                    <span className="text-green-600">Ücretsiz</span>
                  ) : (
                    `${calculateEditTotal().shipping.toLocaleString('tr-TR')} ₺`
                  )}
                </span>
              </div>
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>Toplam</span>
                <span>{calculateEditTotal().total.toLocaleString('tr-TR')} ₺</span>
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setShowEditDialog(false)}>
              İptal
            </Button>
            <Button onClick={handleSaveOrderItems} disabled={isUpdatingOrder}>
              {isUpdatingOrder && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Değişiklikleri Kaydet
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
