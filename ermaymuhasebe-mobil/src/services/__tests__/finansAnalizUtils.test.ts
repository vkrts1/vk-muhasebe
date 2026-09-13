import {
  getPeriodBounds,
  hesapKarneler,
  hesapAlisFaturaKarnesi,
  hesaplaOrtalamaGun,
  hesapOrtalamaVadeler,
  hesapFinansTrend,
} from '../finansAnalizUtils';

describe('getPeriodBounds', () => {
  it('haftalık: son 7 gün', () => {
    const { start, end } = getPeriodBounds('weekly');
    expect(end.getTime() - start.getTime()).toBeCloseTo(7 * 86400000, 3);
  });
  it('aylık: son 30 gün', () => {
    const { start } = getPeriodBounds('monthly');
    expect(start.getTime()).toBeLessThanOrEqual(Date.now());
  });
  it('6 aylık: son 6 ay', () => {
    const { start } = getPeriodBounds('6month');
    const diffMonths = (Date.now() - start.getTime()) / (30 * 86400000);
    expect(diffMonths).toBeGreaterThanOrEqual(5.8);
    expect(diffMonths).toBeLessThanOrEqual(6.25);
  });
  it('yıllık: son 1 yıl', () => {
    const { start } = getPeriodBounds('yearly');
    const diffDays = (Date.now() - start.getTime()) / 86400000;
    expect(diffDays).toBeGreaterThanOrEqual(364);
  });
});

describe('hesapKarneler', () => {
  const hareket = (alacak: number, islemTuru: string, yon?: number | null): any => ({
    alacak,
    borc: 0,
    islemTuru,
    tarih: new Date().toISOString().split('T')[0],
    yonlendirilenCariId: yon ?? null,
  });

  it('kendi nakit/KK/EFT ayrımı yapar', () => {
    const k = hesapKarneler(
      [
        hareket(100, 'Tahsilat (Nakit)'),
        hareket(200, 'Tahsilat (KK)'),
        hareket(300, 'Tahsilat (EFT)'),
        hareket(0, 'Ödeme (Nakit)'), // tek taraflı ödeme: alacak 0 -> sayılmaz
      ],
      [],
      'monthly'
    );
    expect(k.kendiNakit).toBe(100);
    expect(k.kendiKK).toBe(200);
    expect(k.kendiEFT).toBe(300);
  });

  it('yönlendirilen (ciro) ayrımı yapar', () => {
    const k = hesapKarneler(
      [
        hareket(100, 'Tahsilat (Nakit)'),
        hareket(200, 'Tahsilat (Nakit)', 99),
      ],
      [],
      'monthly'
    );
    expect(k.kendiNakit).toBe(100);
    expect(k.yonlendirilenNakit).toBe(200);
  });

  it('Fatura içeren hareketleri hariç tutar', () => {
    const k = hesapKarneler([hareket(500, 'Fatura Ödemesi')], [], 'monthly');
    expect(k.kendiNakit + k.kendiKK + k.kendiEFT).toBe(0);
  });

  it('çekleri vade aralığına ve türüne göre sayar', () => {
    const bugun = new Date();
    const vade = bugun.toISOString().split('T')[0];
    const k = hesapKarneler(
      [],
      [
        { vadeTarihi: vade, tutar: 1000, cekTuru: 'Alınan', durum: 'Portföyde' },
        { vadeTarihi: vade, tutar: 2000, cekTuru: 'Verilen', durum: 'Portföyde' },
        { vadeTarihi: vade, tutar: 3000, cekTuru: 'Alınan', durum: 'Karşılıksız' },
      ],
      'monthly'
    );
    expect(k.kendiCek).toBe(1000);
  });
});

describe('hesapAlisFaturaKarnesi', () => {
  it('toplam/ödenen/kalan/oran/gecikmis/önden hesabı', () => {
    const bugun = new Date().toISOString().split('T')[0];
    const k = hesapAlisFaturaKarnesi(
      [
        { tur: 'Alış', genelToplam: 100, odenen: 40, vadeTarihi: '2026-01-01' }, // geçmiş, kalan 60 -> gecikmiş
        { tur: 'Alış', genelToplam: 200, odenen: 0, vadeTarihi: '2020-01-01' }, // gecikmiş
        { tur: 'Alış', genelToplam: 300, odenen: 300, vadeTarihi: '2099-01-01' }, // önden ödenen
        { tur: 'Satış', genelToplam: 999 }, // hariç
      ]
    );
    expect(k.toplam).toBe(600);
    expect(k.odenen).toBe(340);
    expect(k.kalan).toBe(260);
    expect(k.oran).toBeCloseTo(56.6667, 1);
    expect(k.gecikmis).toBe(260);
    expect(k.ondenOdenen).toBe(300);
  });
});

describe('hesaplaOrtalamaGun', () => {
  it('FIFO eşleştirme ile ağırlıklı gün hesabı', () => {
    const ortalama = hesaplaOrtalamaGun(
      [
        { tarih: '2026-01-01', tutar: 100 },
        { tarih: '2026-01-10', tutar: 100 },
      ],
      [
        { tarih: '2026-01-06', tutar: 50 },   // 1. faturadan 5 gün
        { tarih: '2026-01-16', tutar: 150 },  // 1. fatura kalan 50 (15 gün) + 2. fatura 100 (6 gün)
      ]
    );
    // (5*50 + 15*50 + 6*100) / 200 = (250 + 750 + 600)/200 = 1600/200 = 8
    expect(ortalama).toBe(8);
  });
});

describe('hesapOrtalamaVadeler', () => {
  it('satış/alış ayrımıyla tahsilat ve ödeme gününü hesaplar', () => {
    const v = hesapOrtalamaVadeler(
      [
        { tur: 'Satış', genelToplam: 100, tarih: '2026-01-01' },
        { tur: 'Alış', genelToplam: 100, tarih: '2026-01-01' },
      ],
      [
        { alacak: 100, borc: 0, islemTuru: 'Tahsilat (Nakit)', tarih: '2026-01-10' },
        { alacak: 0, borc: 100, islemTuru: 'Ödeme (Nakit)', tarih: '2026-01-20' },
      ]
    );
    expect(v.tahsilatGunu).toBe(9);
    expect(v.odemeGunu).toBe(19);
  });
});

describe('hesapFinansTrend', () => {
  it('aylık: 12 ay üretir ve Tahsilat/Ödeme/Diğer ayrımı yapar', () => {
    const bugun = new Date();
    const ay = `${bugun.getFullYear()}-${String(bugun.getMonth() + 1).padStart(2, '0')}`;
    const trend = hesapFinansTrend(
      [
        { tarih: `${ay}-05`, giren: 100, cikan: 0, islemTuru: 'Tahsilat (Nakit)' },
        { tarih: `${ay}-06`, giren: 0, cikan: 50, islemTuru: 'Ödeme (Nakit)' },
        { tarih: `${ay}-07`, giren: 20, cikan: 0, islemTuru: 'Virman' },
      ],
      [],
      'Monthly',
      bugun
    );
    expect(trend.length).toBe(12);
    const current = trend[trend.length - 1];
    expect(current.income).toBe(100);
    expect(current.expense).toBe(50);
    expect(current.redirected).toBe(20);
  });

  it('günlük: son 10 gün üretir', () => {
    const trend = hesapFinansTrend([], [], 'Daily', new Date());
    expect(trend.length).toBe(10);
  });

  it('yıllık: son 5 yıl üretir', () => {
    const trend = hesapFinansTrend([], [], 'Yearly', new Date());
    expect(trend.length).toBe(5);
  });
});
