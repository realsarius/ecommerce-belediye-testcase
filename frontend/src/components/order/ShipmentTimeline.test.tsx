import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { Order } from '@/features/orders/types';
import { ShipmentTimeline } from './ShipmentTimeline';

function createOrder(overrides: Partial<Order> = {}): Order {
  return {
    id: 1,
    userId: 10,
    status: 'Shipped',
    totalAmount: 250,
    items: [
      {
        id: 101,
        productId: 501,
        productName: 'Test Ürünü',
        quantity: 1,
        priceSnapshot: 250,
        lineTotal: 250,
      },
    ],
    shippingAddress: 'Test Mahallesi No: 10 Kadıköy / İstanbul',
    createdAt: '2026-03-03T09:00:00.000Z',
    updatedAt: '2026-03-03T09:30:00.000Z',
    ...overrides,
  };
}

describe('ShipmentTimeline', () => {
  it('aktif shipment adımını ve takip bağlantısını gösterir', () => {
    render(
      <ShipmentTimeline
        order={createOrder({
          cargoCompany: 'Yurtiçi Kargo',
          trackingCode: 'TRK-123456',
          shipmentStatus: 'OutForDelivery',
          estimatedDeliveryDate: '2026-03-05T00:00:00.000Z',
        })}
      />,
    );

    expect(screen.getByText('Dağıtımda')).toBeInTheDocument();
    expect(screen.getByText('Bu adım aktif')).toBeInTheDocument();
    expect(screen.getByText('Tahmini Teslimat:')).toBeInTheDocument();

    const trackingLink = screen.getByRole('link', { name: 'Takip Et' });
    expect(trackingLink).toHaveAttribute('href', 'https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code=TRK-123456');
  });

  it('henüz kargo aşamasına geçmeyen sipariş için bilgilendirme gösterir', () => {
    render(
      <ShipmentTimeline
        order={createOrder({
          status: 'Paid',
          shipmentStatus: 'Pending',
          cargoCompany: undefined,
          trackingCode: undefined,
        })}
      />,
    );

    expect(
      screen.getByText('Sipariş henüz kargo aşamasına geçmedi. Hazırlık tamamlandığında takip bilgileri burada görünecek.'),
    ).toBeInTheDocument();
  });
});
