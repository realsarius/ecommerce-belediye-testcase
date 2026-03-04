import { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useGetOrderQuery, useCancelOrderMutation, useProcessPaymentMutation, useUpdateOrderItemsMutation } from '@/features/orders/ordersApi';
import { useSearchProductsQuery } from '@/features/products/productsApi';
import { useGetMyReturnRequestsQuery } from '@/features/returns/returnsApi';
import { useReorderCartMutation } from '@/features/cart/cartApi';
import { useGetFrontendFeaturesQuery } from '@/features/settings/settingsApi';
import { ConfirmModal } from '@/components/admin/ConfirmModal';
import { PaymentProviderLogo, getPaymentProviderLabel } from '@/components/order/PaymentProviderLogo';
import { ReturnTimeline } from '@/components/order/ReturnTimeline';
import { ShipmentTimeline } from '@/components/order/ShipmentTimeline';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
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
import { ArrowLeft, Package, MapPin, CreditCard, FileText, XCircle, RefreshCw, Loader2, Edit, Plus, Minus, Trash2, Search, RotateCcw, ShoppingBag, CalendarDays, Hash, Wallet } from 'lucide-react';
import { toast } from 'sonner';
import type { Order, OrderItem } from '@/features/orders/types';
import type { ReturnRequest } from '@/features/returns/types';
import type { Product } from '@/features/products/types';
import { getOrderStatusLabel, getOrderStatusTone } from '@/lib/orderStatus';

const paymentProviderBadgeClasses = {
  Iyzico: 'bg-sky-500/10 text-sky-700 dark:text-sky-300',
  Stripe: 'bg-indigo-500/10 text-indigo-700 dark:text-indigo-300',
  PayTR: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300',
} as const;

