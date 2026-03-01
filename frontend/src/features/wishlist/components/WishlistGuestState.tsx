import { Heart, Trash2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent } from '@/components/common/card';

interface WishlistGuestStateProps {
    pendingCount: number;
    onClearPending: () => void;
}

export function WishlistGuestState({ pendingCount, onClearPending }: WishlistGuestStateProps) {
    const hasPendingWishlist = pendingCount > 0;

    return (
        <div className="container mx-auto px-4 py-16 text-center">
            <Heart className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
            <h2 className="text-2xl font-semibold mb-2">
                {hasPendingWishlist ? 'Bekleyen Favorileriniz Hazır' : 'Giriş Yapmanız Gerekiyor'}
            </h2>
            <p className="text-muted-foreground mb-6 max-w-xl mx-auto">
                {hasPendingWishlist
                    ? `${pendingCount} ürün favorilerinize eklenmek için bekliyor. Giriş yaptığınızda bu ürünleri hesabınızla otomatik olarak senkronize edeceğiz.`
                    : 'Favorilerinizi hesabınızla senkronize etmek ve tüm cihazlarınızda görmek için lütfen giriş yapın.'}
            </p>
            <div className="max-w-md mx-auto mb-6">
                <Card>
                    <CardContent className="p-6 space-y-3 text-left">
                        <div className="flex items-center justify-between gap-4">
                            <span className="text-sm text-muted-foreground">Bekleyen favori sayısı</span>
                            <Badge variant={hasPendingWishlist ? 'default' : 'secondary'}>
                                {pendingCount}
                            </Badge>
                        </div>
                        <p className="text-sm text-muted-foreground">
                            {hasPendingWishlist
                                ? 'Giriş yaptığınız anda bu ürünler hesabınızdaki favori listenize aktarılacak.'
                                : 'Ürün sayfalarındaki kalp butonu ile favori ürünleri burada biriktirebilirsiniz.'}
                        </p>
                    </CardContent>
                </Card>
            </div>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
                <Button asChild>
                    <Link to="/login">Giriş Yap</Link>
                </Button>
                <Button asChild variant="outline">
                    <Link to="/">Ürünlere Göz At</Link>
                </Button>
                {hasPendingWishlist && (
                    <Button variant="ghost" onClick={onClearPending}>
                        <Trash2 className="h-4 w-4 mr-2" />
                        Bekleyenleri Temizle
                    </Button>
                )}
            </div>
        </div>
    );
}
