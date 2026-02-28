import React, { useState } from 'react';
import {
    useGetProductReviewsQuery,
    useGetReviewSummaryQuery,
    useCanUserReviewQuery
} from '@/features/products/productsApi';
import { StarRating } from './StarRating';
import { ReviewForm } from './ReviewForm';
import { useAppSelector } from '@/app/hooks';
import dayjs from 'dayjs';
import { Button } from '@/components/common/button';

interface ReviewListProps {
    productId: number;
}

export const ReviewList: React.FC<ReviewListProps> = ({ productId }) => {
    const [showForm, setShowForm] = useState(false);
    const { token } = useAppSelector((state) => state.auth);
    const isAuthenticated = !!token;

    // RTK Query ile yorumları ve özet bilgileri çekiyoruz
    const { data: reviews = [], isLoading: isReviewsLoading } = useGetProductReviewsQuery(productId);
    const { data: summary, isLoading: isSummaryLoading } = useGetReviewSummaryQuery(productId);

    // Satın alıp almadığının yetki kontrolü
    const { data: canReview = false, isLoading: isCanReviewLoading } = useCanUserReviewQuery(productId, {
        skip: !isAuthenticated
    });

    if (isReviewsLoading || isSummaryLoading || (isAuthenticated && isCanReviewLoading)) {
        return (
            <div className="flex justify-center items-center py-10">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            </div>
        );
    }

    // Kullanıcı giriş yapmışsa ve form açılmışsa gösterilecek
    const renderFormArea = () => {
        if (!isAuthenticated) {
            return (
                <div className="bg-muted p-4 rounded-lg text-center mt-6">
                    <p className="text-sm text-muted-foreground">
                        Ürünü değerlendirmek için lütfen giriş yapın.
                    </p>
                </div>
            );
        }

        if (!canReview) {
            return (
                <div className="bg-muted p-4 rounded-lg text-center mt-6">
                    <p className="text-sm text-muted-foreground">
                        Sadece bu ürünü satın alan ve teslim edilen müşterilerimiz değerlendirme yapabilir.
                    </p>
                </div>
            );
        }

        if (showForm) {
            return (
                <div className="mt-6 mb-8">
                    <ReviewForm
                        productId={productId}
                        onSuccess={() => setShowForm(false)}
                        onCancel={() => setShowForm(false)}
                    />
                </div>
            );
        }

        return (
            <div className="mt-4 sm:mt-0 flex flex-col sm:items-end">
                <Button onClick={() => setShowForm(true)}>
                    Değerlendirme Yaz
                </Button>
            </div>
        );
    };

    return (
        <div className="py-8 border-t border-gray-200 dark:border-gray-700 mt-10">
            <h2 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white mb-6">Müşteri Değerlendirmeleri</h2>

            {/* Özet Alanı */}
            <div className="sm:flex sm:items-start sm:justify-between grid grid-cols-1 gap-6">
                <div className="flex items-center gap-4">
                    <div className="text-center">
                        <p className="text-5xl font-extrabold text-gray-900 dark:text-white">
                            {summary?.averageRating ? summary.averageRating.toFixed(1) : '0.0'}
                        </p>
                        <div className="mt-2 text-sm text-gray-500 dark:text-gray-400">
                            {summary?.totalReviews || 0} Değerlendirme
                        </div>
                    </div>
                    <div className="flex flex-col border-l border-gray-200 dark:border-gray-700 pl-4 space-y-1">
                        <StarRating rating={Math.round(summary?.averageRating || 0)} readOnly size="md" />
                    </div>
                </div>

                {renderFormArea()}
            </div>

            {/* Yorum Listesi */}
            <div className="mt-10 space-y-8">
                {reviews.length === 0 ? (
                    <p className="text-gray-500 dark:text-gray-400 text-center py-6">
                        Bu ürün için henüz bir değerlendirme yapılmamış. İlk değerlendiren siz olun.
                    </p>
                ) : (
                    reviews.map((review) => (
                        <div key={review.id} className="border-b border-gray-200 dark:border-gray-700 pb-6">
                            <div className="flex items-center justify-between mb-2">
                                <div className="flex items-center gap-3">
                                    <div className="h-10 w-10 text-xl font-bold rounded-full bg-gray-100 flex items-center justify-center text-gray-600 dark:bg-gray-800 dark:text-gray-300">
                                        {review.userFullName.charAt(0).toUpperCase()}
                                    </div>
                                    <div>
                                        <p className="text-sm font-medium text-gray-900 dark:text-white">
                                            {review.userFullName}
                                        </p>
                                        <p className="text-xs text-gray-500 dark:text-gray-400">
                                            {dayjs(review.createdAt).format('DD.MM.YYYY')}
                                        </p>
                                    </div>
                                </div>
                                <StarRating rating={review.rating} readOnly size="sm" />
                            </div>
                            <div className="mt-4 text-sm text-gray-700 dark:text-gray-300 whitespace-pre-line">
                                {review.comment}
                            </div>
                        </div>
                    ))
                )}
            </div>
        </div>
    );
};
