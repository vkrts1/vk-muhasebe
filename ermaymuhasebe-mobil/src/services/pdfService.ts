import AsyncStorage from './storage';
import { Alert, Share } from 'react-native';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { readData, mapAppToDatabase, getFirebaseConfig, loadConfigFromStorage, fetchWithTimeout, getAuthParam } from './firebase';

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

const LOGO_CACHE_KEY = 'ermay_cached_company_logo';
const PROFIL_CACHE_KEY = 'ermay_cached_firma_profili';

let cachedProfil: any | null = null;
let cachedProfilAt: number = 0;

export const resetPdfServiceCache = () => {
  cachedProfil = null;
  cachedProfilAt = 0;
  cachedTasarim = null;
  cachedTasarimAt = 0;
};

export const cleanBase64Logo = (rawLogo: string | null | undefined): string | null => {
  if (!rawLogo || typeof rawLogo !== 'string') return null;
  let clean = rawLogo.trim();
  // data:image/...;base64, prefiksini temizle (ASP.NET Core deserializer için saf Base64 gerekli)
  if (clean.includes('base64,')) {
    clean = clean.split('base64,')[1];
  }
  clean = clean.replace(/\s+/g, '');
  
  const mod4 = clean.length % 4;
  if (mod4 > 0) {
    clean += '='.repeat(4 - mod4);
  }
  
  return clean.length > 0 ? clean : null;
};

export const loadFirmaProfili = async (): Promise<any | null> => {
  if (cachedProfil && Date.now() - cachedProfilAt < TASARIM_CACHE_MS) {
    return cachedProfil;
  }
  try {
    // 1. Önce doğrudan 1 numaralı profili dene (12 saniye timeout)
    let node = await readData('FirmaProfili/1', 12000);
    
    // 2. FirmaProfili genel düğümünü dene
    if (!node || !(node.logoBase64 || node.LogoBase64)) {
      const raw = await readData('FirmaProfili', 12000);
      if (raw) {
        node = raw[1] || raw;
      }
    }

    // 3. Doğrudan REST URL ile Firebase'den çek (mapping/token engellerini aşar)
    if (!node || !(node.logoBase64 || node.LogoBase64)) {
      try {
        const config = getFirebaseConfig() || await loadConfigFromStorage();
        if (config?.url) {
          const cleanUrl = config.url.replace(/\/$/, '');
          const authParam = getAuthParam(config);
          const directUrl = `${cleanUrl}/FirmaProfili/1.json${authParam ? `?${authParam}` : ''}`;
          const directRes = await fetchWithTimeout(directUrl, {
            headers: { 'Cache-Control': 'no-cache', 'Accept': 'application/json' }
          }, 8000);
          if (directRes.ok) {
            const directData = await directRes.json();
            if (directData && (directData.LogoBase64 || directData.logoBase64)) {
              node = { ...(node || {}), ...directData, logoBase64: directData.LogoBase64 || directData.logoBase64 };
            }
          }
        }
      } catch (directErr) {
        console.warn('[pdfService] Direct Firebase fetch error:', directErr);
      }
    }

    // 4. companies/default/FirmaProfili/1 yolunu dene (Tenant yapısı)
    if (!node || !(node.logoBase64 || node.LogoBase64)) {
      const scopedNode = await readData('companies/default/FirmaProfili/1', 8000);
      if (scopedNode) {
        node = { ...(node || {}), ...scopedNode };
      }
    }

    // 5. companies/default/settings/company_logo yolunu dene (Blazor / Cloud ayar yolu)
    if (!node || !(node.logoBase64 || node.LogoBase64)) {
      const directLogo = await readData('companies/default/settings/company_logo', 8000);
      if (directLogo && typeof directLogo === 'string') {
        node = { ...(node || {}), logoBase64: directLogo, LogoBase64: directLogo };
      }
    }

    // 6. Yıllık yoldan dene (companies/default/years/{year}/FirmaProfili/1)
    if (!node || !(node.logoBase64 || node.LogoBase64)) {
      const curYear = new Date().getFullYear().toString();
      const yearlyNode = await readData(`companies/default/years/${curYear}/FirmaProfili/1`, 8000);
      if (yearlyNode) {
        node = { ...(node || {}), ...yearlyNode };
      }
    }

    const foundLogo = node?.logoBase64 || node?.LogoBase64;
    if (foundLogo) {
      cachedProfil = node;
      cachedProfilAt = Date.now();
      // Kalıcı depolamaya sakla (çevrimdışı ve yavaş ağ durumları için)
      AsyncStorage.setItem(LOGO_CACHE_KEY, String(foundLogo)).catch(() => {});
      AsyncStorage.setItem(PROFIL_CACHE_KEY, JSON.stringify(node)).catch(() => {});
      return node;
    }

    if (node) {
      // Profil var ama logo alanı boşsa, daha önceden önbelleğe alınmış kalıcı logoyu ekle
      const cachedLogo = await AsyncStorage.getItem(LOGO_CACHE_KEY);
      if (cachedLogo) {
        node.logoBase64 = cachedLogo;
        node.LogoBase64 = cachedLogo;
      }
      cachedProfil = node;
      cachedProfilAt = Date.now();
      return node;
    }

    // Ağdan hiçbir veri gelmediyse kalıcı önbellekten yükle
    const savedProfilJson = await AsyncStorage.getItem(PROFIL_CACHE_KEY);
    if (savedProfilJson) {
      const parsed = JSON.parse(savedProfilJson);
      cachedProfil = parsed;
      cachedProfilAt = Date.now();
      return parsed;
    }

    const savedLogo = await AsyncStorage.getItem(LOGO_CACHE_KEY);
    if (savedLogo) {
      const fallback = {
        logoBase64: savedLogo,
        LogoBase64: savedLogo,
        logoFatura: true,
        logoSiparis: true,
        logoTeklif: true,
        logoEkstre: true,
        logoRaporlar: true,
        logoTahsilat: true,
        logoOdeme: true,
        logoAcilisBakiye: true
      };
      cachedProfil = fallback;
      cachedProfilAt = Date.now();
      return fallback;
    }

    return null;
  } catch (e) {
    console.error('FirmaProfili okunamadı:', e);
    try {
      const savedLogo = await AsyncStorage.getItem(LOGO_CACHE_KEY);
      if (savedLogo) {
        return {
          logoBase64: savedLogo,
          LogoBase64: savedLogo,
          logoFatura: true,
          logoSiparis: true,
          logoTeklif: true,
          logoEkstre: true,
          logoRaporlar: true,
          logoTahsilat: true,
          logoOdeme: true,
          logoAcilisBakiye: true
        };
      }
    } catch {}
    return null;
  }
};