const getPaymentStatusBadge = (status?: string) => {
  switch (status) {
    case 'Success':
      return { label: 'Ödeme Alındı', className: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300' };
    case 'Pending':
      return { label: 'Ödeme Bekleniyor', className: 'bg-amber-500/10 text-amber-700 dark:text-amber-300' };
    case 'Failed':
      return { label: 'Ödeme Başarısız', className: 'bg-rose-500/10 text-rose-700 dark:text-rose-300' };
    case 'Refunded':
      return { label: 'İade Edildi', className: 'bg-slate-500/10 text-slate-700 dark:text-slate-300' };
    default:
      return null;
  }
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

const getReturnDaysRemaining = (deliveredAt?: string) => {
  if (!deliveredAt) {
    return null;
  }

  const diffInMs = new Date(deliveredAt).getTime() + (14 * 24 * 60 * 60 * 1000) - Date.now();
  return Math.max(0, Math.ceil(diffInMs / (24 * 60 * 60 * 1000)));
};

const getReturnActionLabel = (order: Order) => {
  if (order.status === 'Delivered') {
    const daysRemaining = getReturnDaysRemaining(order.deliveredAt);
    if (daysRemaining === 0) {
      return null;
    }

    return 'İade Talebi Oluştur';
  }

  if (order.status === 'Paid' || order.status === 'Processing') {
    return 'İptal Talebi Oluştur';
  }

  return null;
};

const getLatestReturnRequest = (requests: ReturnRequest[] | undefined, orderId: number) => {
  const matches = (requests ?? [])
    .filter((request) => request.orderId === orderId)
    .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime());

  return matches[0];
};

const hasActiveReturnRequest = (request: ReturnRequest | undefined) =>
  request?.status === 'Pending' || request?.status === 'Approved' || request?.status === 'RefundPending';

const formatOrderDate = (value: string) =>
  new Date(value).toLocaleDateString('tr-TR', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

export default function OrderDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const orderId = parseInt(id || '0');

  const { data: order, isLoading, error } = useGetOrderQuery(orderId);
  const { data: returnRequests, isLoading: isReturnRequestsLoading } = useGetMyReturnRequestsQuery();
  const { data: frontendFeatures } = useGetFrontendFeaturesQuery();
  const [cancelOrder, { isLoading: isCancelling }] = useCancelOrderMutation();
  const [processPayment, { isLoading: isProcessingPayment }] = useProcessPaymentMutation();
  const [updateOrderItems, { isLoading: isUpdatingOrder }] = useUpdateOrderItemsMutation();
  const [reorderCart, { isLoading: isReorderingCart }] = useReorderCartMutation();


  const [showRetryDialog, setShowRetryDialog] = useState(false);
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);
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
    try {
      await cancelOrder(orderId).unwrap();
      setShowCancelConfirm(false);
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

  const handleReorder = async () => {
    try {
      const result = await reorderCart({ orderId }).unwrap();
      const message = result.skippedCount === 0
        ? `${result.addedCount} ürün sepete eklendi.`
        : `${result.requestedCount} üründen ${result.addedCount} ürün sepete eklendi, ${result.skippedCount} ürün atlandı.`;

      if (result.addedCount > 0) {
        toast.success(message);
        if (result.skippedProducts.length > 0) {
          toast.info(
            result.skippedProducts
              .slice(0, 3)
              .map((item) => `${item.name}: ${item.reason}`)
              .join('\n')
          );
        }
        navigate('/cart');
        return;
      }

      toast.info(message);
    } catch (err: unknown) {
      const error = err as { data?: { message?: string } };
      toast.error(error.data?.message || 'Ürünler yeniden sepete eklenemedi');
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

  const canCancel = order.status === 'PendingPayment';
  const canEdit = order.status === 'PendingPayment';
  const latestReturnRequest = getLatestReturnRequest(returnRequests, order.id);
  const effectiveFrontendFeatures = frontendFeatures ?? {
    enableCheckoutLegalConsents: true,
    enableCheckoutInvoiceInfo: true,
    enableShipmentTimeline: true,
    enableReturnAttachments: true,
  };
  const returnActionLabel = hasActiveReturnRequest(latestReturnRequest) ? null : getReturnActionLabel(order);
  const paymentStatusBadge = getPaymentStatusBadge(order.payment?.status);
  const paymentSummaryLabel = order.payment?.status === 'Success'
    ? 'Ödeme alındı'
    : getOrderStatusLabel(order.status);
  const pricingRows = [
    order.discountAmount
      ? { label: 'Kupon İndirimi', value: `-${order.discountAmount.toLocaleString('tr-TR')} ₺`, className: 'text-green-600' }
      : null,
    order.loyaltyDiscountAmount
      ? { label: 'Sadakat Puanı', value: `-${order.loyaltyDiscountAmount.toLocaleString('tr-TR')} ₺`, className: 'text-amber-600' }
      : null,
    order.giftCardAmount
      ? { label: `Gift Card${order.giftCardCode ? ` (${order.giftCardCode})` : ''}`, value: `-${order.giftCardAmount.toLocaleString('tr-TR')} ₺`, className: 'text-emerald-600' }
      : null,
  ].filter(Boolean) as { label: string; value: string; className: string }[];

  return (
    <div className="container mx-auto px-4 py-8">
      <Button variant="ghost" asChild className="mb-8">
        <Link to="/orders">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Siparişlere Dön
        </Link>
      </Button>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1.7fr)_minmax(320px,1fr)]">
        <div className="space-y-6">
          <Card>
            <CardHeader className="gap-5">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div className="space-y-2">
                  <div className="flex flex-wrap items-center gap-3">
                    <CardTitle className="text-3xl">Sipariş #{order.id}</CardTitle>
                    <StatusBadge
                      label={getOrderStatusLabel(order.status)}
                      tone={getOrderStatusTone(order.status)}
                    />
                  </div>
                  <p className="text-sm text-muted-foreground">{formatOrderDate(order.createdAt)}</p>
                </div>
                <div className="grid min-w-full gap-3 sm:min-w-[22rem] sm:grid-cols-3">
                  <div className="rounded-2xl border bg-muted/20 p-4">
                    <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                      <Hash className="h-4 w-4" />
                      Sipariş Özeti
                    </div>
                    <p className="mt-2 text-lg font-semibold">{order.items.length} ürün</p>
                    <p className="text-xs text-muted-foreground">Sepet satırları dahil</p>
                  </div>
                  <div className="rounded-2xl border bg-muted/20 p-4">
                    <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                      <CalendarDays className="h-4 w-4" />
                      Teslimat
                    </div>
                    <p className="mt-2 text-lg font-semibold">
                      {order.deliveredAt ? 'Teslim Edildi' : order.estimatedDeliveryDate ? 'Planlandı' : 'Bekleniyor'}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {order.deliveredAt
                        ? formatOrderDate(order.deliveredAt)
                        : order.estimatedDeliveryDate
                          ? formatOrderDate(order.estimatedDeliveryDate)
                          : 'Kargo planı bekleniyor'}
                    </p>
                  </div>
                  <div className="rounded-2xl border bg-muted/20 p-4">
                    <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                      <Wallet className="h-4 w-4" />
                      Toplam
                    </div>
                    <p className="mt-2 text-lg font-semibold">{order.totalAmount.toLocaleString('tr-TR')} ₺</p>
                    <p className="text-xs text-muted-foreground">{paymentSummaryLabel}</p>
                  </div>
                </div>
              </div>
            </CardHeader>
          </Card>

          <Card>
            <CardHeader>
              <div className="flex items-center justify-between gap-4">
                <div>
                  <CardTitle>Ürünler</CardTitle>
                  <p className="text-sm text-muted-foreground">Siparişinizdeki ürünler ve fiyat özeti</p>
                </div>
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
                <div key={index} className="flex items-center gap-4 rounded-2xl border p-4">
                  <div className="flex h-16 w-16 flex-shrink-0 items-center justify-center rounded-xl bg-muted">
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
              <div className="rounded-2xl border bg-muted/20 p-4">
                <div className="space-y-3">
                  {pricingRows.map((row) => (
                    <div key={row.label} className={`flex justify-between text-sm ${row.className}`}>
                      <span>{row.label}</span>
                      <span>{row.value}</span>
                    </div>
                  ))}
                  {pricingRows.length > 0 ? <Separator /> : null}
                  <div className="flex justify-between text-lg font-bold">
                    <span>Toplam</span>
                    <span>{order.totalAmount.toLocaleString('tr-TR')} ₺</span>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {effectiveFrontendFeatures.enableShipmentTimeline ? (
            <Card>
              <CardHeader>
                <CardTitle>Kargo Takibi</CardTitle>
              </CardHeader>
              <CardContent>
                <ShipmentTimeline order={order} />
              </CardContent>
            </Card>
          ) : null}

          {latestReturnRequest && !isReturnRequestsLoading ? (
            <Card>
              <CardHeader>
                <CardTitle>İade Süreci</CardTitle>
              </CardHeader>
              <CardContent>
                <ReturnTimeline request={latestReturnRequest} />
              </CardContent>
            </Card>
          ) : null}

          <Card>
            <CardHeader>
              <CardTitle>Sipariş İşlemleri</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
              {returnActionLabel && (
                <Button variant="outline" asChild>
                  <Link to={`/returns?orderId=${order.id}`}>
                    <RotateCcw className="mr-2 h-4 w-4" />
                    {returnActionLabel}
                  </Link>
                </Button>
              )}
              <Button
                variant="secondary"
                onClick={handleReorder}
                disabled={isReorderingCart}
              >
                {isReorderingCart ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <ShoppingBag className="mr-2 h-4 w-4" />}
                Tekrar Satın Al
              </Button>
              {canCancel && (
                <Button
                  variant="destructive"
                  onClick={() => setShowCancelConfirm(true)}
                  disabled={isCancelling}
                >
                  <XCircle className="mr-2 h-4 w-4" />
                  Siparişi İptal Et
                </Button>
              )}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                <CardTitle className="text-base">Teslimat Adresi</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="rounded-2xl border bg-muted/20 p-4">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Teslimat Metni</p>
                <p className="mt-2 text-sm leading-6 text-foreground">{order.shippingAddress}</p>
              </div>
              {order.estimatedDeliveryDate ? (
                <div className="rounded-2xl border border-sky-200 bg-sky-50/70 p-4 dark:border-sky-900 dark:bg-sky-950/20">
                  <p className="text-xs font-medium uppercase tracking-wide text-sky-700 dark:text-sky-300">Tahmini Teslimat</p>
                  <p className="mt-2 text-sm font-medium text-sky-900 dark:text-sky-100">
                    {formatOrderDate(order.estimatedDeliveryDate)}
                  </p>
                </div>
              ) : null}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                <CardTitle className="text-base">Ödeme Bilgisi</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="rounded-2xl border bg-muted/20 p-4">
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Ödeme Özeti</p>
                <p className="mt-2 text-sm text-foreground">{paymentSummaryLabel}</p>
                {order.payment?.paymentMethod ? (
                  <p className="mt-1 text-sm text-muted-foreground">
                    Yöntem: {order.payment.paymentMethod}
                  </p>
                ) : null}
              </div>
              {paymentStatusBadge ? (
                <Badge
                  variant="outline"
                  className={paymentStatusBadge.className}
                >
                  {paymentStatusBadge.label}
                </Badge>
              ) : null}
              {order.payment?.provider ? (
                <div className="rounded-2xl border p-4">
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    Sağlayıcı
                  </p>
                  <div className="mt-2 flex items-center gap-2">
                    <PaymentProviderLogo provider={order.payment.provider} className="h-8" />
                    <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${paymentProviderBadgeClasses[order.payment.provider]}`}>
                      {getPaymentProviderLabel(order.payment.provider)}
                    </span>
                    {order.payment.last4Digits ? (
                      <span className="text-sm font-medium text-foreground">
                        •••• {order.payment.last4Digits}
                      </span>
                    ) : null}
                  </div>
                </div>
              ) : null}
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

          {order.invoiceInfo && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <FileText className="h-5 w-5" />
                  <CardTitle className="text-base">Fatura Bilgisi</CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="rounded-2xl border bg-muted/20 p-4">
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Fatura Türü</p>
                  <p className="mt-2 text-sm font-medium text-foreground">
                    {order.invoiceInfo.type === 'Corporate' ? 'Kurumsal' : 'Bireysel'}
                  </p>
                </div>
                <p className="text-muted-foreground text-sm">
                  {order.invoiceInfo.type === 'Corporate'
                    ? `Şirket: ${order.invoiceInfo.companyName || '-'}`
                    : `Ad Soyad: ${order.invoiceInfo.fullName}`}
                </p>
                {order.invoiceInfo.type === 'Corporate' ? (
                  <>
                    <p className="text-muted-foreground text-sm">
                      Vergi Dairesi: {order.invoiceInfo.taxOffice || '-'}
                    </p>
                    <p className="text-muted-foreground text-sm">
                      Vergi Numarası: {order.invoiceInfo.taxNumber || '-'}
                    </p>
                  </>
                ) : order.invoiceInfo.tcKimlikNo ? (
                  <p className="text-muted-foreground text-sm">
                    TC Kimlik No: {order.invoiceInfo.tcKimlikNo}
                  </p>
                ) : null}
                <p className="text-muted-foreground text-sm">
                  Adres: {order.invoiceInfo.invoiceAddress}
                </p>
              </CardContent>
            </Card>
          )}
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

      <ConfirmModal
        open={showCancelConfirm}
        onOpenChange={setShowCancelConfirm}
        title="Siparişi iptal et"
        description="Bu siparişi iptal ettiğinizde ödeme bekleyen süreç sonlandırılır. Devam etmek istediğinize emin misiniz?"
        confirmLabel="Siparişi İptal Et"
        confirmVariant="destructive"
        isLoading={isCancelling}
        onConfirm={handleCancel}
      />

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
