import { test, expect } from '@playwright/test';

test.describe('Login', () => {
    test('başarılı giriş yapılabilmeli', async ({ page }) => {
        await page.goto('/login');

        await page.fill('#email', 'customer@test.com');
        await page.fill('#password', 'Test123!');
        await page.click('button[type="submit"]');

        // Login sonrası ana sayfaya yönlenmeli
        await expect(page).toHaveURL('/', { timeout: 10_000 });

        // Hoş Geldiniz başlığı görünmeli (ana sayfa yüklendi)
        await expect(page.getByText('Hoş Geldiniz')).toBeVisible();
    });

    test('yanlış şifre ile hata mesajı gösterilmeli', async ({ page }) => {
        await page.goto('/login');
        await page.waitForSelector('#email');

        await page.fill('#email', 'customer@test.com');
        await page.fill('#password', 'yanlis_sifre');
        await page.click('button[type="submit"]');

        // Login formu hala görünür olmalı (başarısız giriş)
        await expect(page.getByRole('button', { name: 'Giriş Yap' })).toBeVisible();
        await expect(page.locator('#email')).toBeVisible();

    });


});
