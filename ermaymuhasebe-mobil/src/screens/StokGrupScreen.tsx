import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, SafeAreaView, TextInput, TouchableOpacity, ScrollView, ActivityIndicator, Alert, FlatList } from 'react-native';
import { Layers, Search, Save, CheckSquare, Square, ChevronLeft } from 'lucide-react-native';
import { subscribeToPath, writeData } from '../services/firebase';
import { useNavigation } from '@react-navigation/native';

export default function StokGrupScreen() {
  const navigation = useNavigation();
  const [stoklar, setStoklar] = useState<any[]>([]);
  const [stokGruplar, setStokGruplar] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedStokIds, setSelectedStokIds] = useState<string[]>([]);
  const [hedefGrup, setHedefGrup] = useState('');
  const [saveLoader, setSaveLoader] = useState(false);

  useEffect(() => {
    const unsub = subscribeToPath('Stoklar', (data) => {
      if (data) {
        const list = Array.isArray(data) ? data.filter(Boolean) : Object.keys(data).map(key => ({ ...data[key], firebaseKey: key }));
        setStoklar(list.filter(s => !s.isDeleted && s.IsDeleted !== true));
      } else {
        setStoklar([]);
      }
      setLoading(false);
    });

    const unsubGruplar = subscribeToPath('StokGruplar', (data) => {
      let list: string[] = [];
      if (data) {
        if (Array.isArray(data)) {
          list = data
            .filter(Boolean)
            .filter((x: any) => x.isDeleted !== true && x.IsDeleted !== true)
            .map((x: any) => (typeof x === 'string' ? x : (x.ad || x.Ad || x.grup || x.Grup || '')))
            .filter(Boolean);
        } else if (typeof data === 'object') {
          list = Object.keys(data)
            .map(k => {
              const item = data[k];
              if (!item || item.isDeleted === true || item.IsDeleted === true) return '';
              return typeof item === 'string' ? item : (item.ad || item.Ad || item.grup || item.Grup || k);
            })
            .filter(Boolean);
        }
      }
      setStokGruplar(Array.from(new Set(list)));
    });

    return () => {
      unsub();
      unsubGruplar();
    };
  }, []);

  const existingGroups = Array.from(
    new Set([
      ...stokGruplar,
      ...stoklar.map(s => s.kategori || s.Kategori || s.grup || s.Grup || 'Genel')
    ])
  )
    .map(g => (g || '').trim())
    .filter(Boolean)
    .sort((a, b) => a.localeCompare(b, 'tr-TR'));

  const filteredStoklar = stoklar.filter(s => {
    const grp = s.kategori || s.Kategori || s.grup || s.Grup || 'Genel';
    return (
      (s.stokAdi || s.StokAdi || '').toLocaleLowerCase('tr-TR').includes(searchQuery.toLocaleLowerCase('tr-TR')) ||
      grp.toLocaleLowerCase('tr-TR').includes(searchQuery.toLocaleLowerCase('tr-TR'))
    );
  });

  const toggleSelect = (id: string) => {
    setSelectedStokIds(prev => 
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    );
  };

  const handleSelectAll = () => {
    if (selectedStokIds.length === filteredStoklar.length) {
      setSelectedStokIds([]);
    } else {
      setSelectedStokIds(filteredStoklar.map(s => s.id || s.firebaseKey));
    }
  };

  const handleApplyGroup = async () => {
    if (selectedStokIds.length === 0) {
      Alert.alert('Hata', 'Lütfen en az bir stok kartı seçin.');
      return;
    }
    if (!hedefGrup.trim()) {
      Alert.alert('Hata', 'Lütfen hedef grup adını girin.');
      return;
    }

    setSaveLoader(true);
    const trimmedGroup = hedefGrup.trim();
    try {
      // Save group to StokGruplar as well
      const grpId = Math.floor(Math.random() * 1000000);
      try {
        await writeData(`StokGruplar/${grpId}`, {
          id: grpId,
          ad: trimmedGroup,
          updatedAt: new Date().toISOString(),
          isDeleted: false
        });
      } catch (err) {}

      let count = 0;
      for (const id of selectedStokIds) {
        const stok = stoklar.find(s => (s.id || s.firebaseKey) === id);
        if (stok) {
          const key = stok.firebaseKey || id;
          const ok = await writeData(`Stoklar/${key}`, {
            ...stok,
            kategori: trimmedGroup,
            grup: trimmedGroup
          });
          if (!ok) {
            Alert.alert('Uyarı', `"${stok.stokAdi || stok.id}" grubu güncellenemedi. (Bağlantı sorunu — kayıt sıraya alındı.)`);
            continue;
          }
          count++;
        }
      }
      setSelectedStokIds([]);
      setHedefGrup('');
      Alert.alert('Başarılı', `${count} adet ürünün grubu "${trimmedGroup}" olarak güncellendi.`);
    } catch (e) {
      Alert.alert('Hata', 'Grup güncellenirken bir sorun oluştu.');
    } finally {
      setSaveLoader(false);
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity style={styles.backBtn} onPress={() => navigation.goBack()}>
          <ChevronLeft color="#FFF" size={24} />
        </TouchableOpacity>
        <View style={{ flex: 1, marginLeft: 8 }}>
          <Text style={styles.headerTitle}>Stok Grup Yönetimi</Text>
          <Text style={styles.headerSubtitle}>Ürünleri toplu olarak gruplandırın</Text>
        </View>
      </View>

      <View style={styles.searchSection}>
        <View style={styles.searchContainer}>
          <Search color="#94A3B8" size={18} style={{ marginRight: 8 }} />
          <TextInput
            style={styles.searchInput}
            placeholder="Ürün adı veya grup ara..."
            placeholderTextColor="#64748B"
            value={searchQuery}
            onChangeText={setSearchQuery}
          />
        </View>
        <TouchableOpacity style={styles.selectAllBtn} onPress={handleSelectAll}>
          <Text style={styles.selectAllText}>
            {selectedStokIds.length === filteredStoklar.length ? 'Seçimi Kaldır' : 'Tümünü Seç'}
          </Text>
        </TouchableOpacity>
      </View>

      {/* Mevcut Gruplar (Hızlı Seçim) */}
      <View style={styles.groupSection}>
        <Text style={styles.sectionTitle}>Mevcut Gruplar (Hızlı Seçim)</Text>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.groupChipsContainer}>
          {existingGroups.map(g => (
            <TouchableOpacity 
              key={g} 
              style={[styles.groupChip, hedefGrup === g && styles.groupChipActive]}
              onPress={() => setHedefGrup(g)}
            >
              <Text style={[styles.groupChipText, hedefGrup === g && styles.groupChipTextActive]}>{g}</Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      {/* Liste */}
      {loading ? (
        <ActivityIndicator size="large" color="#0061FF" style={{ flex: 1 }} />
      ) : (
        <FlatList initialNumToRender={20} maxToRenderPerBatch={20} windowSize={5}
          data={filteredStoklar}
          keyExtractor={(item) => String(item.id || item.firebaseKey)}
          contentContainerStyle={{ padding: 20 }}
          renderItem={({ item }) => {
            const isSelected = selectedStokIds.includes(item.id || item.firebaseKey);
            return (
              <TouchableOpacity style={styles.stokCard} onPress={() => toggleSelect(item.id || item.firebaseKey)}>
                <View style={styles.stokRow}>
                  {isSelected ? (
                    <CheckSquare color="#3B82F6" size={22} />
                  ) : (
                    <Square color="#64748B" size={22} />
                  )}
                  <View style={{ flex: 1, marginLeft: 12 }}>
                    <Text style={styles.stokName}>{item.stokAdi}</Text>
                    <View style={styles.grupBadge}>
                      <Layers color="#3B82F6" size={12} style={{ marginRight: 4 }} />
                      <Text style={styles.grupText}>{item.grup || 'Genel'}</Text>
                    </View>
                  </View>
                </View>
              </TouchableOpacity>
            );
          }}
        />
      )}

      {/* Alttaki Grup Atama Paneli */}
      <View style={styles.bottomPanel}>
        <Text style={styles.panelTitle}>{selectedStokIds.length} Ürün Seçildi</Text>
        <View style={styles.actionRow}>
          <TextInput
            style={styles.hedefInput}
            placeholder="Yeni grup adı girin..."
            placeholderTextColor="#64748B"
            value={hedefGrup}
            onChangeText={setHedefGrup}
          />
          <TouchableOpacity 
            style={[styles.applyBtn, saveLoader && { opacity: 0.7 }]} 
            onPress={handleApplyGroup}
            disabled={saveLoader}
          >
            {saveLoader ? (
              <ActivityIndicator size="small" color="#FFF" />
            ) : (
              <>
                <Save color="#FFF" size={18} style={{ marginRight: 6 }} />
                <Text style={styles.applyBtnText}>Uygula</Text>
              </>
            )}
          </TouchableOpacity>
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0A0A0A' },
  header: { padding: 20, paddingTop: 40, flexDirection: 'row', alignItems: 'center' },
  backBtn: { padding: 4 },
  headerTitle: { fontSize: 22, fontWeight: '900', color: '#FFF' },
  headerSubtitle: { color: '#94A3B8', fontSize: 13, marginTop: 4 },
  searchSection: { flexDirection: 'row', alignItems: 'center', paddingHorizontal: 20, marginBottom: 10 },
  searchContainer: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#2A2A2A',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#444',
    paddingHorizontal: 12,
    height: 44,
  },
  searchInput: { flex: 1, color: '#FFF', fontSize: 14 },
  selectAllBtn: { marginLeft: 12, paddingVertical: 10 },
  selectAllText: { color: '#60A5FA', fontWeight: 'bold', fontSize: 13 },
  groupSection: { paddingHorizontal: 20, marginVertical: 10 },
  sectionTitle: { color: '#94A3B8', fontSize: 12, fontWeight: 'bold', marginBottom: 8 },
  groupChipsContainer: { paddingBottom: 6 },
  groupChip: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    backgroundColor: '#161616',
    borderRadius: 20,
    marginRight: 8,
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)'
  },
  groupChipActive: {
    backgroundColor: '#0061FF',
    borderColor: '#0061FF'
  },
  groupChipText: { color: '#94A3B8', fontSize: 12, fontWeight: '600' },
  groupChipTextActive: { color: '#FFF' },
  stokCard: {
    backgroundColor: '#0F0F0F',
    borderRadius: 14,
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.08)',
    padding: 14,
    marginBottom: 8,
  },
  stokRow: { flexDirection: 'row', alignItems: 'center' },
  stokName: { color: '#FFF', fontSize: 14, fontWeight: '700' },
  grupBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 4,
  },
  grupText: { color: '#60A5FA', fontSize: 12, marginLeft: 4 },
  bottomPanel: {
    padding: 20,
    backgroundColor: '#161616',
    borderTopWidth: 1,
    borderTopColor: 'rgba(255,255,255,0.08)',
  },
  panelTitle: { color: '#94A3B8', fontSize: 12, fontWeight: 'bold', marginBottom: 8 },
  actionRow: { flexDirection: 'row', alignItems: 'center' },
  hedefInput: {
    flex: 1,
    backgroundColor: '#2A2A2A',
    borderColor: '#444',
    borderWidth: 1,
    borderRadius: 12,
    paddingHorizontal: 16,
    height: 48,
    color: '#FFF',
    fontSize: 14,
    marginRight: 10,
  },
  applyBtn: {
    backgroundColor: '#10B981',
    paddingHorizontal: 20,
    height: 48,
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  applyBtnText: { color: '#FFF', fontWeight: 'bold', fontSize: 14 }
});
