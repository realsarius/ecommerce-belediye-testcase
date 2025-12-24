import { useState } from 'react';
import { useGetAddressesQuery, useCreateAddressMutation, useDeleteAddressMutation, useUpdateAddressMutation } from '@/features/admin/adminApi';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Skeleton } from '@/components/common/skeleton';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '@/components/common/dialog';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/common/alert-dialog';
import { MapPin, Plus, Trash2, Loader2, Home, Building2, Phone, User, Pencil } from 'lucide-react';
import { toast } from 'sonner';
import type { ShippingAddress } from '@/features/orders/types';

const emptyForm = {
  title: '',
  fullName: '',
  phone: '',
  city: '',
  district: '',
  addressLine: '',
  postalCode: '',
  isDefault: false,
};

export default function Addresses() {
  const { data: addresses, isLoading } = useGetAddressesQuery();
  const [createAddress, { isLoading: isCreatingAddress }] = useCreateAddressMutation();
  const [deleteAddress, { isLoading: isDeletingAddress }] = useDeleteAddressMutation();
  const [updateAddress, { isLoading: isUpdatingAddress }] = useUpdateAddressMutation();
  
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [showEditDialog, setShowEditDialog] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [addressToDelete, setAddressToDelete] = useState<number | null>(null);
  const [editingAddress, setEditingAddress] = useState<ShippingAddress | null>(null);
  
  const [addressForm, setAddressForm] = useState(emptyForm);

  const resetForm = () => {
    setAddressForm(emptyForm);
  };

  const handleAddAddress = async () => {
    try {
      await createAddress(addressForm).unwrap();
      setShowAddDialog(false);
      resetForm();
      toast.success('Adres başarıyla eklendi');
    } catch {
      toast.error('Adres eklenemedi');
    }
  };

  const handleEditClick = (address: ShippingAddress) => {
    setEditingAddress(address);
    setAddressForm({
      title: address.title,
      fullName: address.fullName,
      phone: address.phone,
      city: address.city,
      district: address.district,
      addressLine: address.addressLine,
      postalCode: address.postalCode || '',
      isDefault: address.isDefault,
    });
    setShowEditDialog(true);
  };

  const handleUpdateAddress = async () => {
    if (!editingAddress) return;
    
    try {
      await updateAddress({ id: editingAddress.id, data: addressForm }).unwrap();
      setShowEditDialog(false);
      setEditingAddress(null);
      resetForm();
      toast.success('Adres başarıyla güncellendi');
    } catch {
      toast.error('Adres güncellenemedi');
    }
  };

  const handleDeleteClick = (id: number) => {
    setAddressToDelete(id);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (addressToDelete) {
      try {
        await deleteAddress(addressToDelete).unwrap();
        toast.success('Adres silindi');
      } catch {
        toast.error('Adres silinemedi');
      }
    }
    setDeleteDialogOpen(false);
    setAddressToDelete(null);
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <Skeleton className="h-10 w-48 mb-8" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-48" />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold">Adres Bilgilerim</h1>
          <p className="text-muted-foreground mt-2">Teslimat adreslerinizi yönetin</p>
        </div>
        <Button onClick={() => setShowAddDialog(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Yeni Adres
        </Button>
      </div>

      {addresses && addresses.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {addresses.map((address) => (
            <Card key={address.id} className="relative group">
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {address.title?.toLowerCase().includes('ev') ? (
                      <Home className="h-5 w-5 text-primary" />
                    ) : (
                      <Building2 className="h-5 w-5 text-primary" />
                    )}
                    <CardTitle className="text-lg">{address.title}</CardTitle>
                  </div>
                  <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleEditClick(address)}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-destructive hover:text-destructive"
                      onClick={() => handleDeleteClick(address.id)}
                      disabled={isDeletingAddress}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-center gap-2 text-sm">
                  <User className="h-4 w-4 text-muted-foreground" />
                  <span>{address.fullName}</span>
                </div>
                <div className="flex items-center gap-2 text-sm">
                  <Phone className="h-4 w-4 text-muted-foreground" />
                  <span>{address.phone}</span>
                </div>
                <div className="flex items-start gap-2 text-sm">
                  <MapPin className="h-4 w-4 text-muted-foreground mt-0.5" />
                  <div>
                    <p>{address.addressLine}</p>
                    <p className="text-muted-foreground">
                      {address.district}/{address.city} - {address.postalCode}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card className="py-12">
          <CardContent className="text-center">
            <MapPin className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <CardTitle className="mb-2">Henüz adres eklenmemiş</CardTitle>
            <CardDescription className="mb-4">
              Teslimat için yeni bir adres ekleyin
            </CardDescription>
            <Button onClick={() => setShowAddDialog(true)}>
              <Plus className="mr-2 h-4 w-4" />
              İlk Adresimi Ekle
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Add Address Dialog */}
      <Dialog open={showAddDialog} onOpenChange={(open) => {
        setShowAddDialog(open);
        if (!open) resetForm();
      }}>
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
                placeholder="Ad Soyad"
                value={addressForm.fullName}
                onChange={(e) => setAddressForm({ ...addressForm, fullName: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Telefon</Label>
              <Input
                placeholder="Telefon numarası"
                value={addressForm.phone}
                onChange={(e) => setAddressForm({ ...addressForm, phone: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>İl</Label>
                <Input
                  placeholder="İl"
                  value={addressForm.city}
                  onChange={(e) => setAddressForm({ ...addressForm, city: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>İlçe</Label>
                <Input
                  placeholder="İlçe"
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
                placeholder="Posta kodu"
                value={addressForm.postalCode}
                onChange={(e) => setAddressForm({ ...addressForm, postalCode: e.target.value })}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddDialog(false)}>
              İptal
            </Button>
            <Button onClick={handleAddAddress} disabled={isCreatingAddress}>
              {isCreatingAddress && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Kaydet
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Address Dialog */}
      <Dialog open={showEditDialog} onOpenChange={(open) => {
        setShowEditDialog(open);
        if (!open) {
          setEditingAddress(null);
          resetForm();
        }
      }}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Adresi Düzenle</DialogTitle>
            <DialogDescription>Adres bilgilerini güncelleyin.</DialogDescription>
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
                placeholder="Ad Soyad"
                value={addressForm.fullName}
                onChange={(e) => setAddressForm({ ...addressForm, fullName: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Telefon</Label>
              <Input
                placeholder="Telefon numarası"
                value={addressForm.phone}
                onChange={(e) => setAddressForm({ ...addressForm, phone: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>İl</Label>
                <Input
                  placeholder="İl"
                  value={addressForm.city}
                  onChange={(e) => setAddressForm({ ...addressForm, city: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>İlçe</Label>
                <Input
                  placeholder="İlçe"
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
                placeholder="Posta kodu"
                value={addressForm.postalCode}
                onChange={(e) => setAddressForm({ ...addressForm, postalCode: e.target.value })}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowEditDialog(false)}>
              İptal
            </Button>
            <Button onClick={handleUpdateAddress} disabled={isUpdatingAddress}>
              {isUpdatingAddress && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Güncelle
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Adresi Sil</AlertDialogTitle>
            <AlertDialogDescription>
              Bu adresi silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>İptal</AlertDialogCancel>
            <AlertDialogAction onClick={handleDeleteConfirm} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Sil
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
