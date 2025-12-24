import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/common/card';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Textarea } from '@/components/common/textarea';
import { Label } from '@/components/common/label';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/common/accordion';
import { 
  HelpCircle, 
  MessageCircle, 
  Mail, 
  Phone, 
  Package, 
  CreditCard, 
  Truck, 
  RotateCcw,
  Search,
  Send,
  Loader2
} from 'lucide-react';
import { toast } from 'sonner';

const faqItems = [
  {
    category: 'Sipariş',
    icon: Package,
    questions: [
      {
        q: 'Siparişimi nasıl takip edebilirim?',
        a: 'Siparişlerinizi "Hesabım > Tüm Siparişlerim" bölümünden takip edebilirsiniz. Sipariş detay sayfasında kargo takip numarasını görebilirsiniz.'
      },
      {
        q: 'Siparişimi iptal edebilir miyim?',
        a: 'Siparişiniz henüz kargoya verilmemişse iptal edebilirsiniz. Bunun için sipariş detay sayfasındaki "İptal Et" butonunu kullanabilirsiniz.'
      },
    ]
  },
  {
    category: 'Ödeme',
    icon: CreditCard,
    questions: [
      {
        q: 'Hangi ödeme yöntemlerini kabul ediyorsunuz?',
        a: 'Kredi kartı ve banka kartı ile ödeme kabul ediyoruz. Tüm ödemeler güvenli SSL sertifikası ile şifrelenmektedir.'
      },
      {
        q: 'Ödeme bilgilerim güvende mi?',
        a: 'Evet, tüm ödeme işlemlerimiz 256-bit SSL şifreleme ile korunmaktadır. Kart bilgileriniz sunucularımızda saklanmaz.'
      },
    ]
  },
  {
    category: 'Kargo & Teslimat',
    icon: Truck,
    questions: [
      {
        q: 'Kargo ücreti ne kadar?',
        a: 'Tüm siparişlerde kargo ücretsizdir!'
      },
      {
        q: 'Teslimat ne kadar sürer?',
        a: 'Siparişleriniz genellikle 2-5 iş günü içinde teslim edilir. Teslimat süresi bulunduğunuz bölgeye göre değişebilir.'
      },
    ]
  },
  {
    category: 'İade & Değişim',
    icon: RotateCcw,
    questions: [
      {
        q: 'İade koşulları nelerdir?',
        a: 'Ürünlerinizi teslim aldıktan sonra 14 gün içinde iade edebilirsiniz. Ürünün kullanılmamış ve orijinal ambalajında olması gerekmektedir.'
      },
      {
        q: 'İade işlemi nasıl yapılır?',
        a: 'Sipariş detay sayfasından "İade Talebi Oluştur" butonuna tıklayarak iade talebinizi oluşturabilirsiniz. Kargo ücreti tarafımızca karşılanır.'
      },
    ]
  },
];

export default function Help() {
  const [searchQuery, setSearchQuery] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [contactForm, setContactForm] = useState({
    subject: '',
    message: '',
  });

  const filteredFaq = faqItems.map(category => ({
    ...category,
    questions: category.questions.filter(
      item => 
        item.q.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.a.toLowerCase().includes(searchQuery.toLowerCase())
    )
  })).filter(category => category.questions.length > 0);

  const handleSendMessage = async () => {
    if (!contactForm.subject || !contactForm.message) {
      toast.error('Lütfen tüm alanları doldurun');
      return;
    }
    
    setIsSending(true);
    // TODO: API entegrasyonu
    await new Promise(resolve => setTimeout(resolve, 1500));
    setIsSending(false);
    setContactForm({ subject: '', message: '' });
    toast.success('Mesajınız gönderildi. En kısa sürede size dönüş yapacağız.');
  };

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <div className="text-center mb-12">
        <div className="inline-flex items-center justify-center h-16 w-16 rounded-full bg-primary/10 mb-4">
          <HelpCircle className="h-8 w-8 text-primary" />
        </div>
        <h1 className="text-3xl font-bold mb-2">Yardım Merkezi</h1>
        <p className="text-muted-foreground">Size nasıl yardımcı olabiliriz?</p>
      </div>

      {/* Search */}
      <div className="relative mb-8">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground" />
        <Input
          placeholder="Soru veya konu ara..."
          className="pl-10 h-12 text-lg"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
      </div>

      {/* FAQ Section */}
      <div className="space-y-6 mb-12">
        <h2 className="text-xl font-semibold">Sıkça Sorulan Sorular</h2>
        
        {filteredFaq.length > 0 ? (
          filteredFaq.map((category) => (
            <Card key={category.category}>
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <category.icon className="h-5 w-5 text-primary" />
                  {category.category}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <Accordion type="single" collapsible className="w-full">
                  {category.questions.map((item, idx) => (
                    <AccordionItem key={idx} value={`${category.category}-${idx}`}>
                      <AccordionTrigger className="text-left">
                        {item.q}
                      </AccordionTrigger>
                      <AccordionContent className="text-muted-foreground">
                        {item.a}
                      </AccordionContent>
                    </AccordionItem>
                  ))}
                </Accordion>
              </CardContent>
            </Card>
          ))
        ) : (
          <Card className="py-8">
            <CardContent className="text-center">
              <Search className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <p className="text-muted-foreground">Aramanızla eşleşen sonuç bulunamadı</p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Contact Section */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Contact Form */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MessageCircle className="h-5 w-5" />
              Bize Ulaşın
            </CardTitle>
            <CardDescription>
              Sorularınız için mesaj gönderin, en kısa sürede yanıtlayalım.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label>Konu</Label>
              <Input
                placeholder="Mesajınızın konusu"
                value={contactForm.subject}
                onChange={(e) => setContactForm({ ...contactForm, subject: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>Mesajınız</Label>
              <Textarea
                placeholder="Detaylı açıklama yazın..."
                rows={4}
                value={contactForm.message}
                onChange={(e) => setContactForm({ ...contactForm, message: e.target.value })}
              />
            </div>
            <Button className="w-full" onClick={handleSendMessage} disabled={isSending}>
              {isSending ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Send className="mr-2 h-4 w-4" />
              )}
              Gönder
            </Button>
          </CardContent>
        </Card>

        {/* Contact Info */}
        <Card>
          <CardHeader>
            <CardTitle>İletişim Bilgileri</CardTitle>
            <CardDescription>
              Diğer iletişim kanallarımız
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-start gap-4">
              <div className="h-10 w-10 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                <Mail className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="font-medium">E-posta</p>
                <a href="mailto:destek@eticaret.com" className="text-sm text-muted-foreground hover:text-primary">
                  destek@eticaret.com
                </a>
              </div>
            </div>
            
            <div className="flex items-start gap-4">
              <div className="h-10 w-10 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                <Phone className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="font-medium">Telefon</p>
                <a href="tel:08505551234" className="text-sm text-muted-foreground hover:text-primary">
                  0850 555 12 34
                </a>
                <p className="text-xs text-muted-foreground mt-1">Hafta içi 09:00 - 18:00</p>
              </div>
            </div>

            <div className="flex items-start gap-4">
              <div className="h-10 w-10 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                <MessageCircle className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="font-medium">Canlı Destek</p>
                <p className="text-sm text-muted-foreground">7/24 yardım için canlı destek hattımızı kullanabilirsiniz.</p>
                <Button variant="outline" size="sm" className="mt-2">
                  Canlı Destek Başlat
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
