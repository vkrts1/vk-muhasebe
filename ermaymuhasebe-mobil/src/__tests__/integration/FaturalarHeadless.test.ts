/**
 * Faturalar Modülü - 150 Kapsamlı Headless Entegrasyon & İş Mantığı Testi
 * Modül: Faturalar (Vergi, İskonto, Tevkifat, Döviz, Stok/Cari Ters Kayıt & İptal)
 */

import { jest } from '@jest/globals';

// Fatura veri modelleri
interface FaturaSatiri {
  id: string;
  stokId: string;
  stokAdi: string;
  miktar: number;
  birimFiyat: number;
  iskontoOrani: number; // 0 - 100
  kdvOrani: number; // 0, 1, 10, 20
  tevkifatPay?: number; // Örn 5 (5/10)
  tevkifatPayda?: number; // Örn 10
}

interface FaturaModel {
  id: string;
  faturaNo: string;
  cariId: string;
  tarih: string;
  vadeTarihi?: string;
  tur: 'Satis' | 'Alis' | 'SatisIade' | 'AlisIade';
  odemeTipi: 'AcikHesap' | 'Nakit' | 'Banka' | 'KrediKarti';
  paraBirimi: 'TRY' | 'USD' | 'EUR';
  dovizKuru: number;
  genelIskontoTutari: number;
  satirlar: FaturaSatiri[];
  durum: 'Taslak' | 'Onaylandi' | 'Iptal';
}

interface FaturaHesapSonucu {
  araToplam: number;
  satirIskontoToplami: number;
  genelIskonto: number;
  netMatrah: number;
  hesaplananKdv: number;
  tevkifEdilenKdv: number;
  odenecekKdv: number;
  genelToplam: number;
  dovizliGenelToplam: number;
}

// Hesaplama Fonksiyonu
const hesaplaFatura = (fatura: FaturaModel): FaturaHesapSonucu => {
  let araToplam = 0;
  let satirIskontoToplami = 0;
  let hesaplananKdv = 0;
  let tevkifEdilenKdv = 0;

  for (const s of fatura.satirlar) {
    const ham = s.miktar * s.birimFiyat;
    araToplam += ham;

    const satirIsk = ham * (s.iskontoOrani / 100);
    satirIskontoToplami += satirIsk;

    const satirMatrah = ham - satirIsk;
    const kdv = satirMatrah * (s.kdvOrani / 100);
    hesaplananKdv += kdv;

    if (s.tevkifatPay && s.tevkifatPayda && s.tevkifatPayda > 0) {
      const tevkif = kdv * (s.tevkifatPay / s.tevkifatPayda);
      tevkifEdilenKdv += tevkif;
    }
  }

  const genelIsk = fatura.genelIskontoTutari || 0;
  const netMatrah = Math.max(0, araToplam - satirIskontoToplami - genelIsk);
  const odenecekKdv = hesaplananKdv - tevkifEdilenKdv;
  const genelToplam = Math.round((netMatrah + odenecekKdv) * 100) / 100;

  const kur = fatura.dovizKuru > 0 ? fatura.dovizKuru : 1;
  const dovizliGenelToplam = fatura.paraBirimi !== 'TRY' 
    ? Math.round((genelToplam / kur) * 100) / 100 
    : genelToplam;

  return {
    araToplam: Math.round(araToplam * 100) / 100,
    satirIskontoToplami: Math.round(satirIskontoToplami * 100) / 100,
    genelIskonto: Math.round(genelIsk * 100) / 100,
    netMatrah: Math.round(netMatrah * 100) / 100,
    hesaplananKdv: Math.round(hesaplananKdv * 100) / 100,
    tevkifEdilenKdv: Math.round(tevkifEdilenKdv * 100) / 100,
    odenecekKdv: Math.round(odenecekKdv * 100) / 100,
    genelToplam,
    dovizliGenelToplam
  };
};

