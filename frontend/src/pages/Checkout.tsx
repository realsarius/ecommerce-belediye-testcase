import { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useGetCartQuery } from '@/features/cart/cartApi';
import { useGetAddressesQuery, useCreateAddressMutation } from '@/features/admin/adminApi';
import { useCheckoutMutation, useProcessPaymentMutation } from '@/features/orders/ordersApi';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
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
import { Package, CreditCard, MapPin, Loader2, Plus, Ticket, X, Check } from 'lucide-react';
import { toast } from 'sonner';
import { v4 as uuidv4 } from 'uuid';
import { useValidateCouponMutation } from '@/features/coupons/couponsApi';
import type { CouponValidationResult } from '@/features/coupons/types';

export default function Checkout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { data: cart, isLoading: isCartLoading } = useGetCartQuery();
  const { data: addresses, isLoading: isAddressLoading } = useGetAddressesQuery();
  const [createAddress, { isLoading: isCreatingAddress }] = useCreateAddressMutation();
  const [checkout, { isLoading: isCheckingOut }] = useCheckoutMutation();
  const [processPayment, { isLoading: isProcessingPayment }] = useProcessPaymentMutation();

  const [selectedAddressId, setSelectedAddressId] = useState<string>('');
  const [showAddressDialog, setShowAddressDialog] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState<number | null>(null);
  const [cartSnapshot, setCartSnapshot] = useState<typeof cart | null>(null);
  const [addressForm, setAddressForm] = useState({
    title: '',
    fullName: '',
    phone: '',
    city: '',
    district: '',
    addressLine: '',
    postalCode: '',
    isDefault: false,
  });

  const [paymentForm, setPaymentForm] = useState({
    cardHolderName: '',
    cardNumber: '',
    expireMonth: '',
    expireYear: '',
    cvc: '',
  });

  // Coupon state
  const [couponCode, setCouponCode] = useState(location.state?.couponCode || '');
  const [appliedCoupon, setAppliedCoupon] = useState<CouponValidationResult | null>(null);
  const [validateCoupon, { isLoading: isValidatingCoupon }] = useValidateCouponMutation();

  const isLoading = isCartLoading || isAddressLoading;
  
  // Auto-apply coupon from cart
  useEffect(() => {
    if (cart && location.state?.couponCode && !appliedCoupon && !isValidatingCoupon) {
      validateCoupon({
        code: location.state.couponCode,
        orderTotal: cart.totalAmount,
      })
        .unwrap()
        .then((result) => {
          if (result.isValid) {
            setAppliedCoupon(result);
            toast.success(`Sepet indirimi uygulandı: %${result.coupon?.value}`);
          }
        })
        .catch(() => {
          // Silent fail
        });
    }
  }, [cart, location.state?.couponCode]);
  
  // Yönlendirme yapılıyor mu? (race condition önlemek için)
  const isNavigatingRef = useRef(false);
  
  // BIGBANG cheat code buffer
  const cheatCodeBuffer = useRef('');
  const CHEAT_CODE = 'BIGBANG';
  
  // BIGBANG cheat code listener
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Sadece harf tuşlarını al
      if (e.key.length === 1 && /[a-zA-Z]/.test(e.key)) {
        cheatCodeBuffer.current += e.key.toUpperCase();
        
        // Buffer'ı son 7 karakterle sınırla (BIGBANG uzunluğu)
        if (cheatCodeBuffer.current.length > CHEAT_CODE.length) {
          cheatCodeBuffer.current = cheatCodeBuffer.current.slice(-CHEAT_CODE.length);
        }
        
        // BIGBANG yazıldı mı kontrol et
        if (cheatCodeBuffer.current === CHEAT_CODE) {
          cheatCodeBuffer.current = '';
          
          // Ödeme bilgilerini doldur
          setPaymentForm({
            cardHolderName: 'BERKAN SÖZER',
            cardNumber: '9792 0303 9444 0796',
            expireMonth: '05',
            expireYear: '2027',
            cvc: '654',
          });
          
          // Teslimat adresi kontrolü
          if (addresses && addresses.length > 0) {
            // İlk adresi seç
            setSelectedAddressId(addresses[0].id.toString());
            toast.success('🎮 BIGBANG! Ödeme bilgileri ve adres otomatik dolduruldu!');
          } else {
            // Yeni adres formunu aç ve doldur
            setAddressForm({
              title: 'Ev',
              fullName: 'Ahmet Yılmaz',
              phone: '0543 954 45 21',
              city: 'Manisa',
              district: 'Salihli',
              addressLine: '321 sk No 9',
              postalCode: '45300',
              isDefault: false,
            });
            setShowAddressDialog(true);
            toast.success('🎮 BIGBANG! Ödeme bilgileri dolduruldu, adres formunu kaydedin!');
          }
        }
      }
    };
    
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [addresses]);
  
  // Sepet snapshot'ı güncelle (sadece sepet doluyken)
  const displayCart = cartSnapshot || cart;

  const handleAddressSubmit = async () => {
    try {
      const result = await createAddress(addressForm).unwrap();
      setSelectedAddressId(result.id.toString());
      setShowAddressDialog(false);
      setAddressForm({
        title: '',
        fullName: '',
        phone: '',
        city: '',
        district: '',
        addressLine: '',
        postalCode: '',
        isDefault: false,
      });
      toast.success('Adres eklendi');
    } catch {
      toast.error('Adres eklenemedi');
    }
  };

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

  const handleCheckout = async () => {
    if (!selectedAddressId) {
      toast.error('Lütfen teslimat adresi seçin');
      return;
    }
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

    if (cart && !cartSnapshot) {
      setCartSnapshot(cart);
    }

    let orderIdToUse = pendingOrderId;

    try {
      if (!orderIdToUse) {
        const selectedAddress = addresses?.find(a => a.id.toString() === selectedAddressId);
        if (!selectedAddress) throw new Error('Adres bulunamadı');

        const addressString = `${selectedAddress.fullName}, ${selectedAddress.addressLine}, ${selectedAddress.district}/${selectedAddress.city} P.K: ${selectedAddress.postalCode} - Tel: ${selectedAddress.phone}`;

        const order = await checkout({
          shippingAddress: addressString,
          paymentMethod: 'CreditCard',
          notes: '',
          idempotencyKey: uuidv4(),
          couponCode: appliedCoupon?.isValid ? appliedCoupon.coupon?.code : undefined,
        }).unwrap();

        orderIdToUse = order.id;
        setPendingOrderId(order.id);
      }

      const paymentResult = await processPayment({
        orderId: orderIdToUse,
        cardHolderName: paymentForm.cardHolderName,
        cardNumber: paymentForm.cardNumber.replace(/\s/g, ''),
        expiryDate: `${paymentForm.expireMonth}/${paymentForm.expireYear.slice(-2)}`,
        cvv: paymentForm.cvc,
      }).unwrap();

      if (paymentResult.status !== 'Success') {
        toast.error(paymentResult.errorMessage || 'Ödeme işlemi başarısız oldu. Lütfen kart bilgilerinizi kontrol edip tekrar deneyin.');
        return;
      }

      isNavigatingRef.current = true;
      setPendingOrderId(null);
      setCartSnapshot(null);
      toast.success('Siparişiniz başarıyla oluşturuldu!');
      navigate(`/orders/${orderIdToUse}`);
    } catch (error: unknown) {
      if (orderIdToUse) {
        if (error instanceof Error) {
          toast.error(error.message + ' Lütfen kart bilgilerinizi kontrol edip tekrar deneyin.');
        } else {
          const err = error as { data?: { message?: string } };
          toast.error((err.data?.message || 'Ödeme işlemi başarısız') + ' Lütfen tekrar deneyin.');
        }
        return;
      }

      if (error instanceof Error) {
        toast.error(error.message);
        return;
      }
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Sipariş oluşturulamadı');
    }
  };

  // Sepet boşsa ve loading değilse cart'a yönlendir
  useEffect(() => {
    if (!isLoading && !pendingOrderId && !isNavigatingRef.current && (!cart || cart.items.length === 0)) {
      navigate('/cart');
    }
  }, [cart, navigate, isLoading, pendingOrderId]);

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-48 mb-8" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-48" />
            <Skeleton className="h-64" />
          </div>
          <Skeleton className="h-64" />
        </div>
      </div>
    );
  }

  if (!displayCart || displayCart.items.length === 0) {
    return null;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Ödeme</h1>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-6">
          {/* Shipping Address */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <MapPin className="h-5 w-5" />
                  <CardTitle>Teslimat Adresi</CardTitle>
                </div>
                <Button variant="outline" size="sm" onClick={() => setShowAddressDialog(true)}>
                  <Plus className="mr-2 h-4 w-4" />
                  Yeni Adres
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {addresses && addresses.length > 0 ? (
                <Select value={selectedAddressId} onValueChange={setSelectedAddressId}>
                  <SelectTrigger>
                    <SelectValue placeholder="Adres seçin" />
                  </SelectTrigger>
                  <SelectContent>
                    {addresses.map((addr) => (
                      <SelectItem key={addr.id} value={addr.id.toString()}>
                        {addr.title} - {addr.city}, {addr.district}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              ) : (
                <p className="text-muted-foreground">
                  Kayıtlı adresiniz yok. Yeni adres ekleyin.
                </p>
              )}
            </CardContent>
          </Card>

          {/* Payment */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                <CardTitle>Ödeme Bilgileri</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>Kart Üzerindeki İsim</Label>
                <Input
                  placeholder="KAMURAN OLTACI"
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
            </CardContent>
          </Card>
        </div>

        {/* Order Summary */}
        <div>
          <Card className="sticky top-24">
            <CardHeader>
              <CardTitle>Sipariş Özeti</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {displayCart.items.map((item) => (
                <div key={item.productId} className="flex gap-3">
                  <div className="h-12 w-12 bg-muted rounded flex items-center justify-center flex-shrink-0">
                    <Package className="h-6 w-6 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate text-sm">{item.productName}</p>
                    <p className="text-sm text-muted-foreground">
                      {item.quantity} x {item.unitPrice.toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                  <p className="font-medium text-sm">
                    {item.totalPrice.toLocaleString('tr-TR')} ₺
                  </p>
                </div>
              ))}
              <Separator />
              
              {/* Kupon Girişi */}
              <div className="space-y-2">
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <Ticket className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Kupon kodu"
                      value={couponCode}
                      onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
                      className="pl-9 font-mono"
                      disabled={!!appliedCoupon?.isValid}
                    />
                  </div>
                  {appliedCoupon?.isValid ? (
                    <Button
                      variant="outline"
                      size="icon"
                      onClick={() => {
                        setAppliedCoupon(null);
                        setCouponCode('');
                      }}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  ) : (
                    <Button
                      variant="outline"
                      disabled={!couponCode.trim() || isValidatingCoupon}
                      onClick={async () => {
                        try {
                          const result = await validateCoupon({
                            code: couponCode,
                            orderTotal: displayCart.totalAmount,
                          }).unwrap();
                          if (result.isValid) {
                            setAppliedCoupon(result);
                            toast.success(`Kupon uygulandı: ${result.discountAmount.toLocaleString('tr-TR')} ₺ indirim`);
                          } else {
                            toast.error(result.errorMessage || 'Geçersiz kupon kodu');
                          }
                        } catch {
                          toast.error('Kupon doğrulanamadı');
                        }
                      }}
                    >
                      {isValidatingCoupon ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Uygula'}
                    </Button>
                  )}
                </div>
                {appliedCoupon?.isValid && (
                  <div className="flex items-center gap-2 text-sm text-green-600">
                    <Check className="h-4 w-4" />
                    <span>{appliedCoupon.coupon?.code} kuponu uygulandı</span>
                  </div>
                )}
              </div>
              
              <Separator />
              <div className="flex justify-between">
                <span className="text-muted-foreground">Ara Toplam</span>
                <span>{displayCart.totalAmount.toLocaleString('tr-TR')} ₺</span>
              </div>
              {appliedCoupon?.isValid && (
                <div className="flex justify-between text-green-600">
                  <span>İndirim ({appliedCoupon.coupon?.code})</span>
                  <span>-{appliedCoupon.discountAmount.toLocaleString('tr-TR')} ₺</span>
                </div>
              )}
              <div className="flex justify-between">
                <span className="text-muted-foreground">Kargo</span>
                {displayCart.totalAmount >= 1000 ? (
                  <span className="text-green-600 font-medium">Ücretsiz</span>
                ) : (
                  <span>29,90 ₺</span>
                )}
              </div>
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>Toplam</span>
                <span>
                  {((appliedCoupon?.isValid ? appliedCoupon.finalTotal : displayCart.totalAmount) + (displayCart.totalAmount >= 1000 ? 0 : 29.90)).toLocaleString('tr-TR')} ₺
                </span>
              </div>
              <Button
                className="w-full"
                size="lg"
                onClick={handleCheckout}
                disabled={isCheckingOut || isProcessingPayment}
              >
                {(isCheckingOut || isProcessingPayment) && (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                )}
                Siparişi Tamamla
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Add Address Dialog */}
      <Dialog open={showAddressDialog} onOpenChange={setShowAddressDialog}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Yeni Adres Ekle</DialogTitle>
            <DialogDescription>Teslimat için adres bilgilerini doldurun.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Adres Başlığı</Label>
              <Input
                placeholder="Ev, İş vb."
                value={addressForm.title}
                onChange={(e) => setAddressForm({ ...addressForm, title: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Ad Soyad</Label>
              <Input
                placeholder="Kamuran Oltacı"
                value={addressForm.fullName}
                onChange={(e) => setAddressForm({ ...addressForm, fullName: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Telefon</Label>
              <Input
                placeholder="05XX XXX XX XX"
                value={addressForm.phone}
                onChange={(e) => setAddressForm({ ...addressForm, phone: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>İl</Label>
                <Input
                  placeholder="İstanbul"
                  value={addressForm.city}
                  onChange={(e) => setAddressForm({ ...addressForm, city: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>İlçe</Label>
                <Input
                  placeholder="Kadıköy"
                  value={addressForm.district}
                  onChange={(e) => setAddressForm({ ...addressForm, district: e.target.value })}
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label>Adres</Label>
              <Input
                placeholder="Mahalle, Sokak, No"
                value={addressForm.addressLine}
                onChange={(e) => setAddressForm({ ...addressForm, addressLine: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Posta Kodu</Label>
              <Input
                placeholder="34000"
                value={addressForm.postalCode}
                onChange={(e) => setAddressForm({ ...addressForm, postalCode: e.target.value })}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddressDialog(false)}>
              İptal
            </Button>
            <Button onClick={handleAddressSubmit} disabled={isCreatingAddress}>
              {isCreatingAddress && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Kaydet
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
