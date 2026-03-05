import { useEffect, useMemo, useState } from 'react';
import { AlertCircle } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { useAppSelector } from '@/app/hooks';
import { useResendVerificationMutation } from '@/features/auth/authApi';

const DEFAULT_COOLDOWN_SECONDS = 120;

function extractErrorMessage(error: unknown, fallback: string) {
  if (typeof error !== 'object' || error === null || !('data' in error)) {
    return fallback;
  }

  const data = (error as { data?: unknown }).data;
  if (typeof data !== 'object' || data === null || !('message' in data)) {
    return fallback;
  }

  const message = (data as { message?: unknown }).message;
  return typeof message === 'string' && message.trim().length > 0 ? message : fallback;
}

function extractRetryAfterSeconds(error: unknown) {
  if (typeof error !== 'object' || error === null || !('data' in error)) {
    return 0;
  }

  const data = (error as { data?: unknown }).data;
  if (typeof data !== 'object' || data === null || !('details' in data)) {
    return 0;
  }

  const details = (data as { details?: unknown }).details;
  if (typeof details !== 'object' || details === null) {
    return 0;
  }

  const retryAfterRaw =
    (details as { retryAfterSeconds?: unknown }).retryAfterSeconds ??
    (details as { RetryAfterSeconds?: unknown }).RetryAfterSeconds;

  if (typeof retryAfterRaw !== 'number' || retryAfterRaw <= 0) {
    return 0;
  }

  return Math.round(retryAfterRaw);
}

export function EmailVerificationBanner() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);
  const [cooldownSeconds, setCooldownSeconds] = useState(0);
  const [resendVerification, { isLoading: isResending }] = useResendVerificationMutation();

  const userEmail = useMemo(() => user?.email ?? '', [user?.email]);
  const isEmailVerified = user?.isEmailVerified ?? false;

  useEffect(() => {
    if (cooldownSeconds <= 0) {
      return;
    }

    const timer = window.setTimeout(() => {
      setCooldownSeconds((current) => (current > 0 ? current - 1 : 0));
    }, 1000);

    return () => window.clearTimeout(timer);
  }, [cooldownSeconds]);

  if (!isAuthenticated || !user || isEmailVerified) {
    return null;
  }

  const handleResend = async () => {
    try {
      const result = await resendVerification().unwrap();
      setCooldownSeconds(DEFAULT_COOLDOWN_SECONDS);
      toast.success(result.message || 'Doğrulama e-postası tekrar gönderildi.');
    } catch (error) {
      const retryAfterSeconds = extractRetryAfterSeconds(error);
      if (retryAfterSeconds > 0) {
        setCooldownSeconds(retryAfterSeconds);
      }

      toast.error(extractErrorMessage(error, 'Doğrulama e-postası gönderilemedi.'));
    }
  };

  return (
    <div className="border-b border-amber-500/30 bg-amber-500/10 px-4 py-2.5">
      <div className="mx-auto flex w-full max-w-7xl items-center justify-between gap-4">
        <div className="flex items-center gap-2 text-sm text-amber-700 dark:text-amber-300">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>
            E-posta adresiniz doğrulanmamış. Alışveriş yapabilmek için
            <strong> {userEmail}</strong> adresine gönderilen linke tıklayın.
          </span>
        </div>

        <Button
          variant="ghost"
          className="h-auto shrink-0 px-0 text-xs text-amber-700 underline hover:bg-transparent hover:no-underline dark:text-amber-300"
          onClick={handleResend}
          disabled={isResending || cooldownSeconds > 0}
        >
          {cooldownSeconds > 0
            ? `Tekrar gönder (${cooldownSeconds}s)`
            : isResending
              ? 'Gönderiliyor...'
              : 'Tekrar gönder'}
        </Button>
      </div>
    </div>
  );
}

