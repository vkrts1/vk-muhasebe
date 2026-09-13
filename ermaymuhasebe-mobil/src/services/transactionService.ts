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

export const deleteFaturaCascade = async (faturaIdOrNo: number | string): Promise<boolean> => {
  if (!faturaIdOrNo) return false;

  try {
    const toKeyList = (raw: any) => {
      if (!raw) return [];
      if (Array.isArray(raw)) {
        return raw.map((item, idx) => item ? ({ ...item, firebaseKey: String(item?.id ?? idx) }) : null).filter(Boolean);
      }
      return Object.keys(raw).map((k) => ({ ...raw[k], firebaseKey: k }));
    };

    // 1. Locate the invoice
    let oldFatura: any = null;
    const cleanNo = String(faturaIdOrNo).startsWith('KPL-') ? String(faturaIdOrNo).substring(4).trim() : String(faturaIdOrNo).trim();

    if (typeof faturaIdOrNo === 'number' || (!isNaN(Number(faturaIdOrNo)) && Number(faturaIdOrNo) > 0)) {
      const numId = Number(faturaIdOrNo);
      const raw = await readData(`Faturalar/${numId}`);
      if (raw && (raw.id !== undefined || raw.faturaNo)) {
        oldFatura = { ...raw, id: raw.id ?? numId };
      }
    }

    if (!oldFatura) {
      const allFaturalarRaw = (await readData('Faturalar')) || {};
      const allFaturalar = toKeyList(allFaturalarRaw);
      oldFatura = allFaturalar.find((f: any) =>
        (f.id !== undefined && String(f.id) === cleanNo) ||
        (f.faturaNo && String(f.faturaNo).trim().toLowerCase() === cleanNo.toLowerCase())
      );
    }

    if (!oldFatura) {
      console.warn(`[transactionService] Fatura bulunamadı: ${faturaIdOrNo}`);
      return false;
    }

    const faturaId = oldFatura.id;
    const faturaNo = oldFatura.faturaNo || '';
    const isSatis = oldFatura.tur === 'Satış' || oldFatura.tur === 'Satis';
    const genelToplam = parseFloat(oldFatura.genelToplam) || 0;
    const isPaid = (oldFatura.odenen || 0) > 0 || (oldFatura.odemeSekli && oldFatura.odemeSekli !== 'Açık' && oldFatura.odemeSekli !== 'Acik');

    // 2. Revert Stock balances
    const detayRaw = (await readData(`FaturaDetaylar/${faturaId}`)) || [];
    let detaylar = Array.isArray(detayRaw)
      ? detayRaw.filter(Boolean)
      : Object.keys(detayRaw).map(key => ({ ...(detayRaw as any)[key], id: parseInt(key) || key }));

    if (!detaylar.length && faturaNo) {
      const shRaw = (await readData('StokHareketler')) || {};
      const shList = toKeyList(shRaw);
      const matchSh = shList.filter((h: any) =>
        (h.faturaId !== undefined && (h.faturaId === faturaId || String(h.faturaId) === String(faturaId))) ||
        (h.evrakNo && String(h.evrakNo) === faturaNo)
      );
      detaylar = matchSh.map((m: any) => ({
        stokId: m.stokId,
        miktar: m.miktar || (isSatis ? m.cikan : m.giren) || 0
      }));
    }

    for (const d of detaylar) {
      if (!d.stokId) continue;
      try {
        const freshStok = await readData(`Stoklar/${d.stokId}`);
        if (freshStok) {
          const miktar = parseFloat(d.miktar) || 0;
          const currentMiktar = parseFloat(freshStok.miktar) || 0;
          const yeniMiktar = isSatis ? (currentMiktar + miktar) : (currentMiktar - miktar);
          await writeData(`Stoklar/${d.stokId}`, { ...freshStok, miktar: yeniMiktar, id: d.stokId });
        }
      } catch (e) {
        console.error(`[transactionService] Stok ${d.stokId} bakiye geri alınamadı:`, e);
      }
    }

    // 3. Revert Cari balance
    if (oldFatura.cariId) {
      try {
        const freshCari = await readData(`Cariler/${oldFatura.cariId}`);
        if (freshCari) {
          const updatedCari = { ...freshCari };
          if (isPaid) {
            updatedCari.borc = Math.max(0, (parseFloat(updatedCari.borc) || 0) - genelToplam);
            updatedCari.alacak = Math.max(0, (parseFloat(updatedCari.alacak) || 0) - genelToplam);
          } else if (isSatis) {
            updatedCari.borc = Math.max(0, (parseFloat(updatedCari.borc) || 0) - genelToplam);
          } else {
            updatedCari.alacak = Math.max(0, (parseFloat(updatedCari.alacak) || 0) - genelToplam);
          }
          await writeData(`Cariler/${oldFatura.cariId}`, updatedCari);
        }
      } catch (e) {
        console.error(`[transactionService] Cari ${oldFatura.cariId} bakiye geri alınamadı:`, e);
      }
    }

    // 4. Delete StokHareketler
    const shRaw = (await readData('StokHareketler')) || {};
    const shList = toKeyList(shRaw);
    for (const h of shList) {
      const match =
        (h.faturaId !== undefined && (h.faturaId === faturaId || String(h.faturaId) === String(faturaId))) ||
        (faturaNo && h.evrakNo && String(h.evrakNo) === faturaNo);
      if (match) {
        const k = h.firebaseKey || h.id;
        if (k) { try { await deleteData(`StokHareketler/${k}`); } catch {} }
      }
    }

    // 5. Delete CariHareketler (including KPL- closing movement)
    const chRaw = (await readData('CariHareketler')) || {};
    const chList = toKeyList(chRaw);
    for (const h of chList) {
      const hEvrak = String(h.evrakNo || h.EvrakNo || '');
      const match =
        (h.faturaId !== undefined && (h.faturaId === faturaId || String(h.faturaId) === String(faturaId))) ||
        (faturaNo && (hEvrak === faturaNo || hEvrak === `KPL-${faturaNo}`));
      if (match) {
        const k = h.firebaseKey || h.id;
        if (k) { try { await deleteData(`CariHareketler/${k}`); } catch {} }
      }
    }

    // 6. Delete Kasa / Banka movements if closed
    const khRaw = (await readData('KasaHareketler')) || {};
    const khList = toKeyList(khRaw);
    for (const kh of khList) {
      const khAcik = kh.aciklama || '';
      const khEvrak = kh.evrakNo || '';
      if (faturaNo && (khEvrak === faturaNo || khAcik.includes(faturaNo))) {
        try {
          const kasaId = kh.kasaId || kh.hesapId;
          if (kasaId) {
            const kRef = await readData(`Bankalar/${kasaId}`);
            if (kRef) {
              const giren = kh.tur === 'Giriş' ? (kh.tutar || 0) : (kh.giren || 0);
              const cikan = kh.tur === 'Çıkış' ? (kh.tutar || 0) : (kh.cikan || 0);
              const yeniBakiye = (kRef.bakiye || 0) - giren + cikan;
              await writeData(`Bankalar/${kasaId}`, { ...kRef, kartTuru: 'Kasa', bakiye: yeniBakiye });
            }
          }
          const k = kh.firebaseKey || kh.id;
          if (k) await deleteData(`KasaHareketler/${k}`);
        } catch {}
      }
    }

    const bhRaw = (await readData('BankaHareketler')) || {};
    const bhList = toKeyList(bhRaw);
    for (const bh of bhList) {
      const bhAcik = bh.aciklama || '';
      const bhEvrak = bh.evrakNo || '';
      if (faturaNo && (bhEvrak === faturaNo || bhAcik.includes(faturaNo))) {
        try {
          const bankaId = bh.bankaId || bh.hesapId;
          if (bankaId) {
            const bRef = await readData(`Bankalar/${bankaId}`);
            if (bRef) {
              const giren = bh.giren || bh.borc || 0;
              const cikan = bh.cikan || bh.alacak || 0;
              const yeniBakiye = (bRef.bakiye || 0) - giren + cikan;
              await writeData(`Bankalar/${bankaId}`, { ...bRef, bakiye: yeniBakiye });
            }
          }
          const k = bh.firebaseKey || bh.id;
          if (k) await deleteData(`BankaHareketler/${k}`);
        } catch {}
      }
    }

    // 7. Delete FaturaDetaylar & mark Fatura isDeleted
    try { await deleteData(`FaturaDetaylar/${faturaId}`); } catch {}
    try {
      await writeData(`Faturalar/${faturaId}`, { ...oldFatura, isDeleted: true });
    } catch {}

    return true;
  } catch (err) {
    console.error('[transactionService] deleteFaturaCascade hatası:', err);
    return false;
  }
};

