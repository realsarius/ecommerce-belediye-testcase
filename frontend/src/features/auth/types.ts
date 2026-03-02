// Auth Types

export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  status?: string;
  lastLoginAt?: string | null;
}

export interface AuthResponse {
  success: boolean;
  token?: string;
  refreshToken?: string;
  message?: string;
  user?: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface SocialLoginRequest {
  provider: 'google' | 'apple';
  idToken: string;
  firstName?: string;
  lastName?: string;
  referralCode?: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  referralCode?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
