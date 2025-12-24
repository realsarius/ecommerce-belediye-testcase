import { createContext, useContext, useState, useEffect, useCallback, useRef, type ReactNode } from 'react';
import { toast } from 'sonner';
import { CouponsCheatDialog } from '@/components/common/CouponsCheatDialog';

interface DevToolsContextType {
  isDevToolsEnabled: boolean;
  enableDevTools: () => void;
  disableDevTools: () => void;
  openCouponsDialog: () => void;
}

const DevToolsContext = createContext<DevToolsContextType | null>(null);

export function useDevTools() {
  const context = useContext(DevToolsContext);
  if (!context) {
    throw new Error('useDevTools must be used within DevToolsProvider');
  }
  return context;
}

interface DevToolsProviderProps {
  children: ReactNode;
}

// GTA San Andreas style cheat codes 🎮
const ENABLE_CODE = 'LEAVEMEALONE';
const DISABLE_CODE = 'AEZAKMI';
const COUPONS_CODE = 'ALOVELYDAY';

export function DevToolsProvider({ children }: DevToolsProviderProps) {
  const [isDevToolsEnabled, setIsDevToolsEnabled] = useState(() => {
    // localStorage'dan başlangıç değerini al
    return localStorage.getItem('devToolsEnabled') === 'true';
  });
  
  const [showCouponsDialog, setShowCouponsDialog] = useState(false);
  
  // useRef ile buffer tutarak çift render'ı önle
  const inputBufferRef = useRef('');
  const timeoutRef = useRef<number | null>(null);

  const enableDevTools = useCallback(() => {
    setIsDevToolsEnabled(true);
    localStorage.setItem('devToolsEnabled', 'true');
  }, []);

  const disableDevTools = useCallback(() => {
    setIsDevToolsEnabled(false);
    localStorage.removeItem('devToolsEnabled');
  }, []);

  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    // Eğer bir input/textarea'da isek, cheat code'u tetikleme
    if (
      e.target instanceof HTMLInputElement ||
      e.target instanceof HTMLTextAreaElement
    ) {
      return;
    }

    // Sadece harf tuşlarını kabul et
    if (e.key.length === 1 && /[a-zA-Z]/.test(e.key)) {
      // En uzun kod kadar buffer tut
      const maxLength = Math.max(ENABLE_CODE.length, DISABLE_CODE.length, COUPONS_CODE.length);
      inputBufferRef.current = (inputBufferRef.current + e.key.toUpperCase()).slice(-maxLength);
      
      // Timeout'u sıfırla
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
      timeoutRef.current = window.setTimeout(() => {
        inputBufferRef.current = '';
      }, 3000);
      
      // LEAVEMEALONE - Dev Tools'u aç
      if (inputBufferRef.current.endsWith(ENABLE_CODE)) {
        enableDevTools();
        toast.success('🎮 Cheat Activated!', {
          description: 'LEAVEMEALONE - Dev Tools aktif edildi!',
        });
        inputBufferRef.current = '';
        return;
      }
      
      // AEZAKMI - Dev Tools'u kapat
      if (inputBufferRef.current.endsWith(DISABLE_CODE)) {
        disableDevTools();
        toast.success('🎮 Cheat Deactivated!', {
          description: 'AEZAKMI - Dev Tools devre dışı bırakıldı!',
        });
        inputBufferRef.current = '';
        return;
      }

      // ALOVELYDAY - Kuponları göster
      if (inputBufferRef.current.endsWith(COUPONS_CODE)) {
        if (!isDevToolsEnabled) {
          // DevTools kapalıysa önce açalım mı? Yoksa bağımsız mı çalışsın?
          // GTA mantığı: her şifre bağımsızdır. Ama bu bir "Dev Tool" şifresi.
          // Kullanıcının "Dev Tools biliyorsun" demesinden bağımsız da çalışabileceğini anlıyorum.
          // Ama genelde DevTools açıkken olması daha mantıklı. 
          // Yine de "Cheat" olduğu için direkt çalışsın.
          enableDevTools(); // Otomatik açalım
        }
        setShowCouponsDialog(true);
        toast.success('🎫 Coupon Cheat Activated!', {
          description: 'ALOVELYDAY - Kupon listesi açıldı!',
        });
        inputBufferRef.current = '';
        return;
      }
    }
  }, [enableDevTools, disableDevTools]);

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [handleKeyDown]);

  return (
    <DevToolsContext.Provider value={{
      isDevToolsEnabled,
      enableDevTools,
      disableDevTools,
      openCouponsDialog: () => setShowCouponsDialog(true)
    }}>
      {children}
      <CouponsCheatDialog open={showCouponsDialog} onOpenChange={setShowCouponsDialog} />
    </DevToolsContext.Provider>
  );
}

