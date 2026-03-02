import { Link } from 'react-router-dom';
import { ArrowRight, Gift, History, WalletCards } from 'lucide-react';
import { useGetGiftCardHistoryQuery, useGetGiftCardSummaryQuery } from '@/features/giftCards/giftCardsApi';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';

function formatCurrency(amount: number) {
  return `${amount.toLocaleString('tr-TR')} ₺`;
}

function getTransactionTone(amount: number) {
  return amount >= 0
    ? 'text-emerald-600 dark:text-emerald-400'
    : 'text-rose-600 dark:text-rose-400';
}

export default function GiftCards() {
  const { data: summary, isLoading: isSummaryLoading } = useGetGiftCardSummaryQuery();
  const { data: history, isLoading: isHistoryLoading } = useGetGiftCardHistoryQuery(50);

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <Badge className="mb-3 bg-emerald-100 text-emerald-800 dark:bg-emerald-900/50 dark:text-emerald-200">
            Stored Value
          </Badge>
          <h1 className="text-3xl font-bold tracking-tight">Gift Cardlarım</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Hesabına bağlanan gift card bakiyelerini, kalan tutarlarını ve kullanım geçmişini tek ekranda takip et.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link to="/checkout">
            Checkout'a Git
            <ArrowRight className="ml-2 h-4 w-4" />
          </Link>
        </Button>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.1fr_1.9fr]">
        <div className="space-y-6">
          <Card className="overflow-hidden border-emerald-300/20 bg-gradient-to-br from-emerald-500/10 via-background to-background">
            <CardHeader>
              <div className="flex items-center gap-3">
                <div className="rounded-2xl bg-emerald-500/10 p-3">
                  <WalletCards className="h-6 w-6 text-emerald-600 dark:text-emerald-400" />
                </div>
                <div>
                  <CardTitle>Bakiye Özeti</CardTitle>
                  <CardDescription>Aktif gift card varlıkların</CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-5">
              {isSummaryLoading ? (
                <>
                  <Skeleton className="h-20 w-full" />
                  <Skeleton className="h-28 w-full" />
                </>
              ) : (
                <>
                  <div className="rounded-2xl border border-emerald-400/20 bg-emerald-500/10 p-5">
                    <p className="text-sm text-muted-foreground">Toplam Kullanılabilir Bakiye</p>
                    <p className="mt-3 text-3xl font-bold">
                      {formatCurrency(summary?.totalAvailableBalance ?? 0)}
                    </p>
                    <p className="mt-2 text-sm text-muted-foreground">
                      Aktif card sayısı: {summary?.activeCardCount ?? 0}
                    </p>
                  </div>

                  <div className="space-y-3">
                    <h3 className="font-medium">Kartlar</h3>
                    {summary?.cards?.length ? (
                      summary.cards.map((card) => (
                        <div key={card.id} className="rounded-2xl border border-border/70 bg-muted/20 p-4">
                          <div className="flex items-start justify-between gap-4">
                            <div>
                              <div className="flex items-center gap-2">
                                <Gift className="h-4 w-4 text-emerald-600" />
                                <p className="font-semibold">{card.maskedCode}</p>
                              </div>
                              <p className="mt-2 text-sm text-muted-foreground">
                                {card.description || 'Açıklama eklenmemiş'}
                              </p>
                              <p className="mt-2 text-xs text-muted-foreground">
                                {card.expiresAt
                                  ? `Son kullanım: ${new Date(card.expiresAt).toLocaleDateString('tr-TR')}`
                                  : 'Süresiz'}
                              </p>
                            </div>
                            <div className="text-right">
                              <p className="text-lg font-semibold">{formatCurrency(card.currentBalance)}</p>
                              <p className="text-xs text-muted-foreground">
                                İlk bakiye: {formatCurrency(card.initialBalance)}
                              </p>
                            </div>
                          </div>
                        </div>
                      ))
                    ) : (
                      <div className="rounded-2xl border border-dashed border-border/70 bg-muted/20 p-6 text-center text-sm text-muted-foreground">
                        Henüz hesabına bağlanmış bir gift card yok.
                      </div>
                    )}
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </div>

        <Card className="border-white/10 bg-card/70 backdrop-blur">
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="rounded-2xl bg-primary/10 p-3">
                <History className="h-6 w-6 text-primary" />
              </div>
              <div>
                <CardTitle>Gift Card Geçmişi</CardTitle>
                <CardDescription>Oluşturma, kullanım ve iade hareketleri burada listelenir.</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {isHistoryLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 5 }).map((_, index) => (
                  <Skeleton key={index} className="h-20 w-full" />
                ))}
              </div>
            ) : history?.length ? (
              <div className="space-y-3">
                {history.map((transaction, index) => (
                  <div key={transaction.id}>
                    <div className="flex flex-col gap-3 rounded-2xl border border-border/70 bg-muted/20 p-4 md:flex-row md:items-center md:justify-between">
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">{transaction.type}</Badge>
                          <span className="text-xs text-muted-foreground">{transaction.maskedGiftCardCode}</span>
                          {transaction.orderNumber ? (
                            <span className="text-xs text-muted-foreground">{transaction.orderNumber}</span>
                          ) : null}
                        </div>
                        <p className="mt-3 font-medium">{transaction.description}</p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          {new Date(transaction.createdAt).toLocaleString('tr-TR')}
                        </p>
                      </div>
                      <div className="text-left md:text-right">
                        <p className={`text-lg font-semibold ${getTransactionTone(transaction.amount)}`}>
                          {transaction.amount >= 0 ? '+' : ''}{formatCurrency(transaction.amount)}
                        </p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          İşlem sonrası bakiye: {formatCurrency(transaction.balanceAfter)}
                        </p>
                      </div>
                    </div>
                    {index < history.length - 1 ? <Separator className="my-3 opacity-50" /> : null}
                  </div>
                ))}
              </div>
            ) : (
              <div className="rounded-2xl border border-dashed border-border/70 bg-muted/20 p-8 text-center">
                <p className="font-medium">Henüz gift card hareketin yok.</p>
                <p className="mt-2 text-sm text-muted-foreground">
                  İlk kullanımından sonra bakiye hareketlerin burada görünecek.
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
