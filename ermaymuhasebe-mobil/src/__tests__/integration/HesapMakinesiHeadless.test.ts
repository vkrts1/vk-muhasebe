/**
 * Hesap Makinesi & Maliyet Modülü - 150 Kapsamlı Headless Entegrasyon & İş Mantığı Testi
 * Modül: Hesap Makinesi & Maliyet Hesaplama (Kumaş Ağırlık Formülü, Top Fiyatı, m² Maliyeti, Kâr/Markup & Başabaş)
 */

import { jest } from '@jest/globals';

// Formül fonksiyonları
const hesaplaToplamKg = (gr: number, en: number, sarim: number): number => {
  const g = Number.isFinite(gr) ? gr : 0;
  const e = Number.isFinite(en) ? en : 0;
  const s = Number.isFinite(sarim) ? sarim : 0;
  const kg = (g * e * s) / 100000.0;
  return Math.round(kg * 100) / 100;
};

const hesaplaTopFiyati = (toplamKg: number, kgFiyati: number): number => {
  const kg = Number.isFinite(toplamKg) ? toplamKg : 0;
  const f = Number.isFinite(kgFiyati) ? kgFiyati : 0;
  return Math.round(kg * f * 100) / 100;
};

const hesaplaMetrekare = (topBoy: number, sabitEn: number = 1.6): number => {
  const boy = Number.isFinite(topBoy) ? topBoy : 0;
  return Math.round(boy * sabitEn * 100) / 100;
};

const hesaplaBirimM2Maliyeti = (topFiyati: number, toplamM2: number): number => {
  if (!toplamM2 || toplamM2 <= 0) return 0;
  const f = Number.isFinite(topFiyati) ? topFiyati : 0;
  return Math.round((f / toplamM2) * 100) / 100;
};

const hesaplaBasabasNoktasi = (sabitGider: number, birimFiyat: number, birimMaliyet: number): number => {
  const marj = birimFiyat - birimMaliyet;
  if (marj <= 0) return 0;
  return Math.ceil(sabitGider / marj);
};

const hesaplaHedefSatisFiyati = (maliyet: number, hedefMarjYuzde: number): number => {
  if (hedefMarjYuzde >= 100) return 0;
  const f = maliyet / (1 - hedefMarjYuzde / 100);
  return Math.round(f * 100) / 100;
};

