import { test, expect } from '@playwright/test';

test.describe('Search Suggestions', () => {
    test('2+ karakter yazınca öneri dropdown açılmalı', async ({ page }) => {
        await page.goto('/');

        const searchInput = page.getByPlaceholder('Ürün ara...').first();
        await searchInput.fill('ad');

        // Suggestion dropdown görünmeli (Aranıyor... veya ürün sonuçları)
        const dropdown = page.locator('.absolute').filter({ hasText: /(Aranıyor|TL|Daha fazla|Tüm sonuçlar|bulunamadı)/ });
        await expect(dropdown.first()).toBeVisible({ timeout: 5_000 });
    });

    test('öneri sonucuna tıklayınca ürün detayına gitmeli', async ({ page }) => {
        await page.goto('/');

        const searchInput = page.getByPlaceholder('Ürün ara...').first();
        await searchInput.fill('ad');

        // Suggestion linki görünene kadar bekle
        const suggestionLink = page.locator('a[href^="/products/"]').first();
        await expect(suggestionLink).toBeVisible({ timeout: 5_000 });
        await suggestionLink.click();

        // Ürün detay sayfasında olmalı
        await expect(page).toHaveURL(/\/products\/\d+/);
    });
});
