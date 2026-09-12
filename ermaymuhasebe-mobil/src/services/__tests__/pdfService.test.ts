import { cleanBase64Logo, loadFirmaProfili, resetPdfServiceCache } from '../pdfService';
import * as firebaseModule from '../firebase';
import AsyncStorage from '../storage';

jest.mock('../storage', () => ({
  __esModule: true,
  default: {
    getItem: jest.fn(async () => null),
    setItem: jest.fn(async () => {}),
    removeItem: jest.fn(async () => {}),
  },
}));

jest.mock('../firebase', () => ({
  readData: jest.fn(),
  mapAppToDatabase: jest.fn((_, val) => val),
  getFirebaseConfig: jest.fn(() => null),
  loadConfigFromStorage: jest.fn(async () => null),
  fetchWithTimeout: jest.fn(async () => ({ ok: false, json: async () => null })),
  getAuthParam: jest.fn(() => ''),
}));

jest.mock('expo-file-system/legacy', () => ({
  cacheDirectory: '/cache/',
  writeAsStringAsync: jest.fn(),
  EncodingType: { Base64: 'base64' },
}));

jest.mock('expo-sharing', () => ({
  isAvailableAsync: jest.fn(async () => true),
  shareAsync: jest.fn(async () => {}),
}));

describe('cleanBase64Logo', () => {
  it('returns null for null, undefined, or empty strings', () => {
    expect(cleanBase64Logo(null)).toBeNull();
    expect(cleanBase64Logo(undefined)).toBeNull();
    expect(cleanBase64Logo('')).toBeNull();
    expect(cleanBase64Logo('   ')).toBeNull();
  });

  it('preserves clean Base64 strings without modification', () => {
    const raw = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=';
    expect(cleanBase64Logo(raw)).toBe(raw);
  });

  it('strips data:image/png;base64, prefix', () => {
    const raw = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAE=';
    expect(cleanBase64Logo(raw)).toBe('iVBORw0KGgoAAAANSUhEUgAAAAE=');
  });

  it('strips data:image/jpeg;base64, prefix and whitespace', () => {
    const raw = '  data:image/jpeg;base64,  /9j/4AAQSkZJRgABAQEASABIAAD/2wBD   \n ';
    expect(cleanBase64Logo(raw)).toBe('/9j/4AAQSkZJRgABAQEASABIAAD/2wBD');
  });
});

describe('loadFirmaProfili', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    resetPdfServiceCache();
  });

  it('loads profile directly from FirmaProfili/1 when available', async () => {
    const mockProfile = { id: 1, firmaAdi: 'Ermay', logoBase64: 'abc123logo' };
    (firebaseModule.readData as jest.Mock).mockResolvedValueOnce(mockProfile);

    const result = await loadFirmaProfili();
    expect(result).not.toBeNull();
    expect(result.logoBase64).toBe('abc123logo');
    expect(AsyncStorage.setItem).toHaveBeenCalledWith('ermay_cached_company_logo', 'abc123logo');
  });

  it('falls back to AsyncStorage when network returns null', async () => {
    (firebaseModule.readData as jest.Mock).mockResolvedValue(null);
    (AsyncStorage.getItem as jest.Mock).mockImplementation(async (key: string) => {
      if (key === 'ermay_cached_company_logo') return 'cached_fallback_logo';
      return null;
    });

    const result = await loadFirmaProfili();
    expect(result).not.toBeNull();
    expect(result.logoBase64).toBe('cached_fallback_logo');
  });
});

describe('Native PDF Templates', () => {
  const { generateCariEkstreHtml, generateGenericTableHtml, generateMakbuzHtml } = require('../nativePdfTemplates');

  it('embeds the company logo in Cari Ekstre HTML when provided', () => {
    const payload = {
      Cari: { unvan: 'Örnek Müşteri Ltd.' },
      Hareketler: [
        { id: 1, tarih: '2026-09-12', islemTuru: 'Satış', aciklama: 'Fatura No 101', borc: 1500, alacak: 0 }
      ]
    };
    const logo = 'iVBORw0KGgoAAAANSUhEUgAAAAE=';
    const html = generateCariEkstreHtml(payload, logo, false);

    expect(html).toContain('data:image/png;base64,' + logo);
    expect(html).toContain('Örnek Müşteri Ltd.');
    expect(html).toContain('Genel Bilgiler');
    expect(html).toContain('İşlem Detayları');
    expect(html).toContain('₺1.500,00');
  });

  it('renders Cari Ekstre HTML cleanly without logo when logo is null', () => {
    const payload = {
      Cari: { unvan: 'Test Cari' },
      Hareketler: []
    };
    const html = generateCariEkstreHtml(payload, null, false);

    expect(html).not.toContain('data:image/png;base64,');
    expect(html).toContain('Test Cari');
    expect(html).toContain('Bu müşteri için herhangi bir işlem bulunamadı.');
  });

  it('embeds logo in Generic Table HTML', () => {
    const payload = {
      Title: 'Cari Bakiye Listesi',
      Headers: ['Kod', 'Ünvan', 'Bakiye'],
      Rows: [['C01', 'Müşteri A', '₺500,00']]
    };
    const logo = 'testLogoBase64';
    const html = generateGenericTableHtml(payload, logo);

    expect(html).toContain('data:image/png;base64,testLogoBase64');
    expect(html).toContain('Cari Bakiye Listesi');
    expect(html).toContain('Müşteri A');
  });

  it('embeds logo in Makbuz HTML', () => {
    const payload = {
      MakbuzTipi: 'Tahsilat',
      CariUnvan: 'Ahmet Yılmaz',
      Tarih: '2026-09-12',
      Tutar: 2500,
      Aciklama: 'Nakit Tahsilat'
    };
    const logo = 'testLogoBase64';
    const html = generateMakbuzHtml(payload, logo);

    expect(html).toContain('data:image/png;base64,testLogoBase64');
    expect(html).toContain('TAHSİLAT MAKBUZU');
    expect(html).toContain('Ahmet Yılmaz');
    expect(html).toContain('₺2.500,00');
  });
});