export const deleteFinancialTransaction = async (cariHareket: any): Promise<boolean> => {
  if (!cariHareket) return false;

  // Check if this cariHareket is linked to an invoice (Fatura)
  const rawFaturaId = cariHareket.faturaId || cariHareket.FaturaId;
  const isInvoice = (cariHareket.islemTuru && cariHareket.islemTuru.includes('Fatura')) ||
                    (rawFaturaId && Number(rawFaturaId) > 0) ||
                    (cariHareket.evrakNo && (String(cariHareket.evrakNo).startsWith('FAT') || String(cariHareket.evrakNo).startsWith('KPL-')));

  if (isInvoice) {
    const fId = rawFaturaId || cariHareket.evrakNo;
    const okCascade = await deleteFaturaCascade(fId);
    if (okCascade) return true;
  }

  const refId = cariHareket.refId;
  const evrakNo = cariHareket.evrakNo;
  const cariHareketId = cariHareket.id;
  if (!refId && !evrakNo && !cariHareketId) return false;

  const baseRefId = refId ? (refId.endsWith('-SUP') ? refId.substring(0, refId.length - 4) : refId) : '';

  try {
    const toKeyList = (raw: any) => {
      if (!raw) return [];
      if (Array.isArray(raw)) {
        return raw.map((item, idx) => item ? ({ ...item, firebaseKey: String(item?.id ?? idx) }) : null).filter(Boolean);
      }
      return Object.keys(raw).map((k) => ({ ...raw[k], firebaseKey: k }));
    };

    // Cari harekete bağlı kayıtları topla
    const cariH = (await readData('CariHareketler')) || {};
    const cariHList = toKeyList(cariH);
    const kasaH = (await readData('KasaHareketler')) || {};
    const kasaHList = toKeyList(kasaH);
    const bankaH = (await readData('BankaHareketler')) || {};
    const bankaHList = toKeyList(bankaH);
    const kk = (await readData('KrediKartlari')) || {};
    const kkList = toKeyList(kk);
    const eft = (await readData('EftIslemleri')) || {};
    const eftList = toKeyList(eft);

    const matchHareket = (h: any) => {
      if (refId && (h.refId === baseRefId || h.refId === refId + '-SUP' || h.refId === refId)) return true;
      if (evrakNo && h.evrakNo && String(h.evrakNo) === String(evrakNo)) return true;
      if (cariHareketId && (h.id === cariHareketId || (h.firebaseKey && String(h.firebaseKey) === String(cariHareketId)))) return true;
      return false;
    };

    const supReads: string[] = [];
    const farkCariler: any[] = [];
    let revertFailed = false;

    for (const h of cariHList.filter(matchHareket)) {
      const k = h.firebaseKey || h.id;
      if (k && supReads.indexOf(String(k)) === -1) supReads.push(String(k));
      farkCariler.push({ cariId: h.cariId, borc: h.borc || 0, alacak: h.alacak || 0 });
    }

    if (cariHareket.firebaseKey && supReads.indexOf(String(cariHareket.firebaseKey)) === -1) {
      supReads.push(String(cariHareket.firebaseKey));
    }
    if (cariHareket.id && supReads.indexOf(String(cariHareket.id)) === -1) {
      supReads.push(String(cariHareket.id));
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
    for (const h of kasaHList.filter(matchHareket)) {
      if (h.kasaId !== undefined && h.kasaId !== null) {
        const kRef = await readData(`Bankalar/${h.kasaId}`);
        if (kRef) {
          const okRevert = await writeData(`Bankalar/${h.kasaId}`, { ...kRef, bakiye: (kRef.bakiye || 0) - ((h.giren || 0) - (h.cikan || 0)) });
          if (!okRevert) revertFailed = true;
        }
      }
      const kKey = h.firebaseKey || h.id;
      if (kKey) { try { await deleteData(`KasaHareketler/${kKey}`); } catch {} }
    }

    for (const h of bankaHList.filter(matchHareket)) {
      if (h.bankaId !== undefined && h.bankaId !== null) {
        const bRef = await readData(`Bankalar/${h.bankaId}`);
        if (bRef) {
          const okRevert = await writeData(`Bankalar/${h.bankaId}`, { ...bRef, bakiye: (bRef.bakiye || 0) - ((h.giren || 0) - (h.cikan || 0)) });
          if (!okRevert) revertFailed = true;
        }
      }
      const bKey = h.firebaseKey || h.id;
      if (bKey) { try { await deleteData(`BankaHareketler/${bKey}`); } catch {} }
    }

    // KK / EFT kayıtlarını sil
    for (const k of kkList.filter((x: any) => (baseRefId && x.onayKodu === baseRefId) || (evrakNo && x.evrakNo === evrakNo))) {
      const kkKey = k.firebaseKey || k.id;
      if (kkKey) { try { await deleteData(`KrediKartlari/${kkKey}`); } catch {} }
    }
    for (const e of eftList.filter((x: any) => (baseRefId && x.dekontNo === baseRefId) || (evrakNo && x.evrakNo === evrakNo))) {
      const eftKey = e.firebaseKey || e.id;
      if (eftKey) { try { await deleteData(`EftIslemleri/${eftKey}`); } catch {} }
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
