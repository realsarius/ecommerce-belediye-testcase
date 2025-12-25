import { useState } from 'react';
import { 
  useGetCreditCardsQuery, 
  useAddCreditCardMutation, 
  useDeleteCreditCardMutation,
  useSetDefaultCardMutation,
  type CreditCard 
} from '@/features/creditCards/creditCardsApi';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Skeleton } from '@/components/common/skeleton';
import { Checkbox } from '@/components/common/checkbox';
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/common/select';
import { CreditCard as CreditCardIcon, Plus, Trash2, Loader2, Star } from 'lucide-react';
import { toast } from 'sonner';

const emptyForm = {
  cardAlias: '',
  cardHolderName: '',
  cardNumber: '',
  expireMonth: '',
  expireYear: '',
  cvv: '',
  isDefault: false,
};

export default function CreditCards() {
  const { data: cards, isLoading } = useGetCreditCardsQuery();
  const [addCard, { isLoading: isAddingCard }] = useAddCreditCardMutation();
  const [deleteCard, { isLoading: isDeletingCard }] = useDeleteCreditCardMutation();
  const [setDefaultCard] = useSetDefaultCardMutation();
  
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [cardToDelete, setCardToDelete] = useState<number | null>(null);
  
  const [cardForm, setCardForm] = useState(emptyForm);

  const resetForm = () => {
    setCardForm(emptyForm);
  };

  const formatCardNumber = (value: string) => {
    const digits = value.replace(/\D/g, '').slice(0, 16);
    return digits.replace(/(\d{4})/g, '$1 ').trim();
  };

  const handleAddCard = async () => {
    try {
      await addCard({
        ...cardForm,
        cardNumber: cardForm.cardNumber.replace(/\s/g, ''),
      }).unwrap();
      setShowAddDialog(false);
      resetForm();
      toast.success('Kart başarıyla eklendi');
    } catch (error: unknown) {
      const err = error as { data?: { message?: string; errors?: Record<string, string[]> } };
      if (err.data?.errors) {
        const firstError = Object.values(err.data.errors)[0]?.[0];
        toast.error(firstError || 'Kart eklenemedi');
      } else {
        toast.error(err.data?.message || 'Kart eklenemedi');
      }
    }
  };

  const handleDeleteClick = (id: number) => {
    setCardToDelete(id);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (cardToDelete) {
      try {
        await deleteCard(cardToDelete).unwrap();
        toast.success('Kart silindi');
      } catch {
        toast.error('Kart silinemedi');
      }
    }
    setDeleteDialogOpen(false);
    setCardToDelete(null);
  };

  const handleSetDefault = async (card: CreditCard) => {
    if (card.isDefault) return;
    try {
      await setDefaultCard(card.id).unwrap();
      toast.success('Varsayılan kart güncellendi');
    } catch {
      toast.error('Varsayılan kart güncellenemedi');
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <Skeleton className="h-10 w-48 mb-8" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-40" />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold">Kayıtlı Kartlarım</h1>
          <p className="text-muted-foreground mt-2">Kredi kartlarınızı yönetin</p>
        </div>
        <Button onClick={() => setShowAddDialog(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Yeni Kart
        </Button>
      </div>

      {cards && cards.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {cards.map((card) => (
            <Card key={card.id} className="relative group">
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <CreditCardIcon className="h-5 w-5 text-primary" />
                    <CardTitle className="text-lg">{card.cardAlias}</CardTitle>
                    {card.isDefault && (
                      <Star className="h-4 w-4 text-yellow-500 fill-yellow-500" />
                    )}
                  </div>
                  <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    {!card.isDefault && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleSetDefault(card)}
                        className="text-xs"
                      >
                        Varsayılan Yap
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-destructive hover:text-destructive"
                      onClick={() => handleDeleteClick(card.id)}
                      disabled={isDeletingCard}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-2">
                <div className="font-mono text-lg tracking-wider">
                  •••• •••• •••• {card.last4Digits}
                </div>
                <div className="flex justify-between text-sm text-muted-foreground">
                  <span>{card.cardHolderName}</span>
                  <span>{card.expireMonth}/{card.expireYear.slice(-2)}</span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card className="py-12">
          <CardContent className="text-center">
            <CreditCardIcon className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <CardTitle className="mb-2">Henüz kart eklenmemiş</CardTitle>
            <CardDescription className="mb-4">
              Hızlı ödeme için kredi kartı ekleyin
            </CardDescription>
            <Button onClick={() => setShowAddDialog(true)}>
              <Plus className="mr-2 h-4 w-4" />
              İlk Kartımı Ekle
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Add Card Dialog */}
      <Dialog open={showAddDialog} onOpenChange={(open) => {
        setShowAddDialog(open);
        if (!open) resetForm();
      }}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Yeni Kart Ekle</DialogTitle>
            <DialogDescription>Kredi kartı bilgilerinizi güvenle saklayın.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Kart Takma Adı</Label>
              <Input
                placeholder="Bonus Kartım, Akbank vb."
                value={cardForm.cardAlias}
                onChange={(e) => setCardForm({ ...cardForm, cardAlias: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Kart Üzerindeki İsim</Label>
              <Input
                placeholder="KAMURAN OLTACI"
                value={cardForm.cardHolderName}
                onChange={(e) => setCardForm({ ...cardForm, cardHolderName: e.target.value.toUpperCase() })}
              />
            </div>
            <div className="space-y-2">
              <Label>Kart Numarası</Label>
              <Input
                placeholder="4111 1111 1111 1111"
                value={cardForm.cardNumber}
                onChange={(e) => setCardForm({ ...cardForm, cardNumber: formatCardNumber(e.target.value) })}
              />
            </div>
            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label>Ay</Label>
                <Select
                  value={cardForm.expireMonth}
                  onValueChange={(v) => setCardForm({ ...cardForm, expireMonth: v })}
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
                  value={cardForm.expireYear}
                  onValueChange={(v) => setCardForm({ ...cardForm, expireYear: v })}
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
                <Label>CVV</Label>
                <Input
                  placeholder="123"
                  maxLength={4}
                  value={cardForm.cvv}
                  onChange={(e) => setCardForm({ ...cardForm, cvv: e.target.value.replace(/\D/g, '') })}
                />
              </div>
            </div>
            <div className="flex items-center space-x-2">
              <Checkbox
                id="isDefault"
                checked={cardForm.isDefault}
                onCheckedChange={(checked) => setCardForm({ ...cardForm, isDefault: !!checked })}
              />
              <label htmlFor="isDefault" className="text-sm cursor-pointer">
                Varsayılan kart olarak ayarla
              </label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddDialog(false)}>
              İptal
            </Button>
            <Button onClick={handleAddCard} disabled={isAddingCard}>
              {isAddingCard && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Kaydet
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Kartı Sil</AlertDialogTitle>
            <AlertDialogDescription>
              Bu kartı silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.
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
