import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
    test('header logo ve menü elemanları görünmeli', async ({ page }) => {
        await page.goto('/');

        // Logo (header içindeki ilk match)
        await expect(page.locator('header').getByText('E-Ticaret')).toBeVisible();

        // Nav linkleri
        await expect(page.getByRole('link', { name: 'Ürünler' })).toBeVisible();
    });

    test('giriş yapmadan Giriş Yap ve Kayıt Ol butonları görünmeli', async ({ page }) => {
        await page.goto('/');

        await expect(page.getByRole('link', { name: 'Giriş Yap' })).toBeVisible();
        await expect(page.getByRole('link', { name: 'Kayıt Ol' })).toBeVisible();
    });

    test('Giriş Yap linkine tıklayınca login sayfasına gitmeli', async ({ page }) => {
        await page.goto('/');

        await page.getByRole('link', { name: 'Giriş Yap' }).click();
        await expect(page).toHaveURL('/login');
    });

    test('Kayıt Ol linkine tıklayınca register sayfasına gitmeli', async ({ page }) => {
        await page.goto('/');

        await page.getByRole('link', { name: 'Kayıt Ol' }).click();
        await expect(page).toHaveURL('/register');
    });

    test('ana sayfa hero section görünmeli', async ({ page }) => {
        await page.goto('/');

        await expect(page.getByText('Hoş Geldiniz')).toBeVisible();
        await expect(page.getByText('En kaliteli ürünleri keşfedin')).toBeVisible();
    });
});
