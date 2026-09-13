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
  subscribeToPath: jest.fn(() => () => {}),
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
    const mockProfile = { id: 1, firmaAdi: 'Ermay', logoBase64: 'abcd1234wxyz' };
    (firebaseModule.readData as jest.Mock).mockResolvedValueOnce(mockProfile);

    const result = await loadFirmaProfili();
    expect(result).not.toBeNull();
    expect(result.logoBase64).toBe('abcd1234wxyz');
    expect(AsyncStorage.setItem).toHaveBeenCalledWith('ermay_cached_company_logo', 'abcd1234wxyz');
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

  it('clears logo and AsyncStorage when profile on server has no logo', async () => {
    const mockProfileNoLogo = { id: 1, firmaAdi: 'Ermay', logoBase64: null, LogoBase64: null };
    (firebaseModule.readData as jest.Mock).mockResolvedValueOnce(mockProfileNoLogo);

    const result = await loadFirmaProfili();
    expect(result).not.toBeNull();
    expect(result.logoBase64).toBeNull();
    expect(AsyncStorage.removeItem).toHaveBeenCalledWith('ermay_cached_company_logo');
  });
});
