import { AlertCircle, CheckCircle2, Clock3, RotateCcw, Wallet } from 'lucide-react';
import type { ReturnReasonCategory, ReturnRequest, ReturnRequestStatus } from '@/features/returns/types';

const reasonCategoryLabels: Record<ReturnReasonCategory, string> = {
  WrongProduct: 'Yanlış ürün',
  DefectiveDamaged: 'Hasarlı / arızalı',
  NotAsDescribed: 'Açıklamaya uymuyor',
  ChangedMind: 'Fikrimi değiştirdim',
  LateDelivery: 'Geç teslimat',
  Other: 'Diğer',
};

const returnSteps = [
  { key: 'Pending', label: 'Talep Alındı', icon: Clock3 },
  { key: 'Approved', label: 'Onaylandı', icon: CheckCircle2 },
  { key: 'RefundPending', label: 'Para İadesi İşleniyor', icon: RotateCcw },
  { key: 'Refunded', label: 'İade Tamamlandı', icon: Wallet },
] as const;

function getReturnProgress(status: ReturnRequestStatus) {
  switch (status) {
    case 'Pending':
      return 1;
    case 'Approved':
      return 2;
    case 'RefundPending':
      return 3;
    case 'Refunded':
      return 4;
    case 'Rejected':
      return 1;
    default:
      return 0;
  }
}

interface ReturnTimelineProps {
  request: ReturnRequest;
}

export function ReturnTimeline({ request }: ReturnTimelineProps) {
  const progress = getReturnProgress(request.status);

  return (
    <div className="space-y-4">
      <div className="rounded-xl border bg-muted/20 p-4 text-sm">
        <div className="space-y-1">
          <p>
            <span className="font-medium">Talep Tipi:</span> {request.type === 'Cancellation' ? 'İptal' : 'İade'}
          </p>
          <p>
            <span className="font-medium">Kategori:</span> {reasonCategoryLabels[request.reasonCategory]}
          </p>
          <p>
            <span className="font-medium">Neden:</span> {request.reason}
          </p>
          {request.requestNote ? (
            <p>
              <span className="font-medium">Ek Not:</span> {request.requestNote}
            </p>
          ) : null}
          {request.selectedItems.length > 0 ? (
            <div className="pt-2">
              <p className="font-medium">Seçilen Ürünler:</p>
              <ul className="mt-1 space-y-1 text-muted-foreground">
                {request.selectedItems.map((item) => (
                  <li key={item.orderItemId}>
                    {item.productName} x {item.quantity}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>
      </div>

      <div className="space-y-4">
        {returnSteps.map((step, index) => {
          const Icon = step.icon;
          const isRejectedFirstStep = request.status === 'Rejected' && index === 0;
          const isCompleted = isRejectedFirstStep || progress > index + 1 || (request.status === 'Refunded' && progress === index + 1);
          const isCurrent = progress === index + 1 && request.status !== 'Refunded' && request.status !== 'Rejected';

          return (
            <div key={step.key} className="flex gap-4">
              <div className="flex flex-col items-center">
                <div
                  className={`flex h-9 w-9 items-center justify-center rounded-full border ${
                    isCompleted
                      ? 'border-emerald-500 bg-emerald-500 text-white'
                      : isCurrent
                        ? 'border-sky-500 bg-sky-500/10 text-sky-600'
                        : 'border-border bg-background text-muted-foreground'
                  }`}
                >
                  <Icon className="h-4 w-4" />
                </div>
                {index < returnSteps.length - 1 ? (
                  <div className={`mt-2 h-10 w-px ${progress > index + 1 ? 'bg-emerald-400' : 'bg-border'}`} />
                ) : null}
              </div>
              <div className="space-y-1 pb-2">
                <p className="font-medium">{step.label}</p>
                <p className="text-sm text-muted-foreground">
                  {isCompleted ? 'Tamamlandı' : isCurrent ? 'Bu adım aktif' : 'Henüz ulaşılmadı'}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      {request.status === 'Rejected' ? (
        <div className="rounded-xl border border-rose-400/30 bg-rose-500/10 p-4 text-sm text-rose-700 dark:text-rose-300">
          <div className="flex items-start gap-3">
            <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
            <div className="space-y-1">
              <p className="font-medium">Talep reddedildi</p>
              <p>{request.reviewNote || 'Talebiniz değerlendirme sonucunda reddedildi.'}</p>
            </div>
          </div>
        </div>
      ) : null}

      {request.reviewedAt && request.status !== 'Rejected' ? (
        <div className="rounded-xl border bg-muted/20 p-4 text-sm text-muted-foreground">
          Son güncelleme:{' '}
          <strong>
            {new Date(request.reviewedAt).toLocaleDateString('tr-TR', {
              day: '2-digit',
              month: 'long',
              year: 'numeric',
            })}
          </strong>
          {request.reviewNote ? ` · ${request.reviewNote}` : ''}
        </div>
      ) : null}
    </div>
  );
}
