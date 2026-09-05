import { initializeApp, getApps, getApp } from 'firebase/app';
import { getDatabase, goOnline, goOffline } from 'firebase/database';
import AsyncStorage from './storage';
import { AppState, AppStateStatus, Alert } from 'react-native';
import NetInfo from '@react-native-community/netinfo';

export interface FirebaseConfig {
  url: string;
  secret?: string;
  tenantId?: string;
}

let dbInstance: any = null;
let currentUrl: string = '';

let cachedConfig: FirebaseConfig | null = null;
let cachedYear: string = '';
let cachedTenantId: string = 'default';
let cachedIdToken: string | null = null;

export const getIdToken = (): string | null => {
  return cachedIdToken;
};

export const saveIdToken = async (token: string | null) => {
  cachedIdToken = token;
  if (token) {
    await AsyncStorage.setItem('ermay_firebase_id_token', token);
  } else {
    await AsyncStorage.removeItem('ermay_firebase_id_token');
  }
};

let configListeners: (() => void)[] = [];
let forcePollCallbacks: (() => void)[] = [];
let lastPollTimers: Record<string, any> = {};

// --- Offline cache & write queue ---
const CACHE_PREFIX = 'ermay_cache_';
const QUEUE_KEY = 'ermay_write_queue';

interface QueuedWrite {
  path: string;
  method: 'PUT' | 'POST' | 'DELETE';
  body?: any;
  ts: number;
}

