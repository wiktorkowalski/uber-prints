import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { UserDto } from '../types/api';
import { api } from '../lib/api';
import { toast } from '../hooks/use-toast';

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

    // Listen for unauthorized events from API interceptor
    const handleUnauthorized = () => {
      setUser(null);
      api.clearToken();
      api.clearGuestSessionToken();
      // Show toast notification
      toast({
        title: "Session expired",
        description: "Your session has expired. Please log in again.",
        variant: "destructive",
      });
      // Ensure new guest session is created
      ensureGuestSession();
    };

    window.addEventListener('auth:unauthorized', handleUnauthorized);
    return () => {
      window.removeEventListener('auth:unauthorized', handleUnauthorized);
    };
  }, []);

  const login = async (token: string) => {
    api.setToken(token);
    await fetchUser();
    // Clear guest session token after successful login since user is now authenticated
    api.clearGuestSessionToken();
  };

  const logout = async () => {
    try {
      await api.logout();
    } catch (error) {
      console.error('Logout error:', error);
    }
    setUser(null);
    api.clearToken();
    api.clearGuestSessionToken();
    // Create a new guest session for the logged-out user
    await ensureGuestSession();
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
