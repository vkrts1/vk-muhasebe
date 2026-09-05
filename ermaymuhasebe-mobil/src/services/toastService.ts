let Toast: any = null;
try {
  Toast = require('react-native-toast-message').default;
} catch {
  Toast = null;
}

/**
 * Shows an iOS Liquid Glass Toast Overlay Notification.
 */
export function showSuccessToast(title: string, message?: string) {
  try {
    if (Toast) {
      Toast.show({
        type: 'success',
        text1: title,
        text2: message || '',
        position: 'top',
        visibilityTime: 2500,
      });
    }
  } catch (e) {
    // Graceful fallback
  }
}

export function showErrorToast(title: string, message?: string) {
  try {
    if (Toast) {
      Toast.show({
        type: 'error',
        text1: title,
        text2: message || '',
        position: 'top',
        visibilityTime: 3000,
      });
    }
  } catch (e) {
    // Graceful fallback
  }
}

export function showInfoToast(title: string, message?: string) {
  try {
    if (Toast) {
      Toast.show({
        type: 'info',
        text1: title,
        text2: message || '',
        position: 'top',
        visibilityTime: 2500,
      });
    }
  } catch (e) {
    // Graceful fallback
  }
}
