/**
 * Unit tests for the pure analysis helpers (FIFO cost, cross-rate converter,
 * optimal price, cheque risk and customer LTV). No network/state involved.
 */
import {
  hesapFifo,
  hesapKur,
  hesapOptimalFiyat,
  cekRiskPuani,
  hesapLtv,
} from '../analizUtils';

describe('hesapFifo (FIFO maliyet)', () => {
  it('sifir hareket icin sifir sonuc doner', () => {
    const r = hesapFifo([], { alisFiyati: 10, satisFiyati: 20 });
    expect(r).toEqual({ satilanAdet: 0, fifoMaliyet: 0, satis: 0, kar: 0 });
  });

  it('en eski lotu ilk tuketir (FIFO sirasi)', () => {
    const moves = [
      { tarih: '2026-01-01', miktar: 10, giren: 10, birimFiyat: 5 },
      { tarih: '2026-01-02', miktar: 10, giren: 10, birimFiyat: 8 },
      { tarih: '2026-01-03', miktar: 15, islemTuru: 'Satış', cikan: 15 },
    ];
    const r = hesapFifo(moves, { satisFiyati: 20 });
    expect(r.satilanAdet).toBe(15);
    // 15 adet: 10 adet 5₺ + 5 adet 8₺ = 50 + 40 = 90
    expect(r.fifoMaliyet).toBe(90);
    expect(r.satis).toBe(300);
    expect(r.kar).toBe(210);
  });

  it('harekette birimFiyat yoksa stok alis fiyati kullanilir', () => {
    const moves = [
      { tarih: '2026-01-01', miktar: 4, giren: 4 },
      { tarih: '2026-01-02', miktar: 2, islemTuru: 'Çıkış', cikan: 2 },
    ];
    const r = hesapFifo(moves, { alisFiyati: 100, satisFiyati: 150 });
    expect(r.fifoMaliyet).toBe(200);
    expect(r.satis).toBe(300);
  });

  it('tarih sirasi gozetilir (giris sonrasi satis)', () => {
    const moves = [
      { tarih: '2026-01-02', miktar: 5, islemTuru: 'Satış', cikan: 5 },
      { tarih: '2026-01-01', miktar: 5, giren: 5, birimFiyat: 10 },
    ];
    const r = hesapFifo(moves, {});
    expect(r.satilanAdet).toBe(5);
    expect(r.fifoMaliyet).toBe(50);
  });
});

describe('hesapKur (çapraz kur)', () => {
  const kurlar = { TRY: 1, USD: 33.45, EUR: 36.12, GBP: 42.3 };

  it('USD -> TRY cevirir', () => {
    expect(hesapKur({ miktar: 100, kaynak: 'USD', hedef: 'TRY', kurlar })).toBeCloseTo(3345);
  });

  it('TRY -> USD cevirir', () => {
    expect(hesapKur({ miktar: 3345, kaynak: 'TRY', hedef: 'USD', kurlar })).toBeCloseTo(100, 1);
  });

  it('USD -> EUR capraz kur uygular', () => {
    const cross = 100 * 33.45 / 36.12;
    expect(hesapKur({ miktar: 100, kaynak: 'USD', hedef: 'EUR', kurlar })).toBeCloseTo(cross, 5);
  });

  it('sifir miktar icin 0 doner', () => {
    expect(hesapKur({ miktar: 0, kaynak: 'USD', hedef: 'TRY', kurlar })).toBe(0);
  });
});

describe('hesapOptimalFiyat (desktop mantık)', () => {
  it('alıs + kar + KDV uzerinden önerilen fiyati hesaplar', () => {
    const r = hesapOptimalFiyat({ alisFiyati: 100, hedefKarOrani: 25, kdvOrani: 20, ekMaliyet: 0 });
    const satisKdvHaric = 100 * 1.25;
    expect(r.netKar).toBe(25);
    expect(r.kdv).toBe(satisKdvHaric * 0.2);
    expect(r.satisFiyati).toBe(satisKdvHaric + satisKdvHaric * 0.2);
  });

  it('ek maliyeti toplam maliyete ekler', () => {
    const r = hesapOptimalFiyat({ alisFiyati: 100, ekMaliyet: 25, karOrani: 20, kdvOrani: 0 });
    expect(r.netKar).toBe(25); // (100+25)*0.20
    expect(r.satisFiyati).toBe(150);
  });
});

describe('cekRiskPuani', () => {
  const bugun = '2026-08-09';

  it('iyi bankadan + vadesi gecmis cek yuksek risk olur', () => {
    const r = cekRiskPuani({ banka: '', tutar: 600000, vadeTarihi: '2026-05-01', durum: 'Portföyde' }, bugun);
    expect(r.puan).toBeGreaterThanOrEqual(70);
    expect(r.label).toBe('Yüksek Risk');
  });

  it('tahsil edilmis cek dusuk puana cekilir', () => {
    const r = cekRiskPuani({ banka: '', tutar: 900000, vadeTarihi: '2026-05-01', durum: 'Tahsil Edildi' }, bugun);
    expect(r.puan).toBeLessThanOrEqual(30);
    expect(r.label).toBe('Düşük Risk');
  });

  it('bilinmeyen banka portfoydeki cek orta risk', () => {
    const r = cekRiskPuani({ banka: 'Bilinmeyen Bank', tutar: 200000, vadeTarihi: '2026-09-01', durum: 'Portföyde' }, bugun);
    expect(r.label).toBe('Orta Risk');
  });
});

describe('hesapLtv', () => {
  const faturalar = [
    { tur: 'Satış', cariUnvan: 'A', genelToplam: 100, tarih: '2026-01-01' },
    { tur: 'Satış', cariUnvan: 'A', genelToplam: 100, tarih: '2026-02-01' },
    { tur: 'Satış', cariUnvan: 'B', genelToplam: 300, tarih: '2026-06-01' },
    { tur: 'Alış', cariUnvan: 'A', genelToplam: 1000, tarih: '2026-03-01' },
    { tur: 'Satış', cariUnvan: 'A', genelToplam: 50, tarih: '2026-01-01', isDeleted: true },
  ];

  it('sadece satis faturalarini toplar ve silineni yok sayar', () => {
    const r = hesapLtv(faturalar);
    const a = r.find(x => x.musteri === 'A');
    expect(a).toBeDefined();
    expect(a!.adet).toBe(2);
    expect(a!.ciro).toBe(200);
    expect(a!.sepet).toBe(100);
    expect(r.find(x => x.musteri === 'B')!.ciro).toBe(300);
  });

  it('yuksek ciroya gore LTV ile siralar', () => {
    const r = hesapLtv(faturalar);
    expect(r[0].musteri).toBe('B');
  });
});