export const getCacheKey = (path: string): string => {
  const mapped = mapPathToDatabase(path);
  return CACHE_PREFIX + mapped.replace(/\//g, '_');
};

const readCache = async (path: string): Promise<any | null> => {
  try {
    const raw = await AsyncStorage.getItem(getCacheKey(path));
    if (!raw) return null;
    return JSON.parse(raw);
  } catch (e) {
    return null;
  }
};

export const writeCache = async (path: string, data: any) => {
  try {
    if (data === undefined || data === null) {
      await AsyncStorage.removeItem(getCacheKey(path));
    } else {
      await AsyncStorage.setItem(getCacheKey(path), JSON.stringify(data));
    }
  } catch (e) {
    // cache failures are non-fatal
  }
};

const readQueue = async (): Promise<QueuedWrite[]> => {
  try {
    const raw = await AsyncStorage.getItem(QUEUE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
};

const saveQueue = async (queue: QueuedWrite[]) => {
  try {
    if (queue.length === 0) {
      await AsyncStorage.removeItem(QUEUE_KEY);
    } else {
      await AsyncStorage.setItem(QUEUE_KEY, JSON.stringify(queue));
    }
  } catch (e) {
    // queue persistence failures are non-fatal
  }
};

const enqueue = async (op: QueuedWrite) => {
  const queue = await readQueue();
  queue.push(op);
  await saveQueue(queue);
};

export const addConfigListener = (listener: () => void) => {
  configListeners.push(listener);
  return () => {
    configListeners = configListeners.filter(l => l !== listener);
  };
};

export const notifyConfigListeners = () => {
  configListeners.forEach(l => l());
};

export const loadConfigFromStorage = async () => {
  try {
    const url = await AsyncStorage.getItem('ermay_firebase_url');
    const secret = await AsyncStorage.getItem('ermay_firebase_secret');
    const year = await AsyncStorage.getItem('ermay_active_year');
    const tenantId = await AsyncStorage.getItem('ermay_tenant_id');
    cachedIdToken = await AsyncStorage.getItem('ermay_firebase_id_token');
    
    if (url) cachedConfig = { url, secret: secret || undefined, tenantId: tenantId || 'default' };
    else cachedConfig = null;
    
    if (year) cachedYear = year;
    else cachedYear = new Date().getFullYear().toString();
    
    if (tenantId) cachedTenantId = tenantId;
    else cachedTenantId = 'default';
    
    return cachedConfig;
  } catch (error) {
    console.error('Error loading config from AsyncStorage:', error);
    return null;
  }
};

export const getFirebaseConfig = (): FirebaseConfig | null => {
  return cachedConfig;
};

export const saveFirebaseConfig = async (url: string, secret?: string, tenantId?: string) => {
  try {
    if (url) {
      await AsyncStorage.setItem('ermay_firebase_url', url.trim());
    } else {
      await AsyncStorage.removeItem('ermay_firebase_url');
    }
    
    if (secret) {
      await AsyncStorage.setItem('ermay_firebase_secret', secret.trim());
    } else {
      await AsyncStorage.removeItem('ermay_firebase_secret');
    }

    if (tenantId) {
      await AsyncStorage.setItem('ermay_tenant_id', tenantId.trim());
      cachedTenantId = tenantId.trim();
    }

    await loadConfigFromStorage();
    
    // Re-initialize database instance
    dbInstance = null;
    initFirebase();
    notifyConfigListeners();
  } catch (error) {
    console.error('Error saving config to AsyncStorage:', error);
  }
};

export const saveActiveYear = async (year: string) => {
  try {
    if (year) {
      await AsyncStorage.setItem('ermay_active_year', year);
      cachedYear = year;
      notifyConfigListeners();
    }
  } catch (error) {
    console.error('Error saving active year to AsyncStorage:', error);
  }
};

export const fetchAvailableYears = async (): Promise<string[]> => {
  try {
    const config = getFirebaseConfig();
    const tenant = config?.tenantId || 'default';
    
    // Yılları çekerken tüm veritabanını indirmemek için shallow=true kullanıyoruz
    const { url } = getRestUrl(`companies/${tenant}/years`);
    if (url) {
      const shallowUrl = url.includes('?') ? url + '&shallow=true' : url + '?shallow=true';
      const res = await fetchWithTimeout(shallowUrl, {
        method: 'GET',
        headers: {
          'Accept': 'application/json',
          'Cache-Control': 'no-cache, no-store, must-revalidate',
          'Pragma': 'no-cache',
          'Expires': '0'
        }
      }, 5000);
      if (res.ok) {
        const rawYears = await res.json();
        if (rawYears && typeof rawYears === 'object') {
          const yearKeys = Object.keys(rawYears).filter(k => /^\d{4}$/.test(k));
          if (yearKeys.length > 0) {
            return yearKeys.sort((a, b) => parseInt(b) - parseInt(a));
          }
        }
      }
    }
  } catch (e: any) {
    console.warn('[Firebase REST] Error fetching available years:', e?.message || e);
  }
  const currentYear = new Date().getFullYear().toString();
  return [currentYear];
};

let flushTimer: any = null;
let flushRunning = false;
let netInfoUnsubscribe: any = null;

export const startAutoFlush = () => {
  if (flushTimer) return;
  flushTimer = setInterval(() => {
    if (flushRunning) return;
    flushRunning = true;
    flushPendingWrites().finally(() => {
      flushRunning = false;
    });
  }, 15000);

  // Anlık ağ bağlantısını dinleyip geri geldiğinde hemen flush et
  if (!netInfoUnsubscribe) {
    netInfoUnsubscribe = NetInfo.addEventListener(state => {
      if (state.isConnected && state.isInternetReachable !== false) {
        if (!flushRunning) {
          flushRunning = true;
          flushPendingWrites().finally(() => { flushRunning = false; });
        }
      }
    });
  }

  if (flushPendingWrites) {
    setTimeout(() => {
      if (!flushRunning) {
        flushRunning = true;
        flushPendingWrites().finally(() => { flushRunning = false; });
      }
    }, 3000);
  }
};

export const stopAutoFlush = () => {
  if (flushTimer) {
    clearInterval(flushTimer);
    flushTimer = null;
  }
};

export const getPendingWriteCount = async (): Promise<number> => {
  const queue = await readQueue();
  return queue.length;
};

export const initFirebase = () => {
  const config = getFirebaseConfig();
  if (!config) {
    console.warn('[Firebase] Firebase URL is not configured yet.');
    return null;
  }

  try {
    if (dbInstance && currentUrl === config.url) {
      return dbInstance;
    }

    let dbUrl = config.url;
    if (config.secret) {
      const cleanUrl = config.url.replace(/\/$/, '');
      dbUrl = `${cleanUrl}?auth=${config.secret}`;
    }

    const firebaseConfig = {
      databaseURL: dbUrl
    };

    const app = getApps().length === 0 ? initializeApp(firebaseConfig) : getApp();
    dbInstance = getDatabase(app);
    currentUrl = config.url;
    
    goOnline(dbInstance);
    console.log('[Firebase] Initialized database with URL:', config.url);
    if (typeof startAutoFlush === 'function' && flushTimer === null) {
      try { startAutoFlush(); } catch {}
    }
    return dbInstance;
  } catch (error) {
    console.error('[Firebase] Initialization error:', error);
    return null;
  }
};

// Key conversion mappings
const exceptionMapToCamel: Record<string, string> = {
  Email: 'eposta',
  IBAN: 'iban',
  TCNo: 'tcNo',
  KDVOrani: 'kdvOrani',
  KdvOrani: 'kdvOrani',
  OrtalamaAlisFiyati: 'ortAlisFiyati',
  OrtalamaSatisFiyati: 'ortSatisFiyati',
};

const exceptionMapToPascal: Record<string, string> = {
  eposta: 'Email',
  iban: 'IBAN',
  tcNo: 'TCNo',
  kdv: 'KDV',
  ortAlisFiyati: 'OrtalamaAlisFiyati',
  ortSatisFiyati: 'OrtalamaSatisFiyati',
};

const toCamel = (key: string, resource?: string): string => {
  if (exceptionMapToCamel[key]) return exceptionMapToCamel[key];
  if (key === 'KDV') {
    if (resource && (
      resource.includes('FaturaDetaylar') || 
      resource.includes('SiparisDetaylar') || 
      resource.includes('TeklifDetaylar') ||
      resource.includes('Detaylar')
    )) {
      return 'kdvOrani';
    }
    return 'kdv';
  }
  if (resource && resource.includes('Notes')) {
    if (key === 'Title') return 'baslik';
    if (key === 'Content') return 'aciklama';
  }
  return key.charAt(0).toLowerCase() + key.slice(1);
};

const toPascal = (key: string, resource?: string): string => {
  if (key === 'kdvOrani') {
    if (resource && resource.includes('FaturaDetaylar')) {
      return 'KDVOrani';
    }
    return 'KdvOrani';
  }
  if (exceptionMapToPascal[key]) return exceptionMapToPascal[key];
  if (resource && resource.includes('Notes')) {
    if (key === 'baslik') return 'Title';
    if (key === 'aciklama') return 'Content';
  }
  return key.charAt(0).toUpperCase() + key.slice(1);
};

const deepMapKeys = (obj: any, keyMapper: (key: string, resource?: string) => string, resource?: string): any => {
  if (obj === null || obj === undefined) return obj;
  
  if (Array.isArray(obj)) {
    return obj.map(item => deepMapKeys(item, keyMapper, resource));
  }
  
  if (typeof obj === 'object') {
    if (obj instanceof Date) return obj;
    
    const result: any = {};
    for (const key of Object.keys(obj)) {
      const mappedKey = keyMapper(key, resource);
      result[mappedKey] = deepMapKeys(obj[key], keyMapper, resource);
    }
    return result;
  }
  
  return obj;
};

export const mapDatabaseToApp = (path: string, val: any): any => {
  const resource = path.split('/')[0];
  return deepMapKeys(val, toCamel, resource);
};

export const mapAppToDatabase = (path: string, val: any): any => {
  const resource = path.split('/')[0];
  return deepMapKeys(val, toPascal, resource);
};

export const mapPathToDatabase = (path: string): string => {
  if (
    path.startsWith('companies/') ||
    path.startsWith('security_requests') ||
    path.startsWith('users') ||
    path.startsWith('FirmaProfili')
  ) {
    return path;
  }

  const parts = path.split('/');
  const mappedParts = parts.map((part) => {
    if (/^\d+$/.test(part)) return part;
    return toPascal(part, parts[0]);
  });
  const mappedPath = mappedParts.join('/');

  const tenant = cachedTenantId || 'default';
  let year = cachedYear;
  if (!year) {
    year = new Date().getFullYear().toString();
  }
  return `companies/${tenant}/years/${year}/${mappedPath}`;
};

const getRestUrl = (path: string): { url: string; hasAuth: boolean } => {
  const config = getFirebaseConfig();
  if (!config) {
    return { url: '', hasAuth: false };
  }
  
  const mappedPath = mapPathToDatabase(path);
  const cleanBaseUrl = config.url.replace(/\/$/, '');
  
  // Use Firebase Auth ID Token if available, fallback to DB Secret
  const authParam = cachedIdToken ? `auth=${cachedIdToken}` : (config.secret ? `auth=${config.secret}` : '');
  const url = `${cleanBaseUrl}/${mappedPath}.json${authParam ? `?${authParam}` : ''}`;
  return { url, hasAuth: !!(cachedIdToken || config.secret) };
};

export const subscribeToPath = (path: string, callback: (data: any) => void): (() => void) => {
  let active = true;
  let timerId: any = null;
  let currentInterval = 1200; // Ultra hızlı anlık senkronizasyon (1.2 saniye)

  const poll = async () => {
    if (!active) return;

    if (AppState.currentState !== 'active') {
      timerId = setTimeout(poll, 10000);
      return;
    }

    try {
      const data = await readData(path);
      if (active) {
        callback(data || {});
      }
    } catch (e) {
      console.error(`[Firebase REST] Subscription polling error on path "${path}":`, e);
      if (active) callback(null);
    }
    if (active) {
      timerId = setTimeout(poll, currentInterval);
    }
  };

  const handleAppStateChange = (nextAppState: AppStateStatus) => {
    if (!active) return;
    if (nextAppState === 'active') {
      currentInterval = 1200;
      if (timerId) { clearTimeout(timerId); timerId = null; }
      poll();
    } else {
      currentInterval = 10000;
    }
  };

  const appStateSubscription = AppState.addEventListener('change', handleAppStateChange);

  const forcePollNow = () => {
    if (!active) return;
    if (timerId) { clearTimeout(timerId); timerId = null; }
    setTimeout(poll, 50);
  };

  forcePollCallbacks.push(forcePollNow);

  if (!cachedConfig) {
    setTimeout(poll, 500);
  } else {
    poll();
  }

  return () => {
    active = false;
    if (timerId) { clearTimeout(timerId); timerId = null; }
    appStateSubscription.remove();
    forcePollCallbacks = forcePollCallbacks.filter(f => f !== forcePollNow);
  };
};

const sanitizeForWrite = (path: string, data: any): any => {
  if (data === null || data === undefined) return data;

  const resource = path.split('/')[0];
  const resourceEntities = ['Stoklar', 'Cariler', 'Faturalar', 'CariHareketler', 'StokHareketler',
    'Siparisler', 'Teklifler', 'Bankalar', 'Kasalar', 'Cekler', 'Senetler',
    'KrediKartlari', 'EftIslemleri', 'StokSayimlar', 'PortfoyKartlari',
    'SatisHedefleri', 'HaftalikSatisHedefleri', 'YillikSatisHedefleri',
    'DovizKurlari', 'BelgeArsiv', 'Notes', 'Gorevler', 'Personeller'];

  const shouldAddTenant = resourceEntities.some(r => resource.includes(r)) ||
    ['Stok', 'Cari', 'Fatura', 'Siparis', 'Teklif', 'Banka', 'Kasa', 'Cek', 'Senet',
     'KrediKarti', 'Eft', 'StokSayim', 'Hedef', 'Doviz', 'Note'].some(prefix => resource.startsWith(prefix));

  const sanitizeObj = (obj: any): any => {
    if (obj === null || obj === undefined) return obj;
    if (Array.isArray(obj)) return obj.map(sanitizeObj);
    if (typeof obj !== 'object') return obj;
    if (obj instanceof Date) return obj;

    const result: any = {};
    for (const key of Object.keys(obj)) {
      // Skip internal-only fields
      if (key === 'firebaseKey' || key === 'FirebaseKey') continue;
      const val = obj[key];
      if (val === undefined) continue;
      result[key] = sanitizeObj(val);
    }

    // Ensure IsDeleted has explicit default false for known entities
    if (result.isDeleted === undefined || result.IsDeleted === undefined) {
      if (result.isDeleted !== true) result.isDeleted = false;
    }

    // Ensure TenantId field exists for desktop ITenantEntity compatibility
    if (shouldAddTenant) {
      if (result.tenantId === undefined && result.TenantId === undefined) {
        result.tenantId = cachedTenantId || 'default';
      }
    }

    return result;
  };

  return sanitizeObj(data);
};

export const writeData = async (path: string, data: any): Promise<boolean> => {
  const { url } = getRestUrl(path);
  if (!url) return false;

  try {
    const safeData = sanitizeForWrite(path, data);
    const mappedData = mapAppToDatabase(path, safeData);
    const res = await fetch(url, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(mappedData),
    });
    if (res.ok) {
      writeCache(path, mapDatabaseToApp(path, mappedData));
      forcePollCallbacks.forEach(cb => {
        try { cb(); } catch {}
      });
    } else if (res.status >= 500) {
      // Server reachable but failing; keep for retry so the edit isn't lost
      await enqueue({ path, method: 'PUT', body: mappedData, ts: Date.now() });
    }
    return res.ok;
  } catch (error) {
    console.error(`[Firebase REST] Write error on path "${path}":`, error);
    // Network-level failure -> offline, queue for later sync
    const safeData = sanitizeForWrite(path, data);
    const mappedData = mapAppToDatabase(path, safeData);
    await enqueue({ path, method: 'PUT', body: mappedData, ts: Date.now() });
    return false;
  }
};

export const updateDataBatch = async (updates: Record<string, any>): Promise<boolean> => {
  const config = getFirebaseConfig();
  if (!config || !config.url) return false;

  const tenant = config.tenantId || 'default';
  const year = cachedYear || new Date().getFullYear().toString();
  const cleanBaseUrl = config.url.replace(/\/$/, '');
  const authParam = cachedIdToken ? `auth=${cachedIdToken}` : (config.secret ? `auth=${config.secret}` : '');

  // Kök URL'ye (veya year seviyesine) PATCH atarak atomik güncelleme yapıyoruz
  const url = `${cleanBaseUrl}/companies/${tenant}/years/${year}.json${authParam ? `?${authParam}` : ''}`;
  
  const mappedUpdates: Record<string, any> = {};
  const deltasToSync: { collection: string, id: string, borcDelta: number, alacakDelta: number }[] = [];

  for (const [key, value] of Object.entries(updates)) {
    const [collection, id] = key.split('/');
    if (collection && id) {
      
      // Calculate deltas for continuous carry forward
      if (['Cariler', 'Kasalar', 'Bankalar', 'Portfoy'].includes(collection)) {
         const oldData = await readData(key);
         const oldBorc = oldData ? (oldData.borc || oldData.Borc || 0) : 0;
         const oldAlacak = oldData ? (oldData.alacak || oldData.Alacak || 0) : 0;
         const newBorc = value.borc || value.Borc || 0;
         const newAlacak = value.alacak || value.Alacak || 0;
         
         if (newBorc - oldBorc !== 0 || newAlacak - oldAlacak !== 0) {
            deltasToSync.push({ 
              collection, id, 
              borcDelta: newBorc - oldBorc, 
              alacakDelta: newAlacak - oldAlacak 
            });
         }
      }

      const safeData = sanitizeForWrite(collection, value);
      const mappedData = mapAppToDatabase(collection, safeData);
      const mappedCollection = toPascal(collection, collection);
      mappedUpdates[`${mappedCollection}/${id}`] = mappedData;
      // Also update local cache for this path
      writeCache(key, mapDatabaseToApp(collection, mappedData));
    } else {
      mappedUpdates[key] = value;
    }
  }

  try {
    const res = await fetch(url, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(mappedUpdates),
    });

    if (res.ok) {
      forcePollCallbacks.forEach(cb => { try { cb(); } catch {} });
      
      // Async trigger future balances update
      deltasToSync.forEach(delta => {
        updateFutureBalances(delta.collection, delta.id, delta.borcDelta, delta.alacakDelta);
      });
      
      return true;
    } else {
      console.error('[Firebase REST] Batch update failed with status:', res.status);
      return false;
    }
  } catch (error) {
    console.error('[Firebase REST] Batch update network error:', error);
    return false;
  }
};

export const updateFutureBalances = async (entityType: string, entityId: string | number, borcDelta: number, alacakDelta: number) => {
  if (borcDelta === 0 && alacakDelta === 0) return;
  
  try {
    const config = getFirebaseConfig();
    if (!config) return;
    
    const tenant = config.tenantId || 'default';
    const cleanBaseUrl = config.url.replace(/\/$/, '');
    const authParam = cachedIdToken ? `auth=${cachedIdToken}` : (config.secret ? `auth=${config.secret}` : '');

    const years = await fetchAvailableYears();
    const currentYear = parseInt(cachedYear || new Date().getFullYear().toString(), 10);
    
    const futureYears = years
      .map(y => parseInt(y, 10))
      .filter(y => !isNaN(y) && y > currentYear)
      .sort((a, b) => a - b);
      
    if (futureYears.length === 0) return;

    for (const year of futureYears) {
      const mappedCollection = mapPathToDatabase(entityType);
      const url = `${cleanBaseUrl}/companies/${tenant}/years/${year}/${mappedCollection}/${entityId}.json${authParam ? `?${authParam}` : ''}`;
      
      const getRes = await fetch(url);
      if (getRes.ok) {
        const entityNode = await getRes.json();
        if (entityNode) {
          const currentDevirBorc = entityNode.devirBorc || entityNode.DevirBorc || 0;
          const currentDevirAlacak = entityNode.devirAlacak || entityNode.DevirAlacak || 0;
          const currentAcilis = entityNode.acilisBakiyesi || entityNode.AcilisBakiyesi || 0;
          
          const patchData: any = {};
          
          if (entityType === 'Cariler') {
             patchData.devirBorc = currentDevirBorc + borcDelta;
             patchData.devirAlacak = currentDevirAlacak + alacakDelta;
          } else {
             patchData.acilisBakiyesi = currentAcilis + (borcDelta - alacakDelta);
          }
          
          await fetch(url, {
             method: 'PATCH',
             headers: { 'Content-Type': 'application/json' },
             body: JSON.stringify(patchData)
          });
        }
      }
    }
  } catch (error) {
    console.warn('[Firebase] updateFutureBalances error:', error);
  }
};

export const updateCariBaseInfoInAllYears = async (cariId: number | string, baseInfo: any): Promise<void> => {
  try {
    const config = getFirebaseConfig();
    if (!config) return;
    
    const tenant = config.tenantId || 'default';
    const cleanBaseUrl = config.url.replace(/\/$/, '');
    const authParam = cachedIdToken ? `auth=${cachedIdToken}` : (config.secret ? `auth=${config.secret}` : '');

    const years = await fetchAvailableYears();
    const currentYear = cachedYear || new Date().getFullYear().toString();

    // Sadece Cari tablosunda geçerli temel bilgiler
    const safeBaseInfo = { ...baseInfo };
    // Borç/Alacak ve yıla özgü olabilecek bakiye/ID alanlarını siliyoruz
    delete safeBaseInfo.borc;
    delete safeBaseInfo.alacak;
    delete safeBaseInfo.Borc;
    delete safeBaseInfo.Alacak;

    const mappedBaseInfo = mapAppToDatabase('Cariler', safeBaseInfo);

    for (const year of years) {
      if (year === currentYear) continue; // Aktif yılı zaten writeData güncelledi

      const url = `${cleanBaseUrl}/companies/${tenant}/years/${year}/Cariler/${cariId}.json${authParam ? `?${authParam}` : ''}`;
      
      const getRes = await fetch(url);
      if (getRes.ok) {
        const existingCari = await getRes.json();
        if (existingCari) {
          // Kısmi güncelleme (PATCH)
          await fetch(url, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(mappedBaseInfo),
          });
        }
      }
    }
  } catch (error) {
    console.error('[Firebase] updateCariBaseInfoInAllYears error:', error);
  }
};