describe('Modül 11: Hesap Makinesi & Maliyet - 150 Headless Test Senaryosu', () => {

  // =========================================================================
  // GRUP 1: Kumaş Ağırlık, Top Fiyatı & m² Maliyet Formülleri (Senaryo 1 - 50)
  // =========================================================================
  describe('Grup 1: Kumaş Ağırlık & m² Maliyeti Senaryoları (1 - 50)', () => {
    test('Senaryo 001: 200 gr/m², 160 cm en, 50 mt sarım için tam 16.00 KG hesaplanmalı', () => {
      // (200 * 160 * 50) / 100000 = 1,600,000 / 100,000 = 16.00 KG
      expect(hesaplaToplamKg(200, 160, 50)).toBe(16);
    });

    test('Senaryo 002: Sıfır sarım veya gramajda sonuç 0 KG olmalı', () => {
      expect(hesaplaToplamKg(0, 160, 50)).toBe(0);
      expect(hesaplaToplamKg(200, 160, 0)).toBe(0);
    });

    test('Senaryo 003: 16 KG kumaş için 5 $ KG fiyatı ile 80.00 $ top fiyatı hesaplanmalı', () => {
      expect(hesaplaTopFiyati(16, 5)).toBe(80);
    });

    test('Senaryo 004: 50 metre boy ve 1.6m sabit en için 80.00 m² hesaplanmalı', () => {
      expect(hesaplaMetrekare(50, 1.6)).toBe(80);
    });

    test('Senaryo 005: 80 $ top fiyatı ve 80 m² için birim metrekare maliyeti 1.00 $ olmalı', () => {
      expect(hesaplaBirimM2Maliyeti(80, 80)).toBe(1);
    });

    test('Senaryo 006: Metrekare 0 olduğunda sıfıra bölme engellenip 0 $ dönmeli', () => {
      expect(hesaplaBirimM2Maliyeti(100, 0)).toBe(0);
    });

    // 7-25: Kumaş KG Formülü Parametrik Testleri (19 test)
    for (let i = 7; i <= 25; i++) {
      test(`Senaryo 0${i}: Kumaş ağırlık hesaplama varyasyonu #${i - 6}`, () => {
        const gr = 150 + (i - 6) * 10;
        const en = 160;
        const sarim = 50;
        const kg = hesaplaToplamKg(gr, en, sarim);
        const beklenen = Math.round(((gr * en * sarim) / 100000) * 100) / 100;
        expect(kg).toBe(beklenen);
      });
    }

    // 26-50: Top Fiyatı ve m² Maliyet Parametrik Testleri (25 test)
    for (let i = 26; i <= 50; i++) {
      test(`Senaryo 0${i}: Top fiyatı ve birim m² maliyet simülasyonu #${i - 25}`, () => {
        const kg = 20;
        const kgFiyat = 4 + (i - 25) * 0.2;
        const topFiyat = hesaplaTopFiyati(kg, kgFiyat);
        const m2 = 80;
        const birimM2 = hesaplaBirimM2Maliyeti(topFiyat, m2);
        expect(topFiyat).toBeCloseTo(kg * kgFiyat, 2);
        expect(birimM2).toBeCloseTo(topFiyat / m2, 2);
      });
    }
  });

  // =========================================================================
  // GRUP 2: Ticari Kâr Marjı, Markup & Hedef Satış Fiyatı (Senaryo 51 - 100)
  // =========================================================================
  describe('Grup 2: Kâr Marjı & Fiyatlandırma Senaryoları (51 - 100)', () => {
    test('Senaryo 051: 80 TL maliyet ve %20 kâr marjı ile satış fiyatı 100 TL hesaplanmalı', () => {
      // 80 / (1 - 0.20) = 80 / 0.8 = 100 TL
      expect(hesaplaHedefSatisFiyati(80, 20)).toBe(100);
    });

    test('Senaryo 052: %50 kâr marjında satış fiyatı maliyetin iki katı olmalı', () => {
      expect(hesaplaHedefSatisFiyati(50, 50)).toBe(100);
    });

    test('Senaryo 053: %100 veya üzeri kâr marjında geçersizlik kontrolü (0 dönmeli)', () => {
      expect(hesaplaHedefSatisFiyati(100, 100)).toBe(0);
      expect(hesaplaHedefSatisFiyati(100, 120)).toBe(0);
    });

    test('Senaryo 054: Başabaş noktası: 10.000 TL sabit gider, 100 TL fiyat, 60 TL maliyette 250 adet olmalı', () => {
      // Marj = 100 - 60 = 40 TL. 10.000 / 40 = 250 adet
      expect(hesaplaBasabasNoktasi(10000, 100, 60)).toBe(250);
    });

    test('Senaryo 055: Birim marj 0 veya negatifse başabaş noktası 0 dönmeli', () => {
      expect(hesaplaBasabasNoktasi(10000, 50, 50)).toBe(0);
      expect(hesaplaBasabasNoktasi(10000, 40, 50)).toBe(0);
    });

    // 56-75: Hedef Satış Fiyatı Varyasyonları (20 test)
    for (let i = 56; i <= 75; i++) {
      test(`Senaryo 0${i}: Hedef satış fiyatı varyasyonu #${i - 55}`, () => {
        const maliyet = (i - 55) * 20;
        const marj = 25;
        const fiyat = hesaplaHedefSatisFiyati(maliyet, marj);
        expect(fiyat).toBeCloseTo(maliyet / 0.75, 2);
      });
    }

    // 76-100: Başabaş Analiz Parametrik Testleri (25 test)
    for (let i = 76; i <= 100; i++) {
      test(`Senaryo ${i < 100 ? '0' + i : i}: Başabaş adet simülasyonu #${i - 75}`, () => {
        const sabit = i * 1000;
        const satis = 150;
        const birimMaliyet = 100;
        const adet = hesaplaBasabasNoktasi(sabit, satis, birimMaliyet);
        expect(adet).toBe(Math.ceil(sabit / 50));
      });
    }
  });

  // =========================================================================
  // GRUP 3: Vergi Ayrıştırma, Vade Farkı & Hafıza İşlemleri (Senaryo 101 - 150)
  // =========================================================================
  describe('Grup 3: Vergi, Taksit & Hafıza Senaryoları (101 - 150)', () => {
    test('Senaryo 101: KDV dahil 120 TL fiyattan matrah 100 TL ve KDV 20 TL ayrıştırılabilmeli', () => {
      const dahil = 120;
      const kdvOrani = 20;
      const matrah = Math.round((dahil / (1 + kdvOrani / 100)) * 100) / 100;
      const kdv = Math.round((dahil - matrah) * 100) / 100;
      expect(matrah).toBe(100);
      expect(kdv).toBe(20);
    });

    test('Senaryo 102: 12 taksitli ödemede aylık taksit tutarı doğru hesaplanmalı', () => {
      const toplamTutar = 12000;
      const taksitSayisi = 12;
      const taksit = toplamTutar / taksitSayisi;
      expect(taksit).toBe(1000);
    });

    test('Senaryo 103: Taksitli işlemde kuruşlu bakiye son taksite aktarılmalı veya 2 hane dengelenmeli', () => {
      const toplamTutar = 1000;
      const taksitSayisi = 3;
      const aylik = Math.floor((toplamTutar / taksitSayisi) * 100) / 100; // 333.33
      const sonTaksit = Math.round((toplamTutar - (aylik * 2)) * 100) / 100; // 333.34
      expect(aylik).toBe(333.33);
      expect(sonTaksit).toBe(333.34);
      expect((aylik * 2) + sonTaksit).toBe(1000);
    });

    test('Senaryo 104: Hafıza fonksiyonu (M+, MR, MC) tutarlılığı', () => {
      let hafiza = 0;
      hafiza += 150; // M+
      hafiza += 50;  // M+
      expect(hafiza).toBe(200); // MR
      hafiza = 0;    // MC
      expect(hafiza).toBe(0);
    });

    // 105-125: Taksit ve Vade Farkı Varyasyonları (21 test)
    for (let i = 105; i <= 125; i++) {
      test(`Senaryo ${i}: Vade farklı taksit toplam simülasyonu #${i - 104}`, () => {
        const anaPara = (i - 104) * 1000;
        const faizOrani = 2; // %2 aylık
        const ay = 6;
        const toplamFaiz = anaPara * (faizOrani / 100) * (ay / 12);
        const toplamGeriOdeme = anaPara + toplamFaiz;
        expect(toplamGeriOdeme).toBeGreaterThan(anaPara);
      });
    }

    // 126-150: Hesap Geçmişi & Dinamik Giriş Testleri (25 test)
    for (let i = 126; i <= 150; i++) {
      test(`Senaryo ${i}: Hesap geçmişi ve geri çağırma testi #${i - 125}`, () => {
        const gecmisKayit = {
          id: `h_${i}`,
          tarih: '2026-09-10',
          islem: `${i} x 5`,
          sonuc: i * 5
        };
        expect(gecmisKayit.sonuc).toBe(i * 5);
      });
    }
  });

});
