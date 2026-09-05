/**
 * Unit tests for the Firebase data-mapping, sanitization and offline
 * cache/queue helpers.
 *
 * These avoid real network/firebase calls by mocking AsyncStorage. The
 * mapping functions (mapDatabaseToApp / mapAppToDatabase) are pure and
 * exercised directly.
 */

jest.mock('@react-native-async-storage/async-storage', () => ({
  __esModule: true,
  default: {
    getItem: jest.fn(async () => null),
    setItem: jest.fn(async () => {}),
    removeItem: jest.fn(async () => {}),
  },
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

jest.mock('@react-native-async-storage/async-storage');

import {
  mapDatabaseToApp,
  mapAppToDatabase,
  mapPathToDatabase,
  getCacheKey,
} from '../firebase';

describe('mapDatabaseToApp', () => {
  it('converts PascalCase keys to camelCase', () => {
    const input = { StokAdi: 'Kalem', StokKodu: 'K-001', Miktar: 5 };
    const expected = { stokAdi: 'Kalem', stokKodu: 'K-001', miktar: 5 };
    expect(mapDatabaseToApp('Stoklar', input)).toEqual(expected);
  });

  it('handles exception keys (Email->eposta, IBAN->iban, TCNo->tcNo)', () => {
    const input = { Email: 'x@y.com', IBAN: 'TR', TCNo: '123' };
    const mapped = mapDatabaseToApp('Cariler', input);
    expect(mapped.eposta).toBe('x@y.com');
    expect(mapped.iban).toBe('TR');
    expect(mapped.tcNo).toBe('123');
  });

  it('maps KDV to kdvOrani in FaturaDetaylar context', () => {
    expect(mapDatabaseToApp('FaturaDetaylar', { KDV: 20 }).kdvOrani).toBe(20);
    expect(mapDatabaseToApp('Faturalar', { KDV: 20 }).kdv).toBe(20);
  });

  it('maps Note Title/Content to baslik/aciklama', () => {
    const mapped = mapDatabaseToApp('Notes', { Title: 'Baslik', Content: 'Metin' });
    expect(mapped.baslik).toBe('Baslik');
    expect(mapped.aciklama).toBe('Metin');
  });

  it('recurses into arrays and nested objects', () => {
    const input = { Kalemler: [{ StokAdi: 'X', Detay: { BirimFiyat: 5 } }] };
    const mapped = mapDatabaseToApp('Faturalar', input);
    expect(mapped.kalemler[0].stokAdi).toBe('X');
    expect(mapped.kalemler[0].detay.birimFiyat).toBe(5);
  });
});

describe('mapAppToDatabase', () => {
  it('converts camelCase keys back to PascalCase', () => {
    const input = { stokAdi: 'Kalem', miktar: 5 };
    const expected = { StokAdi: 'Kalem', Miktar: 5 };
    expect(mapAppToDatabase('Stoklar', input)).toEqual(expected);
  });

  it('round-trips with mapDatabaseToApp', () => {
    const original = { StokAdi: 'Kalem', StokKodu: 'K-001', Miktar: 5 };
    const toApp = mapDatabaseToApp('Stoklar', original);
    const toDb = mapAppToDatabase('Stoklar', toApp);
    expect(toDb).toEqual(original);
  });

  it('maps kdvOrani back to KDVOrani in FaturaDetaylar context', () => {
    expect(mapAppToDatabase('FaturaDetaylar', { kdvOrani: 20 }).KDVOrani).toBe(20);
  });
});

describe('mapPathToDatabase', () => {
  const originalConfig = require('../firebase');

  afterEach(() => {
    // cachedConfig is module-private; tests only assert path segments for
    // top-level (non-company-suffixed) resources.
  });

  it('keeps users/security_requests/FirmaProfili unscoped', () => {
    expect(mapPathToDatabase('users/abc').startsWith('users/')).toBe(true);
    expect(mapPathToDatabase('FirmaProfili').startsWith('FirmaProfili')).toBe(true);
    expect(mapPathToDatabase('security_requests/x').startsWith('security_requests/')).toBe(true);
  });

  it('scopes other paths under companies/{tenant}/years/{year}/', () => {
    const mapped = mapPathToDatabase('Stoklar');
    expect(mapped).toMatch(/^companies\/default\/years\/\d{4}\/Stoklar$/);
  });

  it('keeps already-scoped paths intact', () => {
    const scoped = 'companies/default/years/2026/Stoklar';
    expect(mapPathToDatabase(scoped)).toBe(scoped);
  });
});

describe('getCacheKey', () => {
  it('derives a stable cache key from a path', () => {
    const key = getCacheKey('Stoklar');
    expect(key).toContain('ermay_cache_');
  });
});
