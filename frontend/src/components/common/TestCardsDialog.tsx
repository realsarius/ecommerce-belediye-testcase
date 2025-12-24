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
import { CreditCard, Copy, Check, Sparkles } from 'lucide-react';
import { Button } from '@/components/common/button';
import { toast } from 'sonner';

const testCards = [
  { type: 'Visa', number: '4111 1111 1111 1111', color: 'bg-blue-500' },
  { type: 'Visa', number: '4012 8888 8888 1881', color: 'bg-blue-500' },
  { type: 'Mastercard', number: '5555 5555 5555 4444', color: 'bg-orange-500' },
  { type: 'Mastercard', number: '5105 1051 0510 5100', color: 'bg-orange-500' },
  { type: 'Troy', number: '9792 0303 9444 0796', color: 'bg-red-500' },
  { type: 'Amex', number: '3782 822463 10005', color: 'bg-green-500' },
];

const turkishCards = [
  { bank: 'Paraf Visa (Halkbank)', number: '4988 5200 0000 0005' },
  { bank: 'Paraf MC (Halkbank)', number: '5528 7900 0000 0008' },
  { bank: 'Paraf Troy (Halkbank)', number: '9792 1000 0000 0001' },
  { bank: 'Bonus (Garanti)', number: '5406 6754 0667 5403' },
  { bank: 'Axess (Akbank)', number: '4355 0843 5508 4358' },
  { bank: 'Halkbank Visa', number: '4475 0500 0000 0003' },
  { bank: 'Halkbank MC', number: '5818 7758 1877 2285' },
];

// Iyzico Sandbox Test Kartları
const iyzicoSuccessCards = [
  { result: 'Başarılı (iptal/iade yapılamaz)', number: '5406 6700 0000 0009', color: 'bg-green-500' },
];

const iyzicoErrorCards = [
  { error: 'Yetersiz bakiye', number: '4111 1111 1111 1129', color: 'bg-red-500' },
  { error: 'İşlem reddedildi', number: '4129 1111 1111 1111', color: 'bg-red-500' },
  { error: 'Geçersiz işlem', number: '4128 1111 1111 1112', color: 'bg-red-500' },
  { error: 'Kayıp kart', number: '4127 1111 1111 1113', color: 'bg-red-500' },
  { error: 'Çalıntı kart', number: '4126 1111 1111 1114', color: 'bg-red-500' },
  { error: 'Süresi dolmuş kart', number: '4125 1111 1111 1115', color: 'bg-red-500' },
  { error: 'Geçersiz CVC', number: '4124 1111 1111 1116', color: 'bg-red-500' },
  { error: 'Kart sahibine izin verilmedi', number: '4123 1111 1111 1117', color: 'bg-red-500' },
  { error: 'Terminale izin verilmedi', number: '4122 1111 1111 1118', color: 'bg-red-500' },
  { error: 'Dolandırıcılık şüphesi', number: '4121 1111 1111 1119', color: 'bg-red-500' },
  { error: 'Kart geri alınmalı', number: '4120 1111 1111 1110', color: 'bg-red-500' },
  { error: 'Genel hata', number: '4130 1111 1111 1118', color: 'bg-red-500' },
  { error: 'mdStatus 0', number: '4131 1111 1111 1117', color: 'bg-orange-500' },
  { error: 'mdStatus 4', number: '4141 1111 1111 1115', color: 'bg-orange-500' },
  { error: '3D Secure başlatılamadı', number: '4151 1111 1111 1112', color: 'bg-orange-500' },
];

interface TestCardsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function TestCardsDialog({ open, onOpenChange }: TestCardsDialogProps) {
  const [copiedIndex, setCopiedIndex] = useState<number | null>(null);

