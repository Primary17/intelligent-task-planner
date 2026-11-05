import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '../api/endpoints';
import { setUnauthorizedHandler, tokenStore } from '../api/http';
import type { AuthResponse, UserProfile } from '../api/types';

interface AuthState {
  user: UserProfile | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, displayName: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(() => tokenStore.get() !== null);

  const logout = useCallback(() => {
    tokenStore.clear();
    setUser(null);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(logout);
    if (!tokenStore.get()) return;
    authApi.me().then(setUser).catch(logout).finally(() => setLoading(false));
  }, [logout]);

  const accept = useCallback((response: AuthResponse) => {
    tokenStore.set(response.accessToken);
    setUser(response.user);
  }, []);

  const value = useMemo<AuthState>(() => ({
    user,
    loading,
    login: async (email, password) => accept(await authApi.login(email, password)),
    register: async (email, displayName, password) => accept(await authApi.register(email, displayName, password)),
    logout,
  }), [user, loading, accept, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider');
  return context;
}
