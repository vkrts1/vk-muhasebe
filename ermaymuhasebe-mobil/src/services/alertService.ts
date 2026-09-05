/**
 * alertService.ts — Masaüstü `MainViewModel.RunAlertChecksAsync` mobil eşleniği.
 *
 * RTDB verisinden 9 alarm grubunu saf (pure) fonksiyonlarla hesaplar.
 * Ağ / durum bağımlılığı yoktur; sonuçlar uygulama içi bildirim paneli
 * ve (isteğe bağlı) yerel `expo-notifications` için listelenir.
 *
 * Gruplar (masaüstü paritesi):
 *   1. Düşük Stok     2. Negatif Stok     3. Vadesi Geçen Alacak
 *   4. Vade Yaklaşma  5. Cari Risk Limiti 6. Çek/Senet Vade
 *   7. Bakiye Yaşlandırma                  8. Günlük Finansal Özet
 *   9. (Gösterim) Kritik Stok özeti
 */

export interface AlertItem {
  id: string;
  baslik: string;
  mesaj: string;
  tur: 'uyari' | 'info' | 'kritik';
  tarih: string;
}

export interface AlertInput {
  faturalar?: any[];
  stoklar?: any[];
  cariler?: any[];
  cekler?: any[];
  senetler?: any[];
  vadeYaklasmaGun?: number;
  cekSenetVadeGun?: number;
  lowStockThreshold?: number;
  riskLimitAktif?: boolean;
  bugun?: string;
}

const empty = () => [];

/** Gün içi ISO tarihi (varsayılan: bugün). */
const gunTarihi = (bugun?: string) => bugun || new Date().toISOString().split('T')[0];

/** 1. Düşük Stok + 2. Negatif Stok uyarıları. */
export const stokUyarilari = (stoklar: any[] = [], lowStockThreshold = 5): AlertItem[] => {
  const aktif = stoklar.filter(s => !s.isDeleted);
  const cikti: AlertItem[] = [];
  const kritik = aktif.filter(s => s.miktar < lowStockThreshold);
  const negatif = aktif.filter(s => s.miktar < 0);

  if (kritik.length > 0) {
    cikti.push({
      id: 'dusuk_stok',
      baslik: 'Düşük Stok Uyarısı',
      mesaj: `${kritik.length} adet ürünün stoku kritik seviyenin (${lowStockThreshold} Adet) altına düştü!`,
      tur: 'uyari',
      tarih: gunTarihi(),
    });
  }
  if (negatif.length > 0) {
    cikti.push({
      id: 'negatif_stok',
      baslik: 'Negatif Stok Uyarısı',
      mesaj: `${negatif.length} adet ürünün stoku sıfırın altına (eksi bakiye) düştü!`,
      tur: 'kritik',
      tarih: gunTarihi(),
    });
  }
  return cikti;
};

/** 3. Vadesi Geçen Alacak + 4. Vade Yaklaşma uyarıları (açık hesap satış). */
export const alacakUyarilari = (faturalar: any[] = [], yaklasmaGun = 7, bugun?: string): AlertItem[] => {
  const aktif = faturalar.filter(f => !f.isDeleted && f.tur === 'Satış' && (f.odemeSekli || '') !== 'Peşin');
  const bugunS = gunTarihi(bugun);
  const bugunT = new Date(bugunS).getTime();
  const cikti: AlertItem[] = [];

  const gecikenler = aktif.filter(f => {
    const vade = f.vadeTarihi ? new Date(f.vadeTarihi).getTime() : 0;
    const kalan = (f.genelToplam || 0) - (f.odenen || 0);
    return vade > 0 && vade < bugunT && kalan > 0;
  });
  if (gecikenler.length > 0) {
    cikti.push({
      id: 'geciken_alacak',
      baslik: 'Geciken Alacak Uyarısı',
      mesaj: `${gecikenler.length} adet açık hesap faturanın ödeme vadesi geçti!`,
      tur: 'kritik',
      tarih: bugunS,
    });
  }

  const limitT = bugunT + yaklasmaGun * 86400000;
  const yaklasanlar = aktif.filter(f => {
    const vade = f.vadeTarihi ? new Date(f.vadeTarihi).getTime() : 0;
    const kalan = (f.genelToplam || 0) - (f.odenen || 0);
    return vade >= bugunT && vade <= limitT && kalan > 0;
  });
  if (yaklasanlar.length > 0) {
    cikti.push({
      id: 'vade_yaklasan',
      baslik: 'Yaklaşan Fatura Vadesi',
      mesaj: `${yaklasanlar.length} adet faturanın vadesine ${yaklasmaGun} günden az kaldı!`,
      tur: 'uyari',
      tarih: bugunS,
    });
  }
  return cikti;
};

/** 5. Cari Risk Limiti aşımları. */
export const riskLimitiUyarilari = (cariler: any[] = [], aktif = true): AlertItem[] => {
  if (!aktif) return empty();
  const asanlar = cariler.filter(c => !c.isDeleted && (c.riskLimiti || 0) > 0 && ((c.borc || 0) - (c.alacak || 0)) > (c.riskLimiti || 0));
  if (asanlar.length === 0) return empty();
  return [{
    id: 'risk_limiti',
    baslik: 'Risk Limiti Aşımı',
    mesaj: `${asanlar.length} adet carinin borcu tanımlı risk limitini aştı!`,
    tur: 'uyari',
    tarih: gunTarihi(),
  }];
};

