import { Link } from 'react-router-dom';
import { useGetCartQuery, useUpdateCartItemMutation, useRemoveFromCartMutation, useClearCartMutation } from '@/features/cart/cartApi';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/common/card';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import { Input } from '@/components/common/input';
import { Trash2, ShoppingBag, Plus, Minus, Package } from 'lucide-react';
import { toast } from 'sonner';

export default function Cart() {
  const { data: cart, isLoading } = useGetCartQuery();
  const [updateItem] = useUpdateCartItemMutation();
  const [removeItem] = useRemoveFromCartMutation();
  const [clearCart, { isLoading: isClearing }] = useClearCartMutation();

  const handleUpdateQuantity = async (productId: number, quantity: number) => {
    if (quantity < 1) return;
    try {
      await updateItem({ productId, data: { quantity } }).unwrap();
    } catch {
      toast.error('Miktar güncellenemedi');
    }
  };

  const handleRemove = async (productId: number) => {
    try {
      await removeItem(productId).unwrap();
      toast.success('Ürün sepetten kaldırıldı');
    } catch {
      toast.error('Ürün kaldırılamadı');
    }
  };

  const handleClear = async () => {
    try {
      await clearCart().unwrap();
      toast.success('Sepet temizlendi');
    } catch {
      toast.error('Sepet temizlenemedi');
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-48 mb-8" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-4">
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} className="h-32" />
            ))}
          </div>
          <Skeleton className="h-64" />
        </div>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <ShoppingBag className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
        <h2 className="text-2xl font-semibold mb-2">Sepetiniz Boş</h2>
        <p className="text-muted-foreground mb-6">
          Henüz sepetinize ürün eklemediniz
        </p>
        <Button asChild>
          <Link to="/">Alışverişe Başla</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-8">
        <h1 className="text-3xl font-bold">Sepetim</h1>
        <Button variant="outline" onClick={handleClear} disabled={isClearing}>
          <Trash2 className="mr-2 h-4 w-4" />
          Sepeti Temizle
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Cart Items */}
        <div className="lg:col-span-2 space-y-4">
          {cart.items.map((item) => (
            <Card key={item.productId}>
              <CardContent className="p-4">
                <div className="flex items-center gap-4">
                  <div className="h-20 w-20 bg-muted rounded-lg flex items-center justify-center flex-shrink-0">
                    <Package className="h-8 w-8 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h3 className="font-semibold truncate">{item.productName}</h3>
                    <p className="text-lg font-bold text-primary">
                      {item.unitPrice.toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="icon"
                      onClick={() => handleUpdateQuantity(item.productId, item.quantity - 1)}
                      disabled={item.quantity <= 1}
                    >
                      <Minus className="h-4 w-4" />
                    </Button>
                    <Input
                      type="number"
                      value={item.quantity}
                      onChange={(e) =>
                        handleUpdateQuantity(item.productId, parseInt(e.target.value) || 1)
                      }
                      className="w-16 text-center"
                      min={1}
                    />
                    <Button
                      variant="outline"
                      size="icon"
                      onClick={() => handleUpdateQuantity(item.productId, item.quantity + 1)}
                    >
                      <Plus className="h-4 w-4" />
                    </Button>
                  </div>
                  <div className="text-right min-w-[100px]">
                    <p className="font-bold">
                      {item.totalPrice.toLocaleString('tr-TR')} ₺
                    </p>
                  </div>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => handleRemove(item.productId)}
                    className="text-destructive hover:text-destructive"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Order Summary */}
        <div>
          <Card className="sticky top-24">
            <CardHeader>
              <CardTitle>Sipariş Özeti</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Ürünler ({cart.items.length})</span>
                <span>{cart.totalAmount.toLocaleString('tr-TR')} ₺</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Kargo</span>
                <span className="text-green-600">Ücretsiz</span>
              </div>
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>Toplam</span>
                <span>{cart.totalAmount.toLocaleString('tr-TR')} ₺</span>
              </div>
            </CardContent>
            <CardFooter>
              <Button asChild className="w-full" size="lg">
                <Link to="/checkout">Siparişi Tamamla</Link>
              </Button>
            </CardFooter>
          </Card>
        </div>
      </div>
    </div>
  );
}
