let LocalAuthentication: any = null;
try {
  LocalAuthentication = require('expo-local-authentication');
} catch {
  LocalAuthentication = null;
}

export interface BiometricCheckResult {
  isAvailable: boolean;
  biometricType: 'Face ID' | 'Touch ID' | 'Biometrics' | 'None';
}

/**
 * Checks if iOS Face ID or Touch ID hardware is available and enrolled.
 */
export async function checkBiometricsAvailability(): Promise<BiometricCheckResult> {
  try {
    if (!LocalAuthentication) {
      return { isAvailable: false, biometricType: 'None' };
    }

    const hasHardware = await LocalAuthentication.hasHardwareAsync();
    const isEnrolled = await LocalAuthentication.isEnrolledAsync();

    if (!hasHardware || !isEnrolled) {
      return { isAvailable: false, biometricType: 'None' };
    }

    const types = await LocalAuthentication.supportedAuthenticationTypesAsync();
    let biometricType: 'Face ID' | 'Touch ID' | 'Biometrics' = 'Biometrics';

    if (types.includes(LocalAuthentication.AuthenticationType.FACIAL_RECOGNITION)) {
      biometricType = 'Face ID';
    } else if (types.includes(LocalAuthentication.AuthenticationType.FINGERPRINT)) {
      biometricType = 'Touch ID';
    }

    return { isAvailable: true, biometricType };
  } catch (e) {
    console.error('Biometrics check error:', e);
    return { isAvailable: false, biometricType: 'None' };
  }
}

/**
 * Prompts user for iOS Face ID / Touch ID authentication.
 */
export async function authenticateWithBiometrics(
  promptMessage: string = 'Ermay Muhasebe kilidini açmak için Face ID doğrulayın'
): Promise<boolean> {
  try {
    if (!LocalAuthentication) return false;

    const result = await LocalAuthentication.authenticateAsync({
      promptMessage,
      cancelLabel: 'İptal',
      fallbackLabel: 'Şifre Girin',
      disableDeviceFallback: false,
    });

    return result.success;
  } catch (e) {
    console.error('Biometric authentication error:', e);
    return false;
  }
}
