/**
 * Unit tests for the 9-group alert engine (mobile parite of desktop
 * RunAlertChecksAsync). Pure functions, no network/state.
 */
import {
  stokUyarilari,
  alacakUyarilari,
  riskLimitiUyarilari,
  evrakVadeUyarilari,
  yaslandirmaUyarilari,
  gunlukOzet,
  tumUyarilar,
} from '../alertService';

describe('stokUyarilari', () => {
  it('kritik alti stok ve negatif stok uyarisi uretir', () => {
    const stoklar = [
      { id: 1, miktar: 2, isDeleted: false },
      { id: 2, miktar: -3, isDeleted: false },
      { id: 3, miktar: 50, isDeleted: false },
    ];
    const r = stokUyarilari(stoklar, 5);
    expect(r.some(x => x.id === 'dusuk_stok')).toBe(true);
    expect(r.some(x => x.id === 'negatif_stok')).toBe(true);
  });

  it('saglikli stokta uyari uretmez', () => {
    expect(stokUyarilari([{ miktar: 100, isDeleted: false }], 5)).toHaveLength(0);
  });
});

describe('alacakUyarilari', () => {
  const bugun = '2026-08-09';
  it('vadesi gecmis ve yaklasan acik hesap satislarini ayirir', () => {
    const faturalar = [
      { tur: 'Satış', vadeTarihi: '2026-07-01', genelToplam: 100, odenen: 0, odemeSekli: 'Açık', isDeleted: false },
      { tur: 'Satış', vadeTarihi: '2026-08-12', genelToplam: 200, odenen: 0, odemeSekli: 'Açık', isDeleted: false },
      { tur: 'Satış', vadeTarihi: '2026-08-12', genelToplam: 500, odenen: 500, odemeSekli: 'Açık', isDeleted: false },
    ];
    const r = alacakUyarilari(faturalar, 7, bugun);
    expect(r.some(x => x.id === 'geciken_alacak')).toBe(true);
    expect(r.some(x => x.id === 'vade_yaklasan')).toBe(true);
  });

  it('odenmis faturalari yok sayar', () => {
    const r = alacakUyarilari([{ tur: 'Satış', vadeTarihi: '2026-07-01', genelToplam: 100, odenen: 100, odemeSekli: 'Açık', isDeleted: false }], 7, bugun);
    expect(r).toHaveLength(0);
  });
});

describe('riskLimitiUyarilari', () => {
  it('limiti asan cariye uyari uretir', () => {
    const cariler = [
      { id: 1, borc: 50000, alacak: 0, riskLimiti: 10000, isDeleted: false },
      { id: 2, borc: 5000, alacak: 0, riskLimiti: 10000, isDeleted: false },
    ];
    const r = riskLimitiUyarilari(cariler, true);
    expect(r).toHaveLength(1);
  });

  it('kapaliysa uyari uretmez', () => {
    expect(riskLimitiUyarilari([{ borc: 50000, riskLimiti: 10000 }], false)).toHaveLength(0);
  });
});

describe('evrakVadeUyarilari', () => {
  const bugun = '2026-08-09';
  it('2 gun icindeki cekleri bildirir, tahsil edilmisleri haric tutar', () => {
    const cekler = [
      { vadeTarihi: '2026-08-10', durum: 'Portföyde' },
      { vadeTarihi: '2026-08-11', durum: 'Tahsil Edildi' },
    ];
    const r = evrakVadeUyarilari(cekler, [], 2, bugun);
    expect(r).toHaveLength(1);
  });
});

describe('yaslandirmaUyarilari', () => {
  it('30 gunden eski vadeyi bildirir', () => {
    const bugun = '2026-08-09';
    const faturalar = [
      { tur: 'Satış', vadeTarihi: '2026-06-01', genelToplam: 300, odenen: 0, odemeSekli: 'Açık', isDeleted: false },
      { tur: 'Satış', vadeTarihi: '2026-08-05', genelToplam: 300, odenen: 0, odemeSekli: 'Açık', isDeleted: false },
    ];
    const r = yaslandirmaUyarilari(faturalar, bugun);
    expect(r).toHaveLength(1);
  });
});

describe('gunlukOzet', () => {
  it('bugunun satisini toplar', () => {
    const faturalar = [
      { tur: 'Satış', tarih: '2026-08-09', genelToplam: 1000, odenen: 0, isDeleted: false },
      { tur: 'Alış', tarih: '2026-08-09', genelToplam: 400, odenen: 0, isDeleted: false },
    ];
    const ozet = gunlukOzet(faturalar, '2026-08-09');
    expect(ozet.mesaj).toContain('1.000');
  });
});

describe('tumUyarilar', () => {
  it('tum gruplari tek liste halinde toplar', () => {
    const input = {
      faturalar: [{ tur: 'Satış', vadeTarihi: '2026-07-01', genelToplam: 100, odenen: 0, odemeSekli: 'Açık', tarih: '2026-08-09', isDeleted: false }],
      stoklar: [{ id: 1, miktar: -2, isDeleted: false }],
      cariler: [{ id: 1, borc: 50000, riskLimiti: 10000, isDeleted: false }],
      cekler: [{ vadeTarihi: '2026-08-10', durum: 'Portföyde' }],
      bugun: '2026-08-09',
    };
    const r = tumUyarilar(input);
    expect(r.some(x => x.id === 'alarm_ozeti')).toBe(true);
    expect(r.filter(x => x.id !== 'alarm_ozeti').length).toBeGreaterThanOrEqual(4);
  });
});
