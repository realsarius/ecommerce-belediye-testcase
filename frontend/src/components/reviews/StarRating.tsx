import React from 'react';
import { StarIcon as StarSolid } from '@heroicons/react/24/solid';
import { StarIcon as StarOutline } from '@heroicons/react/24/outline';

interface StarRatingProps {
    rating: number;
    readOnly?: boolean;
    onChange?: (rating: number) => void;
    size?: 'sm' | 'md' | 'lg';
}

const sizeClasses = {
    sm: 'h-4 w-4',
    md: 'h-6 w-6',
    lg: 'h-8 w-8',
};

export const StarRating: React.FC<StarRatingProps> = ({
    rating,
    readOnly = true,
    onChange,
    size = 'md',
}) => {
    const stars = Array.from({ length: 5 }, (_, index) => index + 1);
    const iconClass = sizeClasses[size];

    return (
        <div className="flex items-center space-x-1">
            {stars.map((star) => (
                <button
                    key={star}
                    type="button"
                    disabled={readOnly}
                    onClick={() => onChange && onChange(star)}
                    className={`${readOnly ? 'cursor-default' : 'cursor-pointer hover:scale-110 transition-transform'} 
            focus:outline-none`}
                    aria-label={`${star} Yıldız`}
                >
                    {star <= rating ? (
                        <StarSolid className={`${iconClass} text-yellow-400`} />
                    ) : (
                        <StarOutline className={`${iconClass} text-gray-300 dark:text-gray-600`} />
                    )}
                </button>
            ))}
        </div>
    );
};
