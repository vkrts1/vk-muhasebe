import { hesapFifo, hesapKur, hesapOptimalFiyat, cekRiskPuani } from '../services/analizUtils';
import { stokUyarilari, alacakUyarilari, riskLimitiUyarilari, evrakVadeUyarilari, tumUyarilar } from '../services/alertService';

describe('Finansal ve Stok Hesaplama Mantığı Testleri (analizUtils)', () => {
  
  test('hesapKur - Çapraz Kur Çevirme', () => {
    const kurlar = { TRY: 1, USD: 33.0, EUR: 36.0 };
    
    // 100 USD kaç TRY yapar? -> 3300 TRY
    const usdToTry = hesapKur({ miktar: 100, kaynak: 'USD', hedef: 'TRY', kurlar });
    expect(usdToTry).toBe(3300);

    // 100 EUR kaç USD yapar? -> 3600 / 33 = 109.09 USD
    const eurToUsd = hesapKur({ miktar: 100, kaynak: 'EUR', hedef: 'USD', kurlar });
    expect(eurToUsd).toBeCloseTo(109.09, 1);
  });

  test('hesapOptimalFiyat - Maliyet, Kâr ve KDV Hesaplama', () => {
    const params = {
      alisFiyati: 100,
      ekMaliyet: 20,
      karOrani: 25,
      kdvOrani: 20
    };

    // Toplam Maliyet = 120
    // Kâr Tutarı (%25) = 30 -> KDV Hariç Satış = 150
    // KDV (%20) = 30 -> KDV Dahil Satış = 180
    const sonuc = hesapOptimalFiyat(params);
    expect(sonuc.satisFiyati).toBe(180);
    expect(sonuc.netKar).toBe(30);
    expect(sonuc.kdv).toBe(30);
  });

  test('cekRiskPuani - Çek Risk Derecelendirme', () => {
    // Düşük Risk Çek (İyi banka, normal tutar, vadesi gelmemiş)
    const cek1 = { banka: 'Ziraat Bankası', tutar: 50000, vadeTarihi: '2099-01-01', durum: 'Portföyde' };
    const r1 = cekRiskPuani(cek1, '2026-08-12');
    expect(r1.puan).toBe(10); // İyi banka bonusu
    expect(r1.label).toBe('Düşük Risk');

    // Yüksek Risk Çek (Bilinmeyen banka, yüksek tutar, vadesi geçmiş)
    const cek2 = { banka: 'Hayali Bank', tutar: 600000, vadeTarihi: '2025-01-01', durum: 'Portföyde' };
    const r2 = cekRiskPuani(cek2, '2026-08-12');
    expect(r2.puan).toBeGreaterThanOrEqual(70);
    expect(r2.label).toBe('Yüksek Risk');
  });

  test('hesapFifo - FIFO Stok Maliyet ve Karlılık', () => {
    const stok = { id: 1, alisFiyati: 10, satisFiyati: 15 };
    const hareketler = [
      { miktar: 10, islemTuru: 'Alış', birimFiyat: 10, tarih: '2026-08-01' },
      { miktar: 5, islemTuru: 'Alış', birimFiyat: 12, tarih: '2026-08-02' },
      { miktar: 12, islemTuru: 'Satış', tarih: '2026-08-03' } // 12 adet satış en eski lotlardan tüketilecek
    ];

    // Satılan: 12 adet
    // FIFO Maliyeti: 10 adet * 10 + 2 adet * 12 = 124
    // Satış Geliri: 12 adet * 15 (satisFiyati) = 180
    // Kâr: 180 - 124 = 56
    const sonuc = hesapFifo(hareketler, stok);
    expect(sonuc.satilanAdet).toBe(12);
    expect(sonuc.fifoMaliyet).toBe(124);
    expect(sonuc.satis).toBe(180);
    expect(sonuc.kar).toBe(56);
  });
});

describe('Alarm Motoru / Bildirim Servisi Testleri (alertService)', () => {

  test('stokUyarilari - Kritik ve Negatif Stok Tespiti', () => {
    const stoklar = [
      { id: 1, stokAdi: 'Ürün A', miktar: 2 }, // Kritik (Limit 5)
      { id: 2, stokAdi: 'Ürün B', miktar: -3 }, // Negatif ve Kritik
      { id: 3, stokAdi: 'Ürün C', miktar: 10 } // Normal
    ];

    const uyarilar = stokUyarilari(stoklar, 5);
    expect(uyarilar.length).toBe(2);
    expect(uyarilar.find(u => u.id === 'dusuk_stok')).toBeDefined();
    expect(uyarilar.find(u => u.id === 'negatif_stok')).toBeDefined();
  });

  test('alacakUyarilari - Vadesi Geçen ve Yaklaşan Faturalar', () => {
    const faturalar = [
      // Geciken fatura
      { id: 1, tur: 'Satış', genelToplam: 1000, odenen: 0, vadeTarihi: '2026-08-01', odemeSekli: 'Açık Hesap' },
      // Vadesi yaklaşan fatura
      { id: 2, tur: 'Satış', genelToplam: 2000, odenen: 500, vadeTarihi: '2026-08-15', odemeSekli: 'Açık Hesap' },
      // Ödenmiş fatura (uyarı vermemeli)
      { id: 3, tur: 'Satış', genelToplam: 1500, odenen: 1500, vadeTarihi: '2026-08-01', odemeSekli: 'Açık Hesap' }
    ];

    // Bugün: 2026-08-12
    const uyarilar = alacakUyarilari(faturalar, 7, '2026-08-12');
    expect(uyarilar.length).toBe(2);
    expect(uyarilar.find(u => u.id === 'geciken_alacak')).toBeDefined();
    expect(uyarilar.find(u => u.id === 'vade_yaklasan')).toBeDefined();
  });

  test('riskLimitiUyarilari - Cari Risk Limiti Kontrolü', () => {
    const cariler = [
      { id: 1, unvan: 'Cari A', borc: 15000, alacak: 2000, riskLimiti: 10000 }, // Bakiye 13000 > Limit 10000 (Aşım!)
      { id: 2, unvan: 'Cari B', borc: 5000, alacak: 1000, riskLimiti: 10000 }  // Bakiye 4000 <= Limit 10000 (Normal)
    ];

    const uyarilar = riskLimitiUyarilari(cariler, true);
    expect(uyarilar.length).toBe(1);
    expect(uyarilar[0].id).toBe('risk_limiti');
  });
});
