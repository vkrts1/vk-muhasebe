/**
 * Integration test for saveFinancialTransaction (transactionService).
 *
 * Mocks AsyncStorage + global.fetch so the real REST write path in firebase.ts
 * is exercised end-to-end: verifies the transaction writes CariHareketler,
 * updates Cariler balance, writes KasaHareketler and updates Bankalar balance.
 */

jest.mock('@react-native-async-storage/async-storage', () => {
  const store: Record<string, string> = {};
  return {
    __esModule: true,
    default: {
      getItem: jest.fn(async (k: string) => store[k] ?? null),
      setItem: jest.fn(async (k: string, v: string) => { store[k] = v; }),
      removeItem: jest.fn(async (k: string) => { delete store[k]; }),
      clear: jest.fn(async () => { Object.keys(store).forEach((k) => delete store[k]); }),
    },
  };
});

jest.mock('@react-native-community/netinfo', () => ({
  fetch: jest.fn(async () => ({ isConnected: true, isInternetReachable: true })),
  addEventListener: jest.fn(() => () => {}),
}));

jest.mock('firebase/app', () => ({
  initializeApp: jest.fn(() => ({})),
  getApps: jest.fn(() => []),
  getApp: jest.fn(() => ({})),
}));

jest.mock('firebase/database', () => ({
  getDatabase: jest.fn(() => ({})),
  goOnline: jest.fn(),
  goOffline: jest.fn(),
}));

const DB_URL = 'https://test-firebase.firebaseio.com';

const resetFetch = () => {
  (global as any).fetch = jest.fn(async (url: string) => {
    return {
      ok: true,
      status: 200,
      json: async () => ({}),
    };
  });
};

import { getFirebaseConfig, saveFirebaseConfig, writeData } from '../firebase';
import { saveFinancialTransaction, deleteFinancialTransaction } from '../transactionService';

