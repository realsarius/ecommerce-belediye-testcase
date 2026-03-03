import { useState } from 'react';
import { 
  useGetCreditCardsQuery, 
  useDeleteCreditCardMutation,
  useSetDefaultCardMutation,
  type CreditCard 
} from '@/features/creditCards/creditCardsApi';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Skeleton } from '@/components/common/skeleton';
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
import { CreditCard as CreditCardIcon, ArrowRight, ShieldCheck, Trash2, Star } from 'lucide-react';
import { toast } from 'sonner';
import { Link } from 'react-router-dom';

export default function CreditCards() {
  const { data: cards, isLoading } = useGetCreditCardsQuery();
  const [deleteCard, { isLoading: isDeletingCard }] = useDeleteCreditCardMutation();
  const [setDefaultCard] = useSetDefaultCardMutation();
  
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [cardToDelete, setCardToDelete] = useState<number | null>(null);

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
          <p className="text-muted-foreground mt-2">Token ile korunan kayıtlı kartlarınızı yönetin</p>
        </div>
        <Button asChild>
          <Link to="/checkout">
            Checkout&apos;a Git
            <ArrowRight className="h-4 w-4" />
          </Link>
        </Button>
      </div>

      <Card className="mb-6 border-emerald-200/60 bg-emerald-50/60 dark:border-emerald-900/60 dark:bg-emerald-950/20">
        <CardContent className="flex flex-col gap-3 p-5 md:flex-row md:items-center md:justify-between">
          <div className="flex gap-3">
            <ShieldCheck className="mt-0.5 h-5 w-5 text-emerald-600 dark:text-emerald-400" />
            <div className="space-y-1">
              <p className="font-medium">Yeni kart ekleme checkout akışına taşındı.</p>
              <p className="text-sm text-muted-foreground">
                Kartınızı kaydetmek için ödeme sırasında &quot;Bu kartı kaydet&quot; seçeneğini kullanın. Kart verisi sağlayıcı token&apos;ı ile korunur.
              </p>
            </div>
          </div>
          <Button asChild variant="outline" className="shrink-0">
            <Link to="/checkout">Ödemeye Git</Link>
          </Button>
        </CardContent>
      </Card>

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
                {card.isTokenized && (
                  <p className="text-xs text-emerald-600 dark:text-emerald-400">
                    Sağlayıcı token&apos;ı ile korunuyor{card.tokenProvider ? ` (${card.tokenProvider})` : ''}.
                  </p>
                )}
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
              İlk kayıtlı kartınızı oluşturmak için checkout sırasında kartınızı kaydedin
            </CardDescription>
            <Button asChild>
              <Link to="/checkout">
                Checkout&apos;a Git
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
          </CardContent>
        </Card>
      )}

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
