/**
 * transactionService.ts — Masaüstü `FinansService.SaveTransactionAsync` mobil eşleniği.
 *
 * RTDB'de gerçek "transaction" olmadığı için, tüm yazımlar sırayla yapılır ve
 * hata durumunda (net / ağ / 5xx) şimdiye kadar yazılan kayıtlar geri alınır
 * (best-effort rollback). Böylece masaüstündeki atomik finans mantığı korunur:
 *   1) CariHareket (ana)                    + Cari bakiye güncelle
 *   2) Kasa veya Banka hareketi             + hesap bakiye güncelle
 *   3) Kredi Kartı / EFT detay kaydı        (yönteme göre)
 *   4) Yönlendirilen tedarikçi (ciro)       + banka/kasa çıkış hareketi
 */
import { writeData, deleteData, readData } from './firebase';
import { generateInt32Id } from '../utils/IdGenerator';

export type IslemTuru = 'Tahsilat' | 'Ödeme' | 'Alacak Dekontu' | 'Borç Dekontu';
export type OdemeYontemi = 'Nakit' | 'Kredi Kartı' | 'Havale/EFT' | 'Havale / EFT' | 'Çek' | 'Banka';

export interface FinancialTransactionRequest {
  cari: { id: number | string; unvan: string };
  amount: number;
  date: string;
  dateTime?: Date;
  transactionType: IslemTuru;
  method: OdemeYontemi;
  description: string;
  /** Nakit -> kasa (Bankalar/kartTuru=Kasa), diğer -> banka hesabı */
  selectedHesap?: { id: number | string; kartTuru?: string; bakiye?: number } | null;
  bankaAdi?: string;
  kartHesapNo?: string;
  slipDekontPath?: string;
  directedSupplier?: { id: number | string; unvan: string } | null;
  yonlendirmeTarihi?: string;
  /** Çek yöntemi için opsiyonel çek detayları */
  cekDetay?: any;
}

const getEvrakNoPrefix = (method: string): string => {
  if (method.includes('Kredi')) return 'KK-';
  if (method.includes('EFT') || method.includes('Havale')) return 'EFT-';
  if (method.includes('Çek')) return 'CK-';
  return 'TS-';
};

const getMethodAbbr = (method: string): string => {
  if (method.includes('Kredi')) return 'KK';
  if (method.includes('EFT') || method.includes('Havale')) return 'EFT';
  return method;
};

const randomKey = () => Date.now().toString(36) + Math.random().toString(36).substring(2, 8) + Date.now().toString(36).slice(-4);

// writeData ağ/bağlantı sorunlarında throw yerine false döner (firebase.ts).
// Burada tüm kritik yazımları sarar; başarısız olursa üstteki catch/rollback tetiklenir.
const mustWrite = async (path: string, data: any) => {
  const ok = await writeData(path, data);
  if (!ok) throw new Error(`writeData failed: ${path}`);
};

