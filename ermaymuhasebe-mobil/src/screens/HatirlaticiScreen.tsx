import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, SafeAreaView, ScrollView, TouchableOpacity, ActivityIndicator, Alert, Share } from 'react-native';
import { BellRing, Share2, AlertTriangle } from 'lucide-react-native';
import { subscribeToPath } from '../services/firebase';

const formatMoney = (val: number) => {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(val);
};

export default function HatirlaticiScreen() {
  const [faturalar, setFaturalar] = useState<any[]>([]);
  const [cariler, setCariler] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const unsubF = subscribeToPath('Faturalar', (data) => {
      if (data) {
        const list = Array.isArray(data) ? data.filter(Boolean) : Object.keys(data).map((key) => ({ ...data[key], firebaseKey: key }));
        setFaturalar(list.filter((f) => !f.isDeleted));
      }
      setLoading(false);
    });
    const unsubC = subscribeToPath('Cariler', (data) => {
      if (data) {
        const list = Array.isArray(data) ? data.filter(Boolean) : Object.keys(data).map((key) => ({ ...data[key], firebaseKey: key }));
        setCariler(list.filter((c) => !c.isDeleted));
      }
    });
    return () => { unsubF(); unsubC(); };
  }, []);

  const today = new Date().toISOString().split('T')[0];

  const gecikenAlis = faturalar.filter((f) =>
    (f.tur === 'Alış' || f.tur === 'Alis') && f.vadeTarihi && f.vadeTarihi < today && (((f.genelToplam || 0) - (f.odenen || 0)) > 0)
  );

  const tedarikciler = cariler
    .filter((c) => (c.alacak || 0) > (c.borc || 0))
    .map((c) => {
      const borc = (c.alacak || 0) - (c.borc || 0);
      const faturalari = gecikenAlis.filter((f) => String(f.cariId) === String(c.id));
      const gecikmeToplami = faturalari.reduce((s, f) => s + ((f.genelToplam || 0) - (f.odenen || 0)), 0);
      return { c, borc, faturalari, gecikmeToplami };
    })
    .sort((a, b) => b.borc - a.borc);

  const toplamGeciken = gecikenAlis.reduce((s, f) => s + ((f.genelToplam || 0) - (f.odenen || 0)), 0);

  const buildMessage = (t: any) => {
    const lines = [
      `Sayın ${t.c.unvan},`,
      '',
      `Taraflarımız arasındaki bakiyeleriniz itibarıyla ${formatMoney(t.borc)} tutarında ödenmemiş borç bakiyeniz bulunmaktadır.`,
    ];
    if (t.faturalari.length > 0) {
      lines.push('');
      lines.push('Vadesi geçmiş faturalarınız:');
      t.faturalari.forEach((f: any) => {
        lines.push(`- ${f.faturaNo} (${f.tarih}): ${formatMoney((f.genelToplam || 0) - (f.odenen || 0))}`);
      });
    }
    lines.push('');
    lines.push('Ödemenizi en kısa sürede gerçekleştirmenizi rica ederiz. Saygılarımızla.');
    return lines.join('\n');
  };

  const handleRemind = async (t: any) => {
    try {
      await Share.share({ message: buildMessage(t) });
    } catch (e) {
      Alert.alert('Hata', 'Hatırlatma paylaşılamadı.');
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 12 }}>
          <BellRing color="#0061FF" size={24} style={{ marginRight: 8 }} />
          <Text style={styles.headerTitle}>Borç Hatırlatıcı</Text>
        </View>
        <Text style={styles.headerSub}>
          Vadesi geçen alış faturası toplamı: {formatMoney(toplamGeciken)}
        </Text>
      </View>

      {loading ? (
        <View style={styles.center}>
          <ActivityIndicator size="large" color="#0061FF" />
        </View>
      ) : (
        <ScrollView contentContainerStyle={styles.content}>
          {tedarikciler.length === 0 && toplamGeciken === 0 ? (
            <Text style={styles.emptyText}>Ödemesi beklenen cari bulunmuyor. 🎉</Text>
          ) : (
            <>
              {toplamGeciken > 0 && (
                <View style={styles.warningBox}>
                  <AlertTriangle color="#F59E0B" size={18} />
                  <Text style={styles.warningText}>{gecikenAlis.length} fatura vadesini geçti ({formatMoney(toplamGeciken)}).</Text>
                </View>
              )}

              {tedarikciler.map((t, idx) => (
                <View key={idx} style={styles.card}>
                  <View style={styles.cardHeader}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardTitle}>{t.c.unvan}</Text>
                      <Text style={styles.cardSub}>
                        Borç: <Text style={{ color: '#EF4444', fontWeight: 'bold' }}>{formatMoney(t.borc)}</Text>
                        {t.gecikmeToplami > 0 && ` • Geciken: ${formatMoney(t.gecikmeToplami)}`}
                      </Text>
                    </View>
                    <TouchableOpacity style={styles.remindBtn} onPress={() => handleRemind(t)}>
                      <Share2 color="#FFF" size={15} />
                      <Text style={styles.remindBtnText}>Hatırlat</Text>
                    </TouchableOpacity>
                  </View>
                  {t.faturalari.slice(0, 5).map((f: any, i: number) => (
                    <View key={i} style={styles.faturaRow}>
                      <Text style={styles.faturaNo}>{f.faturaNo} ({f.tarih})</Text>
                      <Text style={{ color: '#F59E0B', fontSize: 12 }}>{formatMoney((f.genelToplam || 0) - (f.odenen || 0))}</Text>
                    </View>
                  ))}
                </View>
              ))}
            </>
          )}
        </ScrollView>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0A0A0A' },
  header: { padding: 20, paddingTop: 40, borderBottomWidth: 1, borderBottomColor: 'rgba(255,255,255,0.08)' },
  headerTitle: { fontSize: 20, fontWeight: '900', color: '#FFF', flex: 1 },
  headerSub: { color: '#64748B', fontSize: 13, marginTop: 4 },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  content: { padding: 20 },
  emptyText: { color: '#64748B', textAlign: 'center', marginTop: 40 },
  warningBox: { flexDirection: 'row', alignItems: 'center', backgroundColor: 'rgba(245,158,11,0.1)', borderRadius: 12, padding: 12, marginBottom: 16, gap: 8 },
  warningText: { color: '#F59E0B', fontSize: 13, flex: 1 },
  card: { backgroundColor: '#0F0F0F', borderRadius: 16, borderWidth: 1, borderColor: 'rgba(255,255,255,0.08)', padding: 16, marginBottom: 12 },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start' },
  cardTitle: { fontSize: 16, fontWeight: 'bold', color: '#FFF' },
  cardSub: { color: '#94A3B8', fontSize: 12, marginTop: 4 },
  remindBtn: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#0061FF', paddingHorizontal: 10, paddingVertical: 8, borderRadius: 8, gap: 5 },
  remindBtnText: { color: '#FFF', fontWeight: 'bold', fontSize: 12 },
  faturaRow: { flexDirection: 'row', justifyContent: 'space-between', marginTop: 8, paddingTop: 8, borderTopWidth: 1, borderTopColor: 'rgba(255,255,255,0.05)' },
  faturaNo: { color: '#94A3B8', fontSize: 12 },
});