export const pushData = async (path: string, data: any): Promise<string | null> => {
  const { url } = getRestUrl(path);
  if (!url) return null;

  try {
    const safeData = sanitizeForWrite(path, data);
    const mappedData = mapAppToDatabase(path, safeData);
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(mappedData),
    });
    if (res.ok) {
      const resData = await res.json();
      writeCache(path, mapDatabaseToApp(path, mappedData));
      forcePollCallbacks.forEach(cb => { try { cb(); } catch {} });
      return resData.name;
    } else if (res.status >= 500) {
      await enqueue({ path, method: 'POST', body: mappedData, ts: Date.now() });
    }
    return null;
  } catch (error) {
    console.error(`[Firebase REST] Push error on path "${path}":`, error);
    const safeData = sanitizeForWrite(path, data);
    const mappedData = mapAppToDatabase(path, safeData);
    await enqueue({ path, method: 'POST', body: mappedData, ts: Date.now() });
    return null;
  }
};

export const deleteData = async (path: string): Promise<boolean> => {
  const { url } = getRestUrl(path);
  if (!url) return false;

  try {
    const res = await fetch(url, {
      method: 'DELETE',
    });
    if (res.ok) {
      writeCache(path, null);
      forcePollCallbacks.forEach(cb => { try { cb(); } catch {} });
    } else if (res.status >= 500) {
      await enqueue({ path, method: 'DELETE', ts: Date.now() });
    }
    return res.ok;
  } catch (error) {
    console.error(`[Firebase REST] Delete error on path "${path}":`, error);
    await enqueue({ path, method: 'DELETE', ts: Date.now() });
    return false;
  }
};

