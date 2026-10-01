import { ReactNode, useEffect, useState } from 'react';
import { ThemeContext } from '../hooks/use-theme';
import {
  applyTheme,
  readStoredTheme,
  systemPrefersDark,
  Theme,
  watchSystemTheme,
  writeStoredTheme,
} from '../lib/theme';

export const ThemeProvider = ({ children }: { children: ReactNode }) => {
  // Same read as the inline script in index.html, so the first render matches.
  const [theme, setThemeState] = useState<Theme>(readStoredTheme);
  const [systemDark, setSystemDark] = useState(systemPrefersDark);

  useEffect(() => watchSystemTheme(() => setSystemDark(systemPrefersDark())), []);

  const resolvedTheme = theme === 'system' ? (systemDark ? 'dark' : 'light') : theme;

  useEffect(() => {
    applyTheme(resolvedTheme === 'dark');
  }, [resolvedTheme]);

  const setTheme = (next: Theme) => {
    writeStoredTheme(next);
    setThemeState(next);
  };

  return (
    <ThemeContext.Provider value={{ theme, resolvedTheme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};
