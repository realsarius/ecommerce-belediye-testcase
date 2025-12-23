import { Link } from 'react-router-dom';
import { ShoppingBag, Trash2, Package, ArrowRight } from 'lucide-react';
import { Button } from '@/components/common/button';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetTrigger,
  SheetClose,
} from '@/components/common/sheet';
import { ScrollArea } from '@/components/common/scroll-area';
import { useGetCartQuery, useRemoveFromCartMutation } from '@/features/cart/cartApi';
import { toast } from 'sonner';

interface CartDrawerProps {
  children: React.ReactNode;
}

export function CartDrawer({ children }: CartDrawerProps) {
  const { data: cart } = useGetCartQuery();
  const [removeItem] = useRemoveFromCartMutation();

  const handleRemove = async (productId: number) => {
    try {
      await removeItem(productId).unwrap();
      toast.success('Ürün kaldırıldı');
    } catch {
      toast.error('Kaldırılamadı');
    }
  };

  const cartItemCount = cart?.items?.reduce((sum, item) => sum + item.quantity, 0) || 0;

  return (
    <Sheet>
      <SheetTrigger asChild>
        {children}
      </SheetTrigger>
      <SheetContent className="flex flex-col w-full sm:max-w-md p-0">
        <SheetHeader className="p-6 border-b">
          <div className="flex items-center justify-between">
            <SheetTitle className="flex items-center gap-2">
              <ShoppingBag className="h-5 w-5" />
              Sepetim ({cartItemCount})
            </SheetTitle>
            <SheetDescription className="sr-only">
              Sepet içeriğini ve toplam tutarı görüntüleyin.
            </SheetDescription>
          </div>
        </SheetHeader>

        <ScrollArea className="flex-1 p-6">
          {(!cart || !cart.items || cart.items.length === 0) ? (
            <div className="flex flex-col items-center justify-center h-[50vh] text-center space-y-4">
              <ShoppingBag className="h-12 w-12 text-muted-foreground opacity-20" />
              <p className="text-muted-foreground">Sepetiniz şu an boş.</p>
              <SheetClose asChild>
                <Button variant="outline">Alışverişe Devam Et</Button>
              </SheetClose>
            </div>
          ) : (
            <div className="space-y-6">
              {cart.items.map((item) => (
                <div key={item.productId} className="flex gap-4">
                  <div className="h-20 w-20 bg-muted rounded flex items-center justify-center flex-shrink-0">
                    <Package className="h-8 w-8 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h4 className="text-sm font-semibold truncate leading-none mb-1">
                      {item.productName}
                    </h4>
                    <p className="text-xs text-muted-foreground mb-2">
                      SKU: {item.productSKU}
                    </p>
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-bold">
                        {item.quantity} x {item.unitPrice.toLocaleString('tr-TR')} ₺
                      </p>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-destructive"
                        onClick={() => handleRemove(item.productId)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </ScrollArea>

        {cart && cart.items && cart.items.length > 0 && (
          <div className="p-6 border-t bg-muted/30">
            <div className="space-y-4">
              <div className="flex items-center justify-between font-semibold">
                <span>Toplam</span>
                <span>{cart.totalAmount.toLocaleString('tr-TR')} ₺</span>
              </div>
              <p className="text-xs text-muted-foreground">
                Kargo ve vergiler ödeme adımında hesaplanır.
              </p>
              <div className="grid grid-cols-1 gap-2">
                <SheetClose asChild>
                  <Button asChild className="w-full">
                    <Link to="/checkout">
                      Ödeme Adımına Geç
                      <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </SheetClose>
                <SheetClose asChild>
                  <Button variant="outline" asChild className="w-full">
                    <Link to="/cart">Sepeti Görüntüle</Link>
                  </Button>
                </SheetClose>
              </div>
            </div>
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
}
