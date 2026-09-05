/**
 * analizUtils.ts — Saf (pure) analiz/Araç hesaplarının ortak modülü.
 *
 * Masaüstündeki rapor ve araç ViewModel mantıklarının mobil eşleniklerini
 * içerir; ekranlar bu fonksiyonları kullanır, testler doğrudan bu fonksiyonları
 * çalıştırır (ağ / durum bağımlılığı yok).
 */

export interface StokHareketLite {
  stokId?: string | number;
  tarih?: string;
  miktar?: number;
  giren?: number;
  cikan?: number;
  islemTuru?: string;
  birimFiyat?: number;
}

export interface StokLite {
  id?: string | number;
  stokAdi?: string;
  alisFiyati?: number;
  satisFiyati?: number;
  ortAlisFiyati?: number;
}

export interface FaturaLite {
  tur?: string;
  cariId?: string | number;
  cariUnvan?: string;
  genelToplam?: number;
  tarih?: string;
  kayitTarihi?: string;
  isDeleted?: boolean;
}

export interface CekLite {
  banka?: string;
  tutar?: number;
  vadeTarihi?: string;
  durum?: string;
}

/**
 * FIFO (İlk Giren İlk Çıkar) maliyet hesabı.
 * Stok hareketleri tarih sırasında işlenir; girişler lot kuyruğuna eklenir,
 * çıkışlar en eski lotlardan tüketilir. Lot birim fiyatı: hareket.birimFiyat,
 * yoksa stok alış fiyatı.
 */
export const hesapFifo = (moves: StokHareketLite[], stok: StokLite = {}) => {
  const sirali = [...moves]
    .filter(h => h.miktar && h.miktar > 0)
    .sort((a, b) => String(a.tarih || '').localeCompare(String(b.tarih || '')));

  let lot: { adet: number; birim: number }[] = [];
  let satilanAdet = 0;
  let fifoMaliyet = 0;
  let satis = 0;

  for (const h of sirali) {
    const adet = h.miktar || 0;
    const tur = String(h.islemTuru || '');
    const giris = (h.giren || 0) > 0 || tur.includes('Alış') || tur.includes('Giriş');
    if (giris) {
      lot.push({ adet, birim: h.birimFiyat || stok.alisFiyati || 0 });
    } else {
      let kalan = adet;
      let maliyet = 0;
      while (kalan > 0 && lot.length > 0) {
        const al = Math.min(kalan, lot[0].adet);
        maliyet += al * lot[0].birim;
        lot[0].adet -= al;
        kalan -= al;
        if (lot[0].adet <= 0) lot.shift();
      }
      satilanAdet += adet;
      fifoMaliyet += maliyet;
      satis += adet * (stok.satisFiyati || 0);
    }
  }

  return { satilanAdet, fifoMaliyet, satis, kar: satis - fifoMaliyet };
};

/** Çapraz kur çevirici: kaynak birimdeki miktarı hedef birime çevirir. */
export const hesapKur = (params: {
  miktar: number;
  kaynak: string;
  hedef: string;
  kurlar: Record<string, number>;
}) => {
  const { miktar, kaynak, hedef, kurlar } = params;
  if (miktar <= 0) return 0;
  const kaynakKur = kurlar[kaynak] || 1;
  const hedefKur = kurlar[hedef] || 1;
  if (hedefKur <= 0) return 0;
  return (miktar * kaynakKur) / hedefKur;
};

export interface OptimalFiyatParam {
  alisFiyati: number;
  ekMaliyet?: number;
  karOrani?: number;
  kdvOrani?: number;
}

