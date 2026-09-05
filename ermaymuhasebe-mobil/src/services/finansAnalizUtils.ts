/**
 * finansAnalizUtils.ts — Masaüstü Finans bölümünün (FinansDashboardViewModel,
 * GetFinanceTrendAsync) mobildeki saf (pure) hesaplama eşlenikleri.
 *
 * Ekranlar bu fonksiyonları kullanır; ağ / durum bağımlılığı yoktur, test edilebilir.
 */

export interface CariHareketLite {
  tarih?: string;
  alacak?: number;
  borc?: number;
  islemTuru?: string;
  yonlendirilenCariId?: string | number | null;
  evrakNo?: string;
}

export interface FaturaLite {
  tur?: string;
  genelToplam?: number;
  odenen?: number;
  kalan?: number;
  vadeTarihi?: string;
  tarih?: string;
  isDeleted?: boolean;
}

export interface CekLite {
  vadeTarihi?: string;
  tutar?: number;
  cekTuru?: string;
  durum?: string;
  yonlendirilenCariId?: string | number | null;
  isDeleted?: boolean;
}

export interface SenetLite {
  vadeTarihi?: string;
  tutar?: number;
  cekTuru?: string;
  durum?: string;
  isDeleted?: boolean;
}

export interface KasaBankaHareketLite {
  tarih?: string;
  giren?: number;
  cikan?: number;
  islemTuru?: string | null;
}

export type DashboardPeriod = 'weekly' | 'monthly' | '6month' | 'yearly';

/** Desktop dönem filtresi: Haftalık (-7g), Aylık (-30g), 6 Aylık (-6 ay), Yıllık (-1 yıl). */
export const getPeriodBounds = (period: DashboardPeriod): { start: Date; end: Date } => {
  const end = new Date();
  const start = new Date();
  if (period === 'weekly') start.setDate(start.getDate() - 7);
  else if (period === 'monthly') start.setDate(start.getDate() - 30);
  else if (period === '6month') start.setMonth(start.getMonth() - 6);
  else if (period === 'yearly') start.setFullYear(start.getFullYear() - 1);
  return { start, end };
};

const inRange = (tarih: string | undefined, start: Date, end: Date): boolean => {
  if (!tarih) return false;
  const d = new Date(tarih);
  if (isNaN(d.getTime())) return false;
  return d >= start && d <= end;
};

const isKasaTuru = (t: string | undefined): boolean => {
  if (!t) return false;
  return t.includes('Nakit');
};
const isKkTuru = (t: string | undefined): boolean => {
  if (!t) return false;
  return t.includes('KK') || t.includes('Kredi');
};
const isEftTuru = (t: string | undefined): boolean => {
  if (!t) return false;
  return t.includes('EFT') || t.includes('Havale');
};

export interface FinansKarneler {
  kendiNakit: number;
  kendiKK: number;
  kendiEFT: number;
  kendiCek: number;
  yonlendirilenNakit: number;
  yonlendirilenKK: number;
  yonlendirilenEFT: number;
  yonlendirilenCek: number;
}

/**
 * Desktop FinansDashboardViewModel "Kendi Kasamızda Kalan Net Paralar" ve
 * "Tedarikçiye Yönlendirilen Paralar (Ciro)" kartlarının hesabı.
 * Tahsilat = CariHareket (Alacak>0, "Fatura" içermeyen, tarih aralığında).
 */
