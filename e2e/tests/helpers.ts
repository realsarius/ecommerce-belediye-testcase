import { test, expect, type Page } from '@playwright/test';

/** Yardımcı: customer olarak login yap ve auth state'in UI'a yansımasını bekle */
export async function loginAsCustomer(page: Page) {
    const credentials = await loginViaApi(page, 'customer@test.com', 'Test123!');

    await page.addInitScript((auth) => {
        window.localStorage.setItem('token', auth.token);
        window.localStorage.setItem('refreshToken', auth.refreshToken);
        window.localStorage.setItem('user', JSON.stringify(auth.user));
    }, credentials);

    await page.goto('/');
    await expect.poll(
        async () => page.evaluate(() => Boolean(window.localStorage.getItem('token'))),
        { timeout: 10_000 }
    ).toBe(true);

    await expect(page.getByRole('link', { name: 'Giriş Yap' })).not.toBeVisible({ timeout: 10_000 });
}

/** API üzerinden login olup bearer token döndür. */
export async function loginViaApi(page: Page, email: string, password: string) {
    let lastStatus = 0;
    let lastBody = '';
    let lastErrorMessage = '';

    for (let attempt = 0; attempt < 5; attempt++) {
        try {
            const response = await page.request.post('/api/v1/auth/login', {
                headers: {
                    'Content-Type': 'application/json',
                },
                data: {
                    email,
                    password,
                },
            });

            if (response.ok()) {
                const payload = await response.json();
                return {
                    token: payload.data.token as string,
                    refreshToken: payload.data.refreshToken as string,
                    user: payload.data.user as {
                        id: number;
                        email: string;
                        firstName: string;
                        lastName: string;
                        role: string;
                    },
                };
            }

            lastStatus = response.status();
            lastBody = await response.text();

            if (lastStatus === 429) {
                try {
                    const rateLimitPayload = JSON.parse(lastBody) as { retryAfterSeconds?: number };
                    if (rateLimitPayload.retryAfterSeconds && rateLimitPayload.retryAfterSeconds > 0) {
                        await page.waitForTimeout(rateLimitPayload.retryAfterSeconds * 1000);
                        continue;
                    }
                } catch {
                    // No-op: body JSON parse edilemezse genel backoff ile devam et.
                }
            }
        } catch (error) {
            lastErrorMessage = error instanceof Error ? error.message : String(error);
        }

        await page.waitForTimeout(750);
    }

    throw new Error(`API login failed with ${lastStatus}: ${lastBody || lastErrorMessage}`);
}

/** Varsa cookie banner'ı kapat. */
export async function acceptCookieBannerIfPresent(page: Page) {
    const acceptAllButton = page.getByRole('button', { name: 'Tümünü Kabul Et' });
    if (await acceptAllButton.isVisible().catch(() => false)) {
        await acceptAllButton.click({ force: true });
        await expect(acceptAllButton).toBeHidden({ timeout: 10_000 });
    }
}

/** Varsa mevcut sepeti temizle, testleri deterministik tut. */
export async function clearCartIfNeeded(page: Page) {
    await page.goto('/cart');

    const clearCartButton = page.getByRole('button', { name: 'Sepeti Temizle' });
    if (await clearCartButton.isVisible().catch(() => false)) {
        await clearCartButton.click();
        await expect(page.getByText('Sepetiniz Boş')).toBeVisible({ timeout: 10_000 });
    }
}

/** Ana sayfadan ilk ürünü sepete ekle ve cart ekranında görünürlüğünü doğrula. */
export async function addFirstProductToCart(page: Page, productId = 22) {
    const token = await page.evaluate(() => window.localStorage.getItem('token'));
    if (!token) {
        throw new Error('Customer token bulunamadı');
    }

    const clearResponse = await page.request.delete('http://localhost:5000/api/v1/cart', {
        headers: {
            Authorization: `Bearer ${token}`,
        },
    });
    expect(clearResponse.ok()).toBeTruthy();

    const addResponse = await page.request.post('http://localhost:5000/api/v1/cart/items', {
        headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
        },
        data: {
            productId,
            quantity: 1,
        },
    });
    expect(addResponse.ok()).toBeTruthy();

    await page.goto('/cart');
    await expect(page.getByRole('heading', { name: 'Sepetim' })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('button', { name: 'Siparişi Tamamla' })).toBeVisible({ timeout: 10_000 });
}

/** Checkout ekranında adres seçip yeni kart bilgilerini gerçek alanlardan doldur. */
export async function prepareCheckoutForNewCardPayment(page: Page, cardNumber: string) {
    const newCardButton = page.getByRole('button', { name: /Yeni kart ile öde/i });
    if (await newCardButton.isVisible().catch(() => false)) {
        await newCardButton.click();
    }

    const comboBoxes = page.getByRole('combobox');
    await comboBoxes.nth(0).click();
    await page.getByRole('option', { name: /Ev Adresim/i }).click();

    await page.getByPlaceholder('KAMURAN OLTACI').fill('TEST CUSTOMER');
    await page.getByPlaceholder('4111 1111 1111 1111').fill(cardNumber);

    await comboBoxes.nth(1).click();
    await page.getByRole('option', { name: '12' }).click();

    const nextYear = String(new Date().getFullYear() + 1);
    await comboBoxes.nth(2).click();
    await page.getByRole('option', { name: nextYear }).click();

    await page.getByPlaceholder('123').fill('123');
}

/** Checkout yasal onay dialog'larını açıp kabul et. */
export async function acceptCheckoutLegalConsents(page: Page) {
    for (const consentName of [
        'Ön Bilgilendirme Formu belgesini okudum ve onaylıyorum.',
        'Mesafeli Satış Sözleşmesi metnini okudum ve onaylıyorum.',
    ]) {
        const checkbox = page.getByRole('checkbox', { name: consentName });
        await expect(checkbox).toBeVisible({ timeout: 10_000 });

        const isChecked = await checkbox.isChecked();
        if (!isChecked) {
            await checkbox.click({ force: true });
        }

        await expect(checkbox).toBeChecked({ timeout: 10_000 });
    }
}

/** Retry payment dialog'ındaki ay/yıl select'lerini doldur. */
export async function fillRetryPaymentExpiry(page: Page, targetYear: number) {
    await acceptCookieBannerIfPresent(page);

    const dialog = page.getByRole('dialog', { name: 'Tekrar Ödeme Yap' });
    const comboBoxes = dialog.getByRole('combobox');

    await comboBoxes.nth(0).click();
    await page.keyboard.press('End');
    await page.keyboard.press('Enter');

    if ((await comboBoxes.nth(1).innerText()).trim() !== String(targetYear)) {
        await comboBoxes.nth(1).click();
        await page.getByRole('option', { name: String(targetYear) }).last().click({ force: true });
    }
}
