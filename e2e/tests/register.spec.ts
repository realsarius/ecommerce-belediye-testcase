import { test, expect } from '@playwright/test';

test.describe('Register', () => {
    test('kayıt formu doğru şekilde yüklenmeli', async ({ page }) => {
        await page.goto('/register');

        // Form elemanları görünmeli
        await expect(page.locator('#firstName')).toBeVisible();
        await expect(page.locator('#lastName')).toBeVisible();
        await expect(page.locator('#email')).toBeVisible();
        await expect(page.locator('#password')).toBeVisible();
        await expect(page.locator('#confirmPassword')).toBeVisible();
        await expect(page.getByRole('button', { name: 'Kayıt Ol' })).toBeVisible();
    });

    test('eksik alanlarla kayıt olunamamalı', async ({ page }) => {
        await page.goto('/register');

        // Sadece email doldurup submit
        await page.fill('#email', 'test@test.com');
        await page.getByRole('button', { name: 'Kayıt Ol' }).click();

        // Validation hataları görünmeli
        await expect(page.getByText('İsim en az 2 karakter olmalıdır')).toBeVisible();
        await expect(page.getByText('Soyisim en az 2 karakter olmalıdır')).toBeVisible();
    });

    test('şifre eşleşmezse hata gösterilmeli', async ({ page }) => {
        await page.goto('/register');

        await page.fill('#firstName', 'Test');
        await page.fill('#lastName', 'User');
        await page.fill('#email', 'test@test.com');
        await page.fill('#password', 'Test123!');
        await page.fill('#confirmPassword', 'Farkli456!');
        await page.getByRole('button', { name: 'Kayıt Ol' }).click();

        await expect(page.getByText('Şifreler eşleşmiyor')).toBeVisible();
    });

    test('giriş yap linkine tıklanabilmeli', async ({ page }) => {
        await page.goto('/register');

        await page.click('a[href="/login"]');
        await expect(page).toHaveURL('/login');
    });
});
