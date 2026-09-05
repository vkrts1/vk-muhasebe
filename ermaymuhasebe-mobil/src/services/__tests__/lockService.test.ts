/**
 * lockService birim testleri — obfuscation round-trip ve saf helper'lar.
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
import {
  obfuscatePin,
  decodeObfuscatedPin,
  clearLock,
  setLockEnabled,
} from '../lockService';

beforeEach(async () => {
  await AsyncStorage.clear();
});

describe('lockService obfuscation', () => {
  it('pin round-trip korunur', () => {
    const pin = '1234';
    const encoded = obfuscatePin(pin);
    expect(encoded).not.toContain(pin);
    expect(decodeObfuscatedPin(encoded)).toBe(pin);
  });

  it('farkli pinler farkli encode uretir', () => {
    expect(obfuscatePin('1234')).not.toBe(obfuscatePin('5678'));
  });

  it('bos/bozuk base64 icin bos string dondurur', () => {
    expect(decodeObfuscatedPin('%notbase64%')).toBe('');
  });
});

describe('lockService helpers', () => {
  it('setLockEnabled true/false yonetir', async () => {
    await setLockEnabled(true);
    const stored = await AsyncStorage.getItem('ermay_lock_enabled');
    expect(stored).toBe('true');
    await setLockEnabled(false);
    expect(await AsyncStorage.getItem('ermay_lock_enabled')).toBeNull();
  });

  it('clearLock anahtarlari temizler', async () => {
    await AsyncStorage.setItem('ermay_lock_pin', obfuscatePin('0000'));
    await setLockEnabled(true);
    await clearLock();
    expect(await AsyncStorage.getItem('ermay_lock_pin')).toBeNull();
    expect(await AsyncStorage.getItem('ermay_lock_enabled')).toBeNull();
  });
});
