import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, SafeAreaView, TextInput, TouchableOpacity, ScrollView, Alert, ActivityIndicator, Switch, Image } from 'react-native';
import { Palette, Eye, Layout, Type, FileText, Check, ArrowLeft, Image as ImageIcon } from 'lucide-react-native';
import { useNavigation } from '@react-navigation/native';
import { writeData, subscribeToPath } from '../services/firebase';

export default function FaturaTasarimScreen({ isTab = false }: { isTab?: boolean }) {
  const navigation = useNavigation();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  // Fatura Tasarımı State'leri
  const [showLogo, setShowLogo] = useState(true);
  const [showBirim, setShowBirim] = useState(true);
  const [showKdv, setShowKdv] = useState(true);
  const [showAraToplam, setShowAraToplam] = useState(true);
  const [showAciklama, setShowAciklama] = useState(true);
  const [baslikText, setBaslikText] = useState('SATIŞ FATURASI');
  const [altBilgi, setAltBilgi] = useState('Mal teslimi ve fatura bedeli ödemesi hakkındadır.');
  const [faturaSize, setFaturaSize] = useState('A4');
  const [faturaOrientation, setFaturaOrientation] = useState('Dikey');
  const [markaRengi, setMarkaRengi] = useState('#2563eb');

  // Firma profili logosunu önizlemek için
  const [logoBase64, setLogoBase64] = useState<string | null>(null);

  const renkSecenekleri = [
    { name: 'Mavi', hex: '#2563eb' },
    { name: 'Yeşil', hex: '#16a34a' },
    { name: 'Kırmızı', hex: '#dc2626' },
    { name: 'Turuncu', hex: '#ea580c' },
    { name: 'Mor', hex: '#8b5cf6' },
    { name: 'Koyu Gri', hex: '#475569' },
  ];

  useEffect(() => {
    // 1. Tasarım ayarlarını RTDB'den çek
    const unsubTasarim = subscribeToPath('FaturaTasarimi/1', (data) => {
      if (data) {
        setShowLogo(data.showLogo !== undefined ? data.showLogo : true);
        setShowBirim(data.showBirim !== undefined ? data.showBirim : true);
        setShowKdv(data.showKdv !== undefined ? data.showKdv : true);
        setShowAraToplam(data.showAraToplam !== undefined ? data.showAraToplam : true);
        setShowAciklama(data.showAciklama !== undefined ? data.showAciklama : true);
        setBaslikText(data.baslikText || 'SATIŞ FATURASI');
        setAltBilgi(data.altBilgi || 'Mal teslimi ve fatura bedeli ödemesi hakkındadır.');
        setFaturaSize(data.faturaSize || 'A4');
        setFaturaOrientation(data.faturaOrientation || 'Dikey');
        setMarkaRengi(data.markaRengi || '#2563eb');
      }
    });

    // 2. Firma profilinden logo'yu çek
    const unsubProfile = subscribeToPath('FirmaProfili/1', (data) => {
      if (data) {
        const logo = data.logoBase64 || data.LogoBase64 || null;
        setLogoBase64(logo);
      }
      setLoading(false);
    });

    return () => {
      unsubTasarim();
      unsubProfile();
    };
  }, []);

  const handleSave = async () => {
    setSaving(true);
    try {
      const payload = {
        id: 1,
        tenantId: 'default',
        showLogo,
        showBirim,
        showKdv,
        showAraToplam,
        showAciklama,
        baslikText,
        altBilgi,
        faturaSize,
        faturaOrientation,
        markaRengi,
      };

      const ok = await writeData('FaturaTasarimi/1', payload);
      if (!ok) {
        Alert.alert('Hata', 'Ayarlar kaydedilemedi. (Bağlantı sorunu — kayıt sıraya alındı.)');
        return;
      }
      Alert.alert('Başarılı', 'Fatura tasarım ayarları kaydedildi.');
    } catch (error) {
      Alert.alert('Hata', 'Ayarlar kaydedilirken bir hata oluştu.');
    } finally {
      setSaving(false);
    }
  };

  const Wrapper = isTab ? View : SafeAreaView;

  if (loading) {
    return (
      <Wrapper style={styles.containerLoading}>
        <ActivityIndicator size="large" color="#0061FF" />
        <Text style={styles.loadingText}>Yükleniyor...</Text>
      </Wrapper>
    );
  }

  return (
    <Wrapper style={styles.container}>
      {!isTab && (
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
            <ArrowLeft color="#FFF" size={24} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Fatura Tasarımı</Text>
          <TouchableOpacity onPress={handleSave} disabled={saving} style={styles.saveButtonHeader}>
            {saving ? (
              <ActivityIndicator size="small" color="#FFF" />
            ) : (
              <Check color="#FFF" size={24} />
            )}
          </TouchableOpacity>
        </View>
      )}

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* LOGO PREVIEW SECTION */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <ImageIcon color="#64748B" size={20} />
            <Text style={styles.cardTitle}>Logo Durumu</Text>
          </View>
          <View style={styles.rowBetween}>
            <Text style={styles.label}>Logoyu PDF Belgelerinde Göster</Text>
            <Switch
              value={showLogo}
              onValueChange={setShowLogo}
              trackColor={{ false: '#334155', true: '#0061FF' }}
              thumbColor={showLogo ? '#FFF' : '#94A3B8'}
            />
          </View>
          {showLogo && (
            <View style={styles.logoPreviewContainer}>
              {logoBase64 ? (
                <Image
                  source={{ uri: `data:image/png;base64,${logoBase64}` }}
                  style={styles.logoImage}
                  resizeMode="contain"
                />
              ) : (
                <View style={styles.noLogoPlaceholder}>
                  <Text style={styles.noLogoText}>Logo Yüklenmemiş (Firma Profilinden ekleyebilirsiniz)</Text>
                </View>
              )}
            </View>
          )}
        </View>

        {/* COLUMNS & LAYOUT SWITCHES */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <Layout color="#64748B" size={20} />
            <Text style={styles.cardTitle}>Belge Sütunları ve Gösterim</Text>
          </View>
          
          <View style={styles.rowBetween}>
            <Text style={styles.label}>Birim / Adet Sütununu Göster</Text>
            <Switch
              value={showBirim}
              onValueChange={setShowBirim}
              trackColor={{ false: '#334155', true: '#0061FF' }}
              thumbColor={showBirim ? '#FFF' : '#94A3B8'}
            />
          </View>
          
          <View style={styles.divider} />

          <View style={styles.rowBetween}>
            <Text style={styles.label}>KDV Sütununu Göster</Text>
            <Switch
              value={showKdv}
              onValueChange={setShowKdv}
              trackColor={{ false: '#334155', true: '#0061FF' }}
              thumbColor={showKdv ? '#FFF' : '#94A3B8'}
            />
          </View>

          <View style={styles.divider} />

          <View style={styles.rowBetween}>
            <Text style={styles.label}>Ara Toplam Sütununu Göster</Text>
            <Switch
              value={showAraToplam}
              onValueChange={setShowAraToplam}
              trackColor={{ false: '#334155', true: '#0061FF' }}
              thumbColor={showAraToplam ? '#FFF' : '#94A3B8'}
            />
          </View>

          <View style={styles.divider} />

          <View style={styles.rowBetween}>
            <Text style={styles.label}>Ürün Açıklamalarını Göster</Text>
            <Switch
              value={showAciklama}
              onValueChange={setShowAciklama}
              trackColor={{ false: '#334155', true: '#0061FF' }}
              thumbColor={showAciklama ? '#FFF' : '#94A3B8'}
            />
          </View>
        </View>

        {/* DOCUMENT HEADERS & TEXT */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <Type color="#64748B" size={20} />
            <Text style={styles.cardTitle}>Metin ve Başlıklar</Text>
          </View>
          
          <Text style={styles.inputLabel}>Fatura Başlığı</Text>
          <TextInput
            value={baslikText}
            onChangeText={setBaslikText}
            style={styles.input}
            placeholder="Örn: SATIŞ FATURASI"
            placeholderTextColor="#475569"
          />

          <Text style={styles.inputLabel}>Fatura Alt Bilgisi / Not</Text>
          <TextInput
            value={altBilgi}
            onChangeText={setAltBilgi}
            style={[styles.input, styles.textArea]}
            placeholder="Faturanın en altında görünecek not..."
            placeholderTextColor="#475569"
            multiline
            numberOfLines={3}
          />
        </View>

        {/* PAGE SIZE & ORIENTATION */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <FileText color="#64748B" size={20} />
            <Text style={styles.cardTitle}>Kağıt Ayarları</Text>
          </View>

          <Text style={styles.inputLabel}>Kağıt Boyutu</Text>
          <View style={styles.row}>
            {['A4', 'A5'].map((size) => (
              <TouchableOpacity
                key={size}
                onPress={() => setFaturaSize(size)}
                style={[
                  styles.optionButton,
                  faturaSize === size && styles.optionButtonActive,
                ]}
              >
                <Text
                  style={[
                    styles.optionButtonText,
                    faturaSize === size && styles.optionButtonTextActive,
                  ]}
                >
                  {size}
                </Text>
              </TouchableOpacity>
            ))}
          </View>

          <Text style={styles.inputLabel}>Kağıt Yönü</Text>
          <View style={styles.row}>
            {['Dikey', 'Yatay'].map((orient) => (
              <TouchableOpacity
                key={orient}
                onPress={() => setFaturaOrientation(orient)}
                style={[
                  styles.optionButton,
                  faturaOrientation === orient && styles.optionButtonActive,
                ]}
              >
                <Text
                  style={[
                    styles.optionButtonText,
                    faturaOrientation === orient && styles.optionButtonTextActive,
                  ]}
                >
                  {orient}
                </Text>
              </TouchableOpacity>
            ))}
          </View>
        </View>

        {/* BRAND COLOR */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <Palette color="#64748B" size={20} />
            <Text style={styles.cardTitle}>Marka Rengi</Text>
          </View>
          <Text style={styles.inputLabel}>Fatura Tema Rengi (Hex Kodu)</Text>
          <View style={styles.colorInputContainer}>
            <View style={[styles.colorBadge, { backgroundColor: markaRengi }]} />
            <TextInput
              value={markaRengi}
              onChangeText={setMarkaRengi}
              style={[styles.input, { flex: 1, marginLeft: 10 }]}
              placeholder="#2563eb"
              placeholderTextColor="#475569"
            />
          </View>

          <View style={styles.colorPalette}>
            {renkSecenekleri.map((c) => (
              <TouchableOpacity
                key={c.hex}
                onPress={() => setMarkaRengi(c.hex)}
                style={[
                  styles.paletteItem,
                  { backgroundColor: c.hex },
                  markaRengi.toLocaleLowerCase('tr-TR') === c.hex.toLocaleLowerCase('tr-TR') && styles.paletteItemActive,
                ]}
              />
            ))}
          </View>
        </View>

        {/* SAVE BUTTON */}
        <TouchableOpacity
          onPress={handleSave}
          disabled={saving}
          style={styles.saveButton}
        >
          {saving ? (
            <ActivityIndicator size="small" color="#FFF" />
          ) : (
            <>
              <Check color="#FFF" size={20} />
              <Text style={styles.saveButtonText}>Tasarımı Kaydet</Text>
            </>
          )}
        </TouchableOpacity>
      </ScrollView>
    </Wrapper>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0A0A0A',
  },
  containerLoading: {
    flex: 1,
    backgroundColor: '#0A0A0A',
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    color: '#94A3B8',
    marginTop: 10,
    fontSize: 15,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    height: 56,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255, 255, 255, 0.08)',
  },
  backButton: {
    padding: 4,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#FFF',
  },
  saveButtonHeader: {
    padding: 4,
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 40,
  },
  card: {
    backgroundColor: '#161616',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.08)',
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  cardTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#F8FAFC',
    marginLeft: 8,
  },
  rowBetween: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 8,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  label: {
    fontSize: 14,
    color: '#CBD5E1',
    flex: 1,
  },
  divider: {
    height: 1,
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    marginVertical: 12,
  },
  logoPreviewContainer: {
    marginTop: 16,
    height: 100,
    borderRadius: 8,
    backgroundColor: '#0F0F0F',
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.08)',
    overflow: 'hidden',
  },
  logoImage: {
    width: '90%',
    height: '90%',
  },
  noLogoPlaceholder: {
    paddingHorizontal: 20,
    alignItems: 'center',
  },
  noLogoText: {
    color: '#64748B',
    fontSize: 12,
    textAlign: 'center',
  },
  inputLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: '#94A3B8',
    marginBottom: 6,
    marginTop: 12,
  },
  input: {
    backgroundColor: '#2A2A2A',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    color: '#F8FAFC',
    borderWidth: 1,
    borderColor: '#444',
    fontSize: 14,
  },
  textArea: {
    height: 80,
    textAlignVertical: 'top',
  },
  optionButton: {
    flex: 1,
    backgroundColor: '#2A2A2A',
    borderWidth: 1,
    borderColor: '#444',
    borderRadius: 8,
    paddingVertical: 12,
    alignItems: 'center',
    marginHorizontal: 4,
    marginTop: 4,
  },
  optionButtonActive: {
    backgroundColor: '#0061FF',
    borderColor: '#0061FF',
  },
  optionButtonText: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#94A3B8',
  },
  optionButtonTextActive: {
    color: '#FFF',
  },
  colorInputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 4,
  },
  colorBadge: {
    width: 40,
    height: 40,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.08)',
  },
  colorPalette: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 16,
  },
  paletteItem: {
    width: 40,
    height: 40,
    borderRadius: 20,
    borderWidth: 2,
    borderColor: 'transparent',
  },
  paletteItemActive: {
    borderColor: '#FFF',
  },
  saveButton: {
    flexDirection: 'row',
    backgroundColor: '#10B981',
    borderRadius: 12,
    paddingVertical: 16,
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 16,
  },
  saveButtonText: {
    color: '#FFF',
    fontSize: 16,
    fontWeight: 'bold',
    marginLeft: 8,
  },
});