export const hesapKarneler = (
  cariHareketler: CariHareketLite[],
  cekler: CekLite[],
  period: DashboardPeriod,
  bugun?: Date
): FinansKarneler => {
  const { start, end } = getPeriodBounds(period);
  const today = bugun || new Date();
  const todayStr = today.toISOString().split('T')[0];

  const tahsilat = cariHareketler.filter(h =>
    (h.alacak || 0) > 0 &&
    !(h.islemTuru || '').includes('Fatura') &&
    inRange(h.tarih, start, end)
  );

  const sum = (list: CariHareketLite[]) => list.reduce((s, h) => s + (h.alacak || 0), 0);

  const yonlendirilmis = (h: CariHareketLite) => h.yonlendirilenCariId != null && h.yonlendirilenCariId !== '' && h.yonlendirilenCariId !== 0;

  const kendiNakit = sum(tahsilat.filter(h => !yonlendirilmis(h) && isKasaTuru(h.islemTuru)));
  const kendiKK = sum(tahsilat.filter(h => !yonlendirilmis(h) && isKkTuru(h.islemTuru)));
  const kendiEFT = sum(tahsilat.filter(h => !yonlendirilmis(h) && isEftTuru(h.islemTuru)));

  const yonlendirilenNakit = sum(tahsilat.filter(h => yonlendirilmis(h) && isKasaTuru(h.islemTuru)));
  const yonlendirilenKK = sum(tahsilat.filter(h => yonlendirilmis(h) && isKkTuru(h.islemTuru)));
  const yonlendirilenEFT = sum(tahsilat.filter(h => yonlendirilmis(h) && isEftTuru(h.islemTuru)));

  const aktifCekler = cekler.filter(c => !c.isDeleted && c.cekTuru !== 'Verilen');
  const durumHaric = (d: string | undefined) => d !== 'Kayıp' && d !== 'Iptal' && d !== 'İptal' && d !== 'Karşılıksız';

  const kendiCek = aktifCekler
    .filter(c => inRange(c.vadeTarihi, start, end) && durumHaric(c.durum) && (c.yonlendirilenCariId == null || c.yonlendirilenCariId === '' || c.yonlendirilenCariId === 0))
    .reduce((s, c) => s + (c.tutar || 0), 0);

  const yonlendirilenCek = aktifCekler
    .filter(c => inRange(c.vadeTarihi, start, end) && durumHaric(c.durum) && c.yonlendirilenCariId != null && c.yonlendirilenCariId !== '' && c.yonlendirilenCariId !== 0)
    .reduce((s, c) => s + (c.tutar || 0), 0);

  // Vadesi bugünden önce olan "kendi" çekler (gecikmiş) karniye dahil:
  // Desktop: vade aralıkta; bizde vadesi geçmiş çekler de portföyde sayılır.
  void todayStr;

  return {
    kendiNakit, kendiKK, kendiEFT, kendiCek,
    yonlendirilenNakit, yonlendirilenKK, yonlendirilenEFT, yonlendirilenCek,
  };
};

export interface AlisFaturaKarnesi {
  toplam: number;
  odenen: number;
  kalan: number;
  oran: number;
  gecikmis: number;
  ondenOdenen: number;
}

/** Alış Faturaları Kapatma Karnesi (tüm aktif alış faturaları, dönem dışı). */
export const hesapAlisFaturaKarnesi = (faturalar: FaturaLite[], bugun?: Date): AlisFaturaKarnesi => {
  const today = bugun || new Date();
  const todayStr = today.toISOString().split('T')[0];
  const alis = faturalar.filter(f => !f.isDeleted && (f.tur === 'Alış' || f.tur === 'Alis'));

  const toplam = alis.reduce((s, f) => s + (f.genelToplam || 0), 0);
  const odenen = alis.reduce((s, f) => s + (f.odenen || 0), 0);
  const kalan = alis.reduce((s, f) => s + (f.kalan ?? (f.genelToplam || 0) - (f.odenen || 0)), 0);
  const oran = toplam > 0 ? (odenen / toplam) * 100 : 0;

  const gecikmis = alis
    .filter(f => f.vadeTarihi && f.vadeTarihi < todayStr && (f.kalan ?? (f.genelToplam || 0) - (f.odenen || 0)) > 0)
    .reduce((s, f) => s + (f.kalan ?? (f.genelToplam || 0) - (f.odenen || 0)), 0);

  const ondenOdenen = alis
    .filter(f => f.vadeTarihi && f.vadeTarihi >= todayStr && (f.odenen || 0) > 0)
    .reduce((s, f) => s + (f.odenen || 0), 0);

  return { toplam, odenen, kalan, oran, gecikmis, ondenOdenen };
};

