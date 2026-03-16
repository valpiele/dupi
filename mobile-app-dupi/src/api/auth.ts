import { apiPost } from './client';

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  displayName?: string;
}

export function login(email: string, password: string): Promise<AuthResponse> {
  return apiPost<AuthResponse>('/api/auth/login', { email, password });
}

export function register(email: string, password: string): Promise<AuthResponse> {
  return apiPost<AuthResponse>('/api/auth/register', { email, password });
}

export function googleLogin(idToken: string): Promise<AuthResponse> {
  return apiPost<AuthResponse>('/api/auth/google', { idToken });
}
