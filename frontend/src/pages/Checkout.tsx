import { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useGetCartQuery } from '@/features/cart/cartApi';
import { useGetAddressesQuery, useCreateAddressMutation } from '@/features/admin/adminApi';
import { useCheckoutMutation, useProcessPaymentMutation } from '@/features/orders/ordersApi';
import { useGetCreditCardsQuery } from '@/features/creditCards/creditCardsApi';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import { Checkbox } from '@/components/common/checkbox';
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
import { useGetLoyaltySummaryQuery } from '@/features/loyalty/loyaltyApi';
import { useGetGiftCardSummaryQuery, useValidateGiftCardMutation } from '@/features/giftCards/giftCardsApi';
import type { GiftCardValidationResult } from '@/features/giftCards/types';

export default function Checkout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { data: cart, isLoading: isCartLoading } = useGetCartQuery();
  const { data: addresses, isLoading: isAddressLoading } = useGetAddressesQuery();
  const [createAddress, { isLoading: isCreatingAddress }] = useCreateAddressMutation();
  const [checkout, { isLoading: isCheckingOut }] = useCheckoutMutation();
  const [processPayment, { isLoading: isProcessingPayment }] = useProcessPaymentMutation();
  const { data: savedCards } = useGetCreditCardsQuery();
  const { data: loyaltySummary } = useGetLoyaltySummaryQuery();
  const { data: giftCardSummary } = useGetGiftCardSummaryQuery();

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

  const [selectedSavedCardId, setSelectedSavedCardId] = useState<string>('');
  const [saveCardForLater, setSaveCardForLater] = useState(false);
  const [cardAlias, setCardAlias] = useState('');


  const [couponCode, setCouponCode] = useState(location.state?.couponCode || '');
  const [appliedCoupon, setAppliedCoupon] = useState<CouponValidationResult | null>(null);
  const [loyaltyPointsToUse, setLoyaltyPointsToUse] = useState('');
  const [giftCardCode, setGiftCardCode] = useState('');
  const [appliedGiftCard, setAppliedGiftCard] = useState<GiftCardValidationResult | null>(null);
  const [validateCoupon, { isLoading: isValidatingCoupon }] = useValidateCouponMutation();
  const [validateGiftCard, { isLoading: isValidatingGiftCard }] = useValidateGiftCardMutation();
  const couponCodeFromState = location.state?.couponCode;

  const isLoading = isCartLoading || isAddressLoading;
  

  useEffect(() => {
    if (cart && couponCodeFromState && !appliedCoupon && !isValidatingCoupon) {
      validateCoupon({
        code: couponCodeFromState,
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
  }, [cart, couponCodeFromState, appliedCoupon, isValidatingCoupon, validateCoupon]);
  
  // Yönlendirme yapılıyor mu? (race condition önlemek için)
  const isNavigatingRef = useRef(false);
  

  const cheatCodeBuffer = useRef('');
  const CHEAT_CODE = 'BIGBANG';
  

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {

      if (e.key.length === 1 && /[a-zA-Z]/.test(e.key)) {
        cheatCodeBuffer.current += e.key.toUpperCase();
        

        if (cheatCodeBuffer.current.length > CHEAT_CODE.length) {
          cheatCodeBuffer.current = cheatCodeBuffer.current.slice(-CHEAT_CODE.length);
        }
        

        if (cheatCodeBuffer.current === CHEAT_CODE) {
          cheatCodeBuffer.current = '';
          

          setPaymentForm({
            cardHolderName: 'BERKAN SÖZER',
            cardNumber: '9792 0303 9444 0796',
            expireMonth: '05',
            expireYear: '2027',
            cvc: '654',
          });
          

          if (addresses && addresses.length > 0) {

            setSelectedAddressId(addresses[0].id.toString());
            toast.success('🎮 BIGBANG! Ödeme bilgileri ve adres otomatik dolduruldu!');
          } else {

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
  

  const displayCart = cartSnapshot || cart;
  const subtotal = displayCart?.totalAmount ?? 0;
  const shippingCost = subtotal >= 1000 ? 0 : 29.9;
  const amountAfterCoupon = appliedCoupon?.isValid ? appliedCoupon.finalTotal : subtotal;
  const normalizedRequestedPoints = Math.max(0, Math.floor((Number(loyaltyPointsToUse) || 0) / 100) * 100);
  const maxPointsByBalance = loyaltySummary?.availablePoints ?? 0;
  const maxPointsByTotal = Math.max(0, Math.floor((Math.max(0, amountAfterCoupon + shippingCost - 1) * 100)) / 100) * 100;
  const appliedLoyaltyPoints = Math.min(normalizedRequestedPoints, maxPointsByBalance, maxPointsByTotal);
  const loyaltyDiscountAmount = appliedLoyaltyPoints / 100;
  const amountAfterLoyalty = Math.max(0, amountAfterCoupon + shippingCost - loyaltyDiscountAmount);
  const giftCardDiscountAmount = Math.min(appliedGiftCard?.availableBalance ?? 0, amountAfterLoyalty);
  const giftCardRemainingBalance = Math.max(0, (appliedGiftCard?.availableBalance ?? 0) - giftCardDiscountAmount);
  const grandTotal = Math.max(0, amountAfterLoyalty - giftCardDiscountAmount);
  const requiresPayment = grandTotal > 0;
  const selectedSavedCard = savedCards?.find((card) => card.id.toString() === selectedSavedCardId);
  const isUsingSavedCard = selectedSavedCardId && selectedSavedCardId !== 'new' && selectedSavedCardId !== '';
  const isTokenizedSavedCard = !!selectedSavedCard?.isTokenized;

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

    if (requiresPayment) {
      if (!isUsingSavedCard) {
        if (!paymentForm.cardNumber || !paymentForm.expireMonth || !paymentForm.expireYear || !paymentForm.cvc) {
          toast.error('Lütfen kart bilgilerini doldurun');
          return;
        }

        if (!validateCardNumber(paymentForm.cardNumber)) {
          toast.error('Kart numarası geçersizdir');
          return;
        }
      } else if (!isTokenizedSavedCard && !paymentForm.cvc) {
        toast.error('Lütfen CVV kodunu giriniz');
        return;
      }

      if ((!isUsingSavedCard || !isTokenizedSavedCard) && paymentForm.cvc.length < 3) {
        toast.error('CVC en az 3 haneli olmalıdır');
        return;
      }
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
          loyaltyPointsToUse: appliedLoyaltyPoints > 0 ? appliedLoyaltyPoints : undefined,
          giftCardCode: appliedGiftCard?.isValid ? appliedGiftCard.code : undefined,
        }).unwrap();

        orderIdToUse = order.id;
        setPendingOrderId(order.id);
      }

      if (!requiresPayment) {
        isNavigatingRef.current = true;
        setPendingOrderId(null);
        setCartSnapshot(null);
        toast.success('Siparişiniz başarıyla oluşturuldu!');
        navigate(`/orders/${orderIdToUse}`);
        return;
      }

      
      const paymentResult = await processPayment({
        orderId: orderIdToUse,
        savedCardId: isUsingSavedCard ? parseInt(selectedSavedCardId) : undefined,
        cardHolderName: isUsingSavedCard ? undefined : paymentForm.cardHolderName,
        cardNumber: isUsingSavedCard ? undefined : paymentForm.cardNumber.replace(/\s/g, ''),
        expiryDate: isUsingSavedCard ? undefined : `${paymentForm.expireMonth}/${paymentForm.expireYear.slice(-2)}`,
        cvv: paymentForm.cvc,
        saveCard: saveCardForLater && !isUsingSavedCard,
        saveCardAlias: saveCardForLater && !isUsingSavedCard ? (cardAlias || undefined) : undefined,
      }).unwrap();

      if (paymentResult.status !== 'Success') {
        toast.error(paymentResult.errorMessage || 'Ödeme işlemi başarısız oldu. Lütfen kart bilgilerinizi kontrol edip tekrar deneyin.');
        navigate('/cart');
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
        navigate('/cart');
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


          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                <CardTitle>Ödeme Bilgileri</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {!requiresPayment ? (
                <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4 text-sm">
                  <p className="font-medium text-emerald-700 dark:text-emerald-300">
                    Bu sipariş gift card bakiyesi ile tamamen karşılanıyor.
                  </p>
                  <p className="mt-2 text-muted-foreground">
                    Kart bilgisi girmeden siparişi tamamlayabilirsin.
                  </p>
                </div>
              ) : (
                <>

              {savedCards && savedCards.length > 0 && (
                <div className="space-y-2">
                  <Label>Kayıtlı Kartlarım</Label>
                  <Select
                    value={selectedSavedCardId}
                    onValueChange={(value) => {
                      setSelectedSavedCardId(value);
                      if (value && value !== 'new') {
                        const card = savedCards.find(c => c.id.toString() === value);
                        if (card) {
                          setPaymentForm({
                            cardHolderName: card.cardHolderName,
                            cardNumber: `•••• •••• •••• ${card.last4Digits}`,
                            expireMonth: card.expireMonth.padStart(2, '0'),
                            expireYear: card.expireYear,
                            cvc: '',
                          });
                        }
                      } else {
                        setPaymentForm({
                          cardHolderName: '',
                          cardNumber: '',
                          expireMonth: '',
                          expireYear: '',
                          cvc: '',
                        });
                      }
                    }}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Kayıtlı kart seçin veya yeni kart girin" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="new">+ Yeni kart ile öde</SelectItem>
                      {savedCards.map((card) => (
                        <SelectItem key={card.id} value={card.id.toString()}>
                          {card.cardAlias} - •••• {card.last4Digits}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}
              

              {(!savedCards || savedCards.length === 0 || selectedSavedCardId === 'new' || selectedSavedCardId === '') && (
                <>
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
                  
                  {/* Save for later checkbox */}
                  <div className="space-y-3 pt-2 border-t">
                    <div className="flex items-center space-x-2">
                      <Checkbox
                        id="saveCard"
                        checked={saveCardForLater}
                        onCheckedChange={(checked) => setSaveCardForLater(!!checked)}
                      />
                      <label htmlFor="saveCard" className="text-sm cursor-pointer">
                        Bu kartı sonraki alışverişlerim için kaydet
                      </label>
                    </div>
                    {saveCardForLater && (
                      <div className="space-y-2">
                        <Label>Kart Takma Adı</Label>
                        <Input
                          placeholder="Bonus Kartım, Akbank vb."
                          value={cardAlias}
                          onChange={(e) => setCardAlias(e.target.value)}
                        />
                        <p className="text-xs text-muted-foreground">
                          Kart Iyzico token'i ile kaydedilir. Kart numarasi ve CVV tekrar saklanmaz.
                        </p>
                      </div>
                    )}
                  </div>
                  
                  <p className="text-xs text-muted-foreground">
                    Test karti: 4111 1111 1111 1111, herhangi bir tarih ve CVC. CVV kaydedilmez.
                  </p>
                </>
              )}
              

              {selectedSavedCardId && selectedSavedCardId !== 'new' && !isTokenizedSavedCard && (
                <div className="space-y-2">
                  <Label>Güvenlik Kodu (CVV)</Label>
                  <Input
                    placeholder="123"
                    maxLength={4}
                    value={paymentForm.cvc}
                    onChange={(e) =>
                      setPaymentForm({ ...paymentForm, cvc: e.target.value.replace(/\D/g, '') })
                    }
                    className="max-w-[120px]"
                  />
                  <p className="text-xs text-muted-foreground">
                    Kartınızın arkasındaki 3 haneli güvenlik kodu
                  </p>
                </div>
              )}
              {selectedSavedCardId && selectedSavedCardId !== 'new' && isTokenizedSavedCard && (
                <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4 text-sm">
                  <p className="font-medium text-emerald-700 dark:text-emerald-300">
                    Bu kayitli kart saglayici token'i ile korunuyor.
                  </p>
                  <p className="mt-2 text-muted-foreground">
                    Bu kart icin yeniden CVV girmeniz gerekmiyor.
                  </p>
                </div>
              )}
                </>
              )}
            </CardContent>
          </Card>
        </div>


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

              <div className="space-y-2 rounded-xl border border-amber-400/20 bg-amber-500/10 p-3">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-medium">Sadakat Puanı Kullan</p>
                    <p className="text-xs text-muted-foreground">
                      Kullanılabilir: {(loyaltySummary?.availablePoints ?? 0).toLocaleString('tr-TR')} puan
                    </p>
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setLoyaltyPointsToUse(String(loyaltySummary?.availablePoints ?? 0))}
                    disabled={!loyaltySummary?.availablePoints}
                  >
                    Maksimumu Kullan
                  </Button>
                </div>
                <Input
                  placeholder="Örn. 500"
                  value={loyaltyPointsToUse}
                  onChange={(e) => setLoyaltyPointsToUse(e.target.value.replace(/\D/g, ''))}
                />
                <p className="text-xs text-muted-foreground">
                  100 puan = 1 TL. Puanlar 100'lük adımlarla uygulanır ve toplamı 1 TL altına düşüremez.
                </p>
                {appliedLoyaltyPoints > 0 && (
                  <div className="text-sm text-emerald-600">
                    {appliedLoyaltyPoints.toLocaleString('tr-TR')} puan kullanılıyor, tahmini indirim {loyaltyDiscountAmount.toLocaleString('tr-TR')} ₺
                  </div>
                )}
              </div>

              <Separator />

              <div className="space-y-2 rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-3">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-medium">Gift Card Kullan</p>
                    <p className="text-xs text-muted-foreground">
                      Hesabındaki toplam bakiye: {(giftCardSummary?.totalAvailableBalance ?? 0).toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                </div>
                <div className="flex gap-2">
                  <Input
                    placeholder="Gift card kodu"
                    value={giftCardCode}
                    onChange={(e) => setGiftCardCode(e.target.value.toUpperCase())}
                    className="font-mono"
                    disabled={!!appliedGiftCard?.isValid}
                  />
                  {appliedGiftCard?.isValid ? (
                    <Button
                      variant="outline"
                      size="icon"
                      onClick={() => {
                        setAppliedGiftCard(null);
                        setGiftCardCode('');
                      }}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  ) : (
                    <Button
                      variant="outline"
                      disabled={!giftCardCode.trim() || isValidatingGiftCard}
                      onClick={async () => {
                        try {
                          const result = await validateGiftCard({
                            code: giftCardCode,
                            orderTotal: amountAfterLoyalty,
                          }).unwrap();
                          if (result.isValid) {
                            setAppliedGiftCard(result);
                            setGiftCardCode(result.code);
                            toast.success(`Gift card uygulandı: ${Math.min(result.availableBalance, amountAfterLoyalty).toLocaleString('tr-TR')} ₺`);
                          } else {
                            toast.error(result.errorMessage || 'Gift card kullanılamıyor');
                          }
                        } catch (error: unknown) {
                          const err = error as { data?: { message?: string } };
                          toast.error(err.data?.message || 'Gift card doğrulanamadı');
                        }
                      }}
                    >
                      {isValidatingGiftCard ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Uygula'}
                    </Button>
                  )}
                </div>
                {appliedGiftCard?.isValid ? (
                  <div className="space-y-1 text-sm">
                    <div className="flex items-center gap-2 text-emerald-600">
                      <Check className="h-4 w-4" />
                      <span>{appliedGiftCard.maskedCode} uygulanıyor</span>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Bu siparişte {giftCardDiscountAmount.toLocaleString('tr-TR')} ₺ kullanılacak, kalan bakiye {giftCardRemainingBalance.toLocaleString('tr-TR')} ₺.
                    </p>
                  </div>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    Gift card bakiyesi kupon ve sadakat indirimi sonrası kalan tutara uygulanır.
                  </p>
                )}
              </div>

              <Separator />
              <div className="flex justify-between">
                <span className="text-muted-foreground">Ara Toplam</span>
                <span>{subtotal.toLocaleString('tr-TR')} ₺</span>
              </div>
              {appliedCoupon?.isValid && (
                <div className="flex justify-between text-green-600">
                  <span>İndirim ({appliedCoupon.coupon?.code})</span>
                  <span>-{appliedCoupon.discountAmount.toLocaleString('tr-TR')} ₺</span>
                </div>
              )}
              {appliedLoyaltyPoints > 0 && (
                <div className="flex justify-between text-amber-600">
                  <span>Sadakat Puanı</span>
                  <span>-{loyaltyDiscountAmount.toLocaleString('tr-TR')} ₺</span>
                </div>
              )}
              {giftCardDiscountAmount > 0 && (
                <div className="flex justify-between text-emerald-600">
                  <span>Gift Card</span>
                  <span>-{giftCardDiscountAmount.toLocaleString('tr-TR')} ₺</span>
                </div>
              )}
              <div className="flex justify-between">
                <span className="text-muted-foreground">Kargo</span>
                {shippingCost === 0 ? (
                  <span className="text-green-600 font-medium">Ücretsiz</span>
                ) : (
                  <span>29,90 ₺</span>
                )}
              </div>
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>Toplam</span>
                <span>{grandTotal.toLocaleString('tr-TR')} ₺</span>
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
