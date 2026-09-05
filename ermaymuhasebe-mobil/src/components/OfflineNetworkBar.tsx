import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { WifiOff } from 'lucide-react-native';
import { useNetworkStatus } from '../services/networkService';

export function OfflineNetworkBar() {
  const { isConnected } = useNetworkStatus();

  if (isConnected) return null;

  return (
    <View style={styles.offlineBanner}>
      <WifiOff color="#FF453A" size={14} />
      <Text style={styles.offlineText}>
        ⚠️ İnternet Bağlantısı Yok — Çevrimdışı Mod (İşlemler Kuyruğa Alındı)
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  offlineBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    backgroundColor: 'rgba(255, 69, 58, 0.18)',
    borderWidth: 1,
    borderColor: 'rgba(255, 69, 58, 0.35)',
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 12,
    marginHorizontal: 16,
    marginTop: 8,
    marginBottom: 4,
  },
  offlineText: {
    color: '#FF453A',
    fontSize: 11,
    fontWeight: '700',
  },
});
