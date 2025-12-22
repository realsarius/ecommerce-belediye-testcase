import { useState } from 'react';
import {
  useGetAdminCategoriesQuery,
  useCreateCategoryMutation,
  useUpdateCategoryMutation,
  useDeleteCategoryMutation,
} from '@/features/admin/adminApi';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Badge } from '@/components/common/badge';
import { Checkbox } from '@/components/common/checkbox';
import { Label } from '@/components/common/label';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/common/table';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import { Skeleton } from '@/components/common/skeleton';
import { Plus, Pencil, Trash2, FolderTree } from 'lucide-react';
import { toast } from 'sonner';
import type { Category } from '@/features/products/types';

export default function AdminCategories() {
  const [dialog, setDialog] = useState<{
    open: boolean;
    mode: 'create' | 'edit';
    category?: Category;
  }>({ open: false, mode: 'create' });
  const [formData, setFormData] = useState({ name: '', isActive: true });

  const { data: categories, isLoading } = useGetAdminCategoriesQuery();
  const [createCategory, { isLoading: isCreating }] = useCreateCategoryMutation();
  const [updateCategory, { isLoading: isUpdating }] = useUpdateCategoryMutation();
  const [deleteCategory, { isLoading: isDeleting }] = useDeleteCategoryMutation();

  const openCreateDialog = () => {
    setFormData({ name: '', isActive: true });
    setDialog({ open: true, mode: 'create' });
  };

  const openEditDialog = (category: Category) => {
    setFormData({ name: category.name, isActive: category.isActive });
    setDialog({ open: true, mode: 'edit', category });
  };

  const handleSubmit = async () => {
    if (!formData.name.trim()) {
      toast.error('Kategori adı gereklidir');
      return;
    }
    try {
      if (dialog.mode === 'create') {
        await createCategory(formData).unwrap();
        toast.success('Kategori oluşturuldu');
      } else if (dialog.category) {
        await updateCategory({ id: dialog.category.id, data: formData }).unwrap();
        toast.success('Kategori güncellendi');
      }
      setDialog({ open: false, mode: 'create' });
    } catch {
      toast.error(dialog.mode === 'create' ? 'Kategori oluşturulamadı' : 'Kategori güncellenemedi');
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`"${name}" kategorisini silmek istediğinize emin misiniz?`)) return;
    try {
      await deleteCategory(id).unwrap();
      toast.success('Kategori silindi');
    } catch {
      toast.error('Kategori silinemedi. İçinde ürün olabilir.');
    }
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold">Kategoriler</h1>
        <Button onClick={openCreateDialog}>
          <Plus className="mr-2 h-4 w-4" />
          Yeni Kategori
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-16" />
          ))}
        </div>
      ) : (
        <div className="border rounded-lg">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Kategori</TableHead>
                <TableHead>Ürün Sayısı</TableHead>
                <TableHead>Durum</TableHead>
                <TableHead className="text-right">İşlemler</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {categories?.map((category) => (
                <TableRow key={category.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <div className="h-10 w-10 bg-muted rounded-lg flex items-center justify-center">
                        <FolderTree className="h-5 w-5 text-muted-foreground" />
                      </div>
                      <span className="font-medium">{category.name}</span>
                    </div>
                  </TableCell>
                  <TableCell>{category.productCount}</TableCell>
                  <TableCell>
                    <Badge variant={category.isActive ? 'default' : 'secondary'}>
                      {category.isActive ? 'Aktif' : 'Pasif'}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="ghost" size="icon" onClick={() => openEditDialog(category)}>
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(category.id, category.name)}
                        disabled={isDeleting || category.productCount > 0}
                        className="text-destructive hover:text-destructive"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={dialog.open} onOpenChange={(open) => setDialog({ ...dialog, open })}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {dialog.mode === 'create' ? 'Yeni Kategori' : 'Kategori Düzenle'}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Kategori Adı</Label>
              <Input
                placeholder="Elektronik"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              />
            </div>
            <div className="flex items-center space-x-2">
              <Checkbox
                id="isActive"
                checked={formData.isActive}
                onCheckedChange={(checked) => setFormData({ ...formData, isActive: !!checked })}
              />
              <Label htmlFor="isActive">Aktif</Label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialog({ open: false, mode: 'create' })}>
              İptal
            </Button>
            <Button onClick={handleSubmit} disabled={isCreating || isUpdating}>
              {dialog.mode === 'create' ? 'Oluştur' : 'Güncelle'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
