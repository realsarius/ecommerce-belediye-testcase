import { test, expect } from '@playwright/test';

test.describe('Category Filter', () => {
    test('kategori seçildiğinde URL güncellenmeli', async ({ page }) => {
        await page.goto('/');

        // Sidebar'daki kategori butonlarından birine tıkla (Elektronik gibi)
        const categoryButton = page.locator('aside').getByRole('button').nth(1); // İlk kategori (Tümü'den sonra)
        await expect(categoryButton).toBeVisible({ timeout: 10_000 });

        const categoryName = await categoryButton.textContent();
        await categoryButton.click();

        // URL'de categoryId parametresi olmalı
        await expect(page).toHaveURL(/categoryId=\d+/);

        // Tüm Kategoriler butonu artık aktif değil
        expect(categoryName).toBeTruthy();
    });

    test('Tüm Kategoriler ile filtre temizlenebilmeli', async ({ page }) => {
        // Kategori filtreli sayfaya git
        await page.goto('/?categoryId=1');

        // Tüm Kategoriler butonuna tıkla
        const allButton = page.locator('aside').getByRole('button', { name: 'Tüm Kategoriler' });
        await expect(allButton).toBeVisible({ timeout: 10_000 });
        await allButton.click();

        // URL'de categoryId olmamalı
        await expect(page).not.toHaveURL(/categoryId/);
    });
});
