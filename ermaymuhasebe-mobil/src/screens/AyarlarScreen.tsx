import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, SafeAreaView, TextInput, TouchableOpacity, ScrollView, Alert, ActivityIndicator, Share } from 'react-native';
import { Settings, Globe, Key, Calendar, Wifi, Save, Database, User, MapPin, Phone, Building, Lock, MonitorSmartphone, ArrowLeft, Trash2 } from 'lucide-react-native';
import AsyncStorage from '../services/storage';
import { saveFirebaseConfig, saveActiveYear, getFirebaseConfig, loadConfigFromStorage, goOfflineMode, writeData, subscribeToPath, logoutUser, deleteData, readData, fetchAvailableYears } from '../services/firebase';
import { getLockSettings, savePin, setLockEnabled, clearLock, setLockTimeout, DEFAULT_LOCK_MINUTES } from '../services/lockService';
import { getUiScale, saveUiScale, UiScale } from '../services/themeService';

export default function AyarlarScreen() {
  const [dbUrl, setDbUrl] = useState('');
  const [secret, setSecret] = useState('');
  const [activeYear, setActiveYear] = useState('');
  const [availableYears, setAvailableYears] = useState<string[]>([]);
  const [pdfServerUrl, setPdfServerUrl] = useState('');
  const [loading, setLoading] = useState(true);

  // Firma Profili State
  const [firmaUnvan, setFirmaUnvan] = useState('');
  const [firmaYetkili, setFirmaYetkili] = useState('');
  const [firmaTelefon, setFirmaTelefon] = useState('');
  const [firmaAdres, setFirmaAdres] = useState('');
  const [firmaVergiDairesi, setFirmaVergiDairesi] = useState('');
  const [firmaVergiNo, setFirmaVergiNo] = useState('');
  const [firmaEposta, setFirmaEposta] = useState('');
  const [firmaWebSitesi, setFirmaWebSitesi] = useState('');

  // Yedek Geri Yükleme JSON State
  const [backupJsonInput, setBackupJsonInput] = useState('');

  // Oturum Kilidi State
  const [lockEnabled, setLockEnabledState] = useState(false);
  const [lockHasPin, setLockHasPin] = useState(false);
  const [newPin, setNewPin] = useState('');
  const [lockTimeout, setLockTimeoutState] = useState('30');

  // Görünüm Ölçeği State
  const [uiScale, setUiScaleState] = useState<UiScale>('orta');

  // Kategori Seçim State
  const [currentCategory, setCurrentCategory] = useState<string | null>(null);

  useEffect(() => {
    const fetchSettings = async () => {
      await loadConfigFromStorage();
      const config = getFirebaseConfig();
      if (config) {
        setDbUrl(config.url || '');
        setSecret(config.secret || '');
      }

      const years = await fetchAvailableYears();
      if (years && years.length > 0) setAvailableYears(years);
      const year = await AsyncStorage.getItem('ermay_active_year');
      setActiveYear(year || new Date().getFullYear().toString());

      const savedPdfUrl = await AsyncStorage.getItem('pdf_server_url');
      setPdfServerUrl(savedPdfUrl || 'https://ermay-pdf-api-916435485627.europe-west1.run.app');

      // Oturum kilidi ayarını yükle
      const lockSettings = await getLockSettings();
      setLockEnabledState(lockSettings.enabled);
      setLockHasPin(lockSettings.hasPin);
      setLockTimeoutState(lockSettings.timeoutMinutes.toString());

      // Görünüm ölçeği tercihi
      setUiScaleState(await getUiScale());

      // Firma Profilini Firebase'den çek
      const unsubProfile = subscribeToPath('FirmaProfili/1', (data) => {
        if (data) {
          setFirmaUnvan(data.firmaAdi || data.unvan || '');
          setFirmaYetkili(data.yetkili || '');
          setFirmaTelefon(data.telefon || '');
          setFirmaAdres(data.adres || '');
          setFirmaVergiDairesi(data.vergiDairesi || '');
          setFirmaVergiNo(data.vergiNo || '');
          setFirmaEposta(data.eposta || data.email || '');
          setFirmaWebSitesi(data.webSitesi || '');
        }
      });

      setLoading(false);
      return () => unsubProfile();
    };

    fetchSettings();
  }, []);

  const handleSave = async () => {
    if (!dbUrl) {
      Alert.alert('Hata', 'Firebase URL alanı zorunludur.');
      return;
    }

    setLoading(true);
    try {
      await saveFirebaseConfig(dbUrl, secret);
      await saveActiveYear(activeYear);
      await AsyncStorage.setItem('pdf_server_url', pdfServerUrl);

      // Firma Profilini Firebase'e Kaydet
      const profilePayload = {
        id: 1,
        firmaAdi: firmaUnvan,
        unvan: firmaUnvan,
        yetkili: firmaYetkili,
        telefon: firmaTelefon,
        adres: firmaAdres,
        vergiDairesi: firmaVergiDairesi,
        vergiNo: firmaVergiNo,
        eposta: firmaEposta,
        email: firmaEposta,
        webSitesi: firmaWebSitesi,
      };
      const ok = await writeData('FirmaProfili/1', profilePayload);
      if (!ok) {
        Alert.alert('Hata', 'Ayarlar kaydedilemedi. (Bağlantı sorunu — kayıt sıraya alındı.)');
        return;
      }

      Alert.alert('Başarılı', 'Sistem ayarları ve firma profili kaydedildi.');
    } catch (e) {
      Alert.alert('Hata', 'Ayarlar kaydedilirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  // Veritabanı Yedeğini JSON Olarak Paylaş (Export Backup)
  const handleExportBackup = async () => {
    setLoading(true);
    try {
      Alert.alert('Bilgi', 'Veri tabanı yedek dosyası (JSON) oluşturuluyor...');
      const tables = [
        'FirmaProfili', 'users', 'Stoklar', 'Cariler', 'Faturalar', 
        'CariHareketler', 'StokHareketler', 'Siparisler', 'Teklifler', 
        'Bankalar', 'Cekler', 'Senetler', 'KrediKartlari', 'EftIslemleri', 
        'StokSayimlar', 'PortfoyKartlari', 'SatisHedefleri', 'Notes', 
        'Gorevler', 'BelgeArsiv'
      ];
      
      const backupData: any = {};
      for (const table of tables) {
        const raw = await readData(table);
        if (raw) {
          backupData[table] = raw;
        }
      }
      
      const jsonStr = JSON.stringify(backupData, null, 2);
      await Share.share({
        message: jsonStr,
        title: 'Ermay Muhasebe Sistem Yedeği',
      });
    } catch (error) {
      Alert.alert('Hata', 'Yedek dışa aktarılamadı.');
    } finally {
      setLoading(false);
    }
  };

  // JSON Yapıştırarak Veritabanı Yedeğini Yükle (Import Backup)
  const handleImportBackup = async () => {
    if (!backupJsonInput.trim()) {
      Alert.alert('Hata', 'Lütfen geçerli bir JSON yedek verisi yapıştırın.');
      return;
    }
    setLoading(true);
    try {
      const parsedData = JSON.parse(backupJsonInput);
      for (const key of Object.keys(parsedData)) {
        const ok = await writeData(key, parsedData[key]);
        if (!ok) {
          Alert.alert('Hata', `"${key}" geri yüklenemedi. (Bağlantı sorunu — tablo sıraya alındı.)`);
        }
      }
      setBackupJsonInput('');
      Alert.alert('Başarılı', 'Yedek veri tabanına başarıyla geri yüklendi.');
    } catch (e) {
      Alert.alert('Hata', 'Geçersiz JSON formatı. Lütfen yedek dosyasını doğru yapıştırdığınızdan emin olun.');
    } finally {
      setLoading(false);
    }
  };

  // Veri Temizliği (Soft-Deleted kalıcı temizleme)
  const handleDataCleanup = async () => {
    Alert.alert(
      'Veri Temizliği (Kalıcı Silme)',
      'Veritabanında "silindi" olarak işaretlenmiş (soft-deleted) tüm kayıtlar kalıcı olarak silinecektir. Bu işlem geri alınamaz. Emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Evet, Kalıcı Sil',
          style: 'destructive',
          onPress: async () => {
            setLoading(true);
            try {
              const tables = [
                'Stoklar', 'Cariler', 'Faturalar', 'CariHareketler', 'StokHareketler',
                'Siparisler', 'Teklifler', 'Bankalar', 'Cekler', 'Senetler',
                'KrediKartlari', 'EftIslemleri', 'StokSayimlar', 'PortfoyKartlari',
                'SatisHedefleri', 'Notes', 'Gorevler', 'BelgeArsiv'
              ];
              
              let deletedCount = 0;
              for (const table of tables) {
                const data = await readData(table);
                if (data) {
                  const keys = Object.keys(data);
                  for (const key of keys) {
                    const item = data[key];
                    if (item && (item.isDeleted === true || item.IsDeleted === true)) {
                      await deleteData(`${table}/${key}`);
                      deletedCount++;
                    }
                  }
                }
              }
              Alert.alert('Başarılı', `Veri temizliği tamamlandı. Toplam ${deletedCount} adet çöp kayıt kalıcı olarak temizlendi.`);
            } catch (e) {
              Alert.alert('Hata', 'Veri temizlenirken bir sorun oluştu.');
            } finally {
              setLoading(false);
            }
          }
        }
      ]
    );
  };
  

  const handleSaveLock = async () => {
    const trimmed = newPin.trim();
    if (trimmed.length < 4 || trimmed.length > 6) {
      Alert.alert('Hata', 'PIN 4-6 haneli olmalıdır.');
      return;
    }
    const timeoutVal = parseInt(lockTimeout, 10);
    if (isNaN(timeoutVal) || timeoutVal < 1 || timeoutVal > 1440) {
      Alert.alert('Hata', 'Zaman aşımı 1 ile 1440 dakika arasında olmalıdır.');
      return;
    }
    
    setLoading(true);
    try {
      await savePin(trimmed);
      await setLockEnabled(true);
      await setLockTimeout(timeoutVal);
      setLockEnabledState(true);
      setLockHasPin(true);
      setNewPin('');
      Alert.alert('Başarılı', `Oturum kilidi aktif. ${timeoutVal} dk arka planda kalınca PIN sorulur.`);
    } catch (e) {
      Alert.alert('Hata', 'Kilit ayarları kaydedilirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateTimeoutOnly = async () => {
    const timeoutVal = parseInt(lockTimeout, 10);
    if (isNaN(timeoutVal) || timeoutVal < 1 || timeoutVal > 1440) {
      Alert.alert('Hata', 'Zaman aşımı 1 ile 1440 dakika arasında olmalıdır.');
      return;
    }
    setLoading(true);
    try {
      await setLockTimeout(timeoutVal);
      Alert.alert('Başarılı', `Zaman aşımı güncellendi. ${timeoutVal} dk arka planda kalınca PIN sorulur.`);
    } catch (e) {
      Alert.alert('Hata', 'Zaman aşımı kaydedilirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  const handleDisableLock = async () => {
    Alert.alert(
      'Oturum Kilidini Kaldır',
      'Mevcut PIN silinecek ve kilit özelliği kapatılacak. Emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Evet, Kaldır',
          style: 'destructive',
          onPress: async () => {
            await clearLock();
            setLockEnabledState(false);
            setLockHasPin(false);
            setNewPin('');
            Alert.alert('Başarılı', 'Oturum kilidi kaldırıldı.');
          }
        }
      ]
    );
  };

  // Veri Temizlik: yerel önbellek ve bekleyen yazma kuyruğunu temizle
  const handleClearLocalCache = async () => {
    Alert.alert(
      'Yerel Verileri Temizle',
      'Uygulamanın cihazdaki önbelleği (lastik veri anlık görüntüleri) ve bekleyen yazma kuyruğu silinecek. Bulut verileri etkilenmez. Emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Evet, Temizle',
          style: 'destructive',
          onPress: async () => {
            try {
              const keys = await AsyncStorage.getAllKeys();
              const toRemove = keys.filter(k => k.startsWith('ermay_cache_') || k === 'ermay_write_queue');
              if (toRemove.length > 0) await AsyncStorage.multiRemove(toRemove);
              Alert.alert('Başarılı', `${toRemove.length} önbellek kaydı temizlendi.`);
            } catch (e) {
              Alert.alert('Hata', 'Önbellek temizlenirken hata oluştu.');
            }
          }
        }
      ]
    );
  };

  const handleSaveUiScale = async (scale: UiScale) => {
    await saveUiScale(scale);
    setUiScaleState(scale);
    Alert.alert('Başarılı', 'Görünüm ölçeği güncellendi.');
  };

  const handleLogout = async () => {
    Alert.alert(
      'Yapılandırmayı Sıfırla',
      'Firebase yapılandırmasını silmek istediğinize emin misiniz? Uygulama offline moda geçecektir.',
      [
        { text: 'İptal', style: 'cancel' },
        { 
          text: 'Evet, Sıfırla', 
          style: 'destructive',
          onPress: async () => {
            await goOfflineMode();
            setDbUrl('');
            setSecret('');
            setActiveYear(new Date().getFullYear().toString());
            setPdfServerUrl('http://192.168.1.103:5244');
            Alert.alert('Başarılı', 'Ayarlar temizlendi.');
          }
        }
      ]
    );
  };

  const handleUserLogout = async () => {
    Alert.alert(
      'Oturumu Kapat',
      'Hesap oturumunuz kapatılacak. Emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        { 
          text: 'Evet, Kapat', 
          style: 'destructive',
          onPress: async () => {
            await logoutUser();
          }
        }
      ]
    );
  };

  if (loading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.center}>
          <ActivityIndicator size="large" color="#0061FF" />
        </View>
      </SafeAreaView>
    );
  }

  const categories = [
    { id: 'cloud', title: 'Bulut ve API', description: 'Firebase ve PDF servis bağlantılarını yönetin.', icon: Globe, color: '#F59E0B' },
    { id: 'profile', title: 'Firma Profili', description: 'Firma ünvanı, iletişim ve logo ayarları.', icon: Building, color: '#8B5CF6' },
    { id: 'security', title: 'Güvenlik ve Kilit', description: 'Oturum kilidi, PIN ve şifre ayarları.', icon: Lock, color: '#EF4444' },
    { id: 'backup', title: 'Yedekleme ve Bakım', description: 'Tüm veritabanını yedekleyin veya geri yükleyin.', icon: Database, color: '#10B981' },
    { id: 'theme', title: 'Arayüz Ölçekleme', description: 'Ekran yazı boyutu ve arayüz ölçeği.', icon: MonitorSmartphone, color: '#3B82F6' },
    { id: 'cleanup', title: 'Veri Temizlik', description: 'Silinmiş çöp kayıtları ve yerel önbelleği temizleyin.', icon: Trash2, color: '#64748B' }
  ];

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <View style={{ flexDirection: 'row', alignItems: 'center' }}>
          <Settings color="#0061FF" size={28} style={{ marginRight: 12 }} />
          <Text style={styles.headerTitle}>
            {currentCategory ? categories.find(c => c.id === currentCategory)?.title : 'Sistem Ayarları'}
          </Text>
        </View>
        <Text style={styles.headerSubtitle}>
          {currentCategory ? categories.find(c => c.id === currentCategory)?.description : 'Firebase sunucu ve firma profili yapılandırması'}
        </Text>
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {currentCategory === null ? (
          <View style={styles.categoriesGrid}>
            {categories.map((cat) => {
              const Icon = cat.icon;
              return (
                <TouchableOpacity
                  key={cat.id}
                  style={styles.categoryCard}
                  onPress={() => setCurrentCategory(cat.id)}
                >
                  <View style={[styles.categoryIconBox, { backgroundColor: `${cat.color}15` }]}>
                    <Icon color={cat.color} size={24} />
                  </View>
                  <Text style={styles.categoryTitle}>{cat.title}</Text>
                  <Text style={styles.categoryDesc} numberOfLines={2}>{cat.description}</Text>
                </TouchableOpacity>
              );
            })}
            
            {/* Alt İşlemler */}
            <View style={{ width: '100%', marginTop: 20 }}>
              <TouchableOpacity style={[styles.logoutButton, { borderColor: '#E2E8F0' }]} onPress={handleUserLogout}>
                <Text style={[styles.logoutButtonText, { color: '#E2E8F0' }]}>Oturumu Kapat (Çıkış Yap)</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
                <Text style={styles.logoutButtonText}>Yapılandırmayı Sıfırla (Offline Mod)</Text>
              </TouchableOpacity>
            </View>
          </View>
        ) : (
          <>
            {/* 1. Bulut ve API */}
            {currentCategory === 'cloud' && (
              <View style={styles.card}>
                <View style={styles.cardHeader}>
                  <Database color="#0061FF" size={22} />
                  <Text style={styles.cardTitle}>Firebase Sunucu Ayarları</Text>
                </View>

                <View style={styles.inputGroup}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
                    <Globe color="#94A3B8" size={16} style={{ marginRight: 6 }} />
                    <Text style={styles.inputLabel}>Firebase Realtime Database URL</Text>
                  </View>
                  <TextInput 
                    style={styles.input}
                    placeholder="https://your-app-default-rtdb.firebaseio.com"
                    placeholderTextColor="#64748B"
                    value={dbUrl}
                    onChangeText={setDbUrl}
                    autoCapitalize="none"
                    autoCorrect={false}
                  />
                </View>

                <View style={styles.inputGroup}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
                    <Key color="#94A3B8" size={16} style={{ marginRight: 6 }} />
                    <Text style={styles.inputLabel}>Firebase Database Secret (Opsiyonel)</Text>
                  </View>
                  <TextInput 
                    style={styles.input}
                    placeholder="Database Secret Auth Token"
                    placeholderTextColor="#64748B"
                    value={secret}
                    onChangeText={setSecret}
                    secureTextEntry={true}
                    autoCapitalize="none"
                    autoCorrect={false}
                  />
                </View>

                <View style={styles.inputGroup}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
                    <Key color="#94A3B8" size={16} style={{ marginRight: 6 }} />
                    <Text style={styles.inputLabel}>PDF Rapor Sunucu Adresi</Text>
                  </View>
                  <TextInput 
                    style={styles.input}
                    placeholder="http://192.168.1.103:5244"
                    placeholderTextColor="#64748B"
                    value={pdfServerUrl}
                    onChangeText={setPdfServerUrl}
                    autoCapitalize="none"
                    autoCorrect={false}
                  />
                </View>

                <TouchableOpacity style={styles.saveButton} onPress={handleSave}>
                  <Save color="#FFF" size={20} style={{ marginRight: 8 }} />
                  <Text style={styles.saveButtonText}>Bağlantıları Kaydet</Text>
                </TouchableOpacity>
              </View>
            )}

            {/* 2. Firma Profili */}
            {currentCategory === 'profile' && (
              <View style={styles.card}>
                <View style={styles.cardHeader}>
                  <Building color="#8B5CF6" size={22} />
                  <Text style={styles.cardTitle}>Firma Profili Bilgileri</Text>
                </View>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>Firma Ünvanı</Text>
                  <TextInput style={styles.input} placeholder="Firma Tam Adı..." placeholderTextColor="#64748B" value={firmaUnvan} onChangeText={setFirmaUnvan} />
                </View>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>Firma Yetkilisi</Text>
                  <TextInput style={styles.input} placeholder="Yetkili Adı Soyadı..." placeholderTextColor="#64748B" value={firmaYetkili} onChangeText={setFirmaYetkili} />
                </View>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>Telefon</Text>
                  <TextInput style={styles.input} placeholder="Firma Telefonu..." placeholderTextColor="#64748B" value={firmaTelefon} onChangeText={setFirmaTelefon} />
                </View>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>Vergi Dairesi / No</Text>
                  <View style={{ flexDirection: 'row', gap: 10 }}>
                    <TextInput style={[styles.input, { flex: 1 }]} placeholder="Vergi Dairesi..." placeholderTextColor="#64748B" value={firmaVergiDairesi} onChangeText={setFirmaVergiDairesi} />
                    <TextInput style={[styles.input, { flex: 1 }]} placeholder="Vergi No..." placeholderTextColor="#64748B" value={firmaVergiNo} onChangeText={setFirmaVergiNo} />
                  </View>
                </View>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>E-Posta</Text>
                  <TextInput style={styles.input} placeholder="Firma E-Posta Adresi..." placeholderTextColor="#64748B" value={firmaEposta} onChangeText={setFirmaEposta} keyboardType="email-address" autoCapitalize="none" />
                </View>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>Web Sitesi</Text>
                  <TextInput style={styles.input} placeholder="Firma Web Sitesi..." placeholderTextColor="#64748B" value={firmaWebSitesi} onChangeText={setFirmaWebSitesi} keyboardType="url" autoCapitalize="none" />
                </View>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>Adres</Text>
                  <TextInput style={[styles.input, { height: 60 }]} multiline={true} placeholder="Firma Adresi..." placeholderTextColor="#64748B" value={firmaAdres} onChangeText={setFirmaAdres} />
                </View>

                <TouchableOpacity style={styles.saveButton} onPress={handleSave}>
                  <Save color="#FFF" size={20} style={{ marginRight: 8 }} />
                  <Text style={styles.saveButtonText}>Firma Profilini Kaydet</Text>
                </TouchableOpacity>
              </View>
            )}



            {/* 4. Güvenlik ve Kilit */}
            {currentCategory === 'security' && (
              <View style={styles.card}>
                <View style={styles.cardHeader}>
                  <Lock color="#EF4444" size={22} />
                  <Text style={styles.cardTitle}>Oturum Kilidi PIN Ayarları</Text>
                </View>
                <Text style={[styles.inputLabel, { fontWeight: 'normal' }]}>
                  Durum: <Text style={{ color: lockEnabled ? '#00FF87' : '#EF4444', fontWeight: 'bold' }}>{lockEnabled ? 'Kilit Aktif' : 'Kilit Kapalı'}</Text>
                </Text>
                <Text style={[styles.inputLabel, { fontWeight: 'normal', marginTop: 4, marginBottom: 12 }]}>
                  Uygulama arka planda {lockTimeout} dakika kaldığında PIN sorulur.
                </Text>

                <View style={styles.inputGroup}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
                    <Key color="#94A3B8" size={16} style={{ marginRight: 6 }} />
                    <Text style={styles.inputLabel}>Zaman Aşımı (Dakika)</Text>
                  </View>
                  <TextInput
                    style={styles.input}
                    placeholder="Örn: 30"
                    placeholderTextColor="#64748B"
                    value={lockTimeout}
                    onChangeText={setLockTimeoutState}
                    keyboardType="number-pad"
                    maxLength={4}
                  />
                </View>

                <View style={styles.inputGroup}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
                    <Key color="#94A3B8" size={16} style={{ marginRight: 6 }} />
                    <Text style={styles.inputLabel}>Yeni Kilit PIN (4-6 hane)</Text>
                  </View>
                  <TextInput
                    style={styles.input}
                    placeholder="PIN girin..."
                    placeholderTextColor="#64748B"
                    value={newPin}
                    onChangeText={setNewPin}
                    secureTextEntry
                    keyboardType="number-pad"
                    maxLength={6}
                  />
                </View>

                <TouchableOpacity style={[styles.btn, { backgroundColor: '#EF4444', marginBottom: 12 }]} onPress={handleSaveLock}>
                  <Text style={styles.btnText}>{lockHasPin ? 'PIN ve Süreyi Güncelle' : 'PIN Tanımla ve Aktifleştir'}</Text>
                </TouchableOpacity>

                {lockEnabled && (
                  <TouchableOpacity style={[styles.btn, { backgroundColor: '#3B82F6', marginBottom: 12 }]} onPress={handleUpdateTimeoutOnly}>
                    <Text style={styles.btnText}>Sadece Süreyi Güncelle</Text>
                  </TouchableOpacity>
                )}

                {lockEnabled && (
                  <TouchableOpacity style={[styles.btn, { backgroundColor: 'rgba(255,255,255,0.06)' }]} onPress={handleDisableLock}>
                    <Text style={[styles.btnText, { color: '#EF4444' }]}>Oturum Kilidini Kaldır</Text>
                  </TouchableOpacity>
                )}
              </View>
            )}

            {/* 5. Yedekleme ve Bakım */}
            {currentCategory === 'backup' && (
              <View style={styles.card}>
                <View style={styles.cardHeader}>
                  <Database color="#10B981" size={22} />
                  <Text style={styles.cardTitle}>Veritabanı Yedekleme & Geri Yükleme</Text>
                </View>

                <TouchableOpacity style={[styles.btn, { backgroundColor: '#10B981', marginBottom: 16 }]} onPress={handleExportBackup}>
                  <Text style={styles.btnText}>Yedeği Dışa Aktar (Export JSON)</Text>
                </TouchableOpacity>

                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>JSON Yedek Verisini Yapıştırın</Text>
                  <TextInput 
                    style={[styles.input, { height: 80 }]} 
                    placeholder="JSON yedek metnini buraya yapıştırın..." 
                    placeholderTextColor="#64748B"
                    multiline={true}
                    value={backupJsonInput}
                    onChangeText={setBackupJsonInput}
                  />
                </View>

                <TouchableOpacity style={[styles.btn, { backgroundColor: '#3B82F6' }]} onPress={handleImportBackup}>
                  <Text style={styles.btnText}>Yedeği Geri Yükle (Import JSON)</Text>
                </TouchableOpacity>
              </View>
            )}

            {/* 6. Arayüz Ölçekleme */}
            {currentCategory === 'theme' && (
              <View style={styles.card}>
                <View style={styles.cardHeader}>
                  <MonitorSmartphone color="#3B82F6" size={22} />
                  <Text style={styles.cardTitle}>Görünüm Ölçeği</Text>
                </View>
                <Text style={[styles.inputLabel, { fontWeight: 'normal', marginBottom: 12 }]}>
                  Başlık ve metin boyutu ölçeğini cihazınıza göre özelleştirin (Display ölçek paritesi).
                </Text>
                <View style={{ flexDirection: 'row', gap: 8 }}>
                  {(['kucuk', 'orta', 'buyuk'] as UiScale[]).map(scale => (
                    <TouchableOpacity
                      key={scale}
                      style={[styles.btn, { flex: 1, backgroundColor: uiScale === scale ? '#3B82F6' : 'rgba(255,255,255,0.06)' }]}
                      onPress={() => handleSaveUiScale(scale)}
                    >
                      <Text style={[styles.btnText, uiScale !== scale && { color: '#94A3B8' }]}>
                        {scale === 'kucuk' ? 'Küçük' : scale === 'orta' ? 'Orta' : 'Büyük'}
                      </Text>
                    </TouchableOpacity>
                  ))}
                </View>
              </View>
            )}

            {/* 7. Veri Temizlik */}
            {currentCategory === 'cleanup' && (
              <>
                <View style={styles.card}>
                  <View style={styles.cardHeader}>
                    <Trash2 color="#EF4444" size={22} />
                    <Text style={styles.cardTitle}>Veri Temizliği (Bulut / Firebase)</Text>
                  </View>
                  <Text style={[styles.inputLabel, { fontWeight: 'normal', marginBottom: 12 }]}>
                    Firebase Realtime Database üzerindeki silinmiş (soft-deleted / isDeleted: true) çöpleri kalıcı olarak siler.
                  </Text>
                  <TouchableOpacity style={[styles.btn, { backgroundColor: '#EF4444' }]} onPress={handleDataCleanup}>
                    <Text style={styles.btnText}>Silinen Kayıtları Kalıcı Sil</Text>
                  </TouchableOpacity>
                </View>

                <View style={styles.card}>
                  <View style={styles.cardHeader}>
                    <Database color="#64748B" size={22} />
                    <Text style={styles.cardTitle}>Yerel Önbellek Temizliği</Text>
                  </View>
                  <Text style={[styles.inputLabel, { fontWeight: 'normal', marginBottom: 12 }]}>
                    Cihazın yerel diskindeki çevrimdışı önbelleği ve bekleyen işlem kuyruklarını temizler.
                  </Text>
                  <TouchableOpacity style={[styles.btn, { backgroundColor: '#64748B' }]} onPress={handleClearLocalCache}>
                    <Text style={styles.btnText}>Cihaz Önbelleğini Sıfırla</Text>
                  </TouchableOpacity>
                </View>
              </>
            )}
            
            {/* Geri Dön (Alt) */}
            <TouchableOpacity 
              onPress={() => setCurrentCategory(null)} 
              style={[styles.logoutButton, { borderColor: '#E2E8F0', marginTop: 20 }]}
            >
              <Text style={[styles.logoutButtonText, { color: '#E2E8F0' }]}>Kategorilere Dön</Text>
            </TouchableOpacity>
          </>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0A0A0A',
  },
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  header: {
    padding: 20,
    paddingTop: 40,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255, 255, 255, 0.08)',
  },
  headerTitle: {
    fontSize: 22,
    fontWeight: '900',
    color: '#FFFFFF',
  },
  headerSubtitle: {
    color: '#64748B',
    fontSize: 13,
    marginTop: 4,
  },
  scrollContent: {
    padding: 20,
    paddingBottom: 40,
  },
  card: {
    backgroundColor: '#161616',
    borderRadius: 20,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.08)',
    padding: 16,
    marginBottom: 16,
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  cardTitle: {
    color: '#FFF',
    fontSize: 15,
    fontWeight: 'bold',
    marginLeft: 10,
  },
  statusText: {
    color: '#00FF87',
    fontSize: 14,
    fontWeight: 'bold',
  },
  inputGroup: {
    marginBottom: 14,
  },
  inputLabel: {
    color: '#94A3B8',
    fontSize: 12,
    fontWeight: 'bold',
    marginBottom: 6,
  },
  input: {
    backgroundColor: '#2A2A2A',
    borderColor: '#444',
    borderWidth: 1,
    borderRadius: 12,
    color: '#FFF',
    padding: 12,
    fontSize: 14,
  },
  btn: {
    padding: 14,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  btnText: {
    color: '#FFF',
    fontWeight: 'bold',
    fontSize: 13,
  },
  saveButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#0061FF',
    padding: 16,
    borderRadius: 14,
    marginTop: 10,
  },
  saveButtonText: {
    color: '#FFF',
    fontWeight: 'bold',
    fontSize: 15,
  },
  logoutButton: {
    padding: 16,
    borderRadius: 14,
    borderWidth: 1,
    borderColor: '#EF4444',
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 12,
  },
  logoutButtonText: {
    color: '#EF4444',
    fontWeight: 'bold',
    fontSize: 14,
  },
  categoriesGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    padding: 2,
  },
  categoryCard: {
    width: '48%',
    backgroundColor: 'rgba(255, 255, 255, 0.03)',
    borderRadius: 20,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.05)',
    padding: 16,
    marginBottom: 16,
    height: 140,
    justifyContent: 'center',
  },
  categoryIconBox: {
    width: 44,
    height: 44,
    borderRadius: 14,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 12,
  },
  categoryTitle: {
    color: '#FFF',
    fontSize: 13,
    fontWeight: 'bold',
    marginBottom: 4,
  },
  categoryDesc: {
    color: '#64748B',
    fontSize: 10,
    lineHeight: 14,
  },
  backButtonHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 20,
    backgroundColor: 'rgba(255,255,255,0.04)',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 10,
    alignSelf: 'flex-start',
  },
  backButtonText: {
    color: '#E2E8F0',
    fontSize: 13,
    fontWeight: 'bold',
    marginLeft: 6,
  }
});