/** FIFO benzeri ağırlıklı gün hesabı (desktop CalculateAverageDays). */
export const hesaplaOrtalamaGun = (
  faturalar: { tarih?: string; tutar: number }[],
  odemeler: { tarih?: string; tutar: number }[]
): number => {
  const inv = faturalar
    .filter(f => f.tutar > 0)
    .sort((a, b) => String(a.tarih || '').localeCompare(String(b.tarih || '')));
  const pays = odemeler
    .filter(p => p.tutar > 0)
    .sort((a, b) => String(a.tarih || '').localeCompare(String(b.tarih || '')));

  let invRemaining = inv.map(f => ({ tarih: f.tarih, tutar: f.tutar }));
  let totalWeightedDays = 0;
  let totalMatched = 0;

  for (const p of pays) {
    let payRemaining = p.tutar;
    for (let i = 0; i < invRemaining.length && payRemaining > 0; i++) {
      const invItem = invRemaining[i];
      const matched = Math.min(payRemaining, invItem.tutar);
      if (matched > 0) {
        let gun = 0;
        if (p.tarih && invItem.tarih) {
          const diff = (new Date(p.tarih).getTime() - new Date(invItem.tarih).getTime()) / 86400000;
          gun = diff > 0 ? diff : 0;
        }
        totalWeightedDays += gun * matched;
        totalMatched += matched;
      }
      invItem.tutar -= matched;
      payRemaining -= matched;
      if (invItem.tutar <= 0) invRemaining.splice(i--, 1);
    }
  }

  if (totalMatched <= 0) return 0;
  return Math.round((totalWeightedDays / totalMatched) * 10) / 10;
};

export interface OrtalamaVadeler {
  tahsilatGunu: number;
  odemeGunu: number;
}

/**
 * Ortalama Tahsilat Günü = satış faturaları vs müşteri ödemeleri (Alacak>0, Fatura yok).
 * Ortalama Ödeme Günü = alış faturaları vs tedarikçi ödemeleri (Borc>0).
 */
export const hesapOrtalamaVadeler = (
  faturalar: FaturaLite[],
  cariHareketler: CariHareketLite[]
): OrtalamaVadeler => {
  const aktif = faturalar.filter(f => !f.isDeleted);
  const satisFaturalari = aktif.filter(f => f.tur === 'Satış' || f.tur === 'Satis')
    .map(f => ({ tarih: f.tarih, tutar: f.genelToplam || 0 }));
  const alisFaturalari = aktif.filter(f => f.tur === 'Alış' || f.tur === 'Alis')
    .map(f => ({ tarih: f.tarih, tutar: f.genelToplam || 0 }));

  const musteriOdemeleri = cariHareketler
    .filter(h => (h.alacak || 0) > 0 && !(h.islemTuru || '').includes('Fatura'))
    .map(h => ({ tarih: h.tarih, tutar: h.alacak || 0 }));
  const tedarikciOdemeleri = cariHareketler
    .filter(h => (h.borc || 0) > 0)
    .map(h => ({ tarih: h.tarih, tutar: h.borc || 0 }));

  return {
    tahsilatGunu: hesaplaOrtalamaGun(satisFaturalari, musteriOdemeleri),
    odemeGunu: hesaplaOrtalamaGun(alisFaturalari, tedarikciOdemeleri),
  };
};

export type TrendPeriod = 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';

export interface TrendItem {
  label: string;
  income: number;
  redirected: number;
  expense: number;
}

const isTahsilat = (t: string | null | undefined): boolean => !!t && t.includes('Tahsilat');
const isOdeme = (t: string | null | undefined): boolean =>
  !!t && (t.includes('Ödeme') || t.includes('Odeme'));
const isRedirected = (t: string | null | undefined): boolean => !isTahsilat(t) && !isOdeme(t);

