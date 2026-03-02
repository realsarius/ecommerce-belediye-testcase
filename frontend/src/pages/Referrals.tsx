import { Link } from 'react-router-dom';
import { ArrowRight, Copy, Gift, Sparkles, TicketPercent, Users } from 'lucide-react';
import { toast } from 'sonner';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import { useGetReferralHistoryQuery, useGetReferralSummaryQuery } from '@/features/referrals/referralsApi';

function getTransactionTone(points: number) {
  return points >= 0
    ? 'text-emerald-600 dark:text-emerald-400'
    : 'text-rose-600 dark:text-rose-400';
}

export default function Referrals() {
  const { data: referralSummary, isLoading: isSummaryLoading } = useGetReferralSummaryQuery();
  const { data: referralHistory, isLoading: isHistoryLoading } = useGetReferralHistoryQuery(50);

  const shareLink = referralSummary
    ? `${window.location.origin}/register?ref=${referralSummary.referralCode}`
    : '';

  const handleCopy = async (value: string, message: string) => {
    try {
      await navigator.clipboard.writeText(value);
      toast.success(message);
    } catch {
      toast.error('Kopyalama işlemi başarısız oldu');
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <Badge className="mb-3 bg-sky-100 text-sky-800 dark:bg-sky-900/50 dark:text-sky-200">
            Referral Programı
          </Badge>
          <h1 className="text-3xl font-bold tracking-tight">Arkadaşını Davet Et, Ödül Kazan</h1>
          <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
            Paylaştığın referral koduyla üye olan kullanıcı ilk başarılı siparişini verdiğinde
            {' '}<span className="font-medium text-foreground">{referralSummary?.referrerRewardPoints ?? 500} puan</span>{' '}
            kazanırsın. Yeni kullanıcı da
            {' '}<span className="font-medium text-foreground">{referralSummary?.referredRewardPoints ?? 250} puan</span>{' '}
            hoş geldin ödülü alır.
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
          <Card className="overflow-hidden border-sky-300/20 bg-gradient-to-br from-sky-500/10 via-background to-background">
            <CardHeader>
              <div className="flex items-center gap-3">
                <div className="rounded-2xl bg-sky-500/10 p-3">
                  <Users className="h-6 w-6 text-sky-600 dark:text-sky-400" />
                </div>
                <div>
                  <CardTitle>Referral Özeti</CardTitle>
                  <CardDescription>Kodun, paylaşım linkin ve kazanım metriklerin</CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-5">
              {isSummaryLoading ? (
                <>
                  <Skeleton className="h-24 w-full" />
                  <Skeleton className="h-24 w-full" />
                </>
              ) : (
                <>
                  <div className="rounded-2xl border border-sky-400/20 bg-sky-500/10 p-5">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="text-sm text-muted-foreground">Referral Kodun</p>
                        <p className="mt-3 text-3xl font-bold tracking-[0.2em]">{referralSummary?.referralCode}</p>
                        <p className="mt-2 text-sm text-muted-foreground">
                          Linkinle gelen her ilk siparişte bonus puan kazanırsın.
                        </p>
                      </div>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => referralSummary && handleCopy(referralSummary.referralCode, 'Referral kodu kopyalandı')}
                      >
                        <Copy className="mr-2 h-4 w-4" />
                        Kodu Kopyala
                      </Button>
                    </div>
                  </div>

                  <div className="rounded-2xl border border-emerald-400/20 bg-emerald-500/10 p-4">
                    <p className="text-sm text-muted-foreground">Paylaşım Linki</p>
                    <p className="mt-2 break-all text-sm font-medium">{shareLink}</p>
                    <Button
                      type="button"
                      variant="secondary"
                      className="mt-4"
                      onClick={() => shareLink && handleCopy(shareLink, 'Referral linki kopyalandı')}
                    >
                      <Copy className="mr-2 h-4 w-4" />
                      Linki Kopyala
                    </Button>
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="rounded-xl border border-violet-400/20 bg-violet-500/10 p-4">
                      <div className="flex items-center gap-2 text-sm text-violet-700 dark:text-violet-300">
                        <Users className="h-4 w-4" />
                        Toplam Davet
                      </div>
                      <p className="mt-3 text-2xl font-semibold">
                        {referralSummary?.totalReferrals?.toLocaleString('tr-TR') ?? 0}
                      </p>
                    </div>
                    <div className="rounded-xl border border-amber-400/20 bg-amber-500/10 p-4">
                      <div className="flex items-center gap-2 text-sm text-amber-700 dark:text-amber-300">
                        <TicketPercent className="h-4 w-4" />
                        Toplam Referral Ödülü
                      </div>
                      <p className="mt-3 text-2xl font-semibold">
                        {referralSummary?.totalRewardPoints?.toLocaleString('tr-TR') ?? 0} puan
                      </p>
                    </div>
                    <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4">
                      <div className="flex items-center gap-2 text-sm text-emerald-700 dark:text-emerald-300">
                        <Gift className="h-4 w-4" />
                        Başarılı Referral
                      </div>
                      <p className="mt-3 text-2xl font-semibold">
                        {referralSummary?.successfulReferrals?.toLocaleString('tr-TR') ?? 0}
                      </p>
                    </div>
                    <div className="rounded-xl border border-slate-400/20 bg-slate-500/10 p-4">
                      <div className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
                        <Sparkles className="h-4 w-4" />
                        Bekleyen Referral
                      </div>
                      <p className="mt-3 text-2xl font-semibold">
                        {referralSummary?.pendingReferrals?.toLocaleString('tr-TR') ?? 0}
                      </p>
                    </div>
                  </div>

                  {referralSummary?.referredByCode ? (
                    <div className="rounded-2xl border border-dashed border-border/70 bg-muted/20 p-4 text-sm">
                      Kaydın bir referral koduyla oluşturuldu:
                      {' '}<span className="font-semibold">{referralSummary.referredByCode}</span>
                    </div>
                  ) : null}
                </>
              )}
            </CardContent>
          </Card>
        </div>

        <Card className="border-white/10 bg-card/70 backdrop-blur">
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="rounded-2xl bg-primary/10 p-3">
                <Gift className="h-6 w-6 text-primary" />
              </div>
              <div>
                <CardTitle>Referral Geçmişi</CardTitle>
                <CardDescription>Kayıt, ödül kazanımı ve geri alma hareketlerini burada görebilirsin.</CardDescription>
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
            ) : referralHistory?.length ? (
              <div className="space-y-3">
                {referralHistory.map((transaction, index) => (
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
                          {transaction.relatedUserName ? `${transaction.relatedUserName} • ` : ''}
                          {new Date(transaction.createdAt).toLocaleString('tr-TR')}
                        </p>
                      </div>
                      <div className="text-left md:text-right">
                        <p className={`text-lg font-semibold ${getTransactionTone(transaction.points)}`}>
                          {transaction.points >= 0 ? '+' : ''}{transaction.points.toLocaleString('tr-TR')} puan
                        </p>
                      </div>
                    </div>
                    {index < referralHistory.length - 1 ? <Separator className="my-3 opacity-50" /> : null}
                  </div>
                ))}
              </div>
            ) : (
              <div className="rounded-2xl border border-dashed border-border/70 bg-muted/20 p-8 text-center">
                <p className="font-medium">Henüz referral hareketin yok.</p>
                <p className="mt-2 text-sm text-muted-foreground">
                  Kodunu paylaştığında kayıt ve ödül hareketleri burada görünmeye başlayacak.
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
