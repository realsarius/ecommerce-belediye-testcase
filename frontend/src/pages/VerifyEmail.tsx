import { useCallback, useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, useSearchParams } from 'react-router-dom';
import { CheckCircle2, Loader2, MailX, ShieldCheck } from 'lucide-react';
import { toast } from 'sonner';
import { useAppDispatch } from '@/app/hooks';
import { setCredentials } from '@/features/auth/authSlice';
import {
  useResendVerificationCodeMutation,
  useVerifyEmailCodeMutation,
  useVerifyEmailMutation,
} from '@/features/auth/authApi';
import type { AuthResponse } from '@/features/auth/types';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';

const DEFAULT_COOLDOWN_SECONDS = 120;

const verifyEmailCodeSchema = z.object({
  email: z.string().trim().email('Geçerli bir e-posta adresi girin'),
  code: z.string().trim().regex(/^[0-9]{6}$/, 'Doğrulama kodu 6 haneli olmalıdır'),
});

type VerifyEmailCodeFormData = z.infer<typeof verifyEmailCodeSchema>;

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

export default function VerifyEmail() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token')?.trim() ?? '';
  const dispatch = useAppDispatch();
  const hasRequestedRef = useRef(false);

  const [cooldownSeconds, setCooldownSeconds] = useState(0);
  const [completedResponse, setCompletedResponse] = useState<AuthResponse | null>(null);

  const [verifyEmail, verifyEmailState] = useVerifyEmailMutation();
  const [verifyEmailCode, verifyEmailCodeState] = useVerifyEmailCodeMutation();
  const [resendVerificationCode, resendVerificationCodeState] = useResendVerificationCodeMutation();

  const {
    register,
    handleSubmit,
    getValues,
    setError,
    watch,
    formState: { errors },
  } = useForm<VerifyEmailCodeFormData>({
    resolver: zodResolver(verifyEmailCodeSchema),
    defaultValues: {
      email: '',
      code: '',
    },
  });

  const emailValue = watch('email');

  const completeVerification = useCallback(
    (result: AuthResponse) => {
      setCompletedResponse(result);
      if (result.success && result.token && result.refreshToken && result.user) {
        dispatch(
          setCredentials({
            user: result.user,
            token: result.token,
            refreshToken: result.refreshToken,
          })
        );
      }
    },
    [dispatch]
  );

  useEffect(() => {
    if (cooldownSeconds <= 0) {
      return;
    }

    const timer = window.setTimeout(() => {
      setCooldownSeconds((current) => (current > 0 ? current - 1 : 0));
    }, 1000);

    return () => window.clearTimeout(timer);
  }, [cooldownSeconds]);

  useEffect(() => {
    if (!token || hasRequestedRef.current || completedResponse) {
      return;
    }

    hasRequestedRef.current = true;

    verifyEmail({ token })
      .unwrap()
      .then((result) => {
        completeVerification(result);
      })
      .catch(() => undefined);
  }, [completeVerification, completedResponse, token, verifyEmail]);

  const onSubmitCode = async (values: VerifyEmailCodeFormData) => {
    try {
      const result = await verifyEmailCode(values).unwrap();
      completeVerification(result);
    } catch (error) {
      toast.error(extractErrorMessage(error, 'Doğrulama kodu doğrulanamadı'));
    }
  };

  const handleResendCode = async () => {
    const email = getValues('email').trim();
    if (!z.string().email().safeParse(email).success) {
      setError('email', {
        type: 'manual',
        message: 'Geçerli bir e-posta adresi girin',
      });
      return;
    }

    try {
      const result = await resendVerificationCode({ email }).unwrap();
      setCooldownSeconds(DEFAULT_COOLDOWN_SECONDS);
      toast.success(result.message || 'Doğrulama kodu yeniden gönderildi');
    } catch (error) {
      const retryAfterSeconds = extractRetryAfterSeconds(error);
      if (retryAfterSeconds > 0) {
        setCooldownSeconds(retryAfterSeconds);
      }

      toast.error(extractErrorMessage(error, 'Doğrulama kodu gönderilemedi'));
    }
  };

  const isLoadingTokenVerification = token.length > 0 && verifyEmailState.isLoading && !completedResponse;
  const shouldShowCodeFallback = !completedResponse && (token.length === 0 || verifyEmailState.isError);
  const tokenErrorMessage = verifyEmailState.isError
    ? extractErrorMessage(verifyEmailState.error, 'Doğrulama linkiniz geçersiz veya süresi dolmuş olabilir')
    : null;

  if (completedResponse?.success) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-lg text-center">
          <CardHeader>
            <div className="mb-4 flex justify-center">
              <CheckCircle2 className="h-14 w-14 text-green-600" />
            </div>
            <CardTitle className="text-2xl">E-posta Doğrulandı</CardTitle>
            <CardDescription>
              {completedResponse.message || 'Hesabınız doğrulandı. Artık alışveriş yapabilirsiniz'}
            </CardDescription>
          </CardHeader>
          <CardFooter>
            <Button asChild className="w-full">
              <Link to="/">Alışverişe Başla</Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  if (isLoadingTokenVerification) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-lg text-center">
          <CardHeader>
            <div className="mb-4 flex justify-center">
              <Loader2 className="h-14 w-14 animate-spin text-primary" />
            </div>
            <CardTitle className="text-2xl">E-posta doğrulanıyor</CardTitle>
            <CardDescription>Linkiniz kontrol ediliyor, lütfen bekleyin</CardDescription>
          </CardHeader>
        </Card>
      </div>
    );
  }

  if (shouldShowCodeFallback) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-lg">
          <CardHeader className="text-center">
            <div className="mb-4 flex justify-center">
              <MailX className="h-14 w-14 text-amber-600" />
            </div>
            <CardTitle className="text-2xl">Kod ile Doğrula</CardTitle>
            <CardDescription>
              {token.length === 0
                ? 'Link yerine e-posta adresinize gelen 6 haneli doğrulama kodunu kullanabilirsiniz'
                : 'Doğrulama linki çalışmadıysa e-posta kodu ile devam edebilirsiniz'}
            </CardDescription>
          </CardHeader>

          <form onSubmit={handleSubmit(onSubmitCode)}>
            <CardContent className="space-y-4">
              {tokenErrorMessage ? (
                <div className="rounded-md border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-700 dark:text-amber-300">
                  {tokenErrorMessage}
                </div>
              ) : null}

              <div className="space-y-2">
                <Label htmlFor="email">E-posta Adresi</Label>
                <Input
                  id="email"
                  type="email"
                  autoComplete="email"
                  placeholder="ornek@email.com"
                  {...register('email')}
                />
                {errors.email ? <p className="text-sm text-destructive">{errors.email.message}</p> : null}
              </div>

              <div className="space-y-2">
                <Label htmlFor="code">Doğrulama Kodu</Label>
                <Input
                  id="code"
                  type="text"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  placeholder="123456"
                  maxLength={6}
                  {...register('code')}
                />
                {errors.code ? <p className="text-sm text-destructive">{errors.code.message}</p> : null}
              </div>

              <div className="rounded-md border border-muted bg-muted/40 p-3 text-sm text-muted-foreground">
                <div className="flex items-center gap-2">
                  <ShieldCheck className="h-4 w-4" />
                  <span>Kod 10 dakika geçerlidir ve yalnızca tek kullanımlıktır</span>
                </div>
              </div>
            </CardContent>

            <CardFooter className="flex flex-col gap-2 pt-4">
              <Button type="submit" className="w-full" disabled={verifyEmailCodeState.isLoading}>
                {verifyEmailCodeState.isLoading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                {verifyEmailCodeState.isLoading ? 'Doğrulanıyor...' : 'Kodu Doğrula'}
              </Button>

              <Button
                type="button"
                variant="outline"
                className="w-full"
                onClick={handleResendCode}
                disabled={
                  resendVerificationCodeState.isLoading ||
                  cooldownSeconds > 0 ||
                  !emailValue ||
                  emailValue.trim().length === 0
                }
              >
                {cooldownSeconds > 0
                  ? `Kodu Tekrar Gönder (${cooldownSeconds}s)`
                  : resendVerificationCodeState.isLoading
                    ? 'Gönderiliyor...'
                    : 'Kodu Tekrar Gönder'}
              </Button>

              <Button asChild variant="ghost" className="w-full">
                <Link to="/login">Giriş sayfasına dön</Link>
              </Button>
            </CardFooter>
          </form>
        </Card>
      </div>
    );
  }

  return null;
}
