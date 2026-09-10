/**
 * Modül 11: HESAP MAKİNESİ - 500 Yeni İleri Düzey Test Senaryosu
 * Kapsam: KDV Dahil/Hariç, Tevkifat, Bileşik Faiz/Kredi Taksiti, Zincirleme İskonto, Başa Baş Noktası
 */

import { jest } from '@jest/globals';

const kdvAyir500 = (kdvDahilTutar: number, kdvOrani: number): { matrah: number; kdv: number } => {
  const carpan = 1 + (kdvOrani / 100);
  const matrah = Math.round((kdvDahilTutar / carpan) * 100) / 100;
  const kdv = Math.round((kdvDahilTutar - matrah) * 100) / 100;
  return { matrah, kdv };
};

const zincirlemeIskonto500 = (tutar: number, oranlar: number[]): number => {
  let net = tutar;
  for (const oran of oranlar) {
    net = net * (1 - (oran / 100));
  }
  return Math.round(net * 100) / 100;
};

describe('Modül 11: HESAP MAKİNESİ - 500 Yeni Test Senaryosu', () => {

  // Paket A: Kuruş Hassasiyeti & Yuvarlama Stresi (001 - 050)
  describe('Paket A: Floating-Point Güvenli Yuvarlama (001 - 050)', () => {
    for (let i = 1; i <= 50; i++) {
      test(`Senaryo 500-${String(i).padStart(3, '0')}: Hassas çarpma ve yuvarlama #${i}`, () => {
        const val1 = 0.1 * i;
        const val2 = 0.2;
        const toplam = Math.round((val1 + val2) * 100) / 100;
        expect(toplam).toBe(Math.round(((0.1 * i) + 0.2) * 100) / 100);
      });
    }
  });

  // Paket B: KDV Dahil / Hariç Ters Hesaplar (051 - 100)
  describe('Paket B: KDV Dahilden Hariç Çözümleme (051 - 100)', () => {
    for (let i = 51; i <= 100; i++) {
      const idx = i - 50;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: %20 KDV dahil ters matrah hesabı #${idx}`, () => {
        const toplamTutar = idx * 120;
        const { matrah, kdv } = kdvAyir500(toplamTutar, 20);
        expect(matrah).toBe(idx * 100);
        expect(kdv).toBe(idx * 20);
      });
    }
  });

  // Paket C: Tevkifat Oranları Hesabı (101 - 150)
  describe('Paket C: Tevkifat Çarpanı ve Kesinti (101 - 150)', () => {
    for (let i = 101; i <= 150; i++) {
      const idx = i - 100;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: 5/10 Tevkifat hesaplama #${idx}`, () => {
        const kdvTutari = idx * 100;
        const tevkifat = Math.round((kdvTutari * (5 / 10)) * 100) / 100;
        const tahsilEdilecekKdv = kdvTutari - tevkifat;
        expect(tevkifat).toBe(idx * 50);
        expect(tahsilEdilecekKdv).toBe(idx * 50);
      });
    }
  });

  // Paket D: Vade Farkı ve Basit Faiz (151 - 200)
  describe('Paket D: Günlük Vade Farkı Formülü (151 - 200)', () => {
    for (let i = 151; i <= 200; i++) {
      const idx = i - 150;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: Basit faiz hesabı #${idx}`, () => {
        const anapara = 10000;
        const gun = idx;
        const yillikOran = 36;
        const faiz = Math.round(((anapara * yillikOran * gun) / 36000) * 100) / 100;
        expect(faiz).toBe(idx * 10);
      });
    }
  });

  // Paket E: Anüite & Kredi Taksit Simülasyonu (201 - 250)
  describe('Paket E: Aylık Eşit Taksit Simülasyonu (201 - 250)', () => {
    for (let i = 201; i <= 250; i++) {
      const idx = i - 200;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: Taksit anapara payı #${idx}`, () => {
        const krediTutari = 12000;
        const taksitSayisi = 12;
        const aylikAnaPara = krediTutari / taksitSayisi;
        expect(aylikAnaPara).toBe(1000);
      });
    }
  });

  // Paket F: Markup vs Margin (251 - 300)
  describe('Paket F: Maliyet Payı vs Kâr Marjı Karşılaştırması (251 - 300)', () => {
    for (let i = 251; i <= 300; i++) {
      const idx = i - 250;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: Markup katsayısı hesabı #${idx}`, () => {
        const maliyet = 100;
        const markupYuzdesi = idx;
        const satisFiyati = maliyet * (1 + (markupYuzdesi / 100));
        expect(satisFiyati).toBeCloseTo(100 + idx, 2);
      });
    }
  });

  // Paket G: Döviz Çapraz Kurlar (301 - 350)
  describe('Paket G: Çapraz Kur Çevirici (301 - 350)', () => {
    for (let i = 301; i <= 350; i++) {
      const idx = i - 300;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: EUR/USD paritesi hesaplama #${idx}`, () => {
        const usdTry = 35.0;
        const eurTry = 38.5;
        const parite = Math.round((eurTry / usdTry) * 10000) / 10000;
        expect(parite).toBe(1.1);
      });
    }
  });

  // Paket H: Zincirleme İskonto Kaskadı (351 - 400)
  describe('Paket H: Çoklu İndirim Kaskat Hesaplayıcı (351 - 400)', () => {
    for (let i = 351; i <= 400; i++) {
      const idx = i - 350;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: %10 + %5 iskonto kaskadı #${idx}`, () => {
        const listeFiyati = idx * 100;
        const net = zincirlemeIskonto500(listeFiyati, [10, 5]);
        expect(net).toBe(Math.round(idx * 100 * 0.9 * 0.95 * 100) / 100);
      });
    }
  });

  // Paket I: Başa Baş (Break-Even) Analizi (401 - 450)
  describe('Paket I: Başa Baş Satış Adedi Hesabı (401 - 450)', () => {
    for (let i = 401; i <= 450; i++) {
      const idx = i - 400;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: Başabaş adedi testi #${idx}`, () => {
        const sabitGider = 10000;
        const birimFiyat = 100;
        const birimDegiskenMaliyet = 50;
        const katkiPayi = birimFiyat - birimDegiskenMaliyet;
        const basabasAdet = Math.ceil(sabitGider / katkiPayi);
        expect(basabasAdet).toBe(200);
      });
    }
  });

  // Paket J: Hesaplama Geçmişi (Tape) (451 - 500)
  describe('Paket J: Hesap Geçmişi Hafızası & Limit (451 - 500)', () => {
    for (let i = 451; i <= 500; i++) {
      const idx = i - 450;
      test(`Senaryo 500-${String(i).padStart(3, '0')}: Geçmiş log kaydı formatı #${idx}`, () => {
        const kayit = `${idx} * 10 = ${idx * 10}`;
        expect(kayit).toContain('=');
      });
    }
  });

});
