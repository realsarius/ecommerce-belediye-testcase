import { Checkbox } from '@/components/common/checkbox';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/common/tabs';
import { Textarea } from '@/components/common/textarea';
import type { CheckoutInvoiceInfo, CheckoutInvoiceType } from '@/features/cart/types';

interface SelectedAddressSummary {
  fullName: string;
  fullAddress: string;
}

interface InvoiceInfoProps {
  value: CheckoutInvoiceInfo;
  useShippingAddress: boolean;
  selectedAddress?: SelectedAddressSummary;
  onTypeChange: (type: CheckoutInvoiceType) => void;
  onChange: (value: CheckoutInvoiceInfo) => void;
  onUseShippingAddressChange: (value: boolean) => void;
}

export function InvoiceInfo({
  value,
  useShippingAddress,
  selectedAddress,
  onTypeChange,
  onChange,
  onUseShippingAddressChange,
}: InvoiceInfoProps) {
  const updateField = <K extends keyof CheckoutInvoiceInfo>(field: K, fieldValue: CheckoutInvoiceInfo[K]) => {
    onChange({
      ...value,
      [field]: fieldValue,
    });
  };

  return (
    <div className="space-y-4">
      <Tabs value={value.type} onValueChange={(nextValue) => onTypeChange(nextValue as CheckoutInvoiceType)}>
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="Individual">Bireysel</TabsTrigger>
          <TabsTrigger value="Corporate">Kurumsal</TabsTrigger>
        </TabsList>

        <TabsContent value="Individual" className="space-y-4 pt-2">
          <div className="space-y-2">
            <Label>Ad Soyad</Label>
            <Input
              placeholder="Ad Soyad"
              value={value.fullName ?? ''}
              onChange={(event) => updateField('fullName', event.target.value)}
            />
            {selectedAddress?.fullName && (
              <p className="text-xs text-muted-foreground">
                Teslimat adresindeki isim varsayılan olarak getirildi, istersen düzenleyebilirsin.
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label>TC Kimlik No (Opsiyonel)</Label>
            <Input
              placeholder="11 haneli"
              maxLength={11}
              value={value.tcKimlikNo ?? ''}
              onChange={(event) => updateField('tcKimlikNo', event.target.value.replace(/\D/g, '').slice(0, 11))}
            />
          </div>
        </TabsContent>

        <TabsContent value="Corporate" className="space-y-4 pt-2">
          <div className="space-y-2">
            <Label>Şirket Adı</Label>
            <Input
              placeholder="ABC Teknoloji A.Ş."
              value={value.companyName ?? ''}
              onChange={(event) => updateField('companyName', event.target.value)}
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Vergi Dairesi</Label>
              <Input
                placeholder="Kadıköy"
                value={value.taxOffice ?? ''}
                onChange={(event) => updateField('taxOffice', event.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label>Vergi Numarası</Label>
              <Input
                placeholder="10 haneli"
                maxLength={10}
                value={value.taxNumber ?? ''}
                onChange={(event) => updateField('taxNumber', event.target.value.replace(/\D/g, '').slice(0, 10))}
              />
            </div>
          </div>
        </TabsContent>
      </Tabs>

      <div className="space-y-3 rounded-xl border border-border bg-muted/20 p-4">
        <div className="flex items-start gap-3">
          <Checkbox
            id="use-shipping-address-for-invoice"
            checked={useShippingAddress}
            onCheckedChange={(checked) => onUseShippingAddressChange(!!checked)}
          />
          <label
            htmlFor="use-shipping-address-for-invoice"
            className="text-sm leading-relaxed text-muted-foreground"
          >
            Fatura adresi teslimat adresi ile aynı olsun
          </label>
        </div>

        {useShippingAddress ? (
          <div className="rounded-lg border bg-background/70 p-3 text-sm text-muted-foreground">
            {selectedAddress ? selectedAddress.fullAddress : 'Teslimat adresi seçildiğinde burada görünecek.'}
          </div>
        ) : (
          <div className="space-y-2">
            <Label>Fatura Adresi</Label>
            <Textarea
              rows={4}
              placeholder="Fatura adresini girin"
              value={value.invoiceAddress}
              onChange={(event) => updateField('invoiceAddress', event.target.value)}
            />
          </div>
        )}
      </div>
    </div>
  );
}
