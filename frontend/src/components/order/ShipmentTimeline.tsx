import { CheckCircle2, MapPin, Navigation, Package, Truck } from 'lucide-react';
import type { Order, ShipmentStatus } from '@/features/orders/types';

const shipmentSteps: { status: ShipmentStatus; label: string; icon: typeof Package }[] = [
  { status: 'Preparing', label: 'Hazırlanıyor', icon: Package },
  { status: 'HandedToCargo', label: 'Kargoya Verildi', icon: Truck },
  { status: 'InTransit', label: 'Yolda', icon: MapPin },
  { status: 'OutForDelivery', label: 'Dağıtımda', icon: Navigation },
  { status: 'Delivered', label: 'Teslim Edildi', icon: CheckCircle2 },
];

function getShipmentProgress(status?: ShipmentStatus) {
  if (!status || status === 'Pending') {
    return 0;
  }

  const index = shipmentSteps.findIndex((step) => step.status === status);
  return index === -1 ? 0 : index + 1;
}

function deriveShipmentStatus(order: Order): ShipmentStatus {
  if (order.shipmentStatus) {
    return order.shipmentStatus;
  }

  if (order.status === 'Delivered') {
    return 'Delivered';
  }

  if (order.status === 'Shipped') {
    return 'HandedToCargo';
  }

  if (order.status === 'Processing') {
    return 'Preparing';
  }

  return 'Pending';
}

function buildTrackingUrl(cargoCompany?: string, trackingCode?: string) {
  if (!cargoCompany || !trackingCode) {
    return null;
  }

  const normalizedCompany = cargoCompany.toLocaleLowerCase('tr-TR');

  if (normalizedCompany.includes('yurti')) {
    return `https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code=${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('aras')) {
    return `https://www.araskargo.com.tr/kargo-takip/${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('mng')) {
    return `https://www.mngkargo.com.tr/gonderitakip/${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('ptt')) {
    return `https://gonderitakip.ptt.gov.tr/Track/Verify?q=${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('sürat') || normalizedCompany.includes('surat')) {
    return `https://www.suratkargo.com.tr/KargoTakip/?query=${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('ups')) {
    return `https://www.ups.com/track?tracknum=${encodeURIComponent(trackingCode)}`;
  }

  return null;
}

interface ShipmentTimelineProps {
  order: Order;
}

export function ShipmentTimeline({ order }: ShipmentTimelineProps) {
  const effectiveShipmentStatus = deriveShipmentStatus(order);
  const progress = getShipmentProgress(effectiveShipmentStatus);
  const trackingUrl = buildTrackingUrl(order.cargoCompany, order.trackingCode);

  return (
    <div className="space-y-4">
      <div className="space-y-4">
        {shipmentSteps.map((step, index) => {
          const Icon = step.icon;
          const isCompleted = progress > index;
          const isCurrent = progress === index + 1 && effectiveShipmentStatus !== 'Delivered';

          return (
            <div key={step.status} className="flex gap-4">
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
                {index < shipmentSteps.length - 1 ? (
                  <div className={`mt-2 h-10 w-px ${isCompleted ? 'bg-emerald-400' : 'bg-border'}`} />
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

      {(order.cargoCompany || order.trackingCode) && (
        <div className="rounded-xl border bg-muted/20 p-4 text-sm">
          <div className="space-y-1">
            {order.cargoCompany ? <p><span className="font-medium">Kargo Firması:</span> {order.cargoCompany}</p> : null}
            {order.trackingCode ? <p><span className="font-medium">Takip Kodu:</span> {order.trackingCode}</p> : null}
            {trackingUrl ? (
              <a
                href={trackingUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex font-medium text-primary underline underline-offset-4 hover:no-underline"
              >
                Takip Et
              </a>
            ) : null}
          </div>
        </div>
      )}

      {!order.cargoCompany && !order.trackingCode && effectiveShipmentStatus === 'Pending' ? (
        <div className="rounded-xl border bg-muted/20 p-4 text-sm text-muted-foreground">
          Sipariş henüz kargo aşamasına geçmedi. Hazırlık tamamlandığında takip bilgileri burada görünecek.
        </div>
      ) : null}

      {order.estimatedDeliveryDate ? (
        <div className="rounded-xl border border-blue-400/20 bg-blue-500/10 p-4 text-sm text-blue-700 dark:text-blue-300">
          Tahmini Teslimat:{' '}
          <strong>
            {new Date(order.estimatedDeliveryDate).toLocaleDateString('tr-TR', {
              day: '2-digit',
              month: 'long',
              year: 'numeric',
            })}
          </strong>
        </div>
      ) : null}

      {order.deliveredAt ? (
        <div className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4 text-sm text-emerald-700 dark:text-emerald-300">
          Teslim Edildi:{' '}
          <strong>
            {new Date(order.deliveredAt).toLocaleDateString('tr-TR', {
              day: '2-digit',
              month: 'long',
              year: 'numeric',
            })}
          </strong>
        </div>
      ) : null}
    </div>
  );
}
