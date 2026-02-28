import { describe, it, expect, beforeEach } from 'vitest';
import authReducer, { setCredentials, logout, setUser } from './authSlice';
import type { User } from './types';

const mockUser: User = {
    id: 1,
    email: 'test@test.com',
    firstName: 'Test',
    lastName: 'User',
    role: 'Customer',
};

const mockCredentials = {
    user: mockUser,
    token: 'mock-jwt-token',
    refreshToken: 'mock-refresh-token',
};

describe('authSlice', () => {
    // Her testten önce localStorage'ı temizle
    beforeEach(() => {
        localStorage.clear();
    });

    describe('setCredentials', () => {
        it('state doğru şekilde güncellenmeli', () => {
            const initialState = {
                user: null,
                token: null,
                refreshToken: null,
                isAuthenticated: false,
            };

            const newState = authReducer(initialState, setCredentials(mockCredentials));

            expect(newState.user).toEqual(mockUser);
            expect(newState.token).toBe('mock-jwt-token');
            expect(newState.refreshToken).toBe('mock-refresh-token');
            expect(newState.isAuthenticated).toBe(true);
        });

        it('localStorage a token ve user kaydedilmeli', () => {
            const initialState = {
                user: null,
                token: null,
                refreshToken: null,
                isAuthenticated: false,
            };

            authReducer(initialState, setCredentials(mockCredentials));

            expect(localStorage.getItem('token')).toBe('mock-jwt-token');
            expect(localStorage.getItem('refreshToken')).toBe('mock-refresh-token');
            expect(JSON.parse(localStorage.getItem('user')!)).toEqual(mockUser);
        });
    });

    describe('logout', () => {
        it('state sıfırlanmalı', () => {
            const loggedInState = {
                user: mockUser,
                token: 'mock-jwt-token',
                refreshToken: 'mock-refresh-token',
                isAuthenticated: true,
            };

            const newState = authReducer(loggedInState, logout());

            expect(newState.user).toBeNull();
            expect(newState.token).toBeNull();
            expect(newState.refreshToken).toBeNull();
            expect(newState.isAuthenticated).toBe(false);
        });

        it('localStorage temizlenmeli', () => {
            localStorage.setItem('token', 'mock-jwt-token');
            localStorage.setItem('refreshToken', 'mock-refresh-token');
            localStorage.setItem('user', JSON.stringify(mockUser));

            const loggedInState = {
                user: mockUser,
                token: 'mock-jwt-token',
                refreshToken: 'mock-refresh-token',
                isAuthenticated: true,
            };

            authReducer(loggedInState, logout());

            expect(localStorage.getItem('token')).toBeNull();
            expect(localStorage.getItem('refreshToken')).toBeNull();
            expect(localStorage.getItem('user')).toBeNull();
        });
    });

    describe('setUser', () => {
        it('sadece user bilgisi güncellenmeli, token değişmemeli', () => {
            const currentState = {
                user: mockUser,
                token: 'mock-jwt-token',
                refreshToken: 'mock-refresh-token',
                isAuthenticated: true,
            };

            const updatedUser: User = { ...mockUser, firstName: 'Yeni', lastName: 'İsim' };
            const newState = authReducer(currentState, setUser(updatedUser));

            expect(newState.user?.firstName).toBe('Yeni');
            expect(newState.user?.lastName).toBe('İsim');
            expect(newState.token).toBe('mock-jwt-token'); // Token aynı kalmalı
            expect(newState.isAuthenticated).toBe(true);
        });
    });
});
