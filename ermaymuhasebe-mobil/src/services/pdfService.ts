import AsyncStorage from './storage';
import { Alert, Share } from 'react-native';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { readData, mapAppToDatabase } from './firebase';

// Fatura tasarımı için desteklenen PDF endpoint'leri.
// Bu endpoint'ler Functions tarafında FaturaTasarimi nesnesiyle
// parametrelenir (size/orientation/showLogo/baslik...). Masaüstünde
// tasarım Firebase RTDB "FaturaTasarimi/1" düğümüne yansıtılır, mobil
// burada okur ve PDF API'sine gönderir. Böylece masaüstü kapalı olsa
// bile üretilen PDF'ler birebir aynı olur.
const TASARIM_ENDPOINTS = new Set(['fatura', 'fatura-batch', 'teklif', 'siparis']);
let cachedTasarim: any | null = null;
let cachedTasarimAt: number = 0;
const TASARIM_CACHE_MS = 15000;

let cachedProfil: any | null = null;
let cachedProfilAt: number = 0;

const loadFirmaProfili = async (): Promise<any | null> => {
  if (cachedProfil && Date.now() - cachedProfilAt < TASARIM_CACHE_MS) {
    return cachedProfil;
  }
  try {
    const raw = await readData('FirmaProfili');
    if (!raw) return null;
    const node = raw[1] || raw; // "FirmaProfili/1" düğümü
    cachedProfil = node;
    cachedProfilAt = Date.now();
    return node;
  } catch (e) {
    console.error('FirmaProfili okunamadı:', e);
    return null;
  }
};

const base64ToBytes = (base64: string): number[] | null => {
  try {
    if (typeof globalThis !== 'undefined' && typeof globalThis.atob === 'function') {
      const binary = globalThis.atob(base64);
      const bytes = new Uint8Array(binary.length);
      for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
      return Array.from(bytes);
    }
  } catch (e) {
    console.error('Logo base64 çözülemedi:', e);
  }
  return null;
};

// LogoBytes'i FirmaProfili'nden getirir. Masaüstü logo'yu bu düğüme
// senkron eder, mobil de PDF isteklerinde aynı görseli gönderir.
const getLogoBytes = async (): Promise<number[] | null> => {
  const profil = await loadFirmaProfili();
  if (!profil) return null;
  const logoBase64 = profil.logoBase64 || profil.LogoBase64 || '';
  if (!logoBase64) return null;
  return base64ToBytes(logoBase64);
};

const loadFaturaTasarimi = async (): Promise<any | null> => {
  if (cachedTasarim && Date.now() - cachedTasarimAt < TASARIM_CACHE_MS) {
    return cachedTasarim;
  }
  try {
    const raw = await readData('FaturaTasarimi');
    if (!raw) return null;
    const node = raw[1] || raw; // "FaturaTasarimi/1" düğümü
    const tasarim = mapAppToDatabase('FaturaTasarimi', node);
    cachedTasarim = tasarim;
    cachedTasarimAt = Date.now();
    return tasarim;
  } catch (e) {
    console.error('FaturaTasarimi okunamadı:', e);
    return null;
  }
};

// PDF payload'ına masaüstüyle aynı parametreleri enjekte eder:
// - Logo (FirmaProfili.LogoBase64) her PDF'e gider — masaüstü bunu aynı şekilde basar.
// - Tasarım (FaturaTasarimi) yalnızca fatura/teklif/sipariş endpoint'lerinde uygulanır.
const enrichWithTasarim = async (endpoint: string, payload: any): Promise<any> => {
  const [tasarim, logoBytes] = await Promise.all([
    TASARIM_ENDPOINTS.has(endpoint) ? loadFaturaTasarimi() : Promise.resolve(null),
    getLogoBytes(),
  ]);

  if (Array.isArray(payload)) {
    return payload.map((item) => injectParams(item, tasarim, logoBytes));
  }
  return injectParams(payload, tasarim, logoBytes);
};

const injectParams = (item: any, tasarim: any, logoBytes: number[] | null): any => {
  if (!item || typeof item !== 'object') return item;
  const result = { ...item };
  result.LogoBytes = null; // Bypass logo bytes serialization issue
  result.ShowLogo = false; // Disable logo rendering to isolate crashes
  if (!tasarim) return result;
  if (result.Tasarim === undefined) result.Tasarim = tasarim;
  if (result.Size === undefined && tasarim.FaturaSize) result.Size = tasarim.FaturaSize;
  if (result.Orientation === undefined && tasarim.FaturaOrientation) result.Orientation = tasarim.FaturaOrientation;
  return result;
};

const getPdfServerUrl = async () => {
  try {
    const savedUrl = await AsyncStorage.getItem('pdf_server_url');
    if (savedUrl) return savedUrl;
    
    // Default to 7/24 Cloud Run PDF API
    return 'https://ermay-pdf-api-916435485627.europe-west1.run.app';
  } catch (e) {
    return 'https://ermay-pdf-api-916435485627.europe-west1.run.app';
  }
};

const formatMoney = (val: number) => {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(val || 0);
};




