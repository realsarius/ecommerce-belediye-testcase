import { test, expect } from '@playwright/test';
import { loginAsCustomer } from './helpers';

test.describe('Authenticated Flows', () => {
    test('login sonrası nav linkleri görünmeli', async ({ page }) => {
        await loginAsCustomer(page);

        await expect(page.getByRole('link', { name: 'Siparişlerim' })).toBeVisible();
        await expect(page.getByRole('link', { name: 'Sepetim' })).toBeVisible();
    });

    test('login sonrası sepet sayfasına erişilebilmeli', async ({ page }) => {
        await loginAsCustomer(page);

        await page.getByRole('link', { name: 'Sepetim' }).click();
        await expect(page).toHaveURL('/cart');
    });

    test('login sonrası siparişler sayfasına erişilebilmeli', async ({ page }) => {
        await loginAsCustomer(page);

        await page.getByRole('link', { name: 'Siparişlerim' }).click();
        await expect(page).toHaveURL('/orders');
    });
});