/** 6. Çek/Senet vade hatırlatıcısı. */
export const evrakVadeUyarilari = (cekler: any[] = [], senetler: any[] = [], gun = 2, bugun?: string): AlertItem[] => {
  const bugunS = gunTarihi(bugun);
  const bugunT = new Date(bugunS).getTime();
  const limitT = bugunT + gun * 86400000;
  const durumHaric = (d: string) => d !== 'Tahsil Edildi' && d !== 'Ödendi';

  const yaklasanCekler = cekler.filter(c => {
    const vade = c.vadeTarihi ? new Date(c.vadeTarihi).getTime() : 0;
    return vade >= bugunT && vade <= limitT && durumHaric(c.durum || '');
  });
  const yaklasanSenetler = senetler.filter(s => {
    const vade = s.vadeTarihi ? new Date(s.vadeTarihi).getTime() : 0;
    return vade >= bugunT && vade <= limitT && durumHaric(s.durum || '');
  });
  const toplam = yaklasanCekler.length + yaklasanSenetler.length;
  if (toplam === 0) return empty();
  return [{
    id: 'evrak_vade',
    baslik: 'Evrak Vade Hatırlatıcısı',
    mesaj: `${toplam} adet çek/senedin vadesine ${gun} gün veya daha az kaldı!`,
    tur: 'uyari',
    tarih: bugunS,
  }];
};

/** 7. Bakiye yaşlandırma: vadesi 30 günden fazla geciken açık hesap satışları. */
export const yaslandirmaUyarilari = (faturalar: any[] = [], bugun?: string): AlertItem[] => {
  const bugunS = gunTarihi(bugun);
  const otuzGunOnce = new Date(bugunS).getTime() - 30 * 86400000;
  const yaslilar = faturalar.filter(f => {
    if (f.isDeleted || f.tur !== 'Satış' || (f.odemeSekli || '') === 'Peşin') return false;
    const vade = f.vadeTarihi ? new Date(f.vadeTarihi).getTime() : 0;
    const kalan = (f.genelToplam || 0) - (f.odenen || 0);
    return vade > 0 && vade < otuzGunOnce && kalan > 0;
  });
  if (yaslilar.length === 0) return empty();
  return [{
    id: 'yaslanan_borc',
    baslik: 'Yaşlandırılmış Borç Uyarısı',
    mesaj: `${yaslilar.length} adet faturanın ödeme vadesi 30 günden fazla gecikti!`,
    tur: 'uyari',
    tarih: bugunS,
  }];
};

/** 8. Günlük finansal özet (satış + tahsilat, o günün faturaları). */
export const gunlukOzet = (faturalar: any[] = [], bugun?: string): AlertItem => {
  const bugunS = gunTarihi(bugun);
  const bugunFaturalar = faturalar.filter(f => !f.isDeleted && f.tarih === bugunS);
  const satis = bugunFaturalar.filter(f => f.tur === 'Satış').reduce((s, f) => s + (f.genelToplam || 0), 0);
  const tahsilat = bugunFaturalar.reduce((s, f) => s + ((f.genelToplam || 0) - (f.odenen || 0)), 0);
  return {
    id: 'gunluk_ozet',
    baslik: 'Günlük Finansal Özet',
    mesaj: `Satış: ${satis.toLocaleString('tr-TR', { maximumFractionDigits: 2 })} TL | Tahsilat: ${tahsilat.toLocaleString('tr-TR', { maximumFractionDigits: 2 })} TL`,
    tur: 'info',
    tarih: bugunS,
  };
};

/** 9. Kritik stok / alarm özeti — tüm grupları tek çağrıda toplar. */
export const tumUyarilar = (input: AlertInput): AlertItem[] => {
  const bugunS = gunTarihi(input.bugun);
  const stok = stokUyarilari(input.stoklar, input.lowStockThreshold ?? 5);
  const alacak = alacakUyarilari(input.faturalar, input.vadeYaklasmaGun ?? 7, bugunS);
  const risk = riskLimitiUyarilari(input.cariler, input.riskLimitAktif ?? true);
  const evrak = evrakVadeUyarilari(input.cekler, input.senetler, input.cekSenetVadeGun ?? 2, bugunS);
  const yasli = yaslandirmaUyarilari(input.faturalar, bugunS);
  const ozet = gunlukOzet(input.faturalar, bugunS);

  const kritikSayi = stok.filter(i => i.tur === 'kritik').length;
  const uyariSayi = [...stok, ...alacak, ...risk, ...evrak, ...yasli].filter(i => i.tur === 'uyari').length;

  const ozetItem: AlertItem = {
    id: 'alarm_ozeti',
    baslik: 'Alarm Özeti',
    mesaj: `Kritik: ${kritikSayi} uyarı | Bildirim: ${[...stok, ...alacak, ...risk, ...evrak, ...yasli].length} uyarı`,
    tur: 'info',
    tarih: bugunS,
  };

  return [ozetItem, ...stok, ...alacak, ...risk, ...evrak, ...yasli, ozet];
};

let Notifications: any = null;
try {
  Notifications = require('expo-notifications');
} catch (e) {
  // Safe fail
}

export const initNotifications = async () => {
  if (!Notifications) return;
  try {
    await Notifications.setNotificationHandler({
      handleNotification: async () => ({
        shouldShowAlert: true,
        shouldPlaySound: true,
        shouldSetBadge: false,
      }),
    });
  } catch (e) {
    console.warn("Notifications init error:", e);
  }
};

export const sendLocalNotification = async (title: string, body: string) => {
  if (!Notifications) {
    console.log(`[Bildirim Simüle] ${title}: ${body}`);
    return;
  }
  try {
    await Notifications.scheduleNotificationAsync({
      content: {
        title,
        body,
        sound: true,
      },
      trigger: null,
    });
  } catch (e) {
    console.error("Local notification error:", e);
  }
};
