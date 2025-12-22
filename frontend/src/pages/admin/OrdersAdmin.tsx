import { Link } from 'react-router-dom';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
} from '@/components/common/select';
import { Eye, Package as PackageIcon, Loader2 } from 'lucide-react';
import { useGetAdminOrdersQuery, useUpdateOrderStatusMutation } from '@/features/admin/adminApi';
import { toast } from 'sonner';
import type { OrderStatus } from '@/features/orders/types';

const statusColors: Record<OrderStatus, string> = {
  PendingPayment: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-200',
  Paid: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-200',
  Processing: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-200',
  Shipped: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/30 dark:text-indigo-200',
  Delivered: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-200',
  Cancelled: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-200',
  Refunded: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
};

const statusLabels: Record<OrderStatus, string> = {
  PendingPayment: 'Ödeme Bekleniyor',
  Paid: 'Ödendi',
  Processing: 'Hazırlanıyor',
  Shipped: 'Kargoda',
  Delivered: 'Teslim Edildi',
  Cancelled: 'İptal',
  Refunded: 'İade',
};

export default function OrdersAdmin() {
  const { data: orders, isLoading } = useGetAdminOrdersQuery();
  const [updateStatus, { isLoading: isUpdating }] = useUpdateOrderStatusMutation();

  const handleStatusChange = async (orderId: number, newStatus: string) => {
    try {
      await updateStatus({ id: orderId, status: newStatus }).unwrap();
      toast.success('Sipariş durumu güncellendi');
    } catch {
      toast.error('Guncelleme basarisiz oldu');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-96" />
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-3xl font-bold mb-8">Siparişler</h1>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        {(['PendingPayment', 'Processing', 'Shipped', 'Delivered'] as OrderStatus[]).map((status) => (
          <Card key={status}>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {statusLabels[status]}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-bold">
                {orders?.filter((o) => o.status === status).length || 0}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="border rounded-lg bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Sipariş</TableHead>
              <TableHead>Müşteri</TableHead>
              <TableHead>Ürün</TableHead>
              <TableHead>Tutar</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead>Tarih</TableHead>
              <TableHead className="text-right">İşlemler</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {orders?.map((order) => (
              <TableRow key={order.id}>
                <TableCell>
                  <span className="font-medium">#{order.id}</span>
                </TableCell>
                <TableCell>{order.customerName}</TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <PackageIcon className="h-4 w-4 text-muted-foreground" />
                    <span>{order.items.length} ürün</span>
                  </div>
                </TableCell>
                <TableCell className="font-medium">
                  {order.totalAmount.toLocaleString('tr-TR')} ₺
                </TableCell>
                <TableCell>
                  <Select 
                    defaultValue={order.status} 
                    onValueChange={(value) => handleStatusChange(order.id, value)}
                    disabled={isUpdating}
                  >
                    <SelectTrigger className="w-40 h-8">
                      <Badge className={statusColors[order.status]} variant="secondary">
                        {statusLabels[order.status]}
                        {isUpdating && <Loader2 className="ml-2 h-3 w-3 animate-spin" />}
                      </Badge>
                    </SelectTrigger>
                    <SelectContent>
                      {Object.entries(statusLabels).map(([key, label]) => (
                        <SelectItem key={key} value={key}>
                          {label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </TableCell>
                <TableCell className="text-muted-foreground text-sm">
                  {new Date(order.createdAt).toLocaleDateString('tr-TR')}
                </TableCell>
                <TableCell className="text-right">
                  <Button variant="ghost" size="sm" asChild>
                    <Link to={`/orders/${order.id}`}>
                      <Eye className="h-4 w-4" />
                    </Link>
                  </Button>
                </TableCell>
              </TableRow>
            ))}
            {!orders?.length && (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-10 text-muted-foreground">
                  Henüz sipariş bulunmuyor.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
