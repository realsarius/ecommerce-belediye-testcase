import { test, expect } from '@playwright/test';

test.describe('Help', () => {
    test('yardım sayfası yüklenmeli', async ({ page }) => {
        await page.goto('/help');

        await expect(page.getByRole('heading', { name: 'Yardım Merkezi' })).toBeVisible();
        await expect(page.getByText('En sık ihtiyaç duyulan destek konularını kategoriler halinde bir araya getirdik.')).toBeVisible();
    });

    test('yardım kartları görünmeli', async ({ page }) => {
        await page.goto('/help');

        await expect(page.getByText('Siparişlerim')).toBeVisible();
        await expect(page.getByText('Ödeme', { exact: true })).toBeVisible();
        await expect(page.getByText('İade ve İptal')).toBeVisible();
        await expect(page.getByText('Kargo', { exact: true })).toBeVisible();
        await expect(page.getByRole('link', { name: 'Canlı Destek' })).toBeVisible();
    });

    test('yardım kartlarından ilgili içeriğe gidilebilmeli', async ({ page }) => {
        await page.goto('/help');

        await page.getByRole('link', { name: 'İlgili içeriğe git' }).nth(4).click();
        await expect(page).toHaveURL('/shipping');
    });

    test('ek destek kanalları bölümü görünmeli', async ({ page }) => {
        await page.goto('/help');

        await expect(page.getByRole('heading', { name: 'Ek destek kanalları' })).toBeVisible();
        await expect(page.getByText('Genel sorular için SSS ve yardım merkezi içeriklerini')).toBeVisible();
    });
});
