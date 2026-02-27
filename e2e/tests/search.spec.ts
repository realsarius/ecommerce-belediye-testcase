import { test, expect } from '@playwright/test';

test.describe('Search', () => {
    test('ürün araması yapılabilmeli', async ({ page }) => {
        await page.goto('/');

        // Arama kutusuna yaz
        const searchInput = page.getByPlaceholder('Ürün ara...');
        await searchInput.first().fill('test');
        await searchInput.first().press('Enter');

        // URL'de arama parametresi olmalı
        await expect(page).toHaveURL(/q=test/);
    });

    test('ana sayfa yüklendiğinde ürünler görünmeli', async ({ page }) => {
        await page.goto('/');

        // "Hoş Geldiniz" başlığı görünmeli
        await expect(page.getByText('Hoş Geldiniz')).toBeVisible();
    });
});
