import { test, expect } from '@playwright/test';

test.describe('Product', () => {
    test('ürün kartı primary image varsa görsel göstermeli', async ({ page }) => {
        await page.route('**/api/v1/categories', async (route) => {
            if (route.request().method() !== 'GET') {
                await route.continue();
                return;
            }

            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    data: [
                        {
                            id: 12,
                            name: 'Elektronik',
                            description: 'Kategori',
                            isActive: true,
                            parentCategoryId: null,
                            sortOrder: 0,
                            productCount: 1,
                            childCount: 0,
                        },
                    ],
                }),
            });
        });

        await page.route('**/api/v1/search/products**', async (route) => {
            if (route.request().method() !== 'GET') {
                await route.continue();
                return;
            }

            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    data: {
                        items: [
                            {
                                id: 991,
                                name: 'Görselli Test Ürünü',
                                description: 'Test',
                                price: 1299,
                                originalPrice: 1299,
                                currency: 'TRY',
                                sku: 'IMG-991',
                                isActive: true,
                                categoryId: 12,
                                categoryName: 'Elektronik',
                                stockQuantity: 5,
                                createdAt: '2026-03-06T11:00:00Z',
                                averageRating: 0,
                                reviewCount: 0,
                                wishlistCount: 0,
                                hasActiveCampaign: false,
                                isCampaignFeatured: false,
                                primaryImageUrl: 'https://img.test.local/products/991/primary.webp',
                                images: [],
                                variants: [],
                            },
                        ],
                        page: 1,
                        pageSize: 12,
                        totalCount: 1,
                        totalPages: 1,
                        hasPreviousPage: false,
                        hasNextPage: false,
                    },
                }),
            });
        });

        await page.goto('/');

        const image = page.locator('img[alt="Görselli Test Ürünü"]').first();
        await expect(image).toBeVisible({ timeout: 10_000 });
        await expect(image).toHaveAttribute('src', /primary\.webp/);
    });

    test('ürün kartları ana sayfada listelenmeli', async ({ page }) => {
        await page.goto('/');

        const addToCartButtons = page.getByRole('button', { name: 'Sepete Ekle' });
        await expect(addToCartButtons.first()).toBeVisible({ timeout: 10_000 });
    });

    test('ürün kartına tıklayınca detay sayfasına gitmeli', async ({ page }) => {
        await page.goto('/');

        const productLink = page.locator('a[href^="/products/"]').first();
        await expect(productLink).toBeVisible({ timeout: 10_000 });
        await productLink.click();

        await expect(page).toHaveURL(/\/products\/\d+/);
        await expect(page.getByText('Ürünlere Dön')).toBeVisible();
    });

    test('olmayan ürün için hata mesajı gösterilmeli', async ({ page }) => {
        await page.goto('/products/999999');

        await expect(page.getByText('Ürün Bulunamadı')).toBeVisible({ timeout: 10_000 });
    });

    test('sayfalama çalışmalı', async ({ page }) => {
        await page.goto('/');

        const pageInfo = page.getByText(/Sayfa \d+ \/ \d+/);
        await expect(pageInfo).toBeVisible({ timeout: 10_000 });
    });
});
