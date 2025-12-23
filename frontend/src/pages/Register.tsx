import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, useNavigate, Navigate } from 'react-router-dom';
import { useRegisterMutation } from '@/features/auth/authApi';
import { setCredentials } from '@/features/auth/authSlice';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/common/card';
import { toast } from 'sonner';
import { Loader2, Package } from 'lucide-react';

const registerSchema = z
  .object({
    firstName: z.string().min(2, 'İsim en az 2 karakter olmalıdır'),
    lastName: z.string().min(2, 'Soyisim en az 2 karakter olmalıdır'),
    email: z.string().email('Geçerli bir e-posta adresi girin'),
    password: z
      .string()
      .min(6, 'Şifre en az 6 karakter olmalıdır')
      .regex(/[A-Z]/, 'Şifre en az bir büyük harf içermelidir')
      .regex(/[0-9]/, 'Şifre en az bir rakam içermelidir'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Şifreler eşleşmiyor',
    path: ['confirmPassword'],
  });

type RegisterFormData = z.infer<typeof registerSchema>;

export default function Register() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const [register, { isLoading }] = useRegisterMutation();

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const {
    register: registerField,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    try {
      const result = await register(data).unwrap();
      if (result.success && result.token && result.user) {
        dispatch(
          setCredentials({
            user: result.user,
            token: result.token,
            refreshToken: result.refreshToken || '',
          })
        );
        toast.success('Kayıt başarılı!');
        navigate('/', { replace: true });
      } else {
        toast.error(result.message || 'Kayıt başarısız');
      }
    } catch (error: unknown) {
      const err = error as { data?: { message?: string } };
      toast.error(err.data?.message || 'Kayıt olurken bir hata oluştu');
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center px-4 py-8 bg-muted/30">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="flex justify-center mb-4">
            <Package className="h-12 w-12 text-primary" />
          </div>
          <CardTitle className="text-2xl">Kayıt Ol</CardTitle>
          <CardDescription>
            Yeni bir hesap oluşturarak alışverişe başlayın
          </CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit(onSubmit)}>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">İsim</Label>
                <Input
                  id="firstName"
                  placeholder="Kamuran"
                  {...registerField('firstName')}
                />
                {errors.firstName && (
                  <p className="text-sm text-destructive">{errors.firstName.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Soyisim</Label>
                <Input
                  id="lastName"
                  placeholder="Oltacı"
                  {...registerField('lastName')}
                />
                {errors.lastName && (
                  <p className="text-sm text-destructive">{errors.lastName.message}</p>
                )}
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">E-posta</Label>
              <Input
                id="email"
                type="email"
                placeholder="ornek@email.com"
                {...registerField('email')}
              />
              {errors.email && (
                <p className="text-sm text-destructive">{errors.email.message}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Şifre</Label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                {...registerField('password')}
              />
              {errors.password && (
                <p className="text-sm text-destructive">{errors.password.message}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Şifre Tekrar</Label>
              <Input
                id="confirmPassword"
                type="password"
                placeholder="••••••••"
                {...registerField('confirmPassword')}
              />
              {errors.confirmPassword && (
                <p className="text-sm text-destructive">{errors.confirmPassword.message}</p>
              )}
            </div>
          </CardContent>
          <CardFooter className="flex flex-col space-y-4">
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Kayıt Ol
            </Button>
            <p className="text-sm text-center text-muted-foreground">
              Zaten hesabınız var mı?{' '}
              <Link to="/login" className="text-primary hover:underline">
                Giriş Yap
              </Link>
            </p>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
