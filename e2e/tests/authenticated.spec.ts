import { test, expect } from '@playwright/test';
import { loginAsCustomer } from './helpers';

test.describe('Authenticated Flows', () => {
    test('login sonrası auth state protected route üzerinde korunmalı', async ({ page }) => {
        await loginAsCustomer(page);

        await page.goto('/orders');
        await expect(page).toHaveURL('/orders');
        await expect(page.getByRole('heading', { name: 'Siparişlerim' })).toBeVisible();
    });

    test('login sonrası sepet sayfasına erişilebilmeli', async ({ page }) => {
        await loginAsCustomer(page);

        await page.goto('/cart');
        await expect(page).toHaveURL('/cart');
        await expect(page.getByText(/Sepetim|Sepetiniz Boş/)).toBeVisible();
    });

    test('login sonrası siparişler sayfasına erişilebilmeli', async ({ page }) => {
        await loginAsCustomer(page);

        await page.goto('/orders');
        await expect(page).toHaveURL('/orders');
        await expect(page.getByRole('heading', { name: 'Siparişlerim' })).toBeVisible();
    });
});
