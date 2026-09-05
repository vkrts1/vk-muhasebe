let createZustand: any = null;
try {
  createZustand = require('zustand').create;
} catch {
  createZustand = null;
}

export interface AppStateStore {
  activeCurrency: 'TRY' | 'USD' | 'EUR';
  isBiometricsEnabled: boolean;
  themeMode: 'dark' | 'glass';
  setActiveCurrency: (currency: 'TRY' | 'USD' | 'EUR') => void;
  setBiometricsEnabled: (enabled: boolean) => void;
  setThemeMode: (mode: 'dark' | 'glass') => void;
}

const defaultState: AppStateStore = {
  activeCurrency: 'TRY',
  isBiometricsEnabled: true,
  themeMode: 'glass',
  setActiveCurrency: () => {},
  setBiometricsEnabled: () => {},
  setThemeMode: () => {},
};

export const useAppStateStore = createZustand
  ? createZustand((set: any) => ({
      activeCurrency: 'TRY',
      isBiometricsEnabled: true,
      themeMode: 'glass',
      setActiveCurrency: (currency: 'TRY' | 'USD' | 'EUR') => set({ activeCurrency: currency }),
      setBiometricsEnabled: (enabled: boolean) => set({ isBiometricsEnabled: enabled }),
      setThemeMode: (mode: 'dark' | 'glass') => set({ themeMode: mode }),
    }))
  : (): AppStateStore => defaultState;
