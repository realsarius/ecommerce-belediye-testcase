import { test, expect } from '@playwright/test';

test.describe('Login', () => {
    test('başarılı giriş yapılabilmeli', async ({ page }) => {
        await page.goto('/login');

        await page.fill('#email', 'customer@test.com');
        await page.fill('#password', 'Test123!');
        await page.getByRole('button', { name: 'Giriş Yap' }).click();

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
