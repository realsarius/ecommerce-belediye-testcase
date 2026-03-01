import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test-utils';
import Login from './Login';
import Register from './Register';
import Help from './Help';

describe('Sayfa Render Smoke Testleri', () => {
    describe('Login Sayfası', () => {
        it('çökmeden render edilmeli ve form elemanları görünmeli', () => {
            renderWithProviders(<Login />, { route: '/login' });

            expect(screen.getByLabelText(/e-posta/i)).toBeInTheDocument();
            expect(screen.getByLabelText(/şifre/i)).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /giriş yap/i })).toBeInTheDocument();
        });
    });

    describe('Register Sayfası', () => {
        it('çökmeden render edilmeli ve form elemanları görünmeli', () => {
            renderWithProviders(<Register />, { route: '/register' });

            expect(screen.getByLabelText('İsim')).toBeInTheDocument();
            expect(screen.getByLabelText('E-posta')).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /kayıt ol/i })).toBeInTheDocument();
        });
    });

    describe('Help Sayfası', () => {
        it('çökmeden render edilmeli ve başlık görünmeli', () => {
            renderWithProviders(<Help />, { route: '/help' });

            expect(screen.getByRole('heading', { name: /yardım merkezi/i })).toBeInTheDocument();
        });
    });
});
