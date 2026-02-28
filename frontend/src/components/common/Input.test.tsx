import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { Input } from './input';

describe('Input Component', () => {
    it('doğru placeholder ile render edilmeli', () => {
        render(<Input placeholder="Email giriniz" />);
        expect(screen.getByPlaceholderText('Email giriniz')).toBeInTheDocument();
    });

    it('kullanıcı giriş yaptığında value değişmeli', async () => {
        const handleChange = vi.fn();
        render(<Input placeholder="Arama yap" onChange={handleChange} />);

        const input = screen.getByPlaceholderText('Arama yap');
        await userEvent.type(input, 'test');

        expect(input).toHaveValue('test');
        expect(handleChange).toHaveBeenCalled();
    });

    it('disabled prop aldığında disable olmalı ve yazılamamalı', async () => {
        render(<Input placeholder="İsim" disabled />);

        const input = screen.getByPlaceholderText('İsim');
        expect(input).toBeDisabled();

        await userEvent.type(input, 'ali');
        expect(input).toHaveValue('');
    });

    it('hata durumunda (aria-invalid) gerekli stilleri almalı', () => {
        render(<Input aria-invalid="true" placeholder="Hatalı giriş" />);
        const input = screen.getByPlaceholderText('Hatalı giriş');

        // Radix UI tabanlı bileşenimiz className birleşimi alıyor
        expect(input.className).toContain('aria-invalid:border-destructive');
    });
});
