export type CardBrand = 'Unknown' | 'Visa' | 'Mastercard' | 'Troy' | 'Amex';

export function detectCardBrand(cardNumber?: string | null): CardBrand {
  const digits = (cardNumber ?? '').replace(/\D/g, '');

  if (!digits) {
    return 'Unknown';
  }

  if (/^3[47]/.test(digits)) {
    return 'Amex';
  }

  if (/^(9792|65|2205)/.test(digits)) {
    return 'Troy';
  }

  if (/^(5[1-5]|2(2[2-9]|[3-6]|7[01]|720))/.test(digits)) {
    return 'Mastercard';
  }

  if (/^4/.test(digits)) {
    return 'Visa';
  }

  return 'Unknown';
}

export function getCardBrandLabel(brand: CardBrand) {
  switch (brand) {
    case 'Visa':
      return 'Visa';
    case 'Mastercard':
      return 'Mastercard';
    case 'Troy':
      return 'Troy';
    case 'Amex':
      return 'American Express';
    default:
      return 'Kart';
  }
}
