import React from 'react';
import { StyleSheet, Text, View, SafeAreaView, TouchableOpacity, FlatList, Share } from 'react-native';
import { X, FileSpreadsheet, Share2 } from 'lucide-react-native';
import { useRoute, useNavigation } from '@react-navigation/native';
import { generateReportPdf } from '../services/pdfService';

export default function RaporDetayScreen() {
  const route = useRoute();
  const navigation = useNavigation();
  const { reportResult, pdfConfig } = route.params as any;

  const handleShareReport = async () => {
    if (!reportResult) return;
    await generatePdfAndShare();
  };

  const handleGetPdfReport = async () => {
    if (!reportResult) return;
    await generatePdfAndShare();
  };

  const generatePdfAndShare = async () => {
    try {
      const endpoint = pdfConfig?.endpoint || 'generic';
      const payload = pdfConfig?.payload || {
        title: reportResult.title,
        subtitle: '',
        headers: reportResult.headers,
        rows: reportResult.rows,
      };
      const reportFileName = reportResult.title.replace(/\s+/g, '_') + '.pdf';
      await generateReportPdf(endpoint, payload, reportFileName);
    } catch (err) {
      console.log('PDF Error:', err);
    }
  };

  if (!reportResult) return null;

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.modalContent}>
        <View style={styles.modalHeader}>
          <Text style={styles.modalTitle} numberOfLines={1}>{reportResult.title}</Text>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <X color="#FFF" size={24} />
          </TouchableOpacity>
        </View>

        <View style={{ flex: 1 }}>
          <View style={styles.tableHeaderRow}>
            {reportResult.headers.map((h: string, i: number) => (
              <Text key={i} style={styles.tableHeaderCell}>{h}</Text>
            ))}
          </View>

          <FlatList 
            initialNumToRender={20} 
            maxToRenderPerBatch={20} 
            windowSize={5} 
            data={reportResult.rows}
            keyExtractor={(_, idx) => idx.toString()}
            renderItem={({ item }) => (
              <View style={styles.tableRow}>
                {item.map((cell: string, idx: number) => (
                  <Text key={idx} style={styles.tableCell}>{cell}</Text>
                ))}
              </View>
            )}
          />

          <View style={{ flexDirection: 'row', gap: 10, marginTop: 16 }}>
            <TouchableOpacity style={[styles.shareBtn, { flex: 1, backgroundColor: '#10B981' }]} onPress={handleGetPdfReport}>
              <FileSpreadsheet color="#FFF" size={20} style={{ marginRight: 8 }} />
              <Text style={styles.shareBtnText}>PDF Rapor</Text>
            </TouchableOpacity>

            <TouchableOpacity style={[styles.shareBtn, { flex: 1 }]} onPress={handleShareReport}>
              <Share2 color="#FFF" size={20} style={{ marginRight: 8 }} />
              <Text style={styles.shareBtnText}>Paylaş</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0A0A0A',
  },
  modalContent: {
    backgroundColor: '#0A0A0A',
    padding: 20,
    paddingBottom: 110,
    flex: 1,
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255, 255, 255, 0.1)',
    paddingBottom: 12,
    marginBottom: 12,
  },
  modalTitle: {
    color: '#FFF',
    fontSize: 18,
    fontWeight: '600',
    flex: 1,
    marginRight: 10,
  },
  tableHeaderRow: {
    flexDirection: 'row',
    backgroundColor: '#2A2A2A',
    paddingVertical: 10,
    borderRadius: 8,
    marginBottom: 8,
  },
  tableHeaderCell: {
    flex: 1,
    color: '#94A3B8',
    fontSize: 12,
    fontWeight: '600',
    textAlign: 'center',
  },
  tableRow: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255, 255, 255, 0.05)',
    paddingVertical: 12,
  },
  tableCell: {
    flex: 1,
    color: '#FFF',
    fontSize: 13,
    textAlign: 'center',
  },
  shareBtn: {
    backgroundColor: '#0061FF',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 14,
    borderRadius: 12,
  },
  shareBtnText: {
    color: '#FFF',
    fontSize: 15,
    fontWeight: '600',
  }
});
