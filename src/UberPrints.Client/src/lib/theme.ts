export type Theme = 'light' | 'dark' | 'system';

// Keep in sync with the inline script in index.html.
export const THEME_STORAGE_KEY = 'uberprints-theme';

const DARK_QUERY = '(prefers-color-scheme: dark)';

export function readStoredTheme(): Theme {
  try {
    const value = window.localStorage.getItem(THEME_STORAGE_KEY);
    if (value === 'light' || value === 'dark' || value === 'system') return value;
  } catch {
    // Storage blocked (private mode, site data off): fall back to system.
  }
  return 'system';
}

export function writeStoredTheme(theme: Theme) {
  try {
    window.localStorage.setItem(THEME_STORAGE_KEY, theme);
  } catch {
    // Storage blocked: the choice lasts for this page view only.
  }
}

export function systemPrefersDark(): boolean {
  return typeof window.matchMedia === 'function' && window.matchMedia(DARK_QUERY).matches;
}

export function watchSystemTheme(onChange: () => void): () => void {
  if (typeof window.matchMedia !== 'function') return () => {};
  const query = window.matchMedia(DARK_QUERY);
  query.addEventListener('change', onChange);
  return () => query.removeEventListener('change', onChange);
}

export function applyTheme(isDark: boolean) {
  document.documentElement.classList.toggle('dark', isDark);
}
