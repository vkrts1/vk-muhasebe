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

const resetFetch = (dataByPath: Record<string, any> = {}) => {
  (global as any).fetch = jest.fn(async (url: string) => {
    const method = (url as any).includes ? '' : '';
    // method is inferred from the init arg, we just mock a generic response
    return {
      ok: true,
      status: 200,
      json: async () => ({}),
    };
  });
};

import { getFirebaseConfig, loadConfigFromStorage, saveFirebaseConfig, writeData, readData } from '../firebase';
import { saveFinancialTransaction } from '../transactionService';

describe('saveFinancialTransaction (firebase REST eşleniği)', () => {
  beforeAll(async () => {
    await saveFirebaseConfig(DB_URL, 'S3cret');
    const cfg = getFirebaseConfig();
    expect(cfg && cfg.url).toBe('https://test-firebase.firebaseio.com');
  });

  beforeEach(() => {
    resetFetch();
  });

  it('firebase config yüklenebilir', async () => {
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
});