export const goOfflineMode = async () => {
  cachedConfig = null;
  dbInstance = null;
  await saveIdToken(null);
  await AsyncStorage.removeItem('ermay_logged_user');
  notifyConfigListeners();
};
export const goOnlineMode = () => {};

export const fetchWithTimeout = async (url: string, options: any = {}, timeoutMs = 5000): Promise<Response> => {
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal
    });
    clearTimeout(id);
    return response;
  } catch (error: any) {
    clearTimeout(id);
    if (error.name === 'AbortError') {
      throw new Error('Timeout');
    }
    throw error;
  }
};

export const readData = async (path: string): Promise<any> => {
  const { url } = getRestUrl(path);
  if (!url) return null;

  try {
    const res = await fetchWithTimeout(url, {}, 5000);
    if (res.ok) {
      const val = await res.json();
      const mapped = mapDatabaseToApp(path, val);
      writeCache(path, mapped);
      return mapped;
    }
    console.warn(`[Firebase REST] Read error on path "${path}": Status ${res.status}`);
    // Server reachable but error -> serve cached snapshot if present
    const cached = await readCache(path);
    return cached !== null ? cached : null;
  } catch (error: any) {
    if (error?.message === 'Timeout' || error?.name === 'AbortError') {
      console.warn(`[Firebase REST] Timeout on path "${path}" (5s)`);
    } else {
      console.warn(`[Firebase REST] Read error on path "${path}":`, error.message || error);
    }
    // Network-level failure (offline) -> serve cached snapshot
    const cached = await readCache(path);
    return cached !== null ? cached : null;
  }
};

