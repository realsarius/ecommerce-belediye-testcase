import { test, expect, type Page } from '@playwright/test';

/** Yardımcı: customer olarak login yap ve auth state'in UI'a yansımasını bekle */
export async function loginAsCustomer(page: Page) {
    await page.goto('/login');

    await page.fill('#email', 'customer@test.com');
    await page.fill('#password', 'Test123!');

    // Login formundaki "Giriş Yap" butonunu tıkla
    // NOT: Header'daki "Giriş Yap" bir <a> (role=link), bu yüzden role=button unique
    await page.getByRole('button', { name: 'Giriş Yap' }).click();

    // Login sonrası ana sayfaya yönlenmeli
    await expect(page).toHaveURL('/', { timeout: 10_000 });

    // Auth state'in header'a yansımasını bekle
    await expect(page.getByRole('link', { name: 'Giriş Yap' })).not.toBeVisible({ timeout: 10_000 });
}
