import { test, expect } from '@playwright/test';
import {
    acceptCookieBannerIfPresent,
    acceptCheckoutLegalConsents,
    addFirstProductToCart,
    clearCartIfNeeded,
    fillRetryPaymentExpiry,
    loginAsCustomer,
    prepareCheckoutForNewCardPayment,
} from './helpers';

test.describe('Checkout Payment', () => {
    test('checkout happy-path ile sipariş oluşturup order detail gösterebilmeli', async ({ page }) => {
        await loginAsCustomer(page);
        await acceptCookieBannerIfPresent(page);
        await clearCartIfNeeded(page);
        await addFirstProductToCart(page);

        await page.getByRole('button', { name: 'Siparişi Tamamla' }).click();
        await expect(page).toHaveURL('/checkout');

        await prepareCheckoutForNewCardPayment(page, '5406 6700 0000 0009');
        await acceptCheckoutLegalConsents(page);

        const paymentResponsePromise = page.waitForResponse((response) =>
            response.url().includes('/api/v1/payments') &&
            response.request().method() === 'POST'
        );

        const submitOrderButton = page.getByRole('button', { name: 'Siparişi Tamamla' });
        await expect(submitOrderButton).toBeEnabled();
        await submitOrderButton.click();

        const paymentResponse = await paymentResponsePromise;
        const paymentPayload = await paymentResponse.json();
        expect(paymentPayload.data.status).toBe('Success');

        await expect(page).toHaveURL(/\/orders\/\d+/, { timeout: 20_000 });
        await expect(page.getByText(/Sipariş #\d+/)).toBeVisible({ timeout: 10_000 });
        await expect(page.getByText('Ödeme Bilgisi')).toBeVisible();
        await expect(page.getByText(/^Ödeme Alındı$/)).toBeVisible();
        await expect(page.getByText('Teslimat Adresi')).toBeVisible();
    });

    test('payment fail sonrası pending order üzerinden tekrar ödeme yapabilmeli', async ({ page }) => {
        await loginAsCustomer(page);
        await acceptCookieBannerIfPresent(page);
        await clearCartIfNeeded(page);
        await addFirstProductToCart(page);

        await page.getByRole('button', { name: 'Siparişi Tamamla' }).click();
        await expect(page).toHaveURL('/checkout');

        await prepareCheckoutForNewCardPayment(page, '4111 1111 1111 1129');
        await acceptCheckoutLegalConsents(page);

        const createOrderResponsePromise = page.waitForResponse((response) =>
            response.url().includes('/api/v1/orders') &&
            response.request().method() === 'POST' &&
            response.ok()
        );
        const initialPaymentResponsePromise = page.waitForResponse((response) =>
            response.url().includes('/api/v1/payments') &&
            response.request().method() === 'POST'
        );

        await page.getByRole('button', { name: 'Siparişi Tamamla' }).click();

        const createOrderResponse = await createOrderResponsePromise;
        const createOrderPayload = await createOrderResponse.json();
        const orderId = createOrderPayload.data.id as number;
        const initialPaymentResponse = await initialPaymentResponsePromise;
        const initialPaymentPayload = await initialPaymentResponse.json();
        expect(initialPaymentPayload.data.status).toBe('Failed');

        await expect(page).toHaveURL('/cart', { timeout: 20_000 });

        await page.goto(`/orders/${orderId}`);
        await expect(page.getByText(new RegExp(`Sipariş #${orderId}`))).toBeVisible({ timeout: 10_000 });
        await expect(page.getByRole('button', { name: 'Tekrar Öde' })).toBeVisible();

        await page.getByRole('button', { name: 'Tekrar Öde' }).click();
        const retryDialog = page.getByRole('dialog', { name: 'Tekrar Ödeme Yap' });
        await expect(retryDialog).toBeVisible({ timeout: 10_000 });

        await retryDialog.getByPlaceholder('AD SOYAD').fill('TEST CUSTOMER');
        await retryDialog.getByPlaceholder('4111 1111 1111 1111').fill('5406 6700 0000 0009');
        await fillRetryPaymentExpiry(page, new Date().getFullYear() + 1);
        await retryDialog.getByPlaceholder('123').fill('123');

        const retryPaymentResponsePromise = page.waitForResponse((response) =>
            response.url().includes('/api/v1/payments') &&
            response.request().method() === 'POST'
        );
        await retryDialog.getByRole('button', { name: 'Ödemeyi Tamamla' }).click();

        const retryPaymentResponse = await retryPaymentResponsePromise;
        const retryPaymentPayload = await retryPaymentResponse.json();
        expect(retryPaymentPayload.data.status).toBe('Success');

        await expect(retryDialog).toBeHidden({ timeout: 20_000 });
        await page.reload();
        await expect(page.getByText(/^Ödeme Alındı$/)).toBeVisible({ timeout: 20_000 });
    });
});