export const flushPendingWrites = async (): Promise<number> => {
  const queue = await readQueue();
  if (queue.length === 0) return 0;

  const retryable: QueuedWrite[] = [];
  let flushed = 0;

  for (const op of queue) {
    const { url } = getRestUrl(op.path);
    if (!url) { retryable.push(op); continue; }

    const init: RequestInit = {
      method: op.method,
      headers: { 'Content-Type': 'application/json' },
    };
    if (op.method !== 'DELETE' && op.body !== undefined) {
      init.body = JSON.stringify(op.body);
    }

    try {
      const res = await fetch(url, init);
      if (res.ok) {
        flushed++;
        try {
          const payload = await res.json();
          if ((payload && payload.name) || op.method === 'DELETE') {
            // Basic-path datalara özel bir şey gerekmez
          }
        } catch {}
        forcePollCallbacks.forEach(cb => { try { cb(); } catch {} });
      } else {
        retryable.push(op);
      }
    } catch (error) {
      console.error('[Firebase REST] Flush attempt failed, will retry later:', error);
      retryable.push(op);
    }
  }

  await saveQueue(retryable);
  return flushed;
};

// ---------------------------------------------------------------------------
// Kasa / Banka şema paritesi (Faz 1.1)
// ---------------------------------------------------------------------------
// Masaüstü kasayı ayrı bir `Kasalar` düğümünde değil, `Bankalar` düğümü altında
// `kartTuru: "Kasa"` ile tutar (BankaKart.KartTuru). Mobil de bu şemaya
// geçti. Geriye dönük uyum için eski `Kasalar` düğümü hâlâ okunur ve
// `Bankalar` içindeki `kartTuru === 'Kasa'` kayıtlarla birleştirilir.
export const KASA_KART_TURU = 'Kasa';

