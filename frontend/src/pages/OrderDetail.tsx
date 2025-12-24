import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useGetOrderQuery, useCancelOrderMutation, useProcessPaymentMutation } from '@/features/orders/ordersApi';
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
import { ArrowLeft, Package, MapPin, CreditCard, XCircle, RefreshCw, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import type { OrderStatus } from '@/features/orders/types';

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

export default function OrderDetail() {
  const { id } = useParams<{ id: string }>();
  const orderId = parseInt(id || '0');

  const { data: order, isLoading, error } = useGetOrderQuery(orderId);
  const [cancelOrder, { isLoading: isCancelling }] = useCancelOrderMutation();
  const [processPayment, { isLoading: isProcessingPayment }] = useProcessPaymentMutation();
  
  // Retry payment dialog state
  const [showRetryDialog, setShowRetryDialog] = useState(false);
  const [paymentForm, setPaymentForm] = useState({
    cardHolderName: '',
    cardNumber: '',
    expireMonth: '',
    expireYear: '',
    cvc: '',
  });

  // Luhn algoritması ile kart numarası doğrulama
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
              <CardTitle>Ürünler</CardTitle>
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

          {/* Cancel Button */}
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
    </div>
  );
}
