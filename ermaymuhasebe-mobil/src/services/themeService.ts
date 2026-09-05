/**
 * themeService.ts — Masaüstü ThemeService/Display ölçek eşdeğeri.
 * Kullanıcı tercihi AsyncStorage'da saklanır; ekranlar `useUiScale` ile okur.
 */

import AsyncStorage from './storage';

const SCALE_KEY = 'ermay_ui_scale';

export type UiScale = 'kucuk' | 'orta' | 'buyuk';

export const SCALE_FACTORS: Record<UiScale, number> = {
  kucuk: 0.85,
  orta: 1,
  buyuk: 1.2,
};

export const getUiScale = async (): Promise<UiScale> => {
  const raw = await AsyncStorage.getItem(SCALE_KEY);
  if (raw === 'kucuk' || raw === 'buyuk') return raw;
  return 'orta';
};

export const saveUiScale = async (scale: UiScale) => {
  await AsyncStorage.setItem(SCALE_KEY, scale);
};

/** Base font boyutunu ölçekle çarpar (örn. 24 → 20.4). */
export const scaleFont = (base: number, scale: UiScale): number =>
  Math.round(base * SCALE_FACTORS[scale] * 10) / 10;

export const UI_SCALE_KEY = SCALE_KEY;