export const isKasaRecord = (record: any): boolean => {
  if (!record) return false;
  const t = (record.kartTuru || record.KartTuru || '');
  return t === 'Kasa';
};

export const splitAccounts = (bankalar: any[]): { kasalar: any[]; bankalar: any[] } => {
  const kasalar: any[] = [];
  const bankalar_ = bankalar.filter((b) => {
    if (isKasaRecord(b)) {
      kasalar.push(b);
      return false;
    }
    return true;
  });
  return { kasalar, bankalar: bankalar_ };
};

export const mergeKasalar = (bankKasa: any[], legacyKasa: any[]): any[] => {
  const map = new Map<number | string, any>();
  (bankKasa || []).forEach((k) => {
    if (k && k.isDeleted !== true && k.id !== undefined && k.id !== null) {
      map.set(k.id, k);
    }
  });
  (legacyKasa || []).forEach((k) => {
    if (!k || k.isDeleted === true) return;
    const id = k.id !== undefined && k.id !== null ? k.id : k.firebaseKey;
    if (id !== undefined && id !== null && !map.has(id)) map.set(id, k);
  });
  return Array.from(map.values());
};

// Kasa kaydı masaüstüyle uyumlu `Bankalar/{id}` altında tutulur.
export const getKasaWritePath = (id: number | string): string => `Bankalar/${id}`;

export const toKasaRecord = (kasa: any): any => ({
  ...kasa,
  kartTuru: KASA_KART_TURU,
  hesapAdi: kasa.hesapAdi || kasa.ad || kasa.isim || 'Kasa',
});

