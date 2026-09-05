import { useState, useEffect } from 'react';

let NetInfo: any = null;
try {
  NetInfo = require('@react-native-community/netinfo');
} catch {
  NetInfo = null;
}

export interface NetworkState {
  isConnected: boolean;
  isInternetReachable: boolean;
}

/**
 * Checks current network connectivity status.
 */
export async function getNetworkStatus(): Promise<NetworkState> {
  try {
    if (NetInfo) {
      const state = await NetInfo.fetch();
      return {
        isConnected: !!state.isConnected,
        isInternetReachable: state.isInternetReachable !== false,
      };
    }
    return { isConnected: true, isInternetReachable: true };
  } catch (e) {
    return { isConnected: true, isInternetReachable: true };
  }
}

/**
 * React hook to subscribe to live network connectivity changes.
 */
export function useNetworkStatus(): NetworkState {
  const [status, setStatus] = useState<NetworkState>({
    isConnected: true,
    isInternetReachable: true,
  });

  useEffect(() => {
    if (!NetInfo) return;

    const unsubscribe = NetInfo.addEventListener((state: any) => {
      setStatus({
        isConnected: !!state.isConnected,
        isInternetReachable: state.isInternetReachable !== false,
      });
    });

    return () => {
      unsubscribe();
    };
  }, []);

  return status;
}