/** Optimal satış fiyatı (desktop OptimalFiyatViewModel mantığı). */
export const hesapOptimalFiyat = (p: OptimalFiyatParam & { hedefKarOrani?: number }) => {
  const alis = p.alisFiyati || 0;
  const ek = p.ekMaliyet || 0;
  const karOrani = p.karOrani ?? p.hedefKarOrani ?? 0;
  const kdvOrani = p.kdvOrani || 0;
  const toplamMaliyet = alis + ek;
  const karTutari = toplamMaliyet * (karOrani / 100);
  const satisKdvHaric = toplamMaliyet + karTutari;
  const kdvTutari = satisKdvHaric * (kdvOrani / 100);
  return { satisFiyati: satisKdvHaric + kdvTutari, netKar: karTutari, kdv: kdvTutari };
};

const IYI_BANKALAR = ['ziraat', 'iş', 'isbank', 'garanti', 'akbank', 'yapı', 'yapi', 'qnb', 'finans', 'halk', 'vakıf', 'vakif', 'denizbank', 'teb'];

/** Çek risk puanı + etiketi (FinansScreen ve RaporlarScreen ortak mantığı). */
export const cekRiskPuani = (cek: CekLite, bugun?: string) => {
  let puan = 0;
  const banka = (cek.banka || '').toLowerCase();
  if (IYI_BANKALAR.some(b => banka.includes(b))) puan += 10;
  else if (banka) puan += 35;
  else puan += 40;
  const tutar = cek.tutar || 0;
  if (tutar > 500000) puan += 15;
  else if (tutar > 100000) puan += 8;
  const bugunTarih = bugun || new Date().toISOString().split('T')[0];
  if (cek.vadeTarihi && cek.vadeTarihi < bugunTarih) puan += 35;
  if ((cek.durum || 'Portföyde') === 'Ciro Edildi' || cek.durum === 'Tahsil Edildi') puan = Math.min(puan, 30);
  const label = puan >= 70 ? 'Yüksek Risk' : puan >= 40 ? 'Orta Risk' : 'Düşük Risk';
  return { puan, label };
};

/**
 * Müşteri LTV (Yaşam Boyu Değer): ortalama sepet × aylık frekans × 12 ay.
 * Masaüstünde LTV formülündeki GDV + kar marjı + tekrar satın alma girdilerinin
 * mobildeki veriyle hesaplanabilen saflaştırılmış karşılığı.
 */
export const hesapLtv = (faturalar: FaturaLite[]) => {
  const satis = faturalar.filter(f => (f.tur === 'Satış' || f.tur === 'Satis') && !f.isDeleted);
  const map = new Map<string, { ciro: number; adet: number; ilk?: string; son?: string }>();

  for (const f of satis) {
    const key = f.cariUnvan || 'Bilinmeyen';
    const tarih = f.tarih || f.kayitTarihi;
    const mevcut = map.get(key) || { ciro: 0, adet: 0 };
    mevcut.ciro += f.genelToplam || 0;
    mevcut.adet += 1;
    if (tarih) {
      if (!mevcut.ilk || tarih < mevcut.ilk) mevcut.ilk = tarih;
      if (!mevcut.son || tarih > mevcut.son) mevcut.son = tarih;
    }
    map.set(key, mevcut);
  }

  const sonuc: { musteri: string; ciro: number; adet: number; sepet: number; frekans: number; ltv: number; ilk?: string; son?: string }[] = [];
  for (const [musteri, m] of map.entries()) {
    const sepet = m.adet > 0 ? m.ciro / m.adet : 0;
    const ilk = m.ilk ? new Date(m.ilk).getTime() : Date.now();
    const son = m.son ? new Date(m.son).getTime() : Date.now();
    const ayAraligi = Math.max(1, (son - ilk) / (1000 * 60 * 60 * 24 * 30));
    const frekans = ayAraligi > 0 ? m.adet / ayAraligi : 0;
    const ltv = sepet * frekans * 12;
    sonuc.push({ musteri, ciro: m.ciro, adet: m.adet, sepet, frekans, ltv, ilk: m.ilk, son: m.son });
  }

  return sonuc.sort((a, b) => b.ltv - a.ltv);
};
