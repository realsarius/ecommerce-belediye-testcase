import { Link } from 'react-router-dom';
import { ArrowRight, CircleDollarSign, Gift, Sparkles, TrendingDown, TrendingUp } from 'lucide-react';
import { useGetLoyaltyHistoryQuery, useGetLoyaltySummaryQuery } from '@/features/loyalty/loyaltyApi';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Skeleton } from '@/components/common/skeleton';
import { Separator } from '@/components/common/separator';

function formatPoints(points: number) {
  return `${points >= 0 ? '+' : ''}${points.toLocaleString('tr-TR')} puan`;
}

function getTransactionTone(points: number) {
  return points >= 0
    ? 'text-emerald-600 dark:text-emerald-400'
    : 'text-rose-600 dark:text-rose-400';
}

export default function Loyalty() {
  const { data: loyaltySummary, isLoading: isSummaryLoading } = useGetLoyaltySummaryQuery();
  const { data: loyaltyHistory, isLoading: isHistoryLoading } = useGetLoyaltyHistoryQuery(50);

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <Badge className="mb-3 bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-200">
            Sadakat Programı
          </Badge>
          <h1 className="text-3xl font-bold tracking-tight">Puanlarım ve Ödüllerim</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Her başarılı siparişte puan kazan, checkout ekranında puanlarını indirime dönüştür. Şu anki kural:
            {' '}<span className="font-medium text-foreground">100 puan = 1 TL</span>.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link to="/orders">
            Siparişlerime Dön
            <ArrowRight className="ml-2 h-4 w-4" />
          </Link>
        </Button>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.15fr_1.85fr]">
        <div className="space-y-6">
          <Card className="overflow-hidden border-amber-300/20 bg-gradient-to-br from-amber-500/10 via-background to-background">
            <CardHeader>
              <div className="flex items-center gap-3">
                <div className="rounded-2xl bg-amber-500/10 p-3">
                  <Gift className="h-6 w-6 text-amber-600 dark:text-amber-400" />
                </div>
                <div>
                  <CardTitle>Ödül Özeti</CardTitle>
                  <CardDescription>Puan bakiyen ve tekrar satın alma gücün</CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-5">
              {isSummaryLoading ? (
                <>
                  <Skeleton className="h-20 w-full" />
                  <Skeleton className="h-24 w-full" />
                </>
              ) : (
                <>
                  <div className="rounded-2xl border border-amber-400/20 bg-amber-500/10 p-5">
                    <p className="text-sm text-muted-foreground">Kullanılabilir Bakiye</p>
                    <p className="mt-3 text-3xl font-bold">
                      {loyaltySummary?.availablePoints?.toLocaleString('tr-TR') ?? 0} puan
                    </p>
                    <p className="mt-2 text-sm text-muted-foreground">
                      Tahmini indirim karşılığı: {(loyaltySummary?.availableDiscountAmount ?? 0).toLocaleString('tr-TR')} ₺
                    </p>
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4">
                      <div className="flex items-center gap-2 text-sm text-emerald-700 dark:text-emerald-300">
                        <TrendingUp className="h-4 w-4" />
                        Toplam Kazanılan
                      </div>
                      <p className="mt-3 text-2xl font-semibold">
                        {loyaltySummary?.totalEarnedPoints?.toLocaleString('tr-TR') ?? 0}
                      </p>
                    </div>
                    <div className="rounded-xl border border-rose-400/20 bg-rose-500/10 p-4">
                      <div className="flex items-center gap-2 text-sm text-rose-700 dark:text-rose-300">
                        <TrendingDown className="h-4 w-4" />
                        Toplam Kullanılan
                      </div>
                      <p className="mt-3 text-2xl font-semibold">
                        {loyaltySummary?.totalRedeemedPoints?.toLocaleString('tr-TR') ?? 0}
                      </p>
                    </div>
                  </div>

                  <div className="rounded-2xl border border-sky-400/20 bg-sky-500/10 p-4 text-sm">
                    <div className="flex items-center gap-2 font-medium">
                      <Sparkles className="h-4 w-4 text-sky-600 dark:text-sky-400" />
                      Nasıl Çalışır?
                    </div>
                    <ul className="mt-3 space-y-2 text-muted-foreground">
                      <li>Başarılı ödeme sonrası net tahsil edilen tutar kadar puan kazanırsın.</li>
                      <li>Checkout ekranında puanlarını 100'lük adımlarla kullanabilirsin.</li>
                      <li>İptal veya refund olursa ilgili puan hareketleri otomatik dengelenir.</li>
                    </ul>
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
                <CircleDollarSign className="h-6 w-6 text-primary" />
              </div>
              <div>
                <CardTitle>Puan Geçmişi</CardTitle>
                <CardDescription>Tüm kazanım, kullanım ve iade hareketlerini burada görebilirsin.</CardDescription>
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
            ) : loyaltyHistory?.length ? (
              <div className="space-y-3">
                {loyaltyHistory.map((transaction, index) => (
                  <div key={transaction.id}>
                    <div className="flex flex-col gap-3 rounded-2xl border border-border/70 bg-muted/20 p-4 md:flex-row md:items-center md:justify-between">
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">{transaction.type}</Badge>
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
                        <p className={`text-lg font-semibold ${getTransactionTone(transaction.points)}`}>
                          {formatPoints(transaction.points)}
                        </p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          İşlem sonrası bakiye: {transaction.balanceAfter.toLocaleString('tr-TR')}
                        </p>
                      </div>
                    </div>
                    {index < loyaltyHistory.length - 1 ? <Separator className="my-3 opacity-50" /> : null}
                  </div>
                ))}
              </div>
            ) : (
              <div className="rounded-2xl border border-dashed border-border/70 bg-muted/20 p-8 text-center">
                <p className="font-medium">Henüz sadakat puanı hareketin yok.</p>
                <p className="mt-2 text-sm text-muted-foreground">
                  İlk siparişinle birlikte burada puan kazanımlarını ve kullanım geçmişini göreceksin.
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
