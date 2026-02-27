import { test, expect } from '@playwright/test';

test.describe('Cart', () => {
    test('giriş yapmadan sepete gidince login\'e yönlenmeli', async ({ page }) => {
        await page.goto('/cart');

        // ProtectedRoute login'e yönlendirmeli
        await expect(page).toHaveURL(/\/login/);
    });
});

test.describe('Support', () => {
    test('giriş yapmadan destek sayfasına gidince login\'e yönlenmeli', async ({ page }) => {
        await page.goto('/support');

        // ProtectedRoute login'e yönlendirmeli
        await expect(page).toHaveURL(/\/login/);
    });
});
