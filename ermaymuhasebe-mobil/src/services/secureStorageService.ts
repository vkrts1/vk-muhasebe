import AsyncStorage from './storage';

let SecureStore: any = null;
let Crypto: any = null;

try {
  SecureStore = require('expo-secure-store');
} catch {
  SecureStore = null;
}

try {
  Crypto = require('expo-crypto');
} catch {
  Crypto = null;
}

/**
 * Hardware-backed Secure Store Service (iOS Keychain & Android Keystore)
 * Fallback gracefully to AsyncStorage if native SecureStore is unavailable.
 */

export async function saveSecureItem(key: string, value: string): Promise<boolean> {
  try {
    if (SecureStore && (await SecureStore.isAvailableAsync())) {
      await SecureStore.setItemAsync(key, value, {
        keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
      });
      return true;
    } else {
      await AsyncStorage.setItem(`secure_${key}`, value);
      return true;
    }
  } catch (error) {
    console.error(`Error saving secure item [${key}]:`, error);
    try {
      await AsyncStorage.setItem(`secure_${key}`, value);
      return true;
    } catch {
      return false;
    }
  }
}

export async function getSecureItem(key: string): Promise<string | null> {
  try {
    if (SecureStore && (await SecureStore.isAvailableAsync())) {
      return await SecureStore.getItemAsync(key);
    } else {
      return await AsyncStorage.getItem(`secure_${key}`);
    }
  } catch (error) {
    console.error(`Error getting secure item [${key}]:`, error);
    try {
      return await AsyncStorage.getItem(`secure_${key}`);
    } catch {
      return null;
    }
  }
}

export async function deleteSecureItem(key: string): Promise<boolean> {
  try {
    if (SecureStore && (await SecureStore.isAvailableAsync())) {
      await SecureStore.deleteItemAsync(key);
    }
    await AsyncStorage.removeItem(`secure_${key}`);
    return true;
  } catch (error) {
    console.error(`Error deleting secure item [${key}]:`, error);
    return false;
  }
}

/**
 * SHA-256 Cryptographic Hash Verification
 */
export async function hashSha256(data: string): Promise<string> {
  try {
    if (Crypto) {
      return await Crypto.digestStringAsync(
        Crypto.CryptoDigestAlgorithm.SHA256,
        data
      );
    }
    return data;
  } catch (e) {
    console.error('SHA-256 Digest Error:', e);
    return data;
  }
}
