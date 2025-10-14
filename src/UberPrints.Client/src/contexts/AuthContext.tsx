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
        // Surface error to user - guest session is needed for creating requests
        toast({
          title: "Connection Error",
          description: "Unable to establish session. Please check your internet connection and try again.",
          variant: "destructive",
        });
        throw error; // Re-throw so caller knows it failed
      }
    }
  };

  useEffect(() => {
    let isMounted = true; // Track if component is still mounted

    const initializeAuth = async () => {
      const token = api.getToken();
      if (token) {
        await fetchUser();
      } else {
        // Ensure guest session exists for unauthenticated users
        try {
          await ensureGuestSession();
        } catch {
          // Error already shown to user in ensureGuestSession
        }
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    // Listen for API error events from interceptor
    const handleUnauthorized = async () => {
      if (!isMounted) return;

      setUser(null);
      api.clearToken();
      api.clearGuestSessionToken();
      toast({
        title: "Session expired",
        description: "Your session has expired. Please log in again.",
        variant: "destructive",
      });
      // Ensure new guest session is created
      try {
        await ensureGuestSession();
      } catch {
        // Error already shown to user
      }
    };

    const handleForbidden = (event: Event) => {
      if (!isMounted) return;
      const customEvent = event as CustomEvent;
      toast({
        title: "Access Denied",
        description: customEvent.detail?.message || "You don't have permission to perform this action.",
        variant: "destructive",
      });
    };

    const handleServerError = (event: Event) => {
      if (!isMounted) return;
      const customEvent = event as CustomEvent;
      toast({
        title: "Server Error",
        description: customEvent.detail?.message || "Something went wrong. Please try again later.",
        variant: "destructive",
      });
    };

    const handleNetworkError = (event: Event) => {
      if (!isMounted) return;
      const customEvent = event as CustomEvent;
      toast({
        title: "Network Error",
        description: customEvent.detail?.message || "Please check your internet connection.",
        variant: "destructive",
      });
    };

    window.addEventListener('auth:unauthorized', handleUnauthorized);
    window.addEventListener('auth:forbidden', handleForbidden as EventListener);
    window.addEventListener('api:server-error', handleServerError as EventListener);
    window.addEventListener('api:network-error', handleNetworkError as EventListener);

    // Set up automatic token refresh check
    const checkTokenRefresh = async () => {
      if (!isMounted) return;

      const token = api.getToken();
      if (!token) return;

      try {
        // Attempt to refresh token - backend will determine if needed
        await api.refreshToken();
      } catch (error) {
        // Token refresh failed, will be handled by 401 interceptor
      }
    };

    // Initialize authentication
    initializeAuth();

    // Check token refresh every hour
    const refreshInterval = setInterval(checkTokenRefresh, 60 * 60 * 1000);

    return () => {
      isMounted = false;
      window.removeEventListener('auth:unauthorized', handleUnauthorized);
      window.removeEventListener('auth:forbidden', handleForbidden as EventListener);
      window.removeEventListener('api:server-error', handleServerError as EventListener);
      window.removeEventListener('api:network-error', handleNetworkError as EventListener);
      clearInterval(refreshInterval);
    };
  }, []); // Empty dependency array - only run once on mount

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
