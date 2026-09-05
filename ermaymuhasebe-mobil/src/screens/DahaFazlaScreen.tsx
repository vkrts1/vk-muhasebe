import React from 'react';
import { StyleSheet, Text, View, SafeAreaView, ScrollView, TouchableOpacity } from 'react-native';
import { Settings, BarChart3, CreditCard, Box, PieChart, Info, FileSignature, FileKey2, CalendarDays, LayoutGrid, Calculator, Wrench, Folder, ShieldAlert, BellRing, ClipboardList, Megaphone, Palette } from 'lucide-react-native';
import { useNavigation } from '@react-navigation/native';

export default function DahaFazlaScreen() {
  const navigation = useNavigation<any>();

  const menuItems = [
    { title: 'Müşteri Takip', route: 'MusteriTakip', icon: Folder, color: '#3B82F6' },
    { title: 'Finans', route: 'Finans', icon: CreditCard, color: '#FF416C' },
    { title: 'Vade Takibi', route: 'VadeTakip', icon: CalendarDays, color: '#8B5CF6' },
    { title: 'Raporlar', route: 'Raporlar', icon: PieChart, color: '#0061FF' },
    { title: 'Siparişler', route: 'Siparisler', icon: FileSignature, color: '#6366F1' },
    { title: 'Teklifler', route: 'Teklifler', icon: FileKey2, color: '#F59E0B' },
    { title: 'Görev Panosu', route: 'Kanban', icon: LayoutGrid, color: '#00FF87' },
    { title: 'Hesap Makinası', route: 'MaliyetHesaplama', icon: Calculator, color: '#0D9488' },
    { title: 'Araçlar', route: 'Araclar', icon: Wrench, color: '#F59E0B' },
    { title: 'Ayarlar', route: 'Ayarlar', icon: Settings, color: '#94A3B8' },
  ];

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.headerTitle}>Diğer Modüller</Text>
        <View style={styles.menuContainer}>
          {menuItems.map((item, idx) => {
            const Icon = item.icon;
            return (
              <TouchableOpacity 
                key={idx} 
                style={styles.menuItem}
                onPress={() => navigation.navigate(item.route)}
              >
                <View style={[styles.iconBox, { backgroundColor: `${item.color}15` }]}>
                  <Icon color={item.color} size={24} />
                </View>
                <Text style={styles.menuTitle}>{item.title}</Text>
              </TouchableOpacity>
            )
          })}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0A0A0A',
  },
  content: {
    padding: 20,
    paddingTop: 40,
    paddingBottom: 95,
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: '900',
    color: '#FFFFFF',
    marginBottom: 24,
  },
  menuContainer: {
    backgroundColor: 'rgba(24, 24, 28, 0.75)',
    borderRadius: 20,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.12)',
    overflow: 'hidden',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.4,
    shadowRadius: 10,
    elevation: 8,
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255, 255, 255, 0.06)',
  },
  iconBox: {
    width: 40,
    height: 40,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 16,
  },
  menuTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#E2E8F0',
  }
});
