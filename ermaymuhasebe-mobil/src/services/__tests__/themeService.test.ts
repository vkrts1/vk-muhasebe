/**
 * themeService birim testleri — ölçek tercihi ve font çarpanı.
 */
const memoryStore: Record<string, string> = {};
jest.mock('@react-native-async-storage/async-storage', () => ({
  __esModule: true,
  default: {
    getItem: jest.fn(async (k: string) => (k in memoryStore ? memoryStore[k] : null)),
    setItem: jest.fn(async (k: string, v: string) => { memoryStore[k] = v; }),
    removeItem: jest.fn(async (k: string) => { delete memoryStore[k]; }),
    clear: jest.fn(async () => { Object.keys(memoryStore).forEach(k => delete memoryStore[k]); }),
  },
}));

import AsyncStorage from '../storage';
import { getUiScale, saveUiScale, scaleFont, SCALE_FACTORS } from '../themeService';

beforeEach(async () => {
  await AsyncStorage.clear();
});

describe('getUiScale', () => {
  it('kayit yoksa orta doner', async () => {
    expect(await getUiScale()).toBe('orta');
  });

  it('gecerli tercihi dondurur, gecersiz deger orta olur', async () => {
    await saveUiScale('buyuk');
    expect(await getUiScale()).toBe('buyuk');
    await AsyncStorage.setItem('ermay_ui_scale', 'dev');
    expect(await getUiScale()).toBe('orta');
  });
});

describe('scaleFont', () => {
  it('faktore gore yuvarlar', () => {
    expect(scaleFont(24, 'kucuk')).toBe(20.4);
    expect(scaleFont(24, 'orta')).toBe(24);
    expect(scaleFont(24, 'buyuk')).toBe(28.8);
  });

  it('faktör tablosu tutarli', () => {
    expect(SCALE_FACTORS.kucuk).toBeLessThan(SCALE_FACTORS.orta);
    expect(SCALE_FACTORS.buyuk).toBeGreaterThan(SCALE_FACTORS.orta);
  });
});