export const saveFinancialTransaction = async (req: FinancialTransactionRequest): Promise<boolean> => {
  if (!req.cari || req.amount <= 0) return false;

  const refId = randomKey();
  const evrakNo = getEvrakNoPrefix(req.method) + new Date().toISOString().replace(/[-:.TZ]/g, '').slice(0, 14);

  const isOdeme = req.transactionType === 'Ödeme' || req.transactionType === 'Borç Dekontu';
  const chId = generateInt32Id();
  const mainCH = {
    id: chId,
    cariId: req.cari.id,
    cariUnvan: req.cari.unvan || '',
    tarih: req.date,
    evrakNo,
    refId,
    islemTuru: `${req.transactionType} (${getMethodAbbr(req.method)})`,
    aciklama: `[${req.method}] ${req.description}`.trim(),
    borc: isOdeme ? req.amount : 0,
    alacak: !isOdeme ? req.amount : 0,
    yonlendirilenCariId: req.directedSupplier?.id || undefined,
    yonlendirilenCariUnvan: req.directedSupplier?.unvan || undefined,
  };

  const written: string[] = [];
  const rollback: (() => Promise<void>)[] = [];

  try {
    // --- 1. Cari hareketi + cari bakiyesi ---
    const chKey = String(chId);
    await mustWrite(`CariHareketler/${chKey}`, mainCH);
    written.push(`CariHareketler/${chKey}`);

    const cariKey = String(req.cari.id);
    const cariRef = await readData(`Cariler/${cariKey}`);
    const prevCari = cariRef ? { ...cariRef } : null;
    const cari = cariRef || { id: req.cari.id, unvan: req.cari.unvan, borc: 0, alacak: 0 };
    cari.borc = (cari.borc || 0) + mainCH.borc;
    cari.alacak = (cari.alacak || 0) + mainCH.alacak;
    await mustWrite(`Cariler/${cariKey}`, cari);
    written.push(`Cariler/${cariKey}`);
    if (prevCari) rollback.push(async () => { await writeData(`Cariler/${cariKey}`, prevCari); });

    // --- 2. Kasa / Banka hareketi + hesap bakiyesi ---
    if (req.selectedHesap && req.selectedHesap.id !== undefined && req.selectedHesap.id !== null) {
      const hesapKey = String(req.selectedHesap.id);
      const hesapRef = await readData(`Bankalar/${hesapKey}`);
      const prevHesap = hesapRef ? { ...hesapRef } : null;
      const hesap = hesapRef || { id: req.selectedHesap.id, hesapAdi: 'Kasa', kartTuru: req.selectedHesap.kartTuru || 'Kasa', bakiye: 0 };
      const isKasa = hesap.kartTuru === 'Kasa';

      const hareketId = generateInt32Id();
      const hareket = {
        id: hareketId,
        [isKasa ? 'kasaId' : 'bankaId']: req.selectedHesap.id,
        cariId: req.cari.id,
        cariUnvan: req.cari.unvan,
        tarih: req.date,
        evrakNo,
        refId,
        islemTuru: mainCH.islemTuru,
        aciklama: `${req.cari.unvan} - ${req.transactionType} (${req.description})`,
        giren: !isOdeme ? req.amount : 0,
        cikan: isOdeme ? req.amount : 0,
        tutar: req.amount,
        yonlendirilenCariId: req.directedSupplier?.id || undefined,
        yonlendirilenCariUnvan: req.directedSupplier?.unvan || undefined,
      };
      const hareketPath = isKasa ? 'KasaHareketler' : 'BankaHareketler';
      const hareketKey = String(hareketId);
      await mustWrite(`${hareketPath}/${hareketKey}`, hareket);
      written.push(`${hareketPath}/${hareketKey}`);

      const hesapBakiye = (hesap.bakiye || 0) + (hareket.giren - hareket.cikan);
      const hesapPayload = { ...hesap, kartTuru: isKasa ? 'Kasa' : hesap.kartTuru, bakiye: hesapBakiye };
      await mustWrite(`Bankalar/${hesapKey}`, hesapPayload);
      written.push(`Bankalar/${hesapKey}`);
      if (prevHesap) rollback.push(async () => { await writeData(`Bankalar/${hesapKey}`, prevHesap); });

      // --- 3. Kredi Kartı / EFT detay tabloları ---
      if (req.method.includes('Kredi')) {
        const kkId = generateInt32Id();
        const kk = {
          id: kkId,
          musteriId: req.cari.id,
          musteriUnvan: req.cari.unvan,
          tarih: req.date,
          tutar: req.amount,
          banka: req.bankaAdi || '',
          kartNo: req.kartHesapNo || '',
          onayKodu: refId,
          durum: req.directedSupplier ? 'Tedarikçiye Verildi' : 'Portföyde',
          aciklama: req.description,
          islemTuru: req.transactionType,
          slipDosyaYolu: req.slipDekontPath || '',
          yonlendirilenCariId: req.directedSupplier?.id || undefined,
          yonlendirilenCariUnvan: req.directedSupplier?.unvan || undefined,
        };
        const kkKey = String(kkId);
        await mustWrite(`KrediKartlari/${kkKey}`, kk);
        written.push(`KrediKartlari/${kkKey}`);
      } else if (req.method.includes('EFT') || req.method.includes('Havale')) {
        const eftId = generateInt32Id();
        const eft = {
          id: eftId,
          musteriId: req.cari.id,
          musteriUnvan: req.cari.unvan,
          tarih: req.date,
          tutar: req.amount,
          banka: req.bankaAdi || '',
          bankaId: req.selectedHesap.id,
          hesapNo: req.kartHesapNo || '',
          dekontNo: refId,
          durum: req.directedSupplier ? 'Tedarikçiye Yönlendirildi' : 'Tamamlandı',
          aciklama: req.description,
          islemTuru: req.transactionType,
          dekontPath: req.slipDekontPath || '',
          yonlendirilenCariId: req.directedSupplier?.id || undefined,
          yonlendirilenCariUnvan: req.directedSupplier?.unvan || undefined,
        };
        const eftKey = String(eftId);
        await mustWrite(`EftIslemleri/${eftKey}`, eft);
        written.push(`EftIslemleri/${eftKey}`);
      }
    }

    // --- 4. Yönlendirilen tedarikçi (ciro) ---
    if (req.directedSupplier && req.selectedHesap && req.selectedHesap.id !== undefined && req.selectedHesap.id !== null) {
      const supKey = String(req.directedSupplier.id);
      const supRef = await readData(`Cariler/${supKey}`);
      const prevSup = supRef ? { ...supRef } : null;
      const sup = supRef || { id: req.directedSupplier.id, unvan: req.directedSupplier.unvan, borc: 0, alacak: 0 };
      sup.borc = (sup.borc || 0) + req.amount;
      await mustWrite(`Cariler/${supKey}`, sup);
      written.push(`Cariler/${supKey}`);
      if (prevSup) rollback.push(async () => { await writeData(`Cariler/${supKey}`, prevSup); });

      const supChId = generateInt32Id();
      const supCH = {
        id: supChId,
        cariId: req.directedSupplier.id,
        cariUnvan: req.directedSupplier.unvan || '',
        tarih: req.yonlendirmeTarihi || req.date,
        evrakNo: evrakNo + '-SUP',
        refId: refId + '-SUP',
        islemTuru: `Ödeme (${getMethodAbbr(req.method)} Ciro)`,
        aciklama: `[Ciro] ${req.cari.unvan} üzerinden ciro edilen ${req.method} tahsilatı. (${req.description})`.trim(),
        borc: req.amount,
        alacak: 0,
      };
      const supChKey = String(supChId);
      await mustWrite(`CariHareketler/${supChKey}`, supCH);
      written.push(`CariHareketler/${supChKey}`);

      const hesapKey = String(req.selectedHesap.id);
      const hesapRef = await readData(`Bankalar/${hesapKey}`);
      const prevHesap = hesapRef ? { ...hesapRef } : null;
      const hesap = hesapRef || { id: req.selectedHesap.id, hesapAdi: 'Kasa', kartTuru: req.selectedHesap.kartTuru || 'Kasa', bakiye: 0 };
      const isKasa = hesap.kartTuru === 'Kasa';
      const ciroHareketId = generateInt32Id();
      const ciroHareket = {
        id: ciroHareketId,
        [isKasa ? 'kasaId' : 'bankaId']: req.selectedHesap.id,
        cariId: req.directedSupplier.id,
        cariUnvan: req.directedSupplier.unvan,
        tarih: req.yonlendirmeTarihi || req.date,
        evrakNo: evrakNo + '-SUP',
        refId: refId + '-SUP',
        islemTuru: supCH.islemTuru,
        aciklama: `Ciro Çıkışı -> ${req.directedSupplier.unvan} (${req.cari.unvan} üzerinden)`,
        giren: 0,
        cikan: req.amount,
        tutar: req.amount,
      };
      const ciroPath = isKasa ? 'KasaHareketler' : 'BankaHareketler';
      const ciroKey = String(ciroHareketId);
      await mustWrite(`${ciroPath}/${ciroKey}`, ciroHareket);
      written.push(`${ciroPath}/${ciroKey}`);

      const ciroPayload = { ...hesap, bakiye: (hesap.bakiye || 0) - req.amount };
      await mustWrite(`Bankalar/${hesapKey}`, ciroPayload);
      written.push(`Bankalar/${hesapKey}`);
      if (prevHesap) rollback.push(async () => { await writeData(`Bankalar/${hesapKey}`, prevHesap); });
    }

    return true;
  } catch (error) {
    console.error('[transactionService] İşlem başarısız, geri alınıyor:', error);
    // Rollback: yazılan kayıtları sil ve eski bakiyeleri geri yükle
    for (const r of rollback) { try { await r(); } catch {} }
    for (const p of written) { try { await deleteData(p); } catch {} }
    return false;
  }
};

