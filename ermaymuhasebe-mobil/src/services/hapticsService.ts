import { Platform } from 'react-native';

let Haptics: any = null;
try {
  Haptics = require('expo-haptics');
} catch {
  Haptics = null;
}

/**
 * Apple Taptic Engine Feedback Service
 */
export function triggerLightHaptic() {
  try {
    if (Haptics) {
      Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light);
    }
  } catch (e) {
    // Graceful fallback
  }
}

export function triggerMediumHaptic() {
  try {
    if (Haptics) {
      Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
    }
  } catch (e) {
    // Graceful fallback
  }
}

export function triggerSelectionHaptic() {
  try {
    if (Haptics) {
      Haptics.selectionAsync();
    }
  } catch (e) {
    // Graceful fallback
  }
}

export function triggerSuccessHaptic() {
  try {
    if (Haptics) {
      Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
    }
  } catch (e) {
    // Graceful fallback
  }
}

export function triggerErrorHaptic() {
  try {
    if (Haptics) {
      Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error);
    }
  } catch (e) {
    // Graceful fallback
  }
}
