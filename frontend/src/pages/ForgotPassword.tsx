import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link } from 'react-router-dom';
import { KeyRound, Loader2, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';
import { useForgotPasswordMutation } from '@/features/auth/authApi';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';

const forgotPasswordSchema = z.object({
  email: z.string().trim().email('Geçerli bir e-posta adresi girin'),
});

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

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

export default function ForgotPassword() {
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [forgotPassword, { isLoading }] = useForgotPasswordMutation();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit = async (values: ForgotPasswordFormData) => {
    try {
      const result = await forgotPassword(values).unwrap();
      setIsSubmitted(true);
      toast.success(result.message || 'Şifre sıfırlama linki e-posta adresinize gönderildi.');
    } catch (error) {
      toast.error(extractErrorMessage(error, 'Şifre sıfırlama isteği gönderilemedi.'));
    }
  };

  return (
    <div className="flex min-h-screen items-start justify-center bg-muted/30 px-4 py-12">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mb-4 flex justify-center">
            <KeyRound className="h-12 w-12 text-primary" />
          </div>
          <CardTitle className="text-2xl">Şifremi Unuttum</CardTitle>
          <CardDescription>
            Kayıtlı e-posta adresinizi girin, size sıfırlama linki gönderelim.
          </CardDescription>
        </CardHeader>

        <form onSubmit={handleSubmit(onSubmit)}>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">E-posta Adresi</Label>
              <Input id="email" type="email" placeholder="ornek@email.com" {...register('email')} />
              {errors.email ? <p className="text-sm text-destructive">{errors.email.message}</p> : null}
            </div>

            {isSubmitted ? (
              <div className="rounded-md border border-green-500/30 bg-green-500/10 p-3 text-sm text-green-700 dark:text-green-300">
                <div className="flex items-start gap-2">
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" />
                  <span>
                    E-posta adresinize sıfırlama linki gönderildi. Gelen kutunuzu ve spam klasörünü kontrol edin.
                  </span>
                </div>
              </div>
            ) : null}
          </CardContent>

          <CardFooter className="flex flex-col gap-4 pt-4">
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              {isLoading ? 'Gönderiliyor...' : 'Sıfırlama Linki Gönder'}
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
