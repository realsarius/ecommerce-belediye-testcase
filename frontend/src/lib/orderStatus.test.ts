import { describe, expect, it } from 'vitest';
import { getOrderStatusLabel, getOrderStatusTone } from './orderStatus';

describe('orderStatus helper', () => {
  it('müşteri görünümü için uzun durum etiketini döner', () => {
    expect(getOrderStatusLabel('PendingPayment')).toBe('Ödeme Bekleniyor');
    expect(getOrderStatusLabel('Cancelled')).toBe('İptal Edildi');
  });

  it('dashboard görünümü için kısa durum etiketini döner', () => {
    expect(getOrderStatusLabel('PendingPayment', { compact: true })).toBe('Beklemede');
    expect(getOrderStatusLabel('Shipped', { compact: true })).toBe('Kargoda');
  });

  it('durum tonlarını tutarlı döner', () => {
    expect(getOrderStatusTone('Delivered')).toBe('success');
    expect(getOrderStatusTone('PendingPayment')).toBe('warning');
    expect(getOrderStatusTone('Refunded')).toBe('danger');
    expect(getOrderStatusTone('Paid')).toBe('info');
  });
});
