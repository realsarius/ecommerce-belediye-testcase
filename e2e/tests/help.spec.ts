import { test, expect } from '@playwright/test';

test.describe('Help', () => {
    test('yardım sayfası yüklenmeli', async ({ page }) => {
        await page.goto('/help');

        await expect(page.getByText('Yardım Merkezi')).toBeVisible();
        await expect(page.getByText('Size nasıl yardımcı olabiliriz?')).toBeVisible();
    });

    test('SSS kategorileri görünmeli', async ({ page }) => {
        await page.goto('/help');

        await expect(page.getByText('Sıkça Sorulan Sorular')).toBeVisible();
        await expect(page.getByText('Sipariş', { exact: true })).toBeVisible();
        await expect(page.getByText('Ödeme', { exact: true })).toBeVisible();
        await expect(page.getByText('Kargo & Teslimat')).toBeVisible();
        await expect(page.getByText('İade & Değişim')).toBeVisible();
    });

    test('SSS arama çalışmalı', async ({ page }) => {
        await page.goto('/help');

        await page.fill('input[placeholder="Soru veya konu ara..."]', 'kargo');

        await expect(page.getByText('Kargo & Teslimat')).toBeVisible();
    });

    test('iletişim bilgileri görünmeli', async ({ page }) => {
        await page.goto('/help');

        await expect(page.getByText('Bize Ulaşın')).toBeVisible();
        await expect(page.getByText('İletişim Bilgileri')).toBeVisible();
        await expect(page.getByText('destek@eticaret.com')).toBeVisible();
    });
});
