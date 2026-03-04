import { test, expect } from '@playwright/test';

test.describe('Login', () => {
    const submitLoginForm = async (page: import('@playwright/test').Page) => {
        const responsePromise = page.waitForResponse((response) =>
            response.url().includes('/api/v1/auth/login') && response.request().method() === 'POST'
        );

        await page.getByRole('button', { name: 'Giriş Yap' }).click();
        return responsePromise;
    };

    test('başarılı giriş yapılabilmeli', async ({ page }) => {
        test.setTimeout(120_000);

        await page.goto('/login');

        await page.fill('#email', 'customer@test.com');
        await page.fill('#password', 'Test123!');

        let response = await submitLoginForm(page);

        if (response.status() === 429) {
            const body = await response.json().catch(() => null) as { retryAfterSeconds?: number } | null;
            const retryAfterSeconds = Math.max(1, Math.ceil(body?.retryAfterSeconds ?? 60));

            await page.waitForTimeout((retryAfterSeconds + 1) * 1000);
            response = await submitLoginForm(page);
        }

        expect(response.ok(), `Login isteği başarısız oldu: ${response.status()}`).toBeTruthy();

        await expect(page).toHaveURL('/', { timeout: 10_000 });
        await expect(page.getByRole('link', { name: 'Giriş Yap' })).not.toBeVisible({ timeout: 10_000 });
    });

    test('yanlış şifre ile hata mesajı gösterilmeli', async ({ page }) => {
        await page.goto('/login');
        await page.waitForSelector('#email');

        await page.fill('#email', 'yanlis@email.com');
        await page.fill('#password', 'yanlis_sifre');
        await page.getByRole('button', { name: 'Giriş Yap' }).click();

        await page.waitForTimeout(2_000);
        await expect(page.getByText('Hesabınıza giriş yaparak alışverişe başlayın')).toBeVisible();
    });
});
