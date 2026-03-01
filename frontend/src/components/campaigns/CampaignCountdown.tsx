import { useEffect, useMemo, useState } from 'react';

interface CampaignCountdownProps {
  endsAt?: string | null;
  className?: string;
}

function formatRemainingTime(endsAt: string, now: number): string | null {
  const remainingMs = new Date(endsAt).getTime() - now;
  if (remainingMs <= 0) {
    return null;
  }

  const totalMinutes = Math.floor(remainingMs / 60000);
  const days = Math.floor(totalMinutes / (60 * 24));
  const hours = Math.floor((totalMinutes % (60 * 24)) / 60);
  const minutes = totalMinutes % 60;

  if (days > 0) {
    return `${days}g ${hours}s ${minutes}d`;
  }

  if (hours > 0) {
    return `${hours}s ${minutes}d`;
  }

  return `${Math.max(minutes, 1)} dk`;
}

export function CampaignCountdown({ endsAt, className }: CampaignCountdownProps) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (!endsAt) {
      return;
    }

    const timer = window.setInterval(() => setNow(Date.now()), 60000);
    return () => window.clearInterval(timer);
  }, [endsAt]);

  const remaining = useMemo(
    () => (endsAt ? formatRemainingTime(endsAt, now) : null),
    [endsAt, now]
  );

  if (!remaining) {
    return null;
  }

  return (
    <span className={className}>
      Bitmesine {remaining} kaldı
    </span>
  );
}
