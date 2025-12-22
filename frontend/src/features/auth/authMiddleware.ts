import type { Middleware } from '@reduxjs/toolkit';
import { setCredentials, logout } from './authSlice';

// localStorage gibi yan etkiler (side effects) middleware'de yapılmalıdır.
export const authMiddleware: Middleware = () => (next) => (action) => {
  const result = next(action);

  if (setCredentials.match(action)) {
    localStorage.setItem('token', action.payload.token);
    localStorage.setItem('refreshToken', action.payload.refreshToken);
  }

  if (logout.match(action)) {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
  }

  return result;
};
