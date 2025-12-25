import { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/common/dialog';
import { Badge } from '@/components/common/badge';
import { Separator } from '@/components/common/separator';
import { Users, Copy, Check, Shield, ShoppingBag, Store, LogIn, Loader2 } from 'lucide-react';
import { Button } from '@/components/common/button';
import { toast } from 'sonner';
import { useLoginMutation } from '@/features/auth/authApi';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { logout, setCredentials } from '@/features/auth/authSlice';

const testUsers = [
  {
    role: 'Admin',
    email: 'testadmin@test.com',
    password: 'Test123!',
    description: 'Tüm yetkilere sahip yönetici hesabı',
    icon: Shield,
    color: 'bg-red-500',
    hoverColor: 'hover:bg-red-600',
  },
  {
    role: 'Seller',
    email: 'testseller@test.com',
    password: 'Test123!',
    description: 'Satıcı hesabı - Ürün ve sipariş yönetimi',
    icon: Store,
    color: 'bg-amber-500',
    hoverColor: 'hover:bg-amber-600',
  },
  {
    role: 'Customer',
    email: 'customer@test.com',
    password: 'Test123!',
    description: 'Standart müşteri hesabı',
    icon: ShoppingBag,
    color: 'bg-blue-500',
    hoverColor: 'hover:bg-blue-600',
  },
];

interface TestUsersDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function TestUsersDialog({ open, onOpenChange }: TestUsersDialogProps) {
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [loggingInIndex, setLoggingInIndex] = useState<number | null>(null);
  
  const dispatch = useAppDispatch();
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const [login] = useLoginMutation();

  const copyToClipboard = async (text: string, fieldId: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopiedField(fieldId);
      toast.success('Kopyalandı!');
      setTimeout(() => setCopiedField(null), 2000);
    } catch {
      toast.error('Kopyalanamadı');
    }
  };

  const handleQuickLogin = async (email: string, password: string, role: string, index: number) => {
    try {
      setLoggingInIndex(index);
      

      if (isAuthenticated) {
        dispatch(logout());

        await new Promise(resolve => setTimeout(resolve, 100));
      }
      

      const result = await login({ email, password }).unwrap();
      
      if (result.success && result.token && result.user) {
        dispatch(setCredentials({
          token: result.token,
          refreshToken: result.refreshToken ?? '',
          user: result.user,
        }));
        
        toast.success(`${role} olarak giriş yapıldı!`, {
          description: `Hoş geldiniz, ${result.user.firstName}!`,
        });
        
        onOpenChange(false);
      } else {
        toast.error('Giriş başarısız', {
          description: result.message || 'Bir hata oluştu',
        });
      }
    } catch (error) {
      console.error('Login error:', error);
      toast.error('Giriş yapılamadı', {
        description: 'Lütfen tekrar deneyin',
      });
    } finally {
      setLoggingInIndex(null);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl">
            <Users className="h-5 w-5 text-primary" />
            Test Kullanıcıları
          </DialogTitle>
          <DialogDescription>
            Tek tıkla test kullanıcısı ile giriş yapın veya bilgileri kopyalayın.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 mt-4">
          {testUsers.map((user, index) => {
            const Icon = user.icon;
            const isLoggingIn = loggingInIndex === index;
            
            return (
              <div
                key={index}
                className="p-4 rounded-xl border bg-card hover:shadow-md transition-all"
              >
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <div className={`p-2 rounded-lg ${user.color}`}>
                      <Icon className="h-5 w-5 text-white" />
                    </div>
                    <div>
                      <Badge className={`${user.color} text-white`}>
                        {user.role}
                      </Badge>
                      <p className="text-xs text-muted-foreground mt-1">
                        {user.description}
                      </p>
                    </div>
                  </div>
                  
                  {/* Quick Login Button */}
                  <Button
                    size="sm"
                    className={`${user.color} ${user.hoverColor} text-white`}
                    onClick={() => handleQuickLogin(user.email, user.password, user.role, index)}
                    disabled={isLoggingIn}
                  >
                    {isLoggingIn ? (
                      <>
                        <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                        Giriş...
                      </>
                    ) : (
                      <>
                        <LogIn className="mr-1 h-4 w-4" />
                        Giriş Yap
                      </>
                    )}
                  </Button>
                </div>
                
                <Separator className="my-3" />
                
                <div className="space-y-2">
                  <div className="flex items-center justify-between p-2 rounded-lg bg-muted/50">
                    <div>
                      <span className="text-xs text-muted-foreground">Email</span>
                      <p className="font-mono text-sm">{user.email}</p>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={(e) => {
                        e.stopPropagation();
                        copyToClipboard(user.email, `email-${index}`);
                      }}
                    >
                      {copiedField === `email-${index}` ? (
                        <Check className="h-4 w-4 text-green-500" />
                      ) : (
                        <Copy className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                  
                  <div className="flex items-center justify-between p-2 rounded-lg bg-muted/50">
                    <div>
                      <span className="text-xs text-muted-foreground">Şifre</span>
                      <p className="font-mono text-sm">{user.password}</p>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={(e) => {
                        e.stopPropagation();
                        copyToClipboard(user.password, `password-${index}`);
                      }}
                    >
                      {copiedField === `password-${index}` ? (
                        <Check className="h-4 w-4 text-green-500" />
                      ) : (
                        <Copy className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                </div>
              </div>
            );
          })}

          {/* Bilgi Kutusu */}
          <div className="bg-muted p-4 rounded-lg">
            <p className="text-sm text-muted-foreground">
              <strong>💡 İpucu:</strong> "Giriş Yap" butonuna tıklayarak tek tıkla 
              ilgili kullanıcı ile giriş yapabilirsiniz. {isAuthenticated && 'Mevcut oturumunuz otomatik kapatılır.'}
            </p>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

