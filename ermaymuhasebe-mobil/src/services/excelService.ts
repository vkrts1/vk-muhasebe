import { Alert } from 'react-native';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import * as DocumentPicker from 'expo-document-picker';
import * as XLSX from 'xlsx';

export const exportToExcel = async (rows: any[], sheetName: string, fileName: string): Promise<boolean> => {
  try {
    if (!rows || rows.length === 0) {
      Alert.alert('Uyarı', 'Dışa aktarılacak veri bulunamadı.');
      return false;
    }

    const cacheDir = (FileSystem as any).cacheDirectory || (FileSystem as any).documentDirectory || '';
    const sep = cacheDir.endsWith('/') ? '' : '/';
    const baseName = fileName.replace(/\.(xlsx|csv|xls)$/i, '').replace(/[^a-zA-Z0-9._-]/g, '_');

    let wbout = '';
    let isXlsxSuccess = false;

    try {
      const ws = XLSX.utils.json_to_sheet(rows.filter(Boolean));
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, (sheetName || 'Sayfa').slice(0, 31));
      wbout = XLSX.write(wb, { type: 'base64', bookType: 'xlsx' });
      if (wbout && wbout.length > 50) {
        isXlsxSuccess = true;
      }
    } catch (xlsxErr) {
      console.warn('[excelService] XLSX write fallback to CSV:', xlsxErr);
      isXlsxSuccess = false;
    }

    let fileUri = '';

    if (isXlsxSuccess) {
      fileUri = `${cacheDir}${sep}${baseName}.xlsx`;
      await FileSystem.writeAsStringAsync(fileUri, wbout, {
        encoding: (FileSystem as any).EncodingType?.Base64 || 'base64',
      });
    } else {
      // Fallback to UTF-8 BOM CSV (which opens natively in Excel with proper Turkish chars)
      let csvContent = '\uFEFF';
      const headers = Object.keys(rows[0]);
      csvContent += headers.join(';') + '\n';
      rows.forEach(row => {
        const line = headers.map(h => {
          const val = row[h] !== undefined && row[h] !== null ? String(row[h]) : '';
          if (val.includes(';') || val.includes('\n') || val.includes('"')) {
            return `"${val.replace(/"/g, '""')}"`;
          }
          return val;
        }).join(';');
        csvContent += line + '\n';
      });

      fileUri = `${cacheDir}${sep}${baseName}.csv`;
      await FileSystem.writeAsStringAsync(fileUri, csvContent, {
        encoding: 'utf8',
      });
    }

    const isAvailable = await Sharing.isAvailableAsync();
    if (isAvailable) {
      await Sharing.shareAsync(fileUri, {
        mimeType: isXlsxSuccess 
          ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
          : 'text/csv',
        dialogTitle: `${fileName} Paylaş`,
      });
      return true;
    } else {
      Alert.alert('Bilgi', `Excel dosyası oluşturuldu:\n${fileUri}`);
      return true;
    }
  } catch (error: any) {
    console.error('[excelService] Export error:', error);
    Alert.alert('Hata', `Excel oluşturulamadı: ${error?.message || 'Bilinmeyen hata'}`);
    return false;
  }
};

export const importFromExcel = async (): Promise<any[] | null> => {
  try {
    const result = await DocumentPicker.getDocumentAsync({
      type: [
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'application/vnd.ms-excel',
        'application/octet-stream',
        '*/*'
      ],
      copyToCacheDirectory: true,
      multiple: false
    });

    if (result.canceled || !result.assets || result.assets.length === 0) {
      return null;
    }

    const file = result.assets[0];
    const base64Data = await FileSystem.readAsStringAsync(file.uri, {
      encoding: (FileSystem as any).EncodingType?.Base64 || 'base64'
    });

    const workbook = XLSX.read(base64Data, { type: 'base64' });
    if (!workbook.SheetNames || workbook.SheetNames.length === 0) {
      throw new Error('Excel sayfa verisi bulunamadı.');
    }

    const firstSheet = workbook.Sheets[workbook.SheetNames[0]];
    const rawRows = XLSX.utils.sheet_to_json<any>(firstSheet);

    if (!rawRows || rawRows.length === 0) {
      throw new Error('Excel dosyasında kayıtlı satır bulunamadı.');
    }

    const normalizeKey = (key: string) => {
      return (key || '').toLowerCase().trim()
        .replace(/ı/g, 'i')
        .replace(/ğ/g, 'g')
        .replace(/ü/g, 'u')
        .replace(/ş/g, 's')
        .replace(/ö/g, 'o')
        .replace(/ç/g, 'c')
        .replace(/[\s_\-.:%]/g, '');
    };

    const parsedRows = rawRows.map(row => {
      const getVal = (...aliases: string[]) => {
        for (const alias of aliases) {
          const normAlias = normalizeKey(alias);
          for (const k of Object.keys(row)) {
            if (normalizeKey(k) === normAlias) {
              return row[k];
            }
          }
        }
        return undefined;
      };

      const parseNum = (val: any, fallback = 0) => {
        if (val === undefined || val === null || val === '') return fallback;
        if (typeof val === 'number') return val;
        const str = String(val).trim().replace(',', '.');
        const num = parseFloat(str);
        return isNaN(num) ? fallback : num;
      };

      const stokKodu = getVal('stokkodu', 'stokkod', 'kod', 'urunkodu', 'skodu');
      const stokAdi = getVal('stokadi', 'stokad', 'urunadi', 'ad', 'adi');
      const birim = getVal('birim', 'birimi', 'unit') || 'Adet';
      const kategori = getVal('kategori', 'grup', 'stokgrubu', 'category') || 'Genel';
      const barkod = getVal('barkod', 'barcode') || '';
      const alisFiyati = parseNum(getVal('alisfiyati', 'alis', 'alisfiyat'));
      const satisFiyati = parseNum(getVal('satisfiyati', 'satis', 'satisfiyat'));
      const kdv = parseNum(getVal('kdv', 'kdvorani', 'vergi'), 20);
      const miktar = parseNum(getVal('miktar', 'acilisbakiyesi', 'acilisbakiye', 'adet', 'bakiye'));
      const minSeviye = parseNum(getVal('minseviye', 'kritikseviye', 'minimumseviye'));
      const aciklama = getVal('aciklama', 'not', 'description') || '';

      return {
        stokKodu: String(stokKodu || '').trim(),
        stokAdi: String(stokAdi || '').trim(),
        birim: String(birim).trim(),
        kategori: String(kategori).trim(),
        barkod: String(barkod).trim(),
        alisFiyati,
        satisFiyati,
        kdv,
        miktar,
        minSeviye,
        aciklama: String(aciklama).trim()
      };
    }).filter(r => Boolean(r.stokKodu || r.stokAdi));

    return parsedRows;
  } catch (error: any) {
    console.error('[excelService] Import error:', error);
    Alert.alert('İçe Aktarma Hatası', error?.message || 'Excel dosyası okunamadı.');
    return null;
  }
};
