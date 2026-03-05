import { useEffect, useRef } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { CheckCircle2, Loader2, MailX } from 'lucide-react';
import { useAppDispatch } from '@/app/hooks';
import { setCredentials } from '@/features/auth/authSlice';
import { useConfirmEmailChangeMutation } from '@/features/auth/authApi';
import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/common/card';
import { Button } from '@/components/common/button';

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

export default function ConfirmEmailChange() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token')?.trim() ?? '';
  const dispatch = useAppDispatch();
  const hasRequestedRef = useRef(false);

  const [confirmEmailChange, { isLoading, isSuccess, isError, error, data }] =
    useConfirmEmailChangeMutation();

  useEffect(() => {
    if (!token || hasRequestedRef.current) {
      return;
    }

    hasRequestedRef.current = true;

    confirmEmailChange({ token })
      .unwrap()
      .then((result) => {
        if (result.success && result.token && result.refreshToken && result.user) {
          dispatch(
            setCredentials({
              user: result.user,
              token: result.token,
              refreshToken: result.refreshToken,
            })
          );
        }
      })
      .catch(() => undefined);
  }, [confirmEmailChange, dispatch, token]);

  if (!token) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-lg text-center">
          <CardHeader>
            <div className="mb-4 flex justify-center">
              <MailX className="h-14 w-14 text-destructive" />
            </div>
            <CardTitle className="text-2xl">Geçersiz Onay Linki</CardTitle>
            <CardDescription>
              Linkte token bilgisi bulunamadı. Hesabınızdan tekrar e-posta değişikliği isteyin.
            </CardDescription>
          </CardHeader>
          <CardFooter>
            <Button asChild className="w-full">
              <Link to="/account">Hesabıma Dön</Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-lg text-center">
          <CardHeader>
            <div className="mb-4 flex justify-center">
              <Loader2 className="h-14 w-14 animate-spin text-primary" />
            </div>
            <CardTitle className="text-2xl">E-posta değişikliği onaylanıyor</CardTitle>
            <CardDescription>Linkiniz kontrol ediliyor, lütfen bekleyin.</CardDescription>
          </CardHeader>
        </Card>
      </div>
    );
  }

  if (isSuccess) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-lg text-center">
          <CardHeader>
            <div className="mb-4 flex justify-center">
              <CheckCircle2 className="h-14 w-14 text-green-600" />
            </div>
            <CardTitle className="text-2xl">E-posta Adresi Güncellendi</CardTitle>
            <CardDescription>
              {data?.message || 'E-posta adresiniz başarıyla güncellendi'}
            </CardDescription>
          </CardHeader>
          <CardFooter>
            <Button asChild className="w-full">
              <Link to="/account">Hesabıma Git</Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-lg text-center">
          <CardHeader>
            <div className="mb-4 flex justify-center">
              <MailX className="h-14 w-14 text-destructive" />
            </div>
            <CardTitle className="text-2xl">Geçersiz veya Süresi Dolmuş Link</CardTitle>
            <CardDescription>
              {extractErrorMessage(error, 'Onay linkiniz geçersiz veya süresi dolmuş olabilir')}
            </CardDescription>
          </CardHeader>
          <CardFooter className="flex w-full flex-col gap-2">
            <Button asChild className="w-full">
              <Link to="/account">Hesabıma Dön</Link>
            </Button>
            <Button asChild variant="outline" className="w-full">
              <Link to="/">Ana Sayfaya Dön</Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  return null;
}

