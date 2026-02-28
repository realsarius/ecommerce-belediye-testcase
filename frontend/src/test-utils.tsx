import { render, type RenderOptions } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import { baseApi } from '@/app/api';
import authReducer from '@/features/auth/authSlice';
import productsReducer from '@/features/products/productsSlice';
import type { ReactElement, PropsWithChildren } from 'react';

/**
 * Test için Redux Store oluşturur.
 * Gerçek store ile aynı reducer yapısına sahiptir.
 */
export function createTestStore(preloadedState?: Record<string, unknown>) {
    return configureStore({
        reducer: {
            [baseApi.reducerPath]: baseApi.reducer,
            auth: authReducer,
            products: productsReducer,
        },
        middleware: (getDefaultMiddleware) =>
            getDefaultMiddleware().concat(baseApi.middleware),
        preloadedState,
    });
}

interface RenderWithProvidersOptions extends Omit<RenderOptions, 'wrapper'> {
    route?: string;
    store?: ReturnType<typeof createTestStore>;
}

/**
 * Bileşenleri Redux Provider ve MemoryRouter ile sarmalayarak render eder.
 * Sayfa bileşenlerini test etmek için kullanılır.
 */
export function renderWithProviders(
    ui: ReactElement,
    {
        route = '/',
        store = createTestStore(),
        ...renderOptions
    }: RenderWithProvidersOptions = {}
) {
    function Wrapper({ children }: PropsWithChildren) {
        return (
            <Provider store={store}>
                <MemoryRouter initialEntries={[route]}>
                    {children}
                </MemoryRouter>
            </Provider>
        );
    }

    return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
}
