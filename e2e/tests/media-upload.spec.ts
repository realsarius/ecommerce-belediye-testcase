import { expect, test, type Page, type Route } from '@playwright/test';

const tinyPngBuffer = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO6xT4sAAAAASUVORK5CYII=',
  'base64',
);

type AuthRole = 'Seller' | 'Admin';

async function seedAuthState(page: Page, role: AuthRole) {
  const userId = role === 'Admin' ? 9901 : 9801;
  const user = {
    id: userId,
    email: role === 'Admin' ? 'admin-e2e@test.com' : 'seller-e2e@test.com',
    firstName: role,
    lastName: 'E2E',
    role,
    isEmailVerified: true,
  };

  await page.addInitScript((auth) => {
    window.localStorage.setItem('token', auth.token);
    window.localStorage.setItem('refreshToken', auth.refreshToken);
    window.localStorage.setItem('user', JSON.stringify(auth.user));
  }, {
    token: `${role.toLowerCase()}-token`,
    refreshToken: `${role.toLowerCase()}-refresh-token`,
    user,
  });
}

function okJson(route: Route, payload: unknown) {
  return route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(payload),
  });
}

test.describe('Media Upload E2E', () => {
  test('seller ürün ekranında görsel yükleyip ana görseli değiştirebilmeli', async ({ page }) => {
    await seedAuthState(page, 'Seller');

    const reorderPayloads: Array<{ imageOrders: Array<{ imageId: number; displayOrder: number; isPrimary: boolean }> }> = [];
    let uploadedImageId: number | null = null;
    let uploadCount = 0;

    const sellerProfile = {
      id: 41,
      userId: 9801,
      brandName: 'E2E Seller',
      brandDescription: 'E2E seller profile',
      logoUrl: null,
      bannerImageUrl: null,
      contactEmail: null,
      contactPhone: null,
      websiteUrl: null,
      instagramUrl: null,
      facebookUrl: null,
      xUrl: null,
      isVerified: true,
      createdAt: '2026-03-05T12:00:00Z',
      sellerFirstName: 'Seller',
      sellerLastName: 'E2E',
    };

    const productState = {
      id: 77,
      name: 'E2E Ürün',
      description: 'E2E ürün açıklaması',
      price: 1299,
      originalPrice: 1299,
      currency: 'TRY',
      sku: 'E2E-77',
      isActive: true,
      categoryId: 12,
      categoryName: 'Elektronik',
      stockQuantity: 15,
      createdAt: '2026-03-05T12:00:00Z',
      averageRating: 0,
      reviewCount: 0,
      wishlistCount: 0,
      hasActiveCampaign: false,
      isCampaignFeatured: false,
      images: [
        {
          id: 700,
          imageUrl: 'https://img.test.local/products/seller-41/product-77/existing.webp',
          objectKey: 'products/seller-41/product-77/existing.webp',
          sortOrder: 0,
          isPrimary: true,
        },
      ],
      variants: [],
    };

    await page.route('**/mock-upload/**', async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({ status: 200, body: '' });
        return;
      }

      await route.continue();
    });

    await page.route('**/api/v1/notifications/unread-count', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: { unreadCount: 0 } });
    });

    await page.route('**/api/v1/seller/profile', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: sellerProfile });
    });

    await page.route('**/api/v1/categories', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, {
        data: [
          {
            id: 12,
            name: 'Elektronik',
            description: 'Kategori',
            isActive: true,
            parentCategoryId: null,
            sortOrder: 0,
            productCount: 10,
            childCount: 0,
          },
        ],
      });
    });

    await page.route('**/api/v1/seller/products/77', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: productState });
    });

    await page.route('**/api/v1/media/presign', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as {
        context: string;
        referenceId: number;
      };

      expect(body.context).toBe('product');
      expect(body.referenceId).toBe(77);

      uploadCount += 1;
      const objectKey = `products/seller-41/product-77/e2e-${uploadCount}.png`;

      await okJson(route, {
        data: {
          uploadUrl: `http://localhost:3000/mock-upload/${objectKey}`,
          publicUrl: `https://img.test.local/${objectKey}`,
          objectKey,
        },
      });
    });

    await page.route('**/api/v1/media/confirm', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as {
        context: string;
        objectKey: string;
        isPrimary?: boolean;
        sortOrder?: number;
      };

      expect(body.context).toBe('product');

      uploadedImageId = 800 + uploadCount;
      const image = {
        id: uploadedImageId,
        imageUrl: `https://img.test.local/${body.objectKey}`,
        objectKey: body.objectKey,
        sortOrder: body.sortOrder ?? productState.images.length,
        isPrimary: body.isPrimary ?? false,
      };

      productState.images.push(image);

      await okJson(route, {
        data: {
          imageId: image.id,
          imageUrl: image.imageUrl,
          objectKey: image.objectKey,
          sortOrder: image.sortOrder,
          isPrimary: image.isPrimary,
        },
      });
    });

    await page.route('**/api/v1/media/products/77/images/reorder', async (route) => {
      if (route.request().method() !== 'PUT') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as { imageOrders: Array<{ imageId: number; displayOrder: number; isPrimary: boolean }> };
      reorderPayloads.push(body);
      await okJson(route, { success: true });
    });

    await page.goto('/seller/products/77');

    await expect(page.getByRole('heading', { name: 'Ürün Düzenle' })).toBeVisible({ timeout: 10_000 });

    await page.locator('input[type="file"]').first().setInputFiles({
      name: 'urun-gorseli.png',
      mimeType: 'image/png',
      buffer: tinyPngBuffer,
    });

    await expect.poll(() => uploadCount).toBe(1);
    await expect(page.getByText('2/8')).toBeVisible();

    await page.getByRole('button', { name: 'Ana Yap' }).first().click();

    await expect.poll(() => reorderPayloads.length).toBeGreaterThan(0);
    await expect.poll(() => {
      const latest = reorderPayloads[reorderPayloads.length - 1];
      if (!latest || uploadedImageId === null) {
        return false;
      }

      return latest.imageOrders.some((item) => item.imageId === uploadedImageId && item.isPrimary);
    }).toBe(true);
  });

  test('admin yeni ürün oluşturabilmeli', async ({ page }) => {
    await seedAuthState(page, 'Admin');

    let createPayload: Record<string, unknown> | null = null;

    await page.route('**/api/v1/notifications/unread-count', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: { unreadCount: 0 } });
    });

    await page.route('**/api/v1/frontend-settings/features', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, {
        data: {
          enablePersonalizedRecommendations: true,
          enableCampaignCountdown: true,
          enableWalletCards: true,
          enableCampaignProgressBar: true,
          enableStockProgressBar: true,
          enableStickyFilterBar: true,
          enableInlineRecommendations: true,
          enableAdminProductImageUploader: true,
        },
      });
    });

    await page.route('**/api/v1/categories', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, {
        data: [
          {
            id: 12,
            name: 'Elektronik',
            description: 'Kategori',
            isActive: true,
            parentCategoryId: null,
            sortOrder: 0,
            productCount: 10,
            childCount: 0,
          },
        ],
      });
    });

    await page.route('**/api/v1/admin/products*', async (route) => {
      const method = route.request().method();
      if (method === 'POST') {
        createPayload = route.request().postDataJSON() as Record<string, unknown>;

        await okJson(route, {
          data: {
            id: 909,
            name: createPayload.name,
            description: createPayload.description,
            price: createPayload.price,
            originalPrice: createPayload.price,
            currency: createPayload.currency,
            sku: createPayload.sku,
            isActive: createPayload.isActive,
            categoryId: createPayload.categoryId,
            categoryName: 'Elektronik',
            stockQuantity: createPayload.initialStock,
            createdAt: '2026-03-06T12:00:00Z',
            averageRating: 0,
            reviewCount: 0,
            wishlistCount: 0,
            hasActiveCampaign: false,
            isCampaignFeatured: false,
            images: [],
            variants: [],
          },
        });
        return;
      }

      if (method === 'GET') {
        await okJson(route, {
          data: {
            items: [
              {
                id: 909,
                name: 'Admin Yeni Ürün',
                description: '',
                price: 15499,
                originalPrice: 15499,
                currency: 'TRY',
                sku: 'ADMIN-NEW-909',
                isActive: true,
                categoryId: 12,
                categoryName: 'Elektronik',
                stockQuantity: 25,
                sellerId: 11,
                sellerBrandName: 'E-Ticaret',
                createdAt: '2026-03-06T12:00:00Z',
                averageRating: 0,
                reviewCount: 0,
                wishlistCount: 0,
                hasActiveCampaign: false,
                isCampaignFeatured: false,
                primaryImageUrl: null,
                images: [],
                variants: [],
              },
            ],
            page: 1,
            pageSize: 20,
            totalCount: 1,
            totalPages: 1,
            hasPreviousPage: false,
            hasNextPage: false,
          },
        });
        return;
      }

      await route.continue();
    });

    await page.route('**/api/v1/admin/categories', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, {
        data: [
          {
            id: 12,
            name: 'Elektronik',
            description: 'Kategori',
            imageUrl: null,
            isActive: true,
            parentCategoryId: null,
            sortOrder: 0,
            productCount: 10,
            childCount: 0,
          },
        ],
      });
    });

    await page.goto('/admin/products/new');
    await expect(page.getByRole('heading', { name: 'Yeni Ürün' })).toBeVisible({ timeout: 10_000 });

    await page.getByLabel('Ürün Adı *').fill('Admin Yeni Ürün');
    await page.getByLabel('SKU *').fill('admin-new-909');
    await page.getByLabel('Fiyat *').fill('15499');
    await page.getByLabel('Stok Miktarı').fill('25');

    await page.getByRole('combobox').first().click();
    await page.getByRole('option', { name: 'Elektronik' }).click();

    await page.getByRole('button', { name: 'Oluştur' }).click();

    await expect.poll(() => createPayload !== null).toBe(true);
    await expect.poll(() => createPayload?.categoryId).toBe(12);
    await expect.poll(() => createPayload?.initialStock).toBe(25);
    await expect(page).toHaveURL(/\/admin\/products$/);
    await expect(page.getByRole('heading', { name: 'Ürün Yönetimi' })).toBeVisible({ timeout: 10_000 });
  });

  test('admin ürün ekranında görsel yükleyip ana görseli değiştirebilmeli', async ({ page }) => {
    await seedAuthState(page, 'Admin');

    const reorderPayloads: Array<{ imageOrders: Array<{ imageId: number; displayOrder: number; isPrimary: boolean }> }> = [];
    let uploadedImageId: number | null = null;
    let uploadCount = 0;

    const productState = {
      id: 88,
      name: 'Admin E2E Ürün',
      description: 'Admin panel ürünü',
      price: 999,
      originalPrice: 999,
      currency: 'TRY',
      sku: 'ADMIN-E2E-88',
      isActive: true,
      categoryId: 12,
      categoryName: 'Elektronik',
      stockQuantity: 20,
      createdAt: '2026-03-06T10:00:00Z',
      averageRating: 0,
      reviewCount: 0,
      wishlistCount: 0,
      hasActiveCampaign: false,
      isCampaignFeatured: false,
      images: [
        {
          id: 880,
          imageUrl: 'https://img.test.local/products/seller-11/product-88/existing.webp',
          objectKey: 'products/seller-11/product-88/existing.webp',
          sortOrder: 0,
          isPrimary: true,
        },
      ],
      variants: [],
    };

    await page.route('**/mock-upload/**', async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({ status: 200, body: '' });
        return;
      }

      await route.continue();
    });

    await page.route('**/api/v1/notifications/unread-count', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: { unreadCount: 0 } });
    });

    await page.route('**/api/v1/frontend-settings/features', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, {
        data: {
          enablePersonalizedRecommendations: true,
          enableCampaignCountdown: true,
          enableWalletCards: true,
          enableCampaignProgressBar: true,
          enableStockProgressBar: true,
          enableStickyFilterBar: true,
          enableInlineRecommendations: true,
          enableAdminProductImageUploader: true,
        },
      });
    });

    await page.route('**/api/v1/categories', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, {
        data: [
          {
            id: 12,
            name: 'Elektronik',
            description: 'Kategori',
            isActive: true,
            parentCategoryId: null,
            sortOrder: 0,
            productCount: 10,
            childCount: 0,
          },
        ],
      });
    });

    await page.route('**/api/v1/products/88', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: productState });
    });

    await page.route('**/api/v1/media/presign', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as {
        context: string;
        referenceId: number;
      };

      expect(body.context).toBe('product');
      expect(body.referenceId).toBe(88);

      uploadCount += 1;
      const objectKey = `products/seller-11/product-88/admin-e2e-${uploadCount}.png`;

      await okJson(route, {
        data: {
          uploadUrl: `http://localhost:3000/mock-upload/${objectKey}`,
          publicUrl: `https://img.test.local/${objectKey}`,
          objectKey,
        },
      });
    });

    await page.route('**/api/v1/media/confirm', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as {
        context: string;
        objectKey: string;
        isPrimary?: boolean;
        sortOrder?: number;
      };

      expect(body.context).toBe('product');

      uploadedImageId = 8800 + uploadCount;
      const image = {
        id: uploadedImageId,
        imageUrl: `https://img.test.local/${body.objectKey}`,
        objectKey: body.objectKey,
        sortOrder: body.sortOrder ?? productState.images.length,
        isPrimary: body.isPrimary ?? false,
      };

      productState.images.push(image);

      await okJson(route, {
        data: {
          imageId: image.id,
          imageUrl: image.imageUrl,
          objectKey: image.objectKey,
          sortOrder: image.sortOrder,
          isPrimary: image.isPrimary,
        },
      });
    });

    await page.route('**/api/v1/media/products/88/images/reorder', async (route) => {
      if (route.request().method() !== 'PUT') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as { imageOrders: Array<{ imageId: number; displayOrder: number; isPrimary: boolean }> };
      reorderPayloads.push(body);
      await okJson(route, { success: true });
    });

    await page.goto('/admin/products/88');
    await expect(page.getByRole('heading', { name: 'Ürün Düzenle' })).toBeVisible({ timeout: 10_000 });

    await page.locator('input[type="file"]').first().setInputFiles({
      name: 'admin-urun-gorseli.png',
      mimeType: 'image/png',
      buffer: tinyPngBuffer,
    });

    await expect.poll(() => uploadCount).toBe(1);
    await expect(page.getByText('2/8')).toBeVisible();

    await page.getByRole('button', { name: 'Ana Yap' }).first().click();

    await expect.poll(() => reorderPayloads.length).toBeGreaterThan(0);
    await expect.poll(() => {
      const latest = reorderPayloads[reorderPayloads.length - 1];
      if (!latest || uploadedImageId === null) {
        return false;
      }

      return latest.imageOrders.some((item) => item.imageId === uploadedImageId && item.isPrimary);
    }).toBe(true);
  });

  test('admin kategori ekranında görsel yükleyebilmeli', async ({ page }) => {
    await seedAuthState(page, 'Admin');

    let categoryConfirmCount = 0;
    const categoriesState = [
      {
        id: 51,
        name: 'Elektronik',
        description: 'Elektronik ürünler',
        imageUrl: null,
        isActive: true,
        parentCategoryId: null,
        sortOrder: 0,
        productCount: 15,
        childCount: 2,
      },
    ];

    await page.route('**/mock-upload/**', async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({ status: 200, body: '' });
        return;
      }

      await route.continue();
    });

    await page.route('**/api/v1/notifications/unread-count', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: { unreadCount: 0 } });
    });

    await page.route('**/api/v1/admin/categories', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: categoriesState });
    });

    await page.route('**/api/v1/media/presign', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as { context: string; referenceId: number };
      expect(body.context).toBe('category');
      expect(body.referenceId).toBe(51);

      const objectKey = 'categories/category-51/e2e-category.webp';
      await okJson(route, {
        data: {
          uploadUrl: `http://localhost:3000/mock-upload/${objectKey}`,
          publicUrl: `https://img.test.local/${objectKey}`,
          objectKey,
        },
      });
    });

    await page.route('**/api/v1/media/confirm', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as { context: string; objectKey: string };
      expect(body.context).toBe('category');

      categoryConfirmCount += 1;
      categoriesState[0].imageUrl = `https://img.test.local/${body.objectKey}`;

      await okJson(route, {
        data: {
          imageUrl: categoriesState[0].imageUrl,
          objectKey: body.objectKey,
        },
      });
    });

    await page.goto('/admin/categories');
    await expect(page.getByRole('heading', { name: 'Kategoriler' })).toBeVisible({ timeout: 10_000 });

    await page.getByRole('button', { name: /Elektronik/ }).first().click();
    await page.locator('input[type="file"]').first().setInputFiles({
      name: 'kategori.webp',
      mimeType: 'image/webp',
      buffer: tinyPngBuffer,
    });

    await expect.poll(() => categoryConfirmCount).toBe(1);
    await expect(page.getByRole('button', { name: 'Görseli Değiştir' })).toBeVisible();
  });

  test('seller profil ekranında logo ve banner görsellerini güncelleyebilmeli', async ({ page }) => {
    await seedAuthState(page, 'Seller');

    let logoConfirmCount = 0;
    let bannerConfirmCount = 0;
    const profileState = {
      id: 41,
      userId: 9801,
      brandName: 'E2E Seller',
      brandDescription: 'E2E seller profile',
      logoUrl: '',
      bannerImageUrl: '',
      contactEmail: '',
      contactPhone: '',
      websiteUrl: '',
      instagramUrl: '',
      facebookUrl: '',
      xUrl: '',
      isVerified: true,
      createdAt: '2026-03-05T12:00:00Z',
      sellerFirstName: 'Seller',
      sellerLastName: 'E2E',
    };

    await page.route('**/mock-upload/**', async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({ status: 200, body: '' });
        return;
      }

      await route.continue();
    });

    await page.route('**/api/v1/notifications/unread-count', async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: { unreadCount: 0 } });
    });

    await page.route('**/api/v1/seller/profile', async (route) => {
      const method = route.request().method();
      if (method !== 'GET') {
        await route.continue();
        return;
      }

      await okJson(route, { data: profileState });
    });

    await page.route('**/api/v1/media/presign', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as { context: string; referenceId: number };
      expect(body.referenceId).toBe(41);

      const objectKey = body.context === 'seller-logo'
        ? 'sellers/seller-41/logo.webp'
        : 'sellers/seller-41/banner.webp';

      await okJson(route, {
        data: {
          uploadUrl: `http://localhost:3000/mock-upload/${objectKey}`,
          publicUrl: `https://img.test.local/${objectKey}`,
          objectKey,
        },
      });
    });

    await page.route('**/api/v1/media/confirm', async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }

      const body = route.request().postDataJSON() as { context: string; objectKey: string };
      const imageUrl = `https://img.test.local/${body.objectKey}`;

      if (body.context === 'seller-logo') {
        profileState.logoUrl = imageUrl;
        logoConfirmCount += 1;
      } else if (body.context === 'seller-banner') {
        profileState.bannerImageUrl = imageUrl;
        bannerConfirmCount += 1;
      }

      await okJson(route, {
        data: {
          imageUrl,
          objectKey: body.objectKey,
        },
      });
    });

    await page.goto('/seller/profile');
    await expect(page.getByRole('heading', { name: 'Mağaza Profili' })).toBeVisible({ timeout: 10_000 });

    const fileInputs = page.locator('input[type="file"]');
    await expect(fileInputs).toHaveCount(2);

    await fileInputs.nth(0).setInputFiles({
      name: 'logo.png',
      mimeType: 'image/png',
      buffer: tinyPngBuffer,
    });

    await fileInputs.nth(1).setInputFiles({
      name: 'banner.png',
      mimeType: 'image/png',
      buffer: tinyPngBuffer,
    });

    await expect.poll(() => logoConfirmCount).toBe(1);
    await expect.poll(() => bannerConfirmCount).toBe(1);

    await expect(page.locator('img[alt="Logo Görseli"]')).toHaveAttribute('src', /logo\.webp/);
    await expect(page.locator('img[alt="Banner Görseli"]')).toHaveAttribute('src', /banner\.webp/);
  });
});
