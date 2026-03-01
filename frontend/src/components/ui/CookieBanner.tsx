import { useEffect, useState } from 'react';
import { Cookie, ShieldCheck } from 'lucide-react';
import { Button } from '@/components/common/button';
import { Checkbox } from '@/components/common/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/common/dialog';
import { Label } from '@/components/common/label';
import {
  COOKIE_SETTINGS_EVENT,
  defaultCookiePreferences,
  openCookieSettings,
  persistCookieDecision,
  readCookieConsent,
  readCookiePreferences,
  type CookiePreferences,
} from '@/features/cookies/cookieConsent';

function PreferenceRow({
  title,
  description,
  checked,
  disabled = false,
  onCheckedChange,
}: {
  title: string;
  description: string;
  checked: boolean;
  disabled?: boolean;
  onCheckedChange?: (checked: boolean) => void;
}) {
  return (
    <div className="flex items-start justify-between gap-4 rounded-2xl border border-border/70 bg-card/70 p-4">
      <div className="space-y-1">
        <Label className="text-sm font-medium text-foreground">{title}</Label>
        <p className="text-sm leading-6 text-muted-foreground">{description}</p>
      </div>
      <Checkbox
        checked={checked}
        disabled={disabled}
        onCheckedChange={(value) => onCheckedChange?.(value === true)}
        aria-label={title}
        className="mt-1 h-5 w-5 rounded-md border-border data-[state=checked]:border-rose-500 data-[state=checked]:bg-rose-500"
      />
    </div>
  );
}

export function CookieBanner() {
  const [visible, setVisible] = useState(() => {
    if (typeof window === 'undefined') {
      return false;
    }

    return !readCookieConsent();
  });
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [preferences, setPreferences] = useState<CookiePreferences>(() => {
    if (typeof window === 'undefined') {
      return defaultCookiePreferences;
    }

    return readCookiePreferences();
  });

  useEffect(() => {
    const handleOpen = () => setSettingsOpen(true);
    window.addEventListener(COOKIE_SETTINGS_EVENT, handleOpen);
    return () => window.removeEventListener(COOKIE_SETTINGS_EVENT, handleOpen);
  }, []);

  const acceptAll = () => {
    const nextPreferences = {
      necessary: true as const,
      analytics: true,
      marketing: true,
    };
    setPreferences(nextPreferences);
    persistCookieDecision('accepted', nextPreferences);
    setVisible(false);
    setSettingsOpen(false);
  };

  const acceptNecessaryOnly = () => {
    const nextPreferences = {
      necessary: true as const,
      analytics: false,
      marketing: false,
    };
    setPreferences(nextPreferences);
    persistCookieDecision('rejected', nextPreferences);
    setVisible(false);
    setSettingsOpen(false);
  };

  const saveSettings = () => {
    persistCookieDecision('accepted', preferences);
    setVisible(false);
    setSettingsOpen(false);
  };

  return (
    <>
      {visible && (
        <div className="pointer-events-none fixed inset-x-0 bottom-0 z-[60] px-4 pb-4">
          <div className="pointer-events-auto mx-auto max-w-5xl rounded-3xl border border-border/70 bg-background/95 p-5 text-foreground shadow-2xl shadow-black/10 backdrop-blur data-[state=open]:animate-in data-[state=open]:slide-in-from-bottom-6 dark:border-white/10 dark:bg-[#101318]/95 dark:text-white dark:shadow-black/50">
            <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
              <div className="flex items-start gap-4">
                <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-rose-500/10 text-rose-600 dark:text-rose-300">
                  <Cookie className="h-5 w-5" />
                </div>
                <div className="space-y-2">
                  <p className="font-semibold">Çerez tercihlerinizi yönetin</p>
                  <p className="max-w-2xl text-sm leading-6 text-muted-foreground dark:text-slate-300">
                    Zorunlu çerezlerin yanında analitik ve pazarlama çerezlerini deneyimi geliştirmek için kullanmak
                    istiyoruz. Tercihinizi şimdi belirleyebilir, daha sonra ayarlar üzerinden değiştirebilirsiniz.
                  </p>
                </div>
              </div>
              <div className="flex flex-col gap-3 sm:flex-row">
                <Button onClick={acceptAll}>
                  Tümünü Kabul Et
                </Button>
                <Button variant="ghost" onClick={acceptNecessaryOnly}>
                  Yalnızca Zorunlu
                </Button>
                <Button
                  variant="link"
                  onClick={openCookieSettings}
                  className="justify-start px-0 text-rose-600 dark:text-rose-200"
                >
                  Ayarları Yönet
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}

      <Dialog open={settingsOpen} onOpenChange={setSettingsOpen}>
        <DialogContent className="max-w-xl border-border/70 bg-background text-foreground shadow-2xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <ShieldCheck className="h-5 w-5 text-rose-500 dark:text-rose-300" />
              Çerez ayarları
            </DialogTitle>
            <DialogDescription className="text-muted-foreground">
              Çerez kategorilerini ihtiyaçlarınıza göre yönetebilirsiniz. Zorunlu çerezler güvenlik ve temel site
              işlevleri için her zaman aktiftir.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <PreferenceRow
              title="Zorunlu çerezler"
              description="Oturum güvenliği, sepet, kimlik doğrulama ve temel site işlevleri için gereklidir."
              checked
              disabled
            />
            <PreferenceRow
              title="Analitik çerezler"
              description="Hangi sayfaların daha çok kullanıldığını anlamamıza ve deneyimi iyileştirmemize yardımcı olur."
              checked={preferences.analytics}
              onCheckedChange={(checked) => setPreferences((prev) => ({ ...prev, analytics: checked }))}
            />
            <PreferenceRow
              title="Pazarlama çerezleri"
              description="Kampanya gösterimleri, öneriler ve yeniden etkileşim akışları için kullanılır."
              checked={preferences.marketing}
              onCheckedChange={(checked) => setPreferences((prev) => ({ ...prev, marketing: checked }))}
            />
          </div>

          <DialogFooter>
            <Button variant="ghost" onClick={acceptNecessaryOnly}>
              Yalnızca Zorunlu
            </Button>
            <Button variant="outline" onClick={saveSettings}>
              Seçimi Kaydet
            </Button>
            <Button onClick={acceptAll}>
              Tümünü Kabul Et
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

export default CookieBanner;
