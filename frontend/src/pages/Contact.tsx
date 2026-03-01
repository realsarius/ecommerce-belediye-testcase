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
import { useCreateContactMessageMutation } from '@/features/contact/contactApi';

export default function Contact() {
  const [createContactMessage, { isLoading }] = useCreateContactMessageMutation();
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [subject, setSubject] = useState('genel');
  const [message, setMessage] = useState('');

  const handleSubmit = async () => {
    if (!name.trim() || !email.trim() || !subject.trim() || !message.trim()) {
      toast.error('Lütfen tüm alanları doldurun.');
      return;
    }

    try {
      await createContactMessage({
        name: name.trim(),
        email: email.trim(),
        subject,
        message: message.trim(),
      }).unwrap();

      setName('');
      setEmail('');
      setSubject('genel');
      setMessage('');
      toast.success('Mesajınız alındı. En kısa sürede size dönüş yapacağız.');
    } catch (error) {
      const apiError = error as {
        data?: {
          message?: string;
          errors?: Array<{ message?: string }>;
        };
      };

      const validationMessage = apiError.data?.errors?.[0]?.message;
      toast.error(validationMessage || apiError.data?.message || 'Mesaj gönderilirken bir hata oluştu.');
    }
  };

  return (
    <StaticPageLayout
      eyebrow="Kurumsal"
      title="İletişim"
      description="Destek, sipariş, satıcı iş birliği ve genel bilgi talepleriniz için bize ulaşabileceğiniz kanalları burada bulabilirsiniz."
      lastUpdated="Mart 2026"
      actions={<span className="rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs font-medium text-emerald-700 dark:text-emerald-300">İletişim formu aktif</span>}
    >
      <div className="grid gap-6 lg:grid-cols-[1.15fr_0.85fr] lg:gap-8">
        <Card className="border-border/70 bg-card/80 py-0 shadow-sm">
          <CardHeader className="px-7 pt-7 pb-3">
            <CardTitle>Mesaj Gönderin</CardTitle>
          </CardHeader>
          <CardContent className="space-y-5 px-7 pb-7">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="contact-name">Ad Soyad</Label>
                <Input id="contact-name" placeholder="Adınız ve soyadınız" value={name} onChange={(e) => setName(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="contact-email">E-posta</Label>
                <Input id="contact-email" type="email" placeholder="ornek@eposta.com" value={email} onChange={(e) => setEmail(e.target.value)} />
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
              <Textarea id="contact-message" rows={6} placeholder="Size nasıl yardımcı olabiliriz?" value={message} onChange={(e) => setMessage(e.target.value)} />
            </div>
            <Button onClick={handleSubmit} disabled={isLoading} className="sm:w-auto">
              <Send className="h-4 w-4" />
              {isLoading ? 'Gönderiliyor...' : 'Mesajı Gönder'}
            </Button>
          </CardContent>
        </Card>

        <div className="space-y-5">
          <Card className="border-border/70 bg-card/80 py-0 shadow-sm">
            <CardHeader className="px-7 pt-6 pb-2">
              <CardTitle>Adres</CardTitle>
            </CardHeader>
            <CardContent className="flex items-start gap-4 px-7 pb-6 pt-1">
              <MapPin className="mt-1 h-5 w-5 text-rose-500 dark:text-rose-300" />
              <div className="space-y-1">
                <p>Maslak Mah. Teknoloji Cad. No:12</p>
                <p>Sarıyer / İstanbul</p>
              </div>
            </CardContent>
          </Card>
          <Card className="border-border/70 bg-card/80 py-0 shadow-sm">
            <CardHeader className="px-7 pt-6 pb-2">
              <CardTitle>Telefon</CardTitle>
            </CardHeader>
            <CardContent className="flex items-start gap-4 px-7 pb-6 pt-1">
              <Phone className="mt-1 h-5 w-5 text-rose-500 dark:text-rose-300" />
              <div className="space-y-1">
                <p>0850 000 00 00</p>
                <p className="text-sm text-muted-foreground">Hafta içi 09:00 - 18:00</p>
              </div>
            </CardContent>
          </Card>
          <Card className="border-border/70 bg-card/80 py-0 shadow-sm">
            <CardHeader className="px-7 pt-6 pb-2">
              <CardTitle>E-posta</CardTitle>
            </CardHeader>
            <CardContent className="flex items-start gap-4 px-7 pb-6 pt-1">
              <Mail className="mt-1 h-5 w-5 text-rose-500 dark:text-rose-300" />
              <div className="space-y-1">
                <p>destek@eticaret.com</p>
                <p className="text-sm text-muted-foreground">Yanıt süresi genellikle 1 iş günü içindedir.</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </StaticPageLayout>
  );
}
