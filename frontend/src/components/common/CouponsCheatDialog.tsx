import { useGetActiveCouponsQuery } from '@/features/coupons/couponsApi';
import { CouponType } from '@/features/coupons/types';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/common/dialog';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
import { Copy, Ticket, Percent, DollarSign } from 'lucide-react';
import { toast } from 'sonner';

interface CouponsCheatDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CouponsCheatDialog({ open, onOpenChange }: CouponsCheatDialogProps) {
  // Sadece dialog açıkken query çalışsın (skip: !open)
  const { data: coupons, isLoading, error } = useGetActiveCouponsQuery(undefined, {
    skip: !open,
  });

  const copyToClipboard = (code: string) => {
    navigator.clipboard.writeText(code);
    toast.success('Kupon kodu kopyalandı 📋', {
        description: code,
        duration: 2000
    });
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('tr-TR');
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="text-2xl">🎟️</span>
            <span>Aktif Kuponlar (Cheat Menu)</span>
          </DialogTitle>
          <DialogDescription>
            Test için kullanabileceğiniz aktif kupon kodları. Tıklayarak kopyalayabilirsiniz.
          </DialogDescription>
        </DialogHeader>

        <div className="mt-4">
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-12" />
              ))}
            </div>
          ) : error ? (
            <div className="text-center py-6 text-destructive">
                <p>Kuponlar yüklenemedi. Giriş yapmanız gerekebilir.</p>
            </div>
          ) : coupons && coupons.length > 0 ? (
            <div className="border rounded-lg max-h-[60vh] overflow-y-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Kod</TableHead>
                    <TableHead>İndirim</TableHead>
                    <TableHead>Min. Tutar</TableHead>
                    <TableHead>Limit</TableHead>
                    <TableHead>Son Tarih</TableHead>
                    <TableHead></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {coupons.map((coupon) => (
                    <TableRow key={coupon.id} className="hover:bg-muted/50 cursor-pointer" onClick={() => copyToClipboard(coupon.code)}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Ticket className="h-4 w-4 text-primary" />
                          <span className="font-mono font-bold text-lg">{coupon.code}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1 font-medium">
                          {coupon.type === CouponType.Percentage ? (
                            <Percent className="h-4 w-4 text-muted-foreground" />
                          ) : (
                            <DollarSign className="h-4 w-4 text-muted-foreground" />
                          )}
                          <span>
                            {coupon.type === CouponType.Percentage
                              ? `%${coupon.value}`
                              : `${coupon.value} TL`}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell>
                        {coupon.minOrderAmount ? `${coupon.minOrderAmount} TL` : '-'}
                      </TableCell>
                      <TableCell>
                        <span className={coupon.usageLimit > 0 && coupon.usedCount >= coupon.usageLimit ? 'text-destructive' : ''}>
                          {coupon.usedCount}
                          {coupon.usageLimit > 0 && ` / ${coupon.usageLimit}`}
                        </span>
                      </TableCell>
                      <TableCell>{formatDate(coupon.expiresAt)}</TableCell>
                      <TableCell>
                        <Button variant="ghost" size="icon" onClick={(e) => {
                            e.stopPropagation();
                            copyToClipboard(coupon.code);
                        }}>
                          <Copy className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ) : (
            <div className="text-center py-8 text-muted-foreground">
              Aktif kupon bulunmuyor.
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
