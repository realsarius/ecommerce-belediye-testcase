import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { Button } from './button';

describe('Button Component', () => {
    it('doğru metinle render edilmeli', () => {
        render(<Button>Tıkla</Button>);
        expect(screen.getByRole('button', { name: 'Tıkla' })).toBeInTheDocument();
    });

    it('tıklandığında onClick prop tetiklenmeli', async () => {
        const handleClick = vi.fn();
        render(<Button onClick={handleClick}>Tıkla</Button>);

        const button = screen.getByRole('button', { name: 'Tıkla' });
        await userEvent.click(button);

        expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('disabled prop aldığında tıklanamaz olmalı', async () => {
        const handleClick = vi.fn();
        render(<Button disabled onClick={handleClick}>Tıkla</Button>);

        const button = screen.getByRole('button', { name: 'Tıkla' });
        expect(button).toBeDisabled();

        await userEvent.click(button);
        expect(handleClick).not.toHaveBeenCalled();
    });

    it('farklı varyant classlarını almalı', () => {
        const { rerender } = render(<Button variant="destructive">Sil</Button>);
        const button = screen.getByRole('button', { name: 'Sil' });

        expect(button.getAttribute('data-variant')).toBe('destructive');
        expect(button.className).toContain('bg-destructive');

        rerender(<Button variant="outline">Kenarlık</Button>);
        expect(button.getAttribute('data-variant')).toBe('outline');
        expect(button.className).toContain('border');
    });
});
