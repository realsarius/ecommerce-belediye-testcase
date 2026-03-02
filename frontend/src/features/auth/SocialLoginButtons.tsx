import { useEffect, useMemo, useRef, useState } from 'react';
import { Apple, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { cn } from '@/lib/utils';
import type { SocialLoginRequest } from './types';

type SocialLoginButtonsProps = {
  onSocialLogin: (request: SocialLoginRequest) => Promise<void>;
};

type GoogleCallbackResponse = {
  credential?: string;
};

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (options: {
            client_id: string;
            callback: (response: GoogleCallbackResponse) => void;
            ux_mode?: 'popup';
          }) => void;
          renderButton: (element: HTMLElement, options: Record<string, unknown>) => void;
          prompt: () => void;
        };
      };
    };
    AppleID?: {
      auth: {
        init: (config: Record<string, unknown>) => void;
        signIn: () => Promise<{
          authorization?: { id_token?: string };
          user?: { name?: { firstName?: string; lastName?: string } };
        }>;
      };
    };
  }
}

const GOOGLE_SCRIPT_ID = 'google-identity-script';
const APPLE_SCRIPT_ID = 'apple-signin-script';

function loadScript(id: string, src: string, timeoutMs = 8000) {
  const existing = document.getElementById(id);
  if (existing) {
    if ((id === GOOGLE_SCRIPT_ID && window.google) || (id === APPLE_SCRIPT_ID && window.AppleID)) {
      return Promise.resolve();
    }

    existing.remove();
  }

  return new Promise<void>((resolve, reject) => {
    const script = document.createElement('script');
    script.id = id;
    script.src = src;
    script.async = true;
    script.defer = true;
    const timeout = window.setTimeout(() => reject(new Error(`${id} zaman aşımına uğradı.`)), timeoutMs);
    script.onload = () => {
      window.clearTimeout(timeout);
      resolve();
    };
    script.onerror = () => {
      window.clearTimeout(timeout);
      reject(new Error(`${id} yüklenemedi.`));
    };
    document.head.appendChild(script);
  });
}

export function SocialLoginButtons({ onSocialLogin }: SocialLoginButtonsProps) {
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
  const appleClientId = import.meta.env.VITE_APPLE_CLIENT_ID as string | undefined;
  const appleRedirectUri = useMemo(
    () => (import.meta.env.VITE_APPLE_REDIRECT_URI as string | undefined) ?? `${window.location.origin}/login`,
    []
  );

  const googleButtonRef = useRef<HTMLDivElement | null>(null);
  const googleInitializedRef = useRef(false);
  const [googleLoading, setGoogleLoading] = useState(Boolean(googleClientId));
  const [googleLoadError, setGoogleLoadError] = useState<string | null>(null);
  const [appleLoading, setAppleLoading] = useState(false);

  useEffect(() => {
    if (!googleClientId || !googleButtonRef.current || googleInitializedRef.current) {
      return;
    }

    let active = true;

    void loadScript(GOOGLE_SCRIPT_ID, 'https://accounts.google.com/gsi/client')
      .then(() => {
        if (!active || !window.google || !googleButtonRef.current) {
          return;
        }

        window.google.accounts.id.initialize({
          client_id: googleClientId,
          callback: async (response) => {
            if (!response.credential) {
              toast.error('Google kimlik doğrulaması tamamlanamadı.');
              return;
            }

            await onSocialLogin({
              provider: 'google',
              idToken: response.credential,
            });
          },
          ux_mode: 'popup',
        });

        googleButtonRef.current.innerHTML = '';
        window.google.accounts.id.renderButton(googleButtonRef.current, {
          theme: document.documentElement.classList.contains('dark') ? 'filled_black' : 'outline',
          size: 'large',
          shape: 'pill',
          text: 'continue_with',
          width: 320,
        });

        googleInitializedRef.current = true;
        setGoogleLoadError(null);
        setGoogleLoading(false);
      })
      .catch(() => {
        setGoogleLoadError('Google giriş bileşeni şu anda yüklenemedi.');
        setGoogleLoading(false);
      });

    return () => {
      active = false;
    };
  }, [googleClientId, onSocialLogin]);

  useEffect(() => {
    if (!appleClientId) {
      return;
    }

    void loadScript(
      APPLE_SCRIPT_ID,
      'https://appleid.cdn-apple.com/appleauth/static/jsapi/appleid/1/en_US/appleid.auth.js'
    )
      .then(() => {
        window.AppleID?.auth.init({
          clientId: appleClientId,
          scope: 'name email',
          redirectURI: appleRedirectUri,
          usePopup: true,
        });
      })
      .catch(() => {
        toast.error('Apple giriş bileşeni yüklenemedi.');
      });
  }, [appleClientId, appleRedirectUri]);

  if (!googleClientId && !appleClientId) {
    return null;
  }

  const handleAppleLogin = async () => {
    if (!window.AppleID?.auth || !appleClientId) {
      toast.error('Apple sosyal girişi şu anda kullanılamıyor.');
      return;
    }

    try {
      setAppleLoading(true);
      const result = await window.AppleID.auth.signIn();
      const idToken = result.authorization?.id_token;

      if (!idToken) {
        toast.error('Apple kimlik doğrulaması tamamlanamadı.');
        return;
      }

      await onSocialLogin({
        provider: 'apple',
        idToken,
        firstName: result.user?.name?.firstName,
        lastName: result.user?.name?.lastName,
      });
    } catch {
      toast.error('Apple ile giriş iptal edildi veya başarısız oldu.');
    } finally {
      setAppleLoading(false);
    }
  };

  return (
    <div className="space-y-3">
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-background px-2 text-muted-foreground">veya</span>
        </div>
      </div>

      <div className="space-y-3">
        {googleClientId && (
          <div className="space-y-2">
            <div className="relative min-h-10">
              {googleLoading && (
                <div className="absolute inset-0 z-10">
                  <Button type="button" variant="outline" className="w-full justify-center" disabled>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Google yükleniyor...
                  </Button>
                </div>
              )}
              <div
                className={cn('flex min-h-10 justify-center', googleLoading && 'opacity-0')}
                ref={googleButtonRef}
              />
            </div>
            {googleLoadError && (
              <p className="text-center text-sm text-muted-foreground">
                {googleLoadError}
              </p>
            )}
          </div>
        )}

        {appleClientId && (
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={handleAppleLogin}
            disabled={appleLoading}
          >
            {appleLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Apple className="h-4 w-4" />}
            Apple ile Devam Et
          </Button>
        )}
      </div>
    </div>
  );
}