// SHA-256 implementation in pure JS (for local password verification)
export function sha256(str: string): string {
  const utf8 = unescape(encodeURIComponent(str));
  const words: number[] = [];
  for (let i = 0; i < utf8.length; i++) {
    words[i >> 2] |= (utf8.charCodeAt(i) & 0xff) << (24 - (i % 4) * 8);
  }
  
  const sigBytes = utf8.length;
  words[sigBytes >> 2] |= 0x80 << (24 - (sigBytes % 4) * 8);
  const wlen = (((sigBytes + 8) >> 6) + 1) * 16;
  words[wlen - 1] = sigBytes * 8;
  
  let H0 = 0x6a09e667;
  let H1 = 0xbb67ae85;
  let H2 = 0x3c6ef372;
  let H3 = 0xa54ff53a;
  let H4 = 0x510e527f;
  let H5 = 0x9b05688c;
  let H6 = 0x1f83d9ab;
  let H7 = 0x5be0cd19;
  
  const K = [
    0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
    0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
    0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
    0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
    0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
    0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
    0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
    0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
  ];
  
  const W = new Array(64);
  
  for (let i = 0; i < words.length; i += 16) {
    let a = H0;
    let b = H1;
    let c = H2;
    let d = H3;
    let e = H4;
    let f = H5;
    let g = H6;
    let h = H7;
    
    for (let j = 0; j < 64; j++) {
      if (j < 16) {
        W[j] = words[i + j] | 0;
      } else {
        const gamma0x = W[j - 15];
        const gamma0 = ((gamma0x >>> 7) | (gamma0x << 25)) ^ ((gamma0x >>> 18) | (gamma0x << 14)) ^ (gamma0x >>> 3);
        const gamma1x = W[j - 2];
        const gamma1 = ((gamma1x >>> 17) | (gamma1x << 15)) ^ ((gamma1x >>> 19) | (gamma1x << 13)) ^ (gamma1x >>> 10);
        W[j] = (gamma0 + W[j - 7] + gamma1 + W[j - 16]) | 0;
      }
      
      const ch = (e & f) ^ (~e & g);
      const maj = (a & b) ^ (a & c) ^ (b & c);
      const sigma0 = ((a >>> 2) | (a << 30)) ^ ((a >>> 13) | (a << 19)) ^ ((a >>> 22) | (a << 10));
      const sigma1 = ((e >>> 6) | (e << 26)) ^ ((e >>> 11) | (e << 21)) ^ ((e >>> 25) | (e << 7));
      
      const t1 = (h + sigma1 + ch + K[j] + W[j]) | 0;
      const t2 = (sigma0 + maj) | 0;
      
      h = g;
      g = f;
      f = e;
      e = (d + t1) | 0;
      d = c;
      c = b;
      b = a;
      a = (t1 + t2) | 0;
    }
    
    H0 = (H0 + a) | 0;
    H1 = (H1 + b) | 0;
    H2 = (H2 + c) | 0;
    H3 = (H3 + d) | 0;
    H4 = (H4 + e) | 0;
    H5 = (H5 + f) | 0;
    H6 = (H6 + g) | 0;
    H7 = (H7 + h) | 0;
  }
  
  const bytes: number[] = [];
  const hashArr = [H0, H1, H2, H3, H4, H5, H6, H7];
  for (let i = 0; i < 8; i++) {
    const val = hashArr[i];
    bytes.push((val >>> 24) & 0xff);
    bytes.push((val >>> 16) & 0xff);
    bytes.push((val >>> 8) & 0xff);
    bytes.push(val & 0xff);
  }
  
  return bytesToBase64(bytes);
}

function bytesToBase64(bytes: number[]): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
  let base64 = '';
  const len = bytes.length;
  const remainder = len % 3;
  const mainLen = len - remainder;
  
  let chunk;
  for (let i = 0; i < mainLen; i += 3) {
    chunk = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2];
    base64 += chars[(chunk & 16515072) >> 18] +
              chars[(chunk & 258048) >> 12] +
              chars[(chunk & 4032) >> 6] +
              chars[chunk & 63];
  }
  
  if (remainder === 1) {
    chunk = bytes[mainLen];
    base64 += chars[(chunk & 252) >> 2] + chars[(chunk & 3) << 4] + '==';
  } else if (remainder === 2) {
    chunk = (bytes[mainLen] << 8) | bytes[mainLen + 1];
    base64 += chars[(chunk & 64512) >> 10] + chars[(chunk & 1008) >> 4] + chars[(chunk & 15) << 2] + '=';
  }
  
  return base64;
}

