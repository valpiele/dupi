import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';

interface AuthState {
  token: string | null;
  userId: string | null;
  email: string | null;
  displayName: string | null;
  isLoading: boolean;
  setAuth: (token: string, userId: string, email: string, displayName?: string) => Promise<void>;
  logout: () => Promise<void>;
  loadToken: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  userId: null,
  email: null,
  displayName: null,
  isLoading: true,

  setAuth: async (token, userId, email, displayName) => {
    await SecureStore.setItemAsync('auth_token', token);
    await SecureStore.setItemAsync('auth_user', JSON.stringify({ userId, email, displayName }));
    set({ token, userId, email, displayName: displayName ?? null, isLoading: false });
  },

  logout: async () => {
    await SecureStore.deleteItemAsync('auth_token');
    await SecureStore.deleteItemAsync('auth_user');
    set({ token: null, userId: null, email: null, displayName: null, isLoading: false });
  },

  loadToken: async () => {
    const token = await SecureStore.getItemAsync('auth_token');
    if (token) {
      const userJson = await SecureStore.getItemAsync('auth_user');
      const user = userJson ? JSON.parse(userJson) : {};
      set({
        token,
        userId: user.userId ?? null,
        email: user.email ?? null,
        displayName: user.displayName ?? null,
        isLoading: false,
      });
    } else {
      set({ isLoading: false });
    }
  },
}));
