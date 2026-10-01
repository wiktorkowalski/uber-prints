import { createContext, useContext } from 'react';
import { Theme } from '../lib/theme';

export interface ThemeContextType {
  /** The user's choice. */
  theme: Theme;
  /** What is on screen now: the choice, with 'system' resolved. */
  resolvedTheme: 'light' | 'dark';
  setTheme: (theme: Theme) => void;
}

export const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }
  return context;
};
