import { test, expect } from '@playwright/test';

test.describe('Product', () => {
    test('ürün kartları ana sayfada listelenmeli', async ({ page }) => {
        await page.goto('/');

        const addToCartButtons = page.getByRole('button', { name: 'Sepete Ekle' });
        await expect(addToCartButtons.first()).toBeVisible({ timeout: 10_000 });
    });

    test('ürün kartına tıklayınca detay sayfasına gitmeli', async ({ page }) => {
        await page.goto('/');

        const productLink = page.locator('a[href^="/products/"]').first();
        await expect(productLink).toBeVisible({ timeout: 10_000 });
        await productLink.click();

        await expect(page).toHaveURL(/\/products\/\d+/);
        await expect(page.getByText('Ürünlere Dön')).toBeVisible();
    });

    test('olmayan ürün için hata mesajı gösterilmeli', async ({ page }) => {
        await page.goto('/products/999999');

        await expect(page.getByText('Ürün Bulunamadı')).toBeVisible({ timeout: 10_000 });
    });

    test('sayfalama çalışmalı', async ({ page }) => {
        await page.goto('/');

        const pageInfo = page.getByText(/Sayfa \d+ \/ \d+/);
        await expect(pageInfo).toBeVisible({ timeout: 10_000 });
    });
});
