import { test, expect } from '@playwright/test';

test.describe('Auth Recovery', () => {
    test('verify-email başarılı olduğunda doğrulandı ekranı görünmeli', async ({ page }) => {
        await page.route('**/api/v1/auth/verify-email', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    data: {
                        success: true,
                        message: 'E-posta adresiniz doğrulandı',
                        token: 'verify-access-token',
                        refreshToken: 'verify-refresh-token',
                        user: {
                            id: 101,
                            email: 'verified@example.com',
                            firstName: 'Verify',
                            lastName: 'User',
                            role: 'Customer',
                            isEmailVerified: true,
                        },
                    },
                }),
            });
        });

        await page.goto('/verify-email?token=test-verify-token');

        await expect(page.getByText('E-posta Doğrulandı')).toBeVisible({ timeout: 10_000 });
        await expect(page.getByRole('link', { name: 'Alışverişe Başla' })).toBeVisible();

        await expect
            .poll(async () => {
                return page.evaluate(() => Boolean(window.localStorage.getItem('token')));
            })
            .toBe(true);
    });

    test('verify-email hatasında geçersiz link ekranı görünmeli', async ({ page }) => {
        await page.route('**/api/v1/auth/verify-email', async (route) => {
            await route.fulfill({
                status: 400,
                contentType: 'application/json',
                body: JSON.stringify({
                    message: 'Doğrulama linkinin süresi dolmuş',
                    errorCode: 'EXPIRED_TOKEN',
                }),
            });
        });

        await page.goto('/verify-email?token=expired-token');

        await expect(page.getByText('Geçersiz veya Süresi Dolmuş Link')).toBeVisible({ timeout: 10_000 });
        await expect(page.getByText('Doğrulama linkinin süresi dolmuş')).toBeVisible();
    });

    test('forgot-password formu başarılı istek sonrası bilgilendirme göstermeli', async ({ page }) => {
        await page.route('**/api/v1/auth/forgot-password', async (route) => {
            const payload = route.request().postDataJSON() as { email?: string };
            expect(payload.email).toBe('customer@test.com');

            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    success: true,
                    message: 'Şifre sıfırlama linki e-posta adresinize gönderildi',
                }),
            });
        });

        await page.goto('/forgot-password');
        await page.getByLabel('E-posta Adresi').fill('customer@test.com');
        await page.getByRole('button', { name: 'Sıfırlama Linki Gönder' }).click();

        await expect(
            page.getByText('E-posta adresinize sıfırlama linki gönderildi. Gelen kutunuzu ve spam klasörünü kontrol edin.')
        ).toBeVisible({ timeout: 10_000 });
    });

    test('reset-password başarılı istek sonrası başarı ekranına geçmeli', async ({ page }) => {
        await page.route('**/api/v1/auth/reset-password', async (route) => {
            const payload = route.request().postDataJSON() as { token?: string; newPassword?: string; confirmPassword?: string };
            expect(payload.token).toBe('reset-token');
            expect(payload.newPassword).toBe('StrongPassword1');
            expect(payload.confirmPassword).toBe('StrongPassword1');

            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    success: true,
                    message: 'Şifreniz başarıyla güncellendi',
                }),
            });
        });

        await page.goto('/reset-password?token=reset-token');

        await page.getByLabel('Yeni Şifre').fill('StrongPassword1');
        await page.getByLabel('Şifre Tekrar').fill('StrongPassword1');
        await page.getByRole('button', { name: 'Şifreyi Güncelle' }).click();

        await expect(page.getByText('Şifreniz Güncellendi')).toBeVisible({ timeout: 10_000 });
        await expect(page.getByRole('link', { name: 'Girişe Git' })).toBeVisible();
    });
});
