import React, { useState } from 'react';
import { StarRating } from './StarRating';
import { useCreateReviewMutation } from '@/features/products/productsApi';
import { toast } from 'react-hot-toast';
import { Button } from '@/components/common/button';

interface ReviewFormProps {
    productId: number;
    onSuccess?: () => void;
    onCancel?: () => void;
}

interface ApiErrorResponse {
    data?: {
        message?: string;
    };
}

function isApiErrorResponse(error: unknown): error is ApiErrorResponse {
    return typeof error === 'object' && error !== null && 'data' in error;
}

export const ReviewForm: React.FC<ReviewFormProps> = ({ productId, onSuccess, onCancel }) => {
    const [rating, setRating] = useState(0);
    const [comment, setComment] = useState('');

    const [createReview, { isLoading }] = useCreateReviewMutation();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (rating === 0) {
            toast.error('Lütfen bir puan (yıldız) seçin.');
            return;
        }
        if (comment.trim().length < 10) {
            toast.error('Yorumunuz en az 10 karakter olmalıdır.');
            return;
        }

        try {
            await createReview({
                productId,
                data: { rating, comment },
            }).unwrap();

            toast.success('Değerlendirmeniz başarıyla eklendi!');
            setRating(0);
            setComment('');
            if (onSuccess) onSuccess();
        } catch (error: unknown) {
            let msg = 'Değerlendirme eklenirken bir hata oluştu.';
            if (isApiErrorResponse(error) && error.data?.message) {
                msg = error.data.message;
            }
            toast.error(msg);
        }
    };

    return (
        <div className="bg-card text-card-foreground p-6 rounded-lg shadow-sm border">
            <h3 className="text-lg font-semibold mb-4">
                Değerlendirme Yazın
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                    <label className="block text-sm font-medium mb-2">
                        Puanınız
                    </label>
                    <StarRating rating={rating} readOnly={false} onChange={setRating} size="lg" />
                </div>

                <div>
                    <label htmlFor="comment" className="block text-sm font-medium mb-2">
                        Yorumunuz
                    </label>
                    <textarea
                        id="comment"
                        rows={4}
                        className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                        placeholder="Ürün hakkındaki düşüncelerinizi paylaşın..."
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                        disabled={isLoading}
                    />
                    <p className="mt-1 text-sm text-muted-foreground">En az 10 karakter giriniz.</p>
                </div>

                <div className="flex justify-end gap-3">
                    {onCancel && (
                        <Button
                            type="button"
                            variant="outline"
                            onClick={onCancel}
                            disabled={isLoading}
                        >
                            İptal
                        </Button>
                    )}
                    <Button
                        type="submit"
                        disabled={isLoading}
                    >
                        {isLoading ? 'Gönderiliyor...' : 'Gönder'}
                    </Button>
                </div>
            </form>
        </div>
    );
};
