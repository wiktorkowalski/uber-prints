import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { UserDto } from '../types/api';
import { api } from '../lib/api';

interface AuthContextType {
  user: UserDto | null;
  loading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (token: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchUser = async () => {
    try {
      const userData = await api.getCurrentUser();
      setUser(userData);
    } catch (error) {
      // User not authenticated or token expired
      setUser(null);
      api.clearToken();
    } finally {
      setLoading(false);
    }
  };

  const ensureGuestSession = async () => {
    const guestToken = api.getGuestSessionToken();
    if (!guestToken) {
      try {
        const guestSession = await api.createGuestSession();
        api.setGuestSessionToken(guestSession.guestSessionToken);
      } catch (error) {
        console.error('Failed to create guest session:', error);
      }
    }
  };

  useEffect(() => {
    const token = api.getToken();
    if (token) {
      fetchUser();
    } else {
      // Ensure guest session exists for unauthenticated users
      ensureGuestSession();
      setLoading(false);
    }
  }, []);

  const login = async (token: string) => {
    api.setToken(token);
    await fetchUser();
  };

  const logout = async () => {
    try {
      await api.logout();
    } catch (error) {
      console.error('Logout error:', error);
    }
    setUser(null);
    api.clearToken();
  };

  const refreshUser = async () => {
    await fetchUser();
  };

  const value: AuthContextType = {
    user,
    loading,
    isAuthenticated: !!user,
    isAdmin: user?.isAdmin || false,
    login,
    logout,
    refreshUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
