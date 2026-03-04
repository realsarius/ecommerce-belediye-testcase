import { test, expect } from '@playwright/test';
import {
    acceptCheckoutLegalConsents,
    acceptCookieBannerIfPresent,
    addFirstProductToCart,
    clearCartIfNeeded,
    loginAsCustomer,
    loginViaApi,
    prepareCheckoutForNewCardPayment,
} from './helpers';

test.describe('Shipment And Return', () => {
    test.describe.configure({ retries: 0 });

    test('seller shipment ve customer return akışı uçtan uca ilerleyebilmeli', async ({ page }) => {
        test.setTimeout(90_000);

        await loginAsCustomer(page);
        await acceptCookieBannerIfPresent(page);
        const sellerAuth = await loginViaApi(page, 'testseller@test.com', 'Test123!');
        const adminAuth = await loginViaApi(page, 'testadmin@test.com', 'Test123!');

        const sellerProductsResponse = await page.request.get('http://localhost:5000/api/v1/seller/products', {
            headers: {
                Authorization: `Bearer ${sellerAuth.token}`,
            },
        });
        expect(sellerProductsResponse.ok()).toBeTruthy();
        const sellerProductsPayload = await sellerProductsResponse.json();
        const sellerProduct = sellerProductsPayload.data.items[0] as {
            id: number;
            stockQuantity: number;
        };
        const sellerProductId = sellerProduct.id;

        if (sellerProduct.stockQuantity < 1) {
            const stockPatchResponse = await page.request.patch(`http://localhost:5000/api/v1/seller/products/${sellerProductId}/stock`, {
                headers: {
                    Authorization: `Bearer ${sellerAuth.token}`,
                    'Content-Type': 'application/json',
                },
                data: {
                    delta: 5,
                    reason: 'E2E test stok hazırlığı',
                    notes: 'Shipment return e2e senaryosu için otomatik stok girişi',
                },
            });

            expect(stockPatchResponse.ok()).toBeTruthy();
        }

        await clearCartIfNeeded(page);
        await addFirstProductToCart(page, sellerProductId);

        await page.getByRole('button', { name: 'Siparişi Tamamla' }).click();
        await expect(page).toHaveURL('/checkout');

        await prepareCheckoutForNewCardPayment(page, '5406 6700 0000 0009');
        await acceptCheckoutLegalConsents(page);

        const createOrderResponsePromise = page.waitForResponse((response) =>
            response.url().includes('/api/v1/orders') &&
            response.request().method() === 'POST' &&
            response.ok()
        );
        const paymentResponsePromise = page.waitForResponse((response) =>
            response.url().includes('/api/v1/payments') &&
            response.request().method() === 'POST'
        );

        await page.getByRole('button', { name: 'Siparişi Tamamla' }).click();

        const createOrderResponse = await createOrderResponsePromise;
        const createOrderPayload = await createOrderResponse.json();
        const orderId = createOrderPayload.data.id as number;

        const paymentResponse = await paymentResponsePromise;
        const paymentPayload = await paymentResponse.json();
        expect(paymentPayload.data.status).toBe('Success');

        const customerToken = await page.evaluate(() => window.localStorage.getItem('token'));
        expect(customerToken).toBeTruthy();

        let orderReadyForShipment = false;
        for (let attempt = 0; attempt < 10; attempt++) {
            const orderResponse = await page.request.get(`http://localhost:5000/api/v1/orders/${orderId}`, {
                headers: {
                    Authorization: `Bearer ${customerToken}`,
                },
            });

            expect(orderResponse.ok()).toBeTruthy();
            const orderPayload = await orderResponse.json();
            const currentStatus = orderPayload.data.status as string;
            if (currentStatus === 'Paid' || currentStatus === 'Processing') {
                orderReadyForShipment = true;
                break;
            }

            await page.waitForTimeout(500);
        }

        expect(orderReadyForShipment).toBeTruthy();

        const shipResponse = await page.request.put(`http://localhost:5000/api/v1/seller/orders/${orderId}/ship`, {
            headers: {
                Authorization: `Bearer ${sellerAuth.token}`,
                'Content-Type': 'application/json',
            },
            data: JSON.stringify({
                cargoProvider: 'YurticiKargo',
                trackingCode: 'TRK123456',
                estimatedDeliveryDate: '2026-03-10',
            }),
        });
        if (!shipResponse.ok()) {
            throw new Error(`Ship request failed with ${shipResponse.status()}: ${await shipResponse.text()}`);
        }

        const deliverResponse = await page.request.patch(`http://localhost:5000/api/v1/admin/orders/${orderId}/status`, {
            headers: {
                Authorization: `Bearer ${adminAuth.token}`,
                'Content-Type': 'application/json',
            },
            data: {
                status: 'Delivered',
            },
        });
        expect(deliverResponse.ok()).toBeTruthy();

        await page.goto(`/orders/${orderId}`);
        await expect(page.getByText('Kargo Takibi')).toBeVisible({ timeout: 10_000 });
        await expect(page.getByText('Takip Kodu:')).toBeVisible();
        await expect(page.getByText('TRK123456')).toBeVisible();
        await expect(page.getByText('Teslim Edildi').first()).toBeVisible();

        await page.goto(`/returns?orderId=${orderId}`);
        await expect(page.getByRole('heading', { name: 'İade ve İptal Taleplerim' })).toBeVisible({ timeout: 10_000 });
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(500);

        await page.locator('#return-category').click({ force: true });
        await page.getByRole('option', { name: 'Fikrimi değiştirdim' }).click();
        await page.getByPlaceholder('İade talebinizin kısa nedenini yazın').fill('Numara beklentimi karsilamadi');
        await page.getByRole('button', { name: 'Talep Oluştur' }).click();

        await expect(page.getByText('Talebiniz oluşturuldu.')).toBeVisible({ timeout: 10_000 });

        await page.goto(`/orders/${orderId}`);
        await expect(page.getByText('Talep Tipi:')).toBeVisible({ timeout: 10_000 });
        await expect(page.getByText('Kategori:')).toBeVisible();
        await expect(page.getByText('Numara beklentimi karsilamadi')).toBeVisible();
    });
});