export const deleteFinancialTransaction = async (cariHareket: any): Promise<boolean> => {
  if (!cariHareket) return false;
  const refId = cariHareket.refId;
  if (!refId) return false;
  const baseRefId = refId.endsWith('-SUP') ? refId.substring(0, refId.length - 4) : refId;

  try {
    // Cari harekete bağlı kayıtları topla
    const cariH = (await readData('CariHareketler')) || {};
    const cariHList = Array.isArray(cariH) ? cariH : Object.keys(cariH).map((k) => ({ ...cariH[k], firebaseKey: k }));
    const kasaH = (await readData('KasaHareketler')) || {};
    const kasaHList = Array.isArray(kasaH) ? kasaH : Object.keys(kasaH).map((k) => ({ ...kasaH[k], firebaseKey: k }));
    const bankaH = (await readData('BankaHareketler')) || {};
    const bankaHList = Array.isArray(bankaH) ? bankaH : Object.keys(bankaH).map((k) => ({ ...bankaH[k], firebaseKey: k }));
    const kk = (await readData('KrediKartlari')) || {};
    const kkList = Array.isArray(kk) ? kk : Object.keys(kk).map((k) => ({ ...kk[k], firebaseKey: k }));
    const eft = (await readData('EftIslemleri')) || {};
    const eftList = Array.isArray(eft) ? eft : Object.keys(eft).map((k) => ({ ...eft[k], firebaseKey: k }));

    const supReads: string[] = [];
    const farkCariler: any[] = [];
    let revertFailed = false;

    for (const h of cariHList.filter((x: any) => x.refId === baseRefId || x.refId === refId + '-SUP' || x.refId === refId)) {
      if (h.firebaseKey && supReads.indexOf(h.firebaseKey) === -1) supReads.push(h.firebaseKey);
      farkCariler.push({ cariId: h.cariId, borc: h.borc || 0, alacak: h.alacak || 0 });
    }

    // Cari bakiyeleri geri al
    const uniqueCari = farkCariler.filter((v, i, a) => a.findIndex((x) => x.cariId === v.cariId) === i);
    for (const fc of uniqueCari) {
      const cariRef = await readData(`Cariler/${fc.cariId}`);
      if (cariRef) {
        const okRevert = await writeData(`Cariler/${fc.cariId}`, {
          ...cariRef,
          borc: Math.max(0, (cariRef.borc || 0) - fc.borc),
          alacak: Math.max(0, (cariRef.alacak || 0) - fc.alacak),
        });
        if (!okRevert) revertFailed = true;
      }
    }

    // Kasa hareketlerini sil + bakiye geri
    for (const h of kasaHList.filter((x: any) => x.refId === baseRefId || x.refId === refId + '-SUP' || x.refId === refId)) {
      if (h.kasaId !== undefined && h.kasaId !== null) {
        const kRef = await readData(`Bankalar/${h.kasaId}`);
        if (kRef) {
          const okRevert = await writeData(`Bankalar/${h.kasaId}`, { ...kRef, bakiye: (kRef.bakiye || 0) - ((h.giren || 0) - (h.cikan || 0)) });
          if (!okRevert) revertFailed = true;
        }
      }
      if (h.firebaseKey) { try { await deleteData(`KasaHareketler/${h.firebaseKey}`); } catch {} }
    }

    for (const h of bankaHList.filter((x: any) => x.refId === baseRefId || x.refId === refId + '-SUP' || x.refId === refId)) {
      if (h.bankaId !== undefined && h.bankaId !== null) {
        const bRef = await readData(`Bankalar/${h.bankaId}`);
        if (bRef) {
          const okRevert = await writeData(`Bankalar/${h.bankaId}`, { ...bRef, bakiye: (bRef.bakiye || 0) - ((h.giren || 0) - (h.cikan || 0)) });
          if (!okRevert) revertFailed = true;
        }
      }
      if (h.firebaseKey) { try { await deleteData(`BankaHareketler/${h.firebaseKey}`); } catch {} }
    }

    // KK / EFT kayıtlarını sil
    for (const k of kkList.filter((x: any) => x.onayKodu === baseRefId)) {
      if (k.firebaseKey) { try { await deleteData(`KrediKartlari/${k.firebaseKey}`); } catch {} }
    }
    for (const e of eftList.filter((x: any) => x.dekontNo === baseRefId)) {
      if (e.firebaseKey) { try { await deleteData(`EftIslemleri/${e.firebaseKey}`); } catch {} }
    }

    // Cari hareketleri sil
    for (const key of supReads) {
      try { await deleteData(`CariHareketler/${key}`); } catch {}
    }
    if (revertFailed) {
      console.warn('[transactionService] Bakiye geri alma yazımları sıraya alındı (offline/kısmi).');
      return false;
    }
    return true;
  } catch (error) {
    console.error('[transactionService] Silme işlemi başarısız:', error);
    return false;
  }
};