const fallbackShareAsText = async (payload: any, reportName: string) => {
  try {
    let textContent = `*** ${reportName.toUpperCase()} ***\n\n`;
    if (payload.faturaNo) textContent += `Fatura No: ${payload.faturaNo}\n`;
    if (payload.siparisNo) textContent += `Sipariş No: ${payload.siparisNo}\n`;
    if (payload.tarih) textContent += `Tarih: ${payload.tarih}\n`;
    if (payload.cariUnvan) textContent += `Müşteri: ${payload.cariUnvan}\n\n`;
    
    textContent += `-- DETAYLAR --\n`;
    const detaylar = payload.detaylar || payload.items || [];
    detaylar.forEach((d: any) => {
      textContent += `- ${d.stokAdi || d.aciklama} | ${d.miktar} ${d.birim} x ${formatMoney(d.birimFiyat)} = ${formatMoney(d.toplamTutar || d.tutar)}\n`;
    });
    
    textContent += `\n-- TOPLAMLAR --\n`;
    textContent += `Ara Toplam: ${formatMoney(payload.araToplam)}\n`;
    textContent += `KDV: ${formatMoney(payload.kdvToplam)}\n`;
    textContent += `Genel Toplam: ${formatMoney(payload.genelToplam)}\n`;

    await Share.share({
      title: reportName,
      message: textContent,
    });
  } catch (e) {
    Alert.alert('Hata', 'Metin paylaşımı sırasında hata oluştu.');
  }
};

export const generateReportPdf = async (endpoint: string, payload: any, reportName: string, maxRetries = 3) => {
  let attempt = 0;
  
  while (attempt < maxRetries) {
    try {
      const serverUrl = await getPdfServerUrl();
      const enrichedPayload = await enrichWithTasarim(endpoint, payload);
      const response = await fetch(`${serverUrl}/generate/${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(enrichedPayload),
      });

      if (response.ok) {
        // PDF base64 formatına çevirmek için arrayBuffer kullanıyoruz
        const arrayBuffer = await response.arrayBuffer();
        const base64Pdf = arrayBufferToBase64(arrayBuffer);
        
        const filename = reportName.endsWith('.pdf') ? reportName : `${reportName}.pdf`;
        const fileUri = `${(FileSystem as any).cacheDirectory}${filename.replace(/[^a-zA-Z0-9.]/g, '_')}`;
        
        await FileSystem.writeAsStringAsync(fileUri, base64Pdf, {
          encoding: (FileSystem as any).EncodingType.Base64,
        });

        if (await Sharing.isAvailableAsync()) {
          await Sharing.shareAsync(fileUri, {
            mimeType: 'application/pdf',
            dialogTitle: `${reportName} Paylaş`,
            UTI: 'com.adobe.pdf',
          });
          return { success: true };
        } else {
          Alert.alert('Hata', 'Paylaşım bu cihazda desteklenmiyor.');
          return { success: false };
        }
      } else if (response.status >= 500) {
        throw new Error(`Server Error: ${response.status}`);
      } else {
        const errText = await response.text();
        console.warn('PDF Sunucu Hatası:', errText);
        Alert.alert(
          'Sunucu Hatası',
          `PDF oluşturulamadı.\nSunucu Mesajı: ${errText.substring(0, 100)}...\n\nBelgeyi düz metin (text) olarak paylaşmak ister misiniz?`,
          [
            { text: 'İptal', style: 'cancel' },
            { text: 'Metin Paylaş', onPress: () => fallbackShareAsText(payload, reportName) }
          ]
        );
        return { success: false };
      }
    } catch (error: any) {
      attempt++;
      console.warn(`[pdfService] Fetch attempt ${attempt} failed:`, error.message);
      
      if (attempt >= maxRetries) {
        console.error('PDF generation error after retries:', error);
        Alert.alert(
          'Bağlantı Hatası',
          `PDF Sunucusuna bağlanılamadı. Lütfen sunucunun açık olduğundan emin olun.\n\nBelgeyi düz metin (text) olarak paylaşmak ister misiniz?`,
          [
            { text: 'İptal', style: 'cancel' },
            { text: 'Metin Paylaş', onPress: () => fallbackShareAsText(payload, reportName) }
          ]
        );
        return { success: false };
      }
      // Bekleyip tekrar dene (Exponential Backoff)
      await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
    }
  }
};

// Helper function to convert ArrayBuffer to Base64 in standard JS
function arrayBufferToBase64(buffer: ArrayBuffer) {
  let binary = '';
  const bytes = new Uint8Array(buffer);
  const len = bytes.byteLength;
  for (let i = 0; i < len; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  
  // React Native environment supports btoa via global or we implement a simple one
  try {
    return btoa(binary);
  } catch (e) {
    // Basic fallback base64 encoding if btoa is missing
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
    let base64 = '';
    const bytes = new Uint8Array(buffer);
    const byteLength = bytes.byteLength;
    const byteRemainder = byteLength % 3;
    const mainLength = byteLength - byteRemainder;

    let a, b, c, d;
    let chunk;

    for (let i = 0; i < mainLength; i = i + 3) {
      chunk = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2];
      a = (chunk & 16515072) >> 18;
      b = (chunk & 258048) >> 12;
      c = (chunk & 4032) >> 6;
      d = chunk & 63;
      base64 += chars[a] + chars[b] + chars[c] + chars[d];
    }

    if (byteRemainder === 1) {
      chunk = bytes[mainLength];
      a = (chunk & 252) >> 2;
      b = (chunk & 3) << 4;
      base64 += chars[a] + chars[b] + '==';
    } else if (byteRemainder === 2) {
      chunk = (bytes[mainLength] << 8) | bytes[mainLength + 1];
      a = (chunk & 64512) >> 10;
      b = (chunk & 1008) >> 4;
      c = (chunk & 15) << 2;
      base64 += chars[a] + chars[b] + chars[c] + '=';
    }

    return base64;
  }
}
