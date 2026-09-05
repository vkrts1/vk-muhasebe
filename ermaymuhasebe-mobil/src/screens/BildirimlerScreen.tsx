import React, { useState, useEffect, useCallback } from 'react';
import { StyleSheet, Text, View, SafeAreaView, ScrollView, ActivityIndicator, TouchableOpacity, RefreshControl } from 'react-native';
import { BellRing, ShieldAlert, PackageX, AlertTriangle, Info, RefreshCw } from 'lucide-react-native';
import AsyncStorage from '../services/storage';
import { subscribeToPath } from '../services/firebase';
import { tumUyarilar, AlertItem, initNotifications, sendLocalNotification } from '../services/alertService';

export default function BildirimlerScreen() {
  const [faturalar, setFaturalar] = useState<any[]>([]);
  const [stoklar, setStoklar] = useState<any[]>([]);
  const [cariler, setCariler] = useState<any[]>([]);
  const [cekler, setCekler] = useState<any[]>([]);
  const [senetler, setSenetler] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    const listHelper = (path: string, setter: React.Dispatch<React.SetStateAction<any[]>>) => {
      return subscribeToPath(path, (data) => {
        if (!data) return setter([]);
        setter(Array.isArray(data) ? data.filter(Boolean) : Object.keys(data).map(key => ({ ...data[key], firebaseKey: key })));
      });
    };

    const unsubFaturalar = listHelper('Faturalar', setFaturalar);
    const unsubStoklar = listHelper('Stoklar', setStoklar);
    const unsubCariler = listHelper('Cariler', setCariler);
    const unsubCekler = listHelper('Cekler', setCekler);
    const unsubSenetler = listHelper('Senetler', setSenetler);

    setTimeout(() => setLoading(false), 800);

    return () => {
      unsubFaturalar();
      unsubStoklar();
      unsubCariler();
      unsubCekler();
      unsubSenetler();
    };
  }, []);

  const alerts: AlertItem[] = tumUyarilar({ faturalar, stoklar, cariler, cekler, senetler });

  // Push bildirim tetikleyicisi
  useEffect(() => {
    if (loading || alerts.length === 0) return;
    
    const triggerNotifications = async () => {
      await initNotifications();
      
      try {
        const stored = await AsyncStorage.getItem('ermay_notified_alerts');
        const notifiedIds: string[] = stored ? JSON.parse(stored) : [];
        const newNotifiedIds = [...notifiedIds];
        let hasNew = false;

        const activeAlerts = alerts.filter(a => a.id !== 'alarm_ozeti' && a.id !== 'gunluk_ozet');

        for (const a of activeAlerts) {
          const alertKey = `${a.id}_${a.mesaj.length}`;
          if (!notifiedIds.includes(alertKey)) {
            await sendLocalNotification(a.baslik, a.mesaj);
            newNotifiedIds.push(alertKey);
            hasNew = true;
          }
        }

        if (hasNew) {
          await AsyncStorage.setItem('ermay_notified_alerts', JSON.stringify(newNotifiedIds));
        }
      } catch (e) {
        console.warn("Notification trigger error:", e);
      }
    };

    triggerNotifications();
  }, [alerts, loading]);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 700);
  }, []);

  const iconFor = (tur: AlertItem['tur']) => {
    if (tur === 'kritik') return <ShieldAlert color="#EF4444" size={20} />;
    if (tur === 'uyari') return <AlertTriangle color="#F59E0B" size={20} />;
    return <Info color="#0061FF" size={20} />;
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <View style={{ flexDirection: 'row', alignItems: 'center' }}>
          <BellRing color="#F59E0B" size={28} style={{ marginRight: 12 }} />
          <Text style={styles.headerTitle}>Bildirim Merkezi</Text>
        </View>
        <Text style={styles.headerSubtitle}>Masaüstü alarm motoru (9 grup) ile anlık uyarılar</Text>
      </View>

      <ScrollView
        contentContainerStyle={{ padding: 20, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor="#0061FF" />}
      >
        <View style={styles.summaryCard}>
          <Text style={styles.summaryTitle}>ALARM ÖZETİ</Text>
          <Text style={styles.summaryText}>
            {alerts.filter(a => a.tur !== 'info').length} uyarı • {alerts.filter(a => a.tur === 'kritik').length} kritik
          </Text>
        </View>

        {loading ? (
          <ActivityIndicator size="large" color="#0061FF" style={{ marginTop: 40 }} />
        ) : alerts.length === 0 ? (
          <View style={styles.emptyCard}>
            <PackageX color="#64748B" size={40} style={{ alignSelf: 'center', marginBottom: 12 }} />
            <Text style={styles.emptyText}>Şu an aktif alarm bulunmuyor.</Text>
          </View>
        ) : (
          alerts.map((a, idx) => (
            <View key={idx} style={[styles.alertCard, a.tur === 'kritik' && styles.alertCardCritical]}>
              <View style={styles.alertRow}>
                {iconFor(a.tur)}
                <View style={{ flex: 1, marginLeft: 12 }}>
                  <Text style={[styles.alertTitle, a.tur === 'kritik' && { color: '#EF4444' }]}>{a.baslik}</Text>
                  <Text style={styles.alertMessage}>{a.mesaj}</Text>
                  <Text style={styles.alertDate}>{a.tarih}</Text>
                </View>
              </View>
            </View>
          ))
        )}

        <TouchableOpacity style={styles.refreshButton} onPress={onRefresh}>
          <RefreshCw color="#FFF" size={18} style={{ marginRight: 8 }} />
          <Text style={styles.refreshText}>Bildirimleri Yenile</Text>
        </TouchableOpacity>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0A0A0A' },
  header: { padding: 20, paddingTop: 40, borderBottomWidth: 1, borderBottomColor: 'rgba(255,255,255,0.08)' },
  headerTitle: { fontSize: 24, fontWeight: '900', color: '#FFF' },
  headerSubtitle: { color: '#64748B', fontSize: 13, marginTop: 4 },
  summaryCard: {
    backgroundColor: '#161616',
    borderWidth: 1,
    borderColor: 'rgba(245,158,11,0.3)',
    borderRadius: 14,
    padding: 16,
    marginBottom: 16,
  },
  summaryTitle: { color: '#F59E0B', fontWeight: '900', fontSize: 13, marginBottom: 6 },
  summaryText: { color: '#E2E8F0', fontSize: 14, fontWeight: '700' },
  alertCard: {
    backgroundColor: '#0F0F0F',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
    borderRadius: 12,
    padding: 14,
    marginBottom: 10,
  },
  alertCardCritical: {
    borderColor: 'rgba(239,68,68,0.35)',
    backgroundColor: '#161616',
  },
  alertRow: { flexDirection: 'row', alignItems: 'flex-start' },
  alertTitle: { color: '#F59E0B', fontWeight: '800', fontSize: 14 },
  alertMessage: { color: '#CBD5E1', fontSize: 13, marginTop: 4 },
  alertDate: { color: '#64748B', fontSize: 11, marginTop: 4 },
  emptyCard: { backgroundColor: '#161616', borderRadius: 12, padding: 40, alignItems: 'center' },
  emptyText: { color: '#94A3B8', fontSize: 14, textAlign: 'center' },
  refreshButton: {
    backgroundColor: '#0061FF',
    borderRadius: 12,
    padding: 14,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 12,
  },
  refreshText: { color: '#FFF', fontWeight: 'bold', fontSize: 14 },
});
