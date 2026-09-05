import AsyncStorage from '@react-native-async-storage/async-storage';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

// Hassas anahtarlar listesi - Bunlar SecureStore'da saklanır
const SECURE_KEYS = new Set([
  'ermay_firebase_id_token',
  'ermay_cloud_secret',
  'ermay_auth_token',
  'ermay_user_credentials'
]);

const AsyncStorageWrapper = {
  getItem: async (key: string): Promise<string | null> => {
    try {
      if (SECURE_KEYS.has(key) && Platform.OS !== 'web') {
        const secureVal = await SecureStore.getItemAsync(key);
        if (secureVal !== null) return secureVal;
      }
      return await AsyncStorage.getItem(key);
    } catch (e) {
      return null;
    }
  },
  setItem: async (key: string, value: string): Promise<void> => {
    try {
      if (SECURE_KEYS.has(key) && Platform.OS !== 'web') {
        await SecureStore.setItemAsync(key, value);
      }
      // Yedeklilik ve offline senkronizasyon uyumluluğu için
      await AsyncStorage.setItem(key, value);
    } catch (e) {}
  },
  removeItem: async (key: string): Promise<void> => {
    try {
      if (SECURE_KEYS.has(key) && Platform.OS !== 'web') {
        await SecureStore.deleteItemAsync(key);
      }
      await AsyncStorage.removeItem(key);
    } catch (e) {}
  },
  clear: async (): Promise<void> => {
    try {
      if (Platform.OS !== 'web') {
        for (const k of SECURE_KEYS) {
          try {
            await SecureStore.deleteItemAsync(k);
          } catch (_) {}
        }
      }
      await AsyncStorage.clear();
    } catch (e) {}
  },
  getAllKeys: async (): Promise<readonly string[]> => {
    try {
      return await AsyncStorage.getAllKeys();
    } catch (e) {
      return [];
    }
  },
  multiRemove: async (keys: string[]): Promise<void> => {
    try {
      if (Platform.OS !== 'web') {
        for (const k of keys) {
          if (SECURE_KEYS.has(k)) {
            try {
              await SecureStore.deleteItemAsync(k);
            } catch (_) {}
          }
        }
      }
      await AsyncStorage.multiRemove(keys);
    } catch (e) {}
  }
};

export default AsyncStorageWrapper;

