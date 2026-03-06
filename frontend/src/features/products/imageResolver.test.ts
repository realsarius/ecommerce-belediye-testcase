import { describe, expect, it } from 'vitest';
import { getProductImageUrls } from './imageResolver';

describe('getProductImageUrls', () => {
  it('primaryImageUrl ve images alanlarini tekrarsiz sekilde birlestirir', () => {
    const result = getProductImageUrls({
      primaryImageUrl: 'https://img.example.com/p-primary.webp',
      images: [
        { imageUrl: 'https://img.example.com/p-primary.webp', sortOrder: 3, isPrimary: false },
        { imageUrl: 'https://img.example.com/p-2.webp', sortOrder: 2, isPrimary: false },
        { imageUrl: 'https://img.example.com/p-1.webp', sortOrder: 1, isPrimary: true },
      ],
    });

    expect(result).toEqual([
      'https://img.example.com/p-primary.webp',
      'https://img.example.com/p-1.webp',
      'https://img.example.com/p-2.webp',
    ]);
  });

  it('primaryImageUrl yoksa images listesinden primary ve sortOrder sirasiyla dondurur', () => {
    const result = getProductImageUrls({
      primaryImageUrl: null,
      images: [
        { imageUrl: 'https://img.example.com/p-3.webp', sortOrder: 2, isPrimary: false },
        { imageUrl: 'https://img.example.com/p-1.webp', sortOrder: 5, isPrimary: true },
        { imageUrl: 'https://img.example.com/p-2.webp', sortOrder: 1, isPrimary: false },
      ],
    });

    expect(result).toEqual([
      'https://img.example.com/p-1.webp',
      'https://img.example.com/p-2.webp',
      'https://img.example.com/p-3.webp',
    ]);
  });
});
