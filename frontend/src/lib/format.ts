import dayjs from 'dayjs';
import 'dayjs/locale/tr';

dayjs.locale('tr');

export function formatNumber(value: number, options?: Intl.NumberFormatOptions) {
  return new Intl.NumberFormat('tr-TR', options).format(value);
}

export function formatCurrency(
  value: number,
  currency = 'TRY',
  maximumFractionDigits = 0,
) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits,
  }).format(value);
}

export function formatPercent(value: number, maximumFractionDigits = 1) {
  return `%${formatNumber(value, { maximumFractionDigits })}`;
}

export function formatCompactNumber(value?: number) {
  if (typeof value !== 'number') {
    return '';
  }

  if (Math.abs(value) < 1000) {
    return formatNumber(value);
  }

  return `${formatNumber(Math.round(value / 1000))}k`;
}

export function formatDate(
  value?: string | Date | null,
  pattern = 'DD MMMM YYYY',
) {
  if (!value) {
    return '-';
  }

  return dayjs(value).format(pattern);
}

export function formatDateTime(
  value?: string | Date | null,
  pattern = 'DD MMMM YYYY, HH:mm',
) {
  if (!value) {
    return '-';
  }

  return dayjs(value).format(pattern);
}

export function formatShortDate(value?: string | Date | null) {
  if (!value) {
    return '-';
  }

  return dayjs(value).format('DD MMM');
}

export function formatDateInput(value: Date) {
  return dayjs(value).format('YYYY-MM-DD');
}

export function formatDateTimeLocal(value?: string | Date | null) {
  if (!value) {
    return '';
  }

  return dayjs(value).format('YYYY-MM-DDTHH:mm');
}
