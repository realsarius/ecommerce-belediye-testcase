import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Eye, EyeOff, KeyRound, Loader2, CheckCircle2, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { useResetPasswordMutation } from '@/features/auth/authApi';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';

const resetPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'En az 8 karakter')
      .regex(/[A-Z]/, 'En az 1 büyük harf')
      .regex(/[0-9]/, 'En az 1 rakam'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Şifreler eşleşmiyor',
    path: ['confirmPassword'],
  });

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

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

export default function ResetPassword() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isCompleted, setIsCompleted] = useState(false);
  const [resetPassword, { isLoading }] = useResetPasswordMutation();
  const token = useMemo(() => searchParams.get('token')?.trim() ?? '', [searchParams]);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
  });

  useEffect(() => {
    if (!isCompleted) {
      return;
    }

    const timer = window.setTimeout(() => {
      navigate('/login', { replace: true });
    }, 2000);

    return () => window.clearTimeout(timer);
  }, [isCompleted, navigate]);

  const onSubmit = async (values: ResetPasswordFormData) => {
    if (!token) {
      toast.error('Şifre sıfırlama token bilgisi eksik.');
      return;
    }

    try {
      const result = await resetPassword({
        token,
        newPassword: values.newPassword,
        confirmPassword: values.confirmPassword,
      }).unwrap();

      setIsCompleted(true);
      toast.success(result.message || 'Şifreniz başarıyla güncellendi.');
    } catch (error) {
      toast.error(extractErrorMessage(error, 'Şifre sıfırlama işlemi başarısız oldu.'));
    }
  };

  if (!token) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-md">
          <CardHeader className="text-center">
            <div className="mb-4 flex justify-center">
              <XCircle className="h-12 w-12 text-destructive" />
            </div>
            <CardTitle className="text-2xl">Geçersiz Sıfırlama Linki</CardTitle>
            <CardDescription>
              Linkte gerekli token bilgisi bulunamadı. Lütfen yeni bir sıfırlama linki isteyin.
            </CardDescription>
          </CardHeader>
          <CardFooter>
            <Button asChild className="w-full">
              <Link to="/forgot-password">Yeni Link İste</Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  if (isCompleted) {
    return (
      <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
        <Card className="w-full max-w-md">
          <CardHeader className="text-center">
            <div className="mb-4 flex justify-center">
              <CheckCircle2 className="h-12 w-12 text-green-600" />
            </div>
            <CardTitle className="text-2xl">Şifreniz Güncellendi</CardTitle>
            <CardDescription>
              Yeni şifreniz kaydedildi. 2 saniye içinde giriş sayfasına yönlendirileceksiniz.
            </CardDescription>
          </CardHeader>
          <CardFooter>
            <Button asChild className="w-full">
              <Link to="/login">Girişe Git</Link>
            </Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mb-4 flex justify-center">
            <KeyRound className="h-12 w-12 text-primary" />
          </div>
          <CardTitle className="text-2xl">Yeni Şifre Belirle</CardTitle>
          <CardDescription>
            Hesabınız için güçlü bir şifre belirleyin.
          </CardDescription>
        </CardHeader>

        <form onSubmit={handleSubmit(onSubmit)}>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="newPassword">Yeni Şifre</Label>
              <div className="relative">
                <Input
                  id="newPassword"
                  type={showNewPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  {...register('newPassword')}
                />
                <button
                  type="button"
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground"
                  onClick={() => setShowNewPassword((current) => !current)}
                >
                  {showNewPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.newPassword ? <p className="text-sm text-destructive">{errors.newPassword.message}</p> : null}
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Şifre Tekrar</Label>
              <div className="relative">
                <Input
                  id="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  {...register('confirmPassword')}
                />
                <button
                  type="button"
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground"
                  onClick={() => setShowConfirmPassword((current) => !current)}
                >
                  {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.confirmPassword ? <p className="text-sm text-destructive">{errors.confirmPassword.message}</p> : null}
            </div>
          </CardContent>

          <CardFooter className="flex flex-col gap-4 pt-4">
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              {isLoading ? 'Güncelleniyor...' : 'Şifreyi Güncelle'}
            </Button>

            <p className="text-center text-sm text-muted-foreground">
              <Link to="/login" className="text-primary hover:underline">
                ← Giriş sayfasına dön
              </Link>
            </p>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
