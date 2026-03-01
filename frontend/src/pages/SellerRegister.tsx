import { Link } from 'react-router-dom';
import { ArrowRight, Store } from 'lucide-react';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { StaticPageLayout } from '@/components/common/StaticPageLayout';

export default function SellerRegister() {
  return (
    <StaticPageLayout
      eyebrow="Satıcı"
      title="Satıcı Başvurusu"
      description="Satıcı başvuru deneyimi ayrı bir onboarding akışıyla genişletilecek. İlk aşamada temel bilgi ve rehber içeriklere buradan erişebilirsiniz."
      lastUpdated="Mart 2026"
    >
      <Card className="border-white/10 bg-white/[0.03] py-0">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Store className="h-5 w-5 text-rose-300" />
            Başvuru akışı yakında
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p>
            Satıcı onboarding formu ve değerlendirme akışı ayrı bir fazda açılacak. Bu sırada satıcı rehberini ve
            komisyon yapısını inceleyerek hazırlık yapabilirsiniz.
          </p>
          <div className="flex flex-wrap gap-3">
            <Button asChild>
              <Link to="/seller/guide">
                Satıcı rehberine git
                <ArrowRight className="h-4 w-4" />
              </Link>
            </Button>
            <Button asChild variant="outline">
              <Link to="/seller/pricing">Komisyon oranlarını incele</Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </StaticPageLayout>
  );
}