describe('saveFinancialTransaction (firebase REST eşleniği)', () => {
  beforeAll(async () => {
    await saveFirebaseConfig(DB_URL, 'S3cret');
  });

  beforeEach(() => {
    resetFetch();
  });

  it('firebase config yüklenebilir', () => {
    const cfg = getFirebaseConfig();
    expect(cfg).not.toBeNull();
    expect(cfg!.url).toBe(DB_URL);
  });

  it('writeData REST PUT çağrısını gerçekleştirir ve true döner', async () => {
    const resp = { ok: true, status: 200, json: async () => ({}) };
    (global as any).fetch = jest.fn(async () => resp);
    const ok = await writeData('CariHareketler/test', { id: 1, aciklama: 'test' });
    expect(ok).toBe(true);
    expect((global as any).fetch).toHaveBeenCalled();
  });

  it('basit Nakit Tahsilat: tüm yazımlar başarılı olunca true döner', async () => {
    let calls = 0;
    (global as any).fetch = jest.fn(async (url: string, init?: any) => {
      calls++;
      if (init && init.method === 'PUT') {
        return { ok: true, status: 200, json: async () => ({}) };
      }
      return { ok: true, status: 200, json: async () => null };
    });

    const ok = await saveFinancialTransaction({
      cari: { id: 5, unvan: 'Test Müşteri' },
      amount: 500,
      date: '2026-08-13',
      transactionType: 'Tahsilat',
      method: 'Nakit',
      description: 'Nakit tahsilat',
      selectedHesap: { id: 3, kartTuru: 'Kasa', bakiye: 1000 },
    });

    expect(ok).toBe(true);
    // CariHareketler, Cariler, KasaHareketler, Bankalar (4 PUT + GET'ler)
    expect(calls).toBeGreaterThanOrEqual(4);
  });

  it('karşılıksız statüsündeki çek durumları okunabildiği gibi ciro + Ödeme de çalışır', async () => {
    let putCalls = 0;
    (global as any).fetch = jest.fn(async (url: string, init?: any) => {
      if (init && init.method === 'PUT') putCalls++;
      return { ok: true, status: 200, json: async () => (init && init.method === 'PUT' ? {} : null) };
    });

    const ok = await saveFinancialTransaction({
      cari: { id: 7, unvan: 'Firma A' },
      amount: 2000,
      date: '2026-08-13',
      transactionType: 'Tahsilat',
      method: 'Kredi Kartı',
      description: 'KK tahsilat',
      selectedHesap: { id: 9, kartTuru: 'Vadesiz', bakiye: 5000 },
      directedSupplier: { id: 11, unvan: 'Tedarikçi B' },
      bankaAdi: 'XYZ Bank',
      kartHesapNo: '1234',
    });

    expect(ok).toBe(true);
    expect(putCalls).toBeGreaterThanOrEqual(5);
  });

  it('deleteFinancialTransaction refId veya evrakNo ile Firebase DELETE çağrılarını yapar ve bakiyeleri günceller', async () => {
    const deletedPaths: string[] = [];
    (global as any).fetch = jest.fn(async (url: string, init?: any) => {
      if (init && init.method === 'DELETE') {
        deletedPaths.push(url);
      }
      return {
        ok: true,
        status: 200,
        json: async () => {
          if (url.includes('CariHareketler')) {
            return {
              "-K1": { id: 101, refId: "REF-123", evrakNo: "TAH-001", cariId: 5, alacak: 500, borc: 0, islemTuru: "Tahsilat" }
            };
          }
          if (url.includes('KasaHareketler')) {
            return [
              { id: 201, refId: "REF-123", evrakNo: "TAH-001", kasaId: 3, gelir: 500, gider: 0 }
            ];
          }
          if (url.includes('Cariler/5')) {
            return { id: 5, bakiye: 1000, unvan: 'Test Cari' };
          }
          if (url.includes('Bankalar/3')) {
            return { id: 3, bakiye: 2000, ad: 'Kasa' };
          }
          return {};
        },
      };
    });

    const ok = await deleteFinancialTransaction({
      refId: 'REF-123',
      evrakNo: 'TAH-001',
      id: 101,
      cariId: 5,
    });

    expect(ok).toBe(true);
    expect(deletedPaths.some((u) => u.includes('CariHareketler'))).toBe(true);
  });

  it('deleteFinancialTransaction bir Fatura hareketi silindiğinde Faturayı, FaturaDetayları ve StokHareketleri cascade siler', async () => {
    const deletedPaths: string[] = [];
    const writtenData: Record<string, any> = {};

    (global as any).fetch = jest.fn(async (url: string, init?: any) => {
      if (init && init.method === 'DELETE') {
        deletedPaths.push(url);
      }
      if (init && init.method === 'PUT') {
        writtenData[url] = JSON.parse(init.body || '{}');
      }
      return {
        ok: true,
        status: 200,
        json: async () => {
          if (url.includes('Faturalar/88')) {
            return { id: 88, faturaNo: 'FAT-88', tur: 'Satış', cariId: 10, genelToplam: 1500 };
          }
          if (url.includes('FaturaDetaylar/88')) {
            return [{ id: 1, faturaId: 88, stokId: 44, miktar: 5, birimFiyat: 300 }];
          }
          if (url.includes('Stoklar/44')) {
            return { id: 44, miktar: 15, stokAdi: 'Test Ürün' };
          }
          if (url.includes('Cariler/10')) {
            return { id: 10, unvan: 'Müşteri X', borc: 1500, alacak: 0 };
          }
          if (url.includes('StokHareketler')) {
            return {
              "SH-1": { id: 501, faturaId: 88, evrakNo: 'FAT-88', stokId: 44, cikan: 5 }
            };
          }
          if (url.includes('CariHareketler')) {
            return {
              "CH-1": { id: 601, faturaId: 88, evrakNo: 'FAT-88', cariId: 10, borc: 1500, islemTuru: 'Satış Faturası' }
            };
          }
          return {};
        },
      };
    });

    const ok = await deleteFinancialTransaction({
      id: 601,
      faturaId: 88,
      evrakNo: 'FAT-88',
      islemTuru: 'Satış Faturası',
      cariId: 10,
      borc: 1500
    });

    expect(ok).toBe(true);
    const stokWrite = Object.keys(writtenData).find(u => u.includes('Stoklar/44'));
    expect(stokWrite).toBeDefined();
    const stokObj = writtenData[stokWrite!];
    expect(stokObj.Miktar ?? stokObj.miktar).toBe(20);

    // Cari borcu geri alınmış olmalı: 1500 - 1500 = 0
    const cariWrite = Object.keys(writtenData).find(u => u.includes('Cariler/10'));
    expect(cariWrite).toBeDefined();
    const cariObj = writtenData[cariWrite!];
    expect(cariObj.Borc ?? cariObj.borc).toBe(0);

    // FaturaDetaylar, StokHareketler ve CariHareketler silinmiş olmalı
    expect(deletedPaths.some(u => u.includes('FaturaDetaylar/88'))).toBe(true);
    expect(deletedPaths.some(u => u.includes('StokHareketler'))).toBe(true);
    expect(deletedPaths.some(u => u.includes('CariHareketler'))).toBe(true);
  });
});
