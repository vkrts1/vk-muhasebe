/**
 * lockService.ts — Uygulama içi oturum kilidi (masaüstü 30 dk oturum zaman aşımı paritesi).
 *
 * PIN, masaüstü AuthService.Encrypt (XOR + base64) yöntemiyle hafif obfuscate
 * edilerek AsyncStorage'da saklanır. Bu bir gerçek şifreleme değildir; amaç
 * görünür düz metni gizlemektir.
 */

import AsyncStorage from './storage';

const PIN_KEY = 'ermay_lock_pin';
const ENABLED_KEY = 'ermay_lock_enabled';
const LOCK_KEY = 'ERMAY-SECURE-KEY-2025';

export const DEFAULT_LOCK_MINUTES = 30;
const TIMEOUT_KEY = 'ermay_lock_timeout';

const xorBytes = (text: string): string => {
  const bytes = Array.from(text).map((ch, i) =>
    String.fromCharCode(ch.charCodeAt(0) ^ LOCK_KEY.charCodeAt(i % LOCK_KEY.length))
  );
  return btoa(bytes.join(''));
};

const xorDecode = (b64: string): string => {
  try {
    const bytes = atob(b64);
    return Array.from(bytes)
      .map((ch, i) => String.fromCharCode(ch.charCodeAt(0) ^ LOCK_KEY.charCodeAt(i % LOCK_KEY.length)))
      .join('');
  } catch {
    return '';
  }
};

export const obfuscatePin = (pin: string): string => xorBytes(pin);
export const decodeObfuscatedPin = (encoded: string): string => xorDecode(encoded);

export interface LockSettings {
  enabled: boolean;
  hasPin: boolean;
  timeoutMinutes: number;
}

export const getLockSettings = async (): Promise<LockSettings> => {
  const [enabled, pin, timeoutRaw] = await Promise.all([
    AsyncStorage.getItem(ENABLED_KEY),
    AsyncStorage.getItem(PIN_KEY),
    AsyncStorage.getItem(TIMEOUT_KEY),
  ]);
  return { 
    enabled: enabled === 'true', 
    hasPin: !!pin,
    timeoutMinutes: timeoutRaw ? parseInt(timeoutRaw, 10) : DEFAULT_LOCK_MINUTES 
  };
};

export const setLockTimeout = async (minutes: number) => {
  await AsyncStorage.setItem(TIMEOUT_KEY, minutes.toString());
};

export const savePin = async (pin: string) => {
  await AsyncStorage.setItem(PIN_KEY, obfuscatePin(pin));
};

export const setLockEnabled = async (enabled: boolean) => {
  if (enabled) await AsyncStorage.setItem(ENABLED_KEY, 'true');
  else await AsyncStorage.removeItem(ENABLED_KEY);
};

export const clearLock = async () => {
  await AsyncStorage.removeItem(PIN_KEY);
  await AsyncStorage.removeItem(ENABLED_KEY);
};

export const verifyPin = async (pin: string): Promise<boolean> => {
  const stored = await AsyncStorage.getItem(PIN_KEY);
  if (!stored) return false;
  return decodeObfuscatedPin(stored) === pin;
};

export const LOCK_LAST_BACKGROUND_KEY = 'ermay_last_background';

export const recordBackground = async () => {
  await AsyncStorage.setItem(LOCK_LAST_BACKGROUND_KEY, Date.now().toString());
};

export const clearBackgroundRecord = async () => {
  await AsyncStorage.removeItem(LOCK_LAST_BACKGROUND_KEY);
};

/** Foreground'a dönüşte kilit gerekli mi? (arka planda geçen süre >= dakika) */
export const shouldLock = async (): Promise<boolean> => {
  const [enabled, lastRaw, pin, timeoutRaw] = await Promise.all([
    AsyncStorage.getItem(ENABLED_KEY),
    AsyncStorage.getItem(LOCK_LAST_BACKGROUND_KEY),
    AsyncStorage.getItem(PIN_KEY),
    AsyncStorage.getItem(TIMEOUT_KEY),
  ]);
  if (enabled !== 'true' || !pin) return false;
  if (!lastRaw) return false;
  const last = parseInt(lastRaw, 10);
  if (Number.isNaN(last)) return false;
  const minutes = timeoutRaw ? parseInt(timeoutRaw, 10) : DEFAULT_LOCK_MINUTES;
  return Date.now() - last >= minutes * 60000;
};
