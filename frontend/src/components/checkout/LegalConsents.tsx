import { useState } from 'react';
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

type LegalConsentKey = 'preliminary-info' | 'distance-sales';

interface LegalConsentSection {
  heading: string;
  paragraphs: string[];
}

interface LegalConsentContent {
  title: string;
  description: string;
  sections: LegalConsentSection[];
}

interface LegalConsentsProps {
  preliminaryInfoAccepted: boolean;
  distanceSalesAccepted: boolean;
  onPreliminaryInfoAcceptedChange: (accepted: boolean) => void;
  onDistanceSalesAcceptedChange: (accepted: boolean) => void;
}

const LEGAL_CONTENT: Record<LegalConsentKey, LegalConsentContent> = {
  'preliminary-info': {
    title: 'Ön Bilgilendirme Formu',
    description: 'Siparişten önce temel satış, teslimat ve cayma bilgilerini özetler.',
    sections: [
      {
        heading: 'Satıcı ve Sipariş Özeti',
        paragraphs: [
          'Satın aldığınız ürün veya hizmetin temel nitelikleri, toplam satış bedeli, vergi ve ek masraflar sipariş özeti ekranında ayrıca gösterilir.',
          'Siparişinizi tamamlamadan önce teslimat adresi, ödeme yöntemi, kupon ve indirim etkileri tarafınıza görünür şekilde sunulur.',
        ],
      },
      {
        heading: 'Ödeme ve Teslimat',
        paragraphs: [
          'Ödeme, seçtiğiniz sağlayıcı ve kart yöntemi üzerinden tahsil edilir. 3D Secure gereken işlemlerde bankanızın doğrulama ekranı ayrıca açılabilir.',
          'Teslimat süresi ürün, satıcı ve kargo firmasına göre değişebilir. Kargo bilgileri sipariş sonrasında hesabınızdan takip edilebilir.',
        ],
      },
      {
        heading: 'Cayma ve Destek',
        paragraphs: [
          'Yasal istisnalar saklı kalmak üzere teslimattan itibaren 14 gün içinde cayma hakkınızı kullanabilirsiniz.',
          'İade, iptal ve destek süreçleri sipariş detay ekranı ve ilgili politika sayfaları üzerinden yönetilebilir.',
        ],
      },
    ],
  },
  'distance-sales': {
    title: 'Mesafeli Satış Sözleşmesi',
    description: 'Tarafların hak ve yükümlülüklerini belirleyen temel sözleşme metnidir.',
    sections: [
      {
        heading: 'Sözleşmenin Konusu',
        paragraphs: [
          'Bu sözleşme, elektronik ortamda oluşturduğunuz siparişe ilişkin ürün veya hizmetin satışı, teslimi ve ödeme koşullarını düzenler.',
          'Sipariş verdiğinizde, ödeme ekranında gördüğünüz toplam tutar ve teslimat bilgileri sözleşmenin ayrılmaz parçası olarak kabul edilir.',
        ],
      },
      {
        heading: 'Teslimat ve Sorumluluk',
        paragraphs: [
          'Satıcı, sipariş konusu ürünü yasal süreler içinde kargoya vermek ve teslim sürecini makul özenle yürütmekle yükümlüdür.',
          'Alıcı, teslimat için doğru adres ve iletişim bilgilerini paylaşmaktan sorumludur.',
        ],
      },
      {
        heading: 'Cayma Hakkı ve İade',
        paragraphs: [
          'Mesafeli satışlar mevzuatındaki istisnalar dışında, tüketici teslimden itibaren 14 gün içinde cayma hakkını kullanabilir.',
          'İade onayı sonrasında geri ödeme, kullanılan ödeme yöntemine göre ilgili sağlayıcı üzerinden tamamlanır.',
        ],
      },
    ],
  },
};

export function LegalConsents({
  preliminaryInfoAccepted,
  distanceSalesAccepted,
  onPreliminaryInfoAcceptedChange,
  onDistanceSalesAcceptedChange,
}: LegalConsentsProps) {
  const [openDialog, setOpenDialog] = useState<LegalConsentKey | null>(null);
  const activeContent = openDialog ? LEGAL_CONTENT[openDialog] : null;

  const handleAccept = () => {
    if (openDialog === 'preliminary-info') {
      onPreliminaryInfoAcceptedChange(true);
    }

    if (openDialog === 'distance-sales') {
      onDistanceSalesAcceptedChange(true);
    }

    setOpenDialog(null);
  };

  return (
    <>
      <div className="space-y-3 rounded-xl border border-border bg-muted/20 p-4">
        <div className="flex items-start gap-3">
          <Checkbox
            id="preliminary-info"
            checked={preliminaryInfoAccepted}
            onCheckedChange={(checked) => onPreliminaryInfoAcceptedChange(!!checked)}
          />
          <label htmlFor="preliminary-info" className="text-sm leading-relaxed text-muted-foreground">
            <button
              type="button"
              className="font-medium text-primary underline underline-offset-4 hover:no-underline"
              onClick={() => setOpenDialog('preliminary-info')}
            >
              Ön Bilgilendirme Formu
            </button>{' '}
            belgesini okudum ve onaylıyorum.
          </label>
        </div>

        <div className="flex items-start gap-3">
          <Checkbox
            id="distance-sales"
            checked={distanceSalesAccepted}
            onCheckedChange={(checked) => onDistanceSalesAcceptedChange(!!checked)}
          />
          <label htmlFor="distance-sales" className="text-sm leading-relaxed text-muted-foreground">
            <button
              type="button"
              className="font-medium text-primary underline underline-offset-4 hover:no-underline"
              onClick={() => setOpenDialog('distance-sales')}
            >
              Mesafeli Satış Sözleşmesi
            </button>{' '}
            metnini okudum ve onaylıyorum.
          </label>
        </div>
      </div>

      <Dialog open={openDialog !== null} onOpenChange={(isOpen) => !isOpen && setOpenDialog(null)}>
        <DialogContent className="max-h-[85vh] max-w-2xl overflow-hidden">
          {activeContent && (
            <>
              <DialogHeader>
                <DialogTitle>{activeContent.title}</DialogTitle>
                <DialogDescription>{activeContent.description}</DialogDescription>
              </DialogHeader>

              <div className="max-h-[58vh] space-y-5 overflow-y-auto pr-2 text-sm leading-6 text-muted-foreground">
                {activeContent.sections.map((section) => (
                  <section key={section.heading} className="space-y-2">
                    <h3 className="text-base font-semibold text-foreground">{section.heading}</h3>
                    {section.paragraphs.map((paragraph) => (
                      <p key={paragraph}>{paragraph}</p>
                    ))}
                  </section>
                ))}
              </div>

              <DialogFooter className="gap-2 sm:justify-between">
                <Button type="button" variant="outline" onClick={() => setOpenDialog(null)}>
                  Kapat
                </Button>
                <Button type="button" onClick={handleAccept}>
                  Kabul Ediyorum
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