/**
 * Desktop `GetFinanceTrendAsync` eşleniği.
 * Kasa + Banka hareketleri birleştirilir; IslemTuru Tahsilat => Income,
 * Ödeme => Expense, diğer => Redirected (Giren). Günlük: son 10 gün,
 * Haftalık: son 8 hafta, Aylık: son 12 ay, Yıllık: son 5 yıl.
 */
export const hesapFinansTrend = (
  kasaHareketler: KasaBankaHareketLite[],
  bankaHareketler: KasaBankaHareketLite[],
  period: TrendPeriod,
  bugun?: Date
): TrendItem[] => {
  const today = bugun || new Date();
  const all = [
    ...kasaHareketler.map(h => ({ tarih: h.tarih, giren: h.giren || 0, cikan: h.cikan || 0, islemTuru: h.islemTuru || null })),
    ...bankaHareketler.map(h => ({ tarih: h.tarih, giren: h.giren || 0, cikan: h.cikan || 0, islemTuru: h.islemTuru || null })),
  ].filter(h => h.tarih);

  const make = (label: string, list: typeof all): TrendItem => {
    const income = list.filter(h => isTahsilat(h.islemTuru)).reduce((s, h) => s + h.giren, 0);
    const expense = list.filter(h => isOdeme(h.islemTuru)).reduce((s, h) => s + h.cikan, 0);
    const redirected = list.filter(h => isRedirected(h.islemTuru)).reduce((s, h) => s + h.giren, 0);
    return { label, income, redirected, expense };
  };

  const results: TrendItem[] = [];

  if (period === 'Daily') {
    const start = new Date(today); start.setDate(start.getDate() - 9);
    const startStr = start.toISOString().split('T')[0];
    const dAll = all.filter(h => (h.tarih || '') >= startStr);
    for (let i = 0; i < 10; i++) {
      const d = new Date(start); d.setDate(d.getDate() + i);
      const dayStr = d.toISOString().split('T')[0];
      const dData = dAll.filter(h => (h.tarih || '').startsWith(dayStr));
      results.push(make(d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' }), dData));
    }
  } else if (period === 'Weekly') {
    const start = new Date(today); start.setDate(start.getDate() - 49);
    const startStr = start.toISOString().split('T')[0];
    const wAll = all.filter(h => (h.tarih || '') >= startStr);
    for (let i = 0; i < 8; i++) {
      const d = new Date(start); d.setDate(d.getDate() + i * 7);
      const wEnd = new Date(d); wEnd.setDate(wEnd.getDate() + 6);
      const wStartStr = d.toISOString().split('T')[0];
      const wEndStr = wEnd.toISOString().split('T')[0];
      const wData = wAll.filter(h => (h.tarih || '') >= wStartStr && (h.tarih || '') <= wEndStr);
      const weekNum = getWeekOfYear(d);
      results.push(make(`${weekNum}. Hafta`, wData));
    }
  } else if (period === 'Yearly') {
    const startYear = today.getFullYear() - 4;
    const startStr = `${startYear}-01-01`;
    const yAll = all.filter(h => (h.tarih || '') >= startStr);
    for (let i = 0; i < 5; i++) {
      const year = startYear + i;
      const yData = yAll.filter(h => (h.tarih || '').startsWith(`${year}-`));
      results.push(make(year.toString(), yData));
    }
  } else {
    // Monthly: son 12 ay
    for (let i = 11; i >= 0; i--) {
      const m = new Date(today.getFullYear(), today.getMonth() - i, 1);
      const prefix = `${m.getFullYear()}-${String(m.getMonth() + 1).padStart(2, '0')}`;
      const mData = all.filter(h => (h.tarih || '').startsWith(prefix));
      results.push(make(m.toLocaleDateString('tr-TR', { month: 'short' }), mData));
    }
  }

  return results;
};

const getWeekOfYear = (d: Date): number => {
  const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  const dayNum = date.getUTCDay() || 7;
  date.setUTCDate(date.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
  return Math.ceil((((date.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
};