  const copyToClipboard = async (text: string, index: number) => {
    try {
      await navigator.clipboard.writeText(text.replace(/\s/g, ''));
      setCopiedIndex(index);
      toast.success('Kart numarası kopyalandı!');
      setTimeout(() => setCopiedIndex(null), 2000);
    } catch {
      toast.error('Kopyalanamadı');
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl">
            <Sparkles className="h-5 w-5 text-yellow-500" />
            Test Kartları
            <Sparkles className="h-5 w-5 text-yellow-500" />
          </DialogTitle>
          <DialogDescription>
            Test kartlarını kullanarak ödeme işlemlerini deneyebilirsiniz.
            Son kullanma tarihi ve CVC için herhangi bir geçerli değer kullanın.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 mt-4">
          {/* Genel Test Kartları */}
          <div>
            <h3 className="font-semibold mb-3 flex items-center gap-2">
              <CreditCard className="h-4 w-4" />
              Genel Test Kartları
            </h3>
            <div className="grid gap-2">
              {testCards.map((card, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <Badge className={`${card.color} text-white`}>
                      {card.type}
                    </Badge>
                    <code className="font-mono text-sm">{card.number}</code>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => copyToClipboard(card.number, index)}
                  >
                    {copiedIndex === index ? (
                      <Check className="h-4 w-4 text-green-500" />
                    ) : (
                      <Copy className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              ))}
            </div>
          </div>

          <Separator />

          {/* Türk Bankaları */}
          <div>
            <h3 className="font-semibold mb-3 flex items-center gap-2">
              🇹🇷 Türk Bankaları Test Kartları
            </h3>
            <div className="grid gap-2">
              {turkishCards.map((card, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-medium min-w-[140px]">
                      {card.bank}
                    </span>
                    <code className="font-mono text-sm">{card.number}</code>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => copyToClipboard(card.number, index + 100)}
                  >
                    {copiedIndex === index + 100 ? (
                      <Check className="h-4 w-4 text-green-500" />
                    ) : (
                      <Copy className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              ))}
            </div>
          </div>
          <Separator />

          {/* Iyzico Sandbox Başarılı Kart */}
          <div>
            <h3 className="font-semibold mb-3 flex items-center gap-2">
              ✅ Iyzico Sandbox - Başarılı Ödeme
            </h3>
            <div className="grid gap-2">
              {iyzicoSuccessCards.map((card, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <Badge className={`${card.color} text-white`}>
                      {card.result}
                    </Badge>
                    <code className="font-mono text-sm">{card.number}</code>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => copyToClipboard(card.number, index + 200)}
                  >
                    {copiedIndex === index + 200 ? (
                      <Check className="h-4 w-4 text-green-500" />
                    ) : (
                      <Copy className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              ))}
            </div>
          </div>

          <Separator />

          {/* Iyzico Sandbox Hata Kartları */}
          <div>
            <h3 className="font-semibold mb-3 flex items-center gap-2">
              ❌ Iyzico Sandbox - Hatalı Kartlar (Test Senaryoları)
            </h3>
            <div className="grid gap-2">
              {iyzicoErrorCards.map((card, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <Badge className={`${card.color} text-white text-xs`}>
                      {card.error}
                    </Badge>
                    <code className="font-mono text-sm">{card.number}</code>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => copyToClipboard(card.number, index + 300)}
                  >
                    {copiedIndex === index + 300 ? (
                      <Check className="h-4 w-4 text-green-500" />
                    ) : (
                      <Copy className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              ))}
            </div>
          </div>

          <Separator />

          {/* Bilgi Kutusu */}
          <div className="bg-muted p-4 rounded-lg">
            <p className="text-sm text-muted-foreground">
              <strong>💡 İpucu:</strong> Son kullanma tarihi için herhangi bir gelecek tarih 
              (örn: 12/26) ve CVC için 3 haneli herhangi bir sayı (örn: 123) kullanabilirsiniz.
            </p>
          </div>

          <p className="text-xs text-center text-muted-foreground">
            GTA San Andreas tarzı cheat code: <kbd className="px-1 py-0.5 bg-muted rounded text-xs font-mono">LEAVEMEALONE</kbd> aktif, 
            <kbd className="px-1 py-0.5 bg-muted rounded text-xs font-mono ml-1">AEZAKMI</kbd> devre dışı 🎮
          </p>
        </div>
      </DialogContent>
    </Dialog>
  );
}
