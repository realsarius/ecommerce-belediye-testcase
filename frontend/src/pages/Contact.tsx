import { useState } from 'react';
import { Mail, MapPin, Phone, Send } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/common/select';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';
import { Textarea } from '@/components/common/textarea';

export default function Contact() {
  const [subject, setSubject] = useState('genel');

  return (
    <StaticPageLayout
      eyebrow="Kurumsal"
      title="İletişim"
      description="Destek, sipariş, satıcı iş birliği ve genel bilgi talepleriniz için bize ulaşabileceğiniz kanalları burada bulabilirsiniz."
      lastUpdated="Mart 2026"
      actions={
        <span className="rounded-full border border-amber-400/30 bg-amber-400/10 px-3 py-1 text-xs font-medium text-amber-200">
          Form backend entegrasyonu bir sonraki fazda açılacak
        </span>
      }
    >
      <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
        <Card className="border-white/10 bg-white/[0.03] py-0">
          <CardHeader>
            <CardTitle>Mesaj Gönderin</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="contact-name">Ad Soyad</Label>
                <Input id="contact-name" placeholder="Adınız ve soyadınız" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="contact-email">E-posta</Label>
                <Input id="contact-email" type="email" placeholder="ornek@eposta.com" />
              </div>
            </div>
            <div className="space-y-2">
              <Label>Konu</Label>
              <Select value={subject} onValueChange={setSubject}>
                <SelectTrigger>
                  <SelectValue placeholder="Bir konu seçin" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="genel">Genel Bilgi</SelectItem>
                  <SelectItem value="siparis">Sipariş ve Teslimat</SelectItem>
                  <SelectItem value="iade">İade ve İptal</SelectItem>
                  <SelectItem value="satici">Satıcı İş Birliği</SelectItem>
                  <SelectItem value="teknik">Teknik Sorun</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="contact-message">Mesaj</Label>
              <Textarea id="contact-message" rows={6} placeholder="Size nasıl yardımcı olabiliriz?" />
            </div>
            <Button
              onClick={() =>
                toast.info('İletişim formu kayıt ve event akışı ikinci fazda backend ile açılacak.')
              }
            >
              <Send className="h-4 w-4" />
              Mesajı Gönder
            </Button>
          </CardContent>
        </Card>

        <div className="space-y-4">
          <Card className="border-white/10 bg-white/[0.03] py-0">
            <CardHeader>
              <CardTitle>Adres</CardTitle>
            </CardHeader>
            <CardContent className="flex items-start gap-3">
              <MapPin className="mt-1 h-5 w-5 text-rose-300" />
              <div>
                <p>Maslak Mah. Teknoloji Cad. No:12</p>
                <p>Sarıyer / İstanbul</p>
              </div>
            </CardContent>
          </Card>
          <Card className="border-white/10 bg-white/[0.03] py-0">
            <CardHeader>
              <CardTitle>Telefon</CardTitle>
            </CardHeader>
            <CardContent className="flex items-start gap-3">
              <Phone className="mt-1 h-5 w-5 text-rose-300" />
              <div>
                <p>0850 000 00 00</p>
                <p className="text-sm text-gray-400">Hafta içi 09:00 - 18:00</p>
              </div>
            </CardContent>
          </Card>
          <Card className="border-white/10 bg-white/[0.03] py-0">
            <CardHeader>
              <CardTitle>E-posta</CardTitle>
            </CardHeader>
            <CardContent className="flex items-start gap-3">
              <Mail className="mt-1 h-5 w-5 text-rose-300" />
              <div>
                <p>destek@eticaret.com</p>
                <p className="text-sm text-gray-400">Yanıt süresi genellikle 1 iş günü içindedir.</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </StaticPageLayout>
  );
}