// Döküman tipine göre logo gösterim iznini belirle
const isLogoEnabledForEndpoint = (endpoint: string, profil: any): boolean => {
  if (!profil) return true;
  const ep = endpoint.toLowerCase();
  if (ep.includes('fatura')) return profil.logoFatura ?? profil.LogoFatura ?? true;
  if (ep.includes('siparis')) return profil.logoSiparis ?? profil.LogoSiparis ?? true;
  if (ep.includes('teklif')) return profil.logoTeklif ?? profil.LogoTeklif ?? true;
  if (ep.includes('ekstre')) return profil.logoEkstre ?? profil.LogoEkstre ?? true;
  if (ep.includes('makbuz') || ep.includes('eft') || ep.includes('kk')) return profil.logoTahsilat ?? profil.LogoTahsilat ?? true;
  return profil.logoRaporlar ?? profil.LogoRaporlar ?? true;
};

const loadFaturaTasarimi = async (): Promise<any | null> => {
  if (cachedTasarim && Date.now() - cachedTasarimAt < TASARIM_CACHE_MS) {
    return cachedTasarim;
  }
  try {
    let node = await readData('FaturaTasarimi/1');
    if (!node) {
      const raw = await readData('FaturaTasarimi');
      if (raw) node = raw[1] || raw;
    }
    if (!node) return null;
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
  const [tasarim, profil] = await Promise.all([
    TASARIM_ENDPOINTS.has(endpoint) ? loadFaturaTasarimi() : Promise.resolve(null),
    loadFirmaProfili(),
  ]);

  let rawLogo = profil?.logoBase64 || profil?.LogoBase64 || null;
  if (!rawLogo) {
    try {
      rawLogo = await AsyncStorage.getItem(LOGO_CACHE_KEY);
    } catch {}
  }
  const cleanLogo = cleanBase64Logo(rawLogo);
  const isLogoAllowed = isLogoEnabledForEndpoint(endpoint, profil);
  const shouldShowLogo = Boolean(cleanLogo && isLogoAllowed);

  if (Array.isArray(payload)) {
    return payload.map((item) => injectParams(item, tasarim, cleanLogo, shouldShowLogo));
  }
  return injectParams(payload, tasarim, cleanLogo, shouldShowLogo);
};

const injectParams = (item: any, tasarim: any, logoBase64: string | null, shouldShowLogo: boolean): any => {
  if (!item || typeof item !== 'object') return item;
  const result = { ...item };
  
  const cleanLogo = cleanBase64Logo(logoBase64);

  if (shouldShowLogo && cleanLogo) {
    result.LogoBytes = cleanLogo;
    result.ShowLogo = true;
  } else {
    result.LogoBytes = null;
    result.ShowLogo = false;
  }

  if (!tasarim) return result;
  if (result.Tasarim === undefined) result.Tasarim = tasarim;
  if (result.Size === undefined && tasarim.FaturaSize) result.Size = tasarim.FaturaSize;
  if (result.Orientation === undefined && tasarim.FaturaOrientation) result.Orientation = tasarim.FaturaOrientation;
  return result;
};

const getPdfServerUrl = async () => {
  try {
    const savedUrl = await AsyncStorage.getItem('pdf_server_url');
    if (savedUrl && !savedUrl.includes('916435485627')) return savedUrl;
    
    // Default to 7/24 Cloud Run PDF API
    return 'https://ermay-pdf-api-390930978984.europe-west1.run.app';
  } catch (e) {
    return 'https://ermay-pdf-api-390930978984.europe-west1.run.app';
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