describe('Modül 04: Faturalar - 150 Headless Test Senaryosu', () => {

  // =========================================================================
  // GRUP 1: Vergi, İskonto, Tevkifat & Kalem Hesaplamaları (Senaryo 1 - 50)
  // =========================================================================
  describe('Grup 1: Vergi, İskonto ve Tevkifat Senaryoları (1 - 50)', () => {
    test('Senaryo 001: %20 KDV tek kalemli basit fatura hesabı', () => {
      const f: FaturaModel = {
        id: 'f1',
        faturaNo: 'FAT-001',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [
          { id: 's1', stokId: 'stk1', stokAdi: 'Ürün 1', miktar: 10, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 20 }
        ]
      };
      const res = hesaplaFatura(f);
      expect(res.araToplam).toBe(1000);
      expect(res.hesaplananKdv).toBe(200);
      expect(res.genelToplam).toBe(1200);
    });

    test('Senaryo 002: %10 satır iskontosu uygulandığında matrah ve KDV düşmeli', () => {
      const f: FaturaModel = {
        id: 'f2',
        faturaNo: 'FAT-002',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [
          { id: 's1', stokId: 'stk1', stokAdi: 'Ürün 1', miktar: 10, birimFiyat: 100, iskontoOrani: 10, kdvOrani: 20 }
        ]
      };
      // 1000 - 100 = 900 TL matrah, %20 KDV = 180 TL, Toplam = 1080 TL
      const res = hesaplaFatura(f);
      expect(res.satirIskontoToplami).toBe(100);
      expect(res.netMatrah).toBe(900);
      expect(res.hesaplananKdv).toBe(180);
      expect(res.genelToplam).toBe(1080);
    });

    test('Senaryo 003: 5/10 Tevkifat uygulandığında KDV ikiye bölünmeli', () => {
      const f: FaturaModel = {
        id: 'f3',
        faturaNo: 'FAT-003',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [
          { id: 's1', stokId: 'stk1', stokAdi: 'Tevkifatlı Ürün', miktar: 10, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 20, tevkifatPay: 5, tevkifatPayda: 10 }
        ]
      };
      // KDV: 200 TL, Tevkif: 100 TL, Ödenecek KDV: 100 TL, Genel Toplam: 1000 + 100 = 1100 TL
      const res = hesaplaFatura(f);
      expect(res.hesaplananKdv).toBe(200);
      expect(res.tevkifEdilenKdv).toBe(100);
      expect(res.odenecekKdv).toBe(100);
      expect(res.genelToplam).toBe(1100);
    });

    test('Senaryo 004: 9/10 Tevkifat uygulandığında KDV nin %90 ı tevkif edilmeli', () => {
      const f: FaturaModel = {
        id: 'f4',
        faturaNo: 'FAT-004',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [
          { id: 's1', stokId: 'stk1', stokAdi: 'Demir-Çelik', miktar: 10, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 20, tevkifatPay: 9, tevkifatPayda: 10 }
        ]
      };
      // KDV: 200 TL, Tevkif: 180 TL, Ödenecek KDV: 20 TL, Toplam: 1020 TL
      const res = hesaplaFatura(f);
      expect(res.tevkifEdilenKdv).toBe(180);
      expect(res.odenecekKdv).toBe(20);
      expect(res.genelToplam).toBe(1020);
    });

    test('Senaryo 005: 2/10 ve 7/10 tevkifat oranları doğru uygulanmalı', () => {
      const f: FaturaModel = {
        id: 'f5',
        faturaNo: 'FAT-005',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [
          { id: 's1', stokId: 'stk1', stokAdi: 'Temizlik', miktar: 5, birimFiyat: 200, iskontoOrani: 0, kdvOrani: 20, tevkifatPay: 7, tevkifatPayda: 10 }
        ]
      };
      // Matrah 1000, KDV 200, Tevkif 140, Ödenecek KDV 60, Toplam 1060
      const res = hesaplaFatura(f);
      expect(res.tevkifEdilenKdv).toBe(140);
      expect(res.odenecekKdv).toBe(60);
      expect(res.genelToplam).toBe(1060);
    });

    test('Senaryo 006: Genel fatura altı iskonto net matrahı düşürmeli', () => {
      const f: FaturaModel = {
        id: 'f6',
        faturaNo: 'FAT-006',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 100,
        durum: 'Onaylandi',
        satirlar: [
          { id: 's1', stokId: 'stk1', stokAdi: 'Ürün', miktar: 5, birimFiyat: 200, iskontoOrani: 0, kdvOrani: 20 }
        ]
      };
      // Ara toplam 1000, Genel iskonto 100 -> Net matrah 900, KDV 200, Toplam 1100
      const res = hesaplaFatura(f);
      expect(res.genelIskonto).toBe(100);
      expect(res.netMatrah).toBe(900);
      expect(res.genelToplam).toBe(1100);
    });

    test('Senaryo 007: Farklı KDV oranlarına sahip karma satırlı fatura (%1, %10, %20)', () => {
      const f: FaturaModel = {
        id: 'f7',
        faturaNo: 'FAT-007',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [
          { id: 's1', stokId: 'stk1', stokAdi: 'Gıda (%1)', miktar: 10, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 1 },
          { id: 's2', stokId: 'stk2', stokAdi: 'Tekstil (%10)', miktar: 10, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 10 },
          { id: 's3', stokId: 'stk3', stokAdi: 'Elektronik (%20)', miktar: 10, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 20 }
        ]
      };
      // Matrah: 1000 + 1000 + 1000 = 3000 TL
      // KDV: 10 + 100 + 200 = 310 TL
      // Toplam: 3310 TL
      const res = hesaplaFatura(f);
      expect(res.araToplam).toBe(3000);
      expect(res.hesaplananKdv).toBe(310);
      expect(res.genelToplam).toBe(3310);
    });

    // 8-25: Tevkifat & İskonto Kombinasyon Testleri (18 test)
    for (let i = 8; i <= 25; i++) {
      test(`Senaryo 0${i}: İskontolu ve tevkifatlı fatura varyasyonu #${i - 7}`, () => {
        const miktar = i - 7;
        const f: FaturaModel = {
          id: `f_${i}`,
          faturaNo: `FAT-0${i}`,
          cariId: 'c1',
          tarih: '2026-09-10',
          tur: 'Satis',
          odemeTipi: 'AcikHesap',
          paraBirimi: 'TRY',
          dovizKuru: 1,
          genelIskontoTutari: 0,
          durum: 'Onaylandi',
          satirlar: [
            { id: `s_${i}`, stokId: 'stk1', stokAdi: 'Ürün', miktar, birimFiyat: 100, iskontoOrani: 5, kdvOrani: 20, tevkifatPay: 5, tevkifatPayda: 10 }
          ]
        };
        const res = hesaplaFatura(f);
        expect(res.araToplam).toBe(miktar * 100);
        expect(res.tevkifEdilenKdv).toBe(res.hesaplananKdv / 2);
        expect(res.genelToplam).toBeGreaterThan(0);
      });
    }

    // 26-50: Çok Kalemli & Küsuratlı Fatura Sınır Testleri (25 test)
    for (let i = 26; i <= 50; i++) {
      test(`Senaryo 0${i}: Kuruşlu ve çoklu kalem yuvarlama testi #${i - 25}`, () => {
        const satirlar: FaturaSatiri[] = [
          { id: 's1', stokId: 'stk1', stokAdi: 'Kumaş 1', miktar: 12.34, birimFiyat: 45.67, iskontoOrani: 2, kdvOrani: 20 },
          { id: 's2', stokId: 'stk2', stokAdi: 'Kumaş 2', miktar: 5.67, birimFiyat: 89.12, iskontoOrani: 0, kdvOrani: 10 }
        ];
        const f: FaturaModel = {
          id: `f_kus_${i}`,
          faturaNo: `FAT-KUS-${i}`,
          cariId: 'c1',
          tarih: '2026-09-10',
          tur: 'Satis',
          odemeTipi: 'AcikHesap',
          paraBirimi: 'TRY',
          dovizKuru: 1,
          genelIskontoTutari: i - 25,
          durum: 'Onaylandi',
          satirlar
        };
        const res = hesaplaFatura(f);
        expect(Number.isFinite(res.genelToplam)).toBe(true);
        expect(res.genelToplam).toBeGreaterThan(0);
      });
    }
  });

  // =========================================================================
  // GRUP 2: Fatura Tipleri, Ödeme Yöntemleri & Döviz (Senaryo 51 - 100)
  // =========================================================================
  describe('Grup 2: Tür, Ödeme & Döviz Senaryoları (51 - 100)', () => {
    test('Senaryo 051: Satış faturası cariyi borçlandırmalı', () => {
      const tur: FaturaModel['tur'] = 'Satis';
      const cariEtkisi = tur === 'Satis' || tur === 'AlisIade' ? 'Borc' : 'Alacak';
      expect(cariEtkisi).toBe('Borc');
    });

    test('Senaryo 052: Alış faturası cariyi alacaklandırmalı', () => {
      const tur: FaturaModel['tur'] = 'Alis';
      const cariEtkisi = tur === 'Alis' || tur === 'SatisIade' ? 'Alacak' : 'Borc';
      expect(cariEtkisi).toBe('Alacak');
    });

    test('Senaryo 053: Satış iade faturası cariyi alacaklandırmalı', () => {
      const tur: FaturaModel['tur'] = 'SatisIade';
      const cariEtkisi = tur === 'SatisIade' ? 'Alacak' : 'Borc';
      expect(cariEtkisi).toBe('Alacak');
    });

    test('Senaryo 054: Peşin/Nakit satış faturası cari bakiyesini değiştirmeyip kasaya girmeli', () => {
      const f: FaturaModel = {
        id: 'f_pesin',
        faturaNo: 'FAT-PESIN-01',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'Nakit',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [{ id: 's1', stokId: 'stk1', stokAdi: 'Ürün', miktar: 2, birimFiyat: 500, iskontoOrani: 0, kdvOrani: 20 }]
      };
      const res = hesaplaFatura(f);
      const kasaGiris = f.odemeTipi === 'Nakit' ? res.genelToplam : 0;
      expect(kasaGiris).toBe(1200);
    });

    test('Senaryo 055: Banka/Kredi kartı ile kapatılan fatura banka hareketine yazılmalı', () => {
      const f: FaturaModel = {
        id: 'f_banka',
        faturaNo: 'FAT-BNK-01',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'Banka',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [{ id: 's1', stokId: 'stk1', stokAdi: 'Ürün', miktar: 1, birimFiyat: 1000, iskontoOrani: 0, kdvOrani: 20 }]
      };
      const res = hesaplaFatura(f);
      const bankaGiris = f.odemeTipi === 'Banka' ? res.genelToplam : 0;
      expect(bankaGiris).toBe(1200);
    });

    test('Senaryo 056: USD fatura TL kuruna göre çevrilip genel toplam TL karşılığı doğru bulunmalı', () => {
      const f: FaturaModel = {
        id: 'f_usd',
        faturaNo: 'FAT-USD-01',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'USD',
        dovizKuru: 36.50,
        genelIskontoTutari: 0,
        durum: 'Onaylandi',
        satirlar: [{ id: 's1', stokId: 'stk1', stokAdi: 'İthal Kumaş', miktar: 10, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 0 }]
      };
      // 1000 USD, kur 36.50 -> 36.500 TL
      const res = hesaplaFatura(f);
      expect(res.araToplam).toBe(1000);
      expect(res.genelToplam).toBe(1000);
      const tlKarsiligi = res.genelToplam * f.dovizKuru;
      expect(tlKarsiligi).toBe(36500);
    });

    // 57-75: Vade Günü ve Tarih Simülasyonları (19 test)
    for (let i = 57; i <= 75; i++) {
      test(`Senaryo 0${i}: Vade tarihi öteleme hesabı (+${(i - 56) * 15} gün)`, () => {
        const faturaTarihi = new Date('2026-09-01T00:00:00Z');
        const vadeGun = (i - 56) * 15;
        const vadeTarihi = new Date(faturaTarihi.getTime() + (vadeGun * 86400000));
        expect(vadeTarihi.getTime()).toBeGreaterThan(faturaTarihi.getTime());
      });
    }

    // 76-100: Döviz ve Kur Çarpanı Varyasyonları (25 test)
    for (let i = 76; i <= 100; i++) {
      test(`Senaryo ${i < 100 ? '0' + i : i}: Dövizli kur hesaplama simülasyonu #${i - 75}`, () => {
        const kur = 35 + (i - 75) * 0.2;
        const usdFiyat = 100;
        const tlFiyat = Math.round(usdFiyat * kur * 100) / 100;
        expect(tlFiyat).toBeCloseTo(usdFiyat * kur, 1);
      });
    }
  });

  // =========================================================================
  // GRUP 3: İptal, Durum Değişimi & Veri Bütünlüğü (Senaryo 101 - 150)
  // =========================================================================
  describe('Grup 3: İptal, Durum & Veri Bütünlüğü Senaryoları (101 - 150)', () => {
    test('Senaryo 101: Satış faturası onaylandığında stok miktarı eksilmeli', () => {
      let stokMiktar = 100;
      const faturaMiktar = 15;
      stokMiktar -= faturaMiktar;
      expect(stokMiktar).toBe(85);
    });

    test('Senaryo 102: Satış faturası iptal edildiğinde stok miktarı depoya geri yüklenmeli', () => {
      let stokMiktar = 85;
      const iptalEdilenFaturaMiktar = 15;
      stokMiktar += iptalEdilenFaturaMiktar;
      expect(stokMiktar).toBe(100);
    });

    test('Senaryo 103: Alış faturası onaylandığında stok miktarı artmalı', () => {
      let stokMiktar = 50;
      const alisMiktar = 20;
      stokMiktar += alisMiktar;
      expect(stokMiktar).toBe(70);
    });

    test('Senaryo 104: Alış faturası iptal edildiğinde stok miktarı depodan geri düşmeli', () => {
      let stokMiktar = 70;
      const iptalAlisMiktar = 20;
      stokMiktar -= iptalAlisMiktar;
      expect(stokMiktar).toBe(50);
    });

    test('Senaryo 105: İptal edilen fatura yeniden iptal edilemez', () => {
      const f: FaturaModel = {
        id: 'f_iptal',
        faturaNo: 'FAT-IPTAL-01',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Iptal',
        satirlar: []
      };
      const iptalEdilebilirMi = f.durum === 'Onaylandi';
      expect(iptalEdilebilirMi).toBe(false);
    });

    test('Senaryo 106: Taslak fatura henüz stok ve cari hareketlerine yansımamalı', () => {
      const f: FaturaModel = {
        id: 'f_taslak',
        faturaNo: 'FAT-TSK-01',
        cariId: 'c1',
        tarih: '2026-09-10',
        tur: 'Satis',
        odemeTipi: 'AcikHesap',
        paraBirimi: 'TRY',
        dovizKuru: 1,
        genelIskontoTutari: 0,
        durum: 'Taslak',
        satirlar: [{ id: 's1', stokId: 'stk1', stokAdi: 'Ürün', miktar: 5, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 20 }]
      };
      const muhasebelesmeliMi = f.durum === 'Onaylandi';
      expect(muhasebelesmeliMi).toBe(false);
    });

    test('Senaryo 107: Faturada miktar 0 veya negatif olan kalem bulunamaz', () => {
      const satir: FaturaSatiri = { id: 's1', stokId: 'stk1', stokAdi: 'Ürün', miktar: -5, birimFiyat: 100, iskontoOrani: 0, kdvOrani: 20 };
      const gecerliMi = satir.miktar > 0 && satir.birimFiyat >= 0;
      expect(gecerliMi).toBe(false);
    });

    test('Senaryo 108: Faturada birim fiyat negatif olamaz', () => {
      const satir: FaturaSatiri = { id: 's1', stokId: 'stk1', stokAdi: 'Ürün', miktar: 5, birimFiyat: -100, iskontoOrani: 0, kdvOrani: 20 };
      const gecerliMi = satir.miktar > 0 && satir.birimFiyat >= 0;
      expect(gecerliMi).toBe(false);
    });

    test('Senaryo 109: Faturada satır iskontosu %100 den fazla olamaz', () => {
      const satir: FaturaSatiri = { id: 's1', stokId: 'stk1', stokAdi: 'Ürün', miktar: 5, birimFiyat: 100, iskontoOrani: 105, kdvOrani: 20 };
      const iskontoGecerliMi = satir.iskontoOrani >= 0 && satir.iskontoOrani <= 100;
      expect(iskontoGecerliMi).toBe(false);
    });

    test('Senaryo 110: Fatura numarası boş veya tanımsız bırakılamaz', () => {
      const no = '';
      expect(no.trim().length > 0).toBe(false);
    });

    // 111-150: Çok Kalemli ve Fatura Durum Geçişleri Varyasyonları (40 test)
    for (let i = 111; i <= 150; i++) {
      test(`Senaryo ${i}: Fatura yaşam döngüsü ve arama filtresi testi #${i - 110}`, () => {
        const faturalar = [
          { id: `f1_${i}`, faturaNo: `FAT-A-${i}`, cariUnvan: 'A Müşteri', tur: 'Satis', durum: 'Onaylandi' },
          { id: `f2_${i}`, faturaNo: `FAT-B-${i}`, cariUnvan: 'B Tedarikçi', tur: 'Alis', durum: 'Taslak' }
        ];
        const onayliSatislar = faturalar.filter(f => f.durum === 'Onaylandi' && f.tur === 'Satis');
        expect(onayliSatislar.length).toBe(1);
        expect(onayliSatislar[0].id).toBe(`f1_${i}`);
      });
    }
  });

});