export const loginUser = async (usernameOrEmail: string, password: string): Promise<{ success: boolean; error?: string; user?: any }> => {
  try {
    console.log('[loginUser] Başlıyor... usernameOrEmail:', usernameOrEmail);
    console.log('[loginUser] readData(FirmaProfili/1) çağrılıyor...');
    const profil = await readData('FirmaProfili/1');
    console.log('[loginUser] FirmaProfili sonucu:', profil ? 'VAR' : 'NULL');
    if (!profil) {
      // Return a temporary success if first setup and default credentials used
      if (usernameOrEmail === 'admin' && password === '123') {
        const defaultUser = { username: 'admin', role: 'Admin' };
        await AsyncStorage.setItem('ermay_logged_user', JSON.stringify(defaultUser));
        return { success: true, user: defaultUser };
      }
      return { success: false, error: 'Sistem profili alınamadı. Lütfen bağlantı ayarlarını kontrol edin.' };
    }

    const isFirebaseAuthEnabled = profil.isFirebaseAuthEnabled || false;
    const firebaseAuthApiKey = profil.firebaseAuthApiKey || '';
    console.log('[loginUser] isFirebaseAuthEnabled:', isFirebaseAuthEnabled, 'apiKey:', firebaseAuthApiKey ? 'VAR' : 'YOK');

    if (isFirebaseAuthEnabled && firebaseAuthApiKey) {
      const res = await fetchWithTimeout(`https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=${firebaseAuthApiKey}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: usernameOrEmail,
          password: password,
          returnSecureToken: true
        })
      }, 5000);

      if (!res.ok) {
        const errJson = await res.json();
        const errMsg = errJson?.error?.message || '';
        let userFriendlyMsg = 'Giriş başarısız.';
        if (errMsg.includes('EMAIL_NOT_FOUND') || errMsg.includes('INVALID_PASSWORD')) {
          userFriendlyMsg = 'E-posta veya şifre hatalı.';
        } else if (errMsg.includes('USER_DISABLED')) {
          userFriendlyMsg = 'Bu kullanıcı hesabı engellenmiş.';
        }
        return { success: false, error: userFriendlyMsg };
      }

      const authData = await res.json();
      await saveIdToken(authData.idToken);
      
      const usersData = await readData('users');
      let matchedUser: any = null;
      if (usersData) {
        const usersList = Object.values(usersData);
        matchedUser = usersList.find((u: any) => 
          (u.email || u.Email)?.toLowerCase() === usernameOrEmail.toLowerCase() || 
          u.firebaseAuthUid === authData.localId
        );
      }

      const userData = matchedUser ? {
        username: matchedUser.username || matchedUser.Username || usernameOrEmail.split('@')[0],
        email: matchedUser.email || matchedUser.Email || usernameOrEmail,
        role: matchedUser.role || matchedUser.Role || 'Admin',
        firebaseAuthUid: authData.localId
      } : {
        username: usernameOrEmail.split('@')[0],
        email: usernameOrEmail,
        role: 'Admin',
        firebaseAuthUid: authData.localId
      };

      await AsyncStorage.setItem('ermay_logged_user', JSON.stringify(userData));
      return { success: true, user: userData };

    } else {
      console.log('[loginUser] Local auth modunda, readData(users) çağrılıyor...');
      const usersData = await readData('users');
      console.log('[loginUser] users sonucu:', usersData ? 'VAR (' + Object.keys(usersData).length + ' kayıt)' : 'NULL');
      if (!usersData) {
        if (usernameOrEmail === 'admin' && password === '123') {
          const defaultUser = { username: 'admin', role: 'Admin' };
          await AsyncStorage.setItem('ermay_logged_user', JSON.stringify(defaultUser));
          return { success: true, user: defaultUser };
        }
        return { success: false, error: 'Sistemde kayıtlı kullanıcı bulunamadı.' };
      }

      const usersList = Object.values(usersData);
      console.log('[loginUser] usersList count:', usersList.length);
      const matchedUser: any = usersList.find((u: any) => 
        (u.username || u.Username || '').toLowerCase() === usernameOrEmail.toLowerCase() || 
        (u.email || u.Email || '').toLowerCase() === usernameOrEmail.toLowerCase()
      );
      console.log('[loginUser] matchedUser:', matchedUser ? 'BULUNDU' : 'BULUNAMADI');

      if (!matchedUser) {
        return { success: false, error: 'Kullanıcı adı veya şifre hatalı.' };
      }

      const storedHash = matchedUser.password || matchedUser.Password || '';
      const storedSalt = matchedUser.passwordSalt || matchedUser.PasswordSalt || '';

      let verifyResult = false;
      if (!storedSalt) {
        verifyResult = sha256(password) === storedHash;
      } else {
        verifyResult = sha256(password + storedSalt) === storedHash;
      }

      if (!verifyResult) {
        return { success: false, error: 'Kullanıcı adı veya şifre hatalı.' };
      }

      const userData = {
        username: matchedUser.username || matchedUser.Username,
        email: matchedUser.email || matchedUser.Email,
        role: matchedUser.role || matchedUser.Role || 'User',
      };

      await AsyncStorage.setItem('ermay_logged_user', JSON.stringify(userData));
      return { success: true, user: userData };
    }
  } catch (error: any) {
    console.error('Login error:', error);
    return { success: false, error: 'Veritabanı bağlantı hatası oluştu.' };
  }
};

export const logoutUser = async () => {
  await saveIdToken(null);
  await AsyncStorage.removeItem('ermay_logged_user');
  notifyConfigListeners();
};

export const getLoggedUser = async (): Promise<any | null> => {
  try {
    const raw = await AsyncStorage.getItem('ermay_logged_user');
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};
