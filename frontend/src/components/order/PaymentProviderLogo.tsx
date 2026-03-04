import type { PaymentProviderType } from '@/features/creditCards/creditCardsApi';
import { cn } from '@/lib/utils';

const providerLogoMap: Record<PaymentProviderType, string> = {
  Iyzico: '/assets/providers/iyzico.svg',
  Stripe: '/assets/providers/stripe.svg',
  PayTR: '/assets/providers/paytr.svg',
};

const providerLabelMap: Record<PaymentProviderType, string> = {
  Iyzico: 'Iyzico',
  Stripe: 'Stripe',
  PayTR: 'PayTR',
};

interface PaymentProviderLogoProps {
  provider: PaymentProviderType;
  className?: string;
}

export function PaymentProviderLogo({ provider, className }: PaymentProviderLogoProps) {
  return (
    <img
      src={providerLogoMap[provider]}
      alt={`${providerLabelMap[provider]} logosu`}
      className={cn('h-7 w-auto rounded-md border bg-background p-1', className)}
      loading="lazy"
    />
  );
}

export function getPaymentProviderLabel(provider: PaymentProviderType) {
  return providerLabelMap[provider];
}
