/**
 * Native iOS / Mobile PDF HTML Templates
 * QuestPDF masaüstü tasarımı ile %100 piksel ve font uyumlu HTML şablonları.
 * Apple WebKit Native PDF motoru (expo-print) ile doğrudan cihazda yüksek çözünürlüklü
 * vektörel PDF üretilmesini sağlar. Logo her zaman doğrudan data URI olarak gömülür.
 */

export const formatCurrency = (val: number | string | undefined | null): string => {
  const num = typeof val === 'number' ? val : parseFloat(String(val || '0')) || 0;
  return `₺${num.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
};

export const formatDate = (dateStr: string | undefined | null): string => {
  if (!dateStr) return '-';
  try {
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return String(dateStr).substring(0, 10);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}.${month}.${year}`;
  } catch {
    return String(dateStr).substring(0, 10);
  }
};

export const getPaymentMethodLabel = (aciklama: string | undefined | null): string => {
  if (!aciklama) return '-';
  const upper = aciklama.toLocaleUpperCase('tr-TR');
  if (upper.includes('NAKİT') || upper.includes('NAKIT')) return 'Nakit';
  if (upper.includes('KREDİ') || upper.includes('KREDI')) return 'Kredi Kartı';
  if (upper.includes('HAVALE') || upper.includes('EFT')) return 'Havale';
  if (upper.includes('ÇEK') || upper.includes('CEK')) return 'Çek';
  if (upper.includes('BANKA')) return 'Banka Havalesi';
  return '-';
};

const getBaseStyles = () => `
  @page {
    size: A4;
    margin: 10mm 12mm;
  }
  * {
    box-sizing: border-box;
    -webkit-print-color-adjust: exact !important;
    print-color-adjust: exact !important;
  }
  body {
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    color: #222222;
    margin: 0;
    padding: 0;
    background: #ffffff;
    font-size: 9pt;
    line-height: 1.35;
  }
  .logo-wrapper {
    margin-bottom: 12px;
  }
  .company-logo {
    max-height: 50px;
    max-width: 140px;
    object-fit: contain;
    display: block;
  }
  .card {
    border: 0.5pt solid #dddddd;
    margin-bottom: 12px;
    background: #ffffff;
  }
  .card-title {
    font-size: 11pt;
    font-weight: 600;
    padding: 8px 10px;
    color: #111111;
  }
  .card-body {
    padding: 6px 10px 10px 10px;
  }
  .info-table {
    width: 100%;
    border-collapse: collapse;
  }
  .info-table td {
    padding: 2.5pt 0;
    font-size: 9pt;
    vertical-align: top;
  }
  .info-label {
    font-weight: bold;
    width: 130px;
    color: #222222;
  }
  .info-value {
    color: #222222;
  }
  .color-red {
    color: #cc0000;
    font-weight: bold;
  }
  .color-green {
    color: #008000;
    font-weight: bold;
  }
  .table {
    width: 100%;
    border-collapse: collapse;
  }
  .table th {
    background-color: #f9f9f9;
    color: #222222;
    font-weight: bold;
    font-size: 8pt;
    padding: 4.5pt;
    border: 0.5pt solid #eeeeee;
    text-align: left;
  }
  .table td {
    padding: 4pt;
    font-size: 8pt;
    border: 0.5pt solid #eeeeee;
    color: #222222;
  }
  .table tr:nth-child(even) td {
    background-color: #fcfcfc;
  }
  .text-right {
    text-align: right;
  }
  .text-center {
    text-align: center;
  }
  .badge {
    display: inline-block;
    padding: 2px 6px;
    border-radius: 3px;
    font-size: 7.5pt;
    font-weight: 600;
  }
  .badge-sale {
    background: #eef7ff;
    color: #0066cc;
  }
  .badge-payment {
    background: #eefbf1;
    color: #008000;
  }
  .badge-collection {
    background: #fdf5e6;
    color: #cc7700;
  }
  .badge-other {
    background: #f5f5f5;
    color: #555555;
  }
  .nested-table {
    width: 100%;
    border-collapse: collapse;
    margin: 4pt 0;
    background: #fafafa;
    border: 0.5pt dashed #cccccc;
  }
  .nested-table th {
    background: #f0f0f0;
    font-size: 7.5pt;
    padding: 3pt;
    border: 0.5pt solid #e0e0e0;
  }
  .nested-table td {
    font-size: 7.5pt;
    padding: 2.5pt 4pt;
    border: 0.5pt solid #e8e8e8;
  }
`;

/**
 * Cari Ekstre & Detaylı Cari Ekstre HTML Şablonu
 * Masaüstü QuestPDF çıktısıyla birebir uyumludur.
 */
export const generateCariEkstreHtml = (
  payload: any,
  logoBase64: string | null,
  isDetayli: boolean = false
): string => {
  const cari = payload.Cari || payload.cari || {};
  const cariUnvan = cari.unvan || cari.Unvan || '-';

  const hareketler: any[] = [...(payload.Hareketler || payload.hareketler || [])].sort((a, b) => {
    const tA = new Date(a.tarih || a.Tarih || 0).getTime();
    const tB = new Date(b.tarih || b.Tarih || 0).getTime();
    if (tA !== tB) return tA - tB;
    return (a.id || a.Id || 0) - (b.id || b.Id || 0);
  });

  let runningBalance = 0;
  let totalBorc = 0;
  let totalAlacak = 0;

  const rows = hareketler.map((h: any) => {
    const borc = typeof h.borc === 'number' ? h.borc : (typeof h.Borc === 'number' ? h.Borc : parseFloat(h.borc || h.Borc || 0) || 0);
    const alacak = typeof h.alacak === 'number' ? h.alacak : (typeof h.Alacak === 'number' ? h.Alacak : parseFloat(h.alacak || h.Alacak || 0) || 0);
    totalBorc += borc;
    totalAlacak += alacak;
    runningBalance += (borc - alacak);
    return {
      h,
      borc,
      alacak,
      bakiye: runningBalance
    };
  });

  const netBakiye = totalBorc - totalAlacak;
  const balanceClass = netBakiye > 0 ? 'color-red' : 'color-green';
  const balanceLabel = netBakiye > 0 ? ' (Borçlu)' : ' (Alacaklı)';

  const detayDict = payload.DetayDictionary || payload.detayDictionary || {};

  const logoHtml = logoBase64 ? `
    <div class="logo-wrapper">
      <img class="company-logo" src="data:image/png;base64,${logoBase64}" alt="Şirket Logosu" />
    </div>
  ` : '';

  let tableRowsHtml = '';
  if (rows.length === 0) {
    tableRowsHtml = `
      <tr>
        <td colspan="6" class="text-center" style="padding: 18pt; color: #555555; font-size: 8pt;">
          Bu müşteri için herhangi bir işlem bulunamadı.
        </td>
      </tr>
    `;
  } else {
    tableRowsHtml = rows.map(({ h, borc, alacak, bakiye }) => {
      const isSale = borc > 0;
      const rawTuru = String(h.islemTuru || h.IslemTuru || '').toLocaleUpperCase('tr-TR');
      let badgeText = h.islemTuru || h.IslemTuru || (isSale ? 'Satış' : 'Tahsilat');
      let badgeClass = 'badge-other';

      if (rawTuru.includes('FATURA') || rawTuru.includes('SATIŞ') || rawTuru.includes('SATIS')) {
        badgeText = 'Satış';
        badgeClass = 'badge-sale';
      } else if (rawTuru.includes('ÖDEME') || rawTuru.includes('ODEME')) {
        badgeText = 'Ödeme';
        badgeClass = 'badge-payment';
      } else if (rawTuru.includes('TAHSİLAT') || rawTuru.includes('TAHSILAT')) {
        badgeText = 'Tahsilat';
        badgeClass = 'badge-collection';
      } else if (rawTuru.includes('ALIŞ') || rawTuru.includes('ALIS')) {
        badgeText = 'Alış';
        badgeClass = 'badge-sale';
      }

      const method = !isSale ? getPaymentMethodLabel(h.aciklama || h.Aciklama) : '-';
      let desc = h.aciklama || h.Aciklama || '-';
      if (method !== '-') {
        desc = `${desc} (${method})`;
      }

      const fid = h.faturaId || h.FaturaId;
      let nestedItemsHtml = '';

      if (isDetayli && fid && detayDict[fid] && Array.isArray(detayDict[fid]) && detayDict[fid].length > 0) {
        const items = detayDict[fid];
        const itemRows = items.map((item: any) => {
          const itemAd = item.stokAdi || item.StokAdi || item.aciklama || item.Aciklama || '-';
          const miktar = item.miktar || item.Miktar || 1;
          const birim = item.birim || item.Birim || 'Adet';
          const birimFiyat = item.birimFiyat || item.BirimFiyat || 0;
          const kdv = item.kdvOrani || item.KdvOrani || item.kdv || 0;
          const tutar = item.toplamTutar || item.ToplamTutar || (miktar * birimFiyat);

          return `
            <tr>
              <td>${itemAd}</td>
              <td class="text-right">${miktar} ${birim}</td>
              <td class="text-right">${formatCurrency(birimFiyat)}</td>
              <td class="text-center">%${kdv}</td>
              <td class="text-right" style="font-weight: 600;">${formatCurrency(tutar)}</td>
            </tr>
          `;
        }).join('');

        nestedItemsHtml = `
          <tr>
            <td colspan="6" style="padding: 4pt 8pt; background-color: #f7f9fa;">
              <div style="font-size: 7.5pt; font-weight: bold; margin-bottom: 2pt; color: #444;">Fatura Kalemleri (Evrak Ref: ${h.evrakNo || h.EvrakNo || fid}):</div>
              <table class="nested-table">
                <thead>
                  <tr>
                    <th>Ürün / Hizmet</th>
                    <th class="text-right" style="width: 70px;">Miktar</th>
                    <th class="text-right" style="width: 75px;">Birim Fiyat</th>
                    <th class="text-center" style="width: 45px;">KDV</th>
                    <th class="text-right" style="width: 80px;">Tutar</th>
                  </tr>
                </thead>
                <tbody>
                  ${itemRows}
                </tbody>
              </table>
            </td>
          </tr>
        `;
      }

      return `
        <tr>
          <td style="width: 12%;">${formatDate(h.tarih || h.Tarih)}</td>
          <td style="width: 11%;"><span class="badge ${badgeClass}">${badgeText}</span></td>
          <td style="width: 37%;">${desc}</td>
          <td class="text-right" style="width: 13%;">${isSale ? `<span class="color-red">${formatCurrency(borc)}</span>` : '-'}</td>
          <td class="text-right" style="width: 13%;">${!isSale ? `<span class="color-green">${formatCurrency(alacak)}</span>` : '-'}</td>
          <td class="text-right" style="width: 14%; font-weight: 600;">${formatCurrency(bakiye)}</td>
        </tr>
        ${nestedItemsHtml}
      `;
    }).join('');
  }

  return `<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="utf-8">
  <title>Cari Ekstre - ${cariUnvan}</title>
  <style>
    ${getBaseStyles()}
  </style>
</head>
<body>
  ${logoHtml}

  <!-- GENEL BİLGİLER CARD -->
  <div class="card">
    <div class="card-title">Genel Bilgiler</div>
    <div class="card-body">
      <table class="info-table">
        <tr>
          <td class="info-label">Müşteri Adı:</td>
          <td class="info-value"><strong>${cariUnvan}</strong></td>
        </tr>
        <tr>
          <td class="info-label">Toplam Satış:</td>
          <td class="info-value">${formatCurrency(totalBorc)}</td>
        </tr>
        <tr>
          <td class="info-label">Toplam Ödeme:</td>
          <td class="info-value">${formatCurrency(totalAlacak)}</td>
        </tr>
        <tr>
          <td class="info-label">Bakiye:</td>
          <td class="info-value"><span class="${balanceClass}">${formatCurrency(netBakiye)}${balanceLabel}</span></td>
        </tr>
      </table>
    </div>
  </div>

  <!-- İŞLEM DETAYLARI CARD -->
  <div class="card">
    <div class="card-title">İşlem Detayları</div>
    <div style="padding: 0;">
      <table class="table">
        <thead>
          <tr>
            <th style="width: 12%;">Tarih</th>
            <th style="width: 11%;">İşlem Tipi</th>
            <th style="width: 37%;">Açıklama</th>
            <th class="text-right" style="width: 13%;">Borç</th>
            <th class="text-right" style="width: 13%;">Alacak</th>
            <th class="text-right" style="width: 14%;">Bakiye</th>
          </tr>
        </thead>
        <tbody>
          ${tableRowsHtml}
        </tbody>
      </table>
    </div>
  </div>
</body>
</html>`;
};

/**
 * Generic Table (Cari Bakiye Listesi, Raporlar vb.) HTML Şablonu
 */
export const generateGenericTableHtml = (payload: any, logoBase64: string | null): string => {
  const title = payload.Title || payload.title || 'Rapor';
  const subtitle = payload.Subtitle || payload.subtitle || '';
  const headers: string[] = payload.Headers || payload.headers || [];
  const rows: any[][] = payload.Rows || payload.rows || [];

  const logoHtml = logoBase64 ? `
    <div class="logo-wrapper">
      <img class="company-logo" src="data:image/png;base64,${logoBase64}" alt="Logo" />
    </div>
  ` : '';

  const headerHtml = headers.map(h => `<th>${h}</th>`).join('');
  const rowsHtml = rows.map(row => {
    const cells = (row || []).map((c: any, i: number) => {
      const isNum = typeof c === 'number' || (typeof c === 'string' && /^[₺$€]?\s*[-+]?[0-9.,]+(\s*₺)?$/.test(c.trim()));
      const alignClass = isNum && i > 1 ? 'text-right' : '';
      return `<td class="${alignClass}">${c !== null && c !== undefined ? c : '-'}</td>`;
    }).join('');
    return `<tr>${cells}</tr>`;
  }).join('');

  return `<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="utf-8">
  <title>${title}</title>
  <style>
    ${getBaseStyles()}
  </style>
</head>
<body>
  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px;">
    ${logoHtml}
    <div style="text-align: right; flex: 1;">
      <h2 style="margin: 0; font-size: 14pt; color: #111;">${title}</h2>
      ${subtitle ? `<p style="margin: 3px 0 0 0; font-size: 8pt; color: #666;">${subtitle}</p>` : ''}
    </div>
  </div>

  <table class="table">
    <thead>
      <tr>${headerHtml}</tr>
    </thead>
    <tbody>
      ${rowsHtml || '<tr><td colspan="10" class="text-center" style="padding: 15px;">Kayıt bulunamadı.</td></tr>'}
    </tbody>
  </table>
</body>
</html>`;
};

/**
 * Tahsilat / Ödeme Makbuzu HTML Şablonu
 */
export const generateMakbuzHtml = (payload: any, logoBase64: string | null): string => {
  const makbuzTipi = payload.MakbuzTipi || payload.makbuzTipi || 'Tahsilat';
  const cariUnvan = payload.CariUnvan || payload.cariUnvan || '-';
  const tarih = formatDate(payload.Tarih || payload.tarih);
  const tutar = payload.Tutar || payload.tutar || 0;
  const aciklama = payload.Aciklama || payload.aciklama || '-';

  const logoHtml = logoBase64 ? `
    <div class="logo-wrapper">
      <img class="company-logo" src="data:image/png;base64,${logoBase64}" alt="Logo" />
    </div>
  ` : '';

  return `<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="utf-8">
  <title>${makbuzTipi} Makbuzu</title>
  <style>
    ${getBaseStyles()}
  </style>
</head>
<body>
  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
    ${logoHtml}
    <div style="text-align: right;">
      <h1 style="margin: 0; font-size: 15pt; color: #111;">${makbuzTipi.toLocaleUpperCase('tr-TR')} MAKBUZU</h1>
      <p style="margin: 3px 0 0 0; font-size: 9pt; color: #555;">Tarih: ${tarih}</p>
    </div>
  </div>

  <div class="card" style="margin-top: 20px;">
    <div class="card-body" style="padding: 15px;">
      <table class="info-table" style="font-size: 10pt;">
        <tr>
          <td class="info-label" style="width: 140px; padding: 6px 0;">Sayın / Müşteri:</td>
          <td class="info-value" style="padding: 6px 0;"><strong>${cariUnvan}</strong></td>
        </tr>
        <tr>
          <td class="info-label" style="padding: 6px 0;">Ödeme / İşlem Türü:</td>
          <td class="info-value" style="padding: 6px 0;">${makbuzTipi}</td>
        </tr>
        <tr>
          <td class="info-label" style="padding: 6px 0;">Açıklama:</td>
          <td class="info-value" style="padding: 6px 0;">${aciklama}</td>
        </tr>
        <tr>
          <td class="info-label" style="padding: 10px 0; font-size: 12pt;">Tutar:</td>
          <td class="info-value" style="padding: 10px 0; font-size: 14pt; font-weight: bold; color: #008000;">
            ${formatCurrency(tutar)}
          </td>
        </tr>
      </table>
    </div>
  </div>

  <div style="display: flex; justify-content: space-between; margin-top: 60px; padding: 0 40px;">
    <div style="text-align: center; width: 180px;">
      <p style="font-size: 9pt; font-weight: bold; margin-bottom: 40px;">Teslim Eden</p>
      <div style="border-top: 1px dashed #999; padding-top: 5px; font-size: 8pt; color: #666;">İmza / Kaşe</div>
    </div>
    <div style="text-align: center; width: 180px;">
      <p style="font-size: 9pt; font-weight: bold; margin-bottom: 40px;">Teslim Alan</p>
      <div style="border-top: 1px dashed #999; padding-top: 5px; font-size: 8pt; color: #666;">İmza / Kaşe</div>
    </div>
  </div>
</body>
</html>`;
};

/**
 * Kasa Ekstresi HTML Şablonu
 */
export const generateKasaEkstreHtml = (payload: any, logoBase64: string | null): string => {
  const kasa = payload.Kasa || payload.kasa || {};
  const kasaAdi = kasa.bankaAdi || kasa.BankaAdi || kasa.kasaAdi || 'Kasa';
  const hareketler = payload.Hareketler || payload.hareketler || [];

  const logoHtml = logoBase64 ? `
    <div class="logo-wrapper">
      <img class="company-logo" src="data:image/png;base64,${logoBase64}" alt="Logo" />
    </div>
  ` : '';

  let bakiye = 0;
  const rowsHtml = hareketler.map((h: any) => {
    const giris = h.giris || h.Giris || (h.hareketTuru === 'Giris' ? h.tutar : 0) || 0;
    const cikis = h.cikis || h.Cikis || (h.hareketTuru === 'Cikis' ? h.tutar : 0) || 0;
    bakiye += (giris - cikis);

    return `
      <tr>
        <td>${formatDate(h.tarih || h.Tarih)}</td>
        <td>${h.islemTuru || h.IslemTuru || '-'}</td>
        <td>${h.aciklama || h.Aciklama || '-'}</td>
        <td class="text-right">${giris > 0 ? formatCurrency(giris) : '-'}</td>
        <td class="text-right">${cikis > 0 ? formatCurrency(cikis) : '-'}</td>
        <td class="text-right" style="font-weight: bold;">${formatCurrency(bakiye)}</td>
      </tr>
    `;
  }).join('');

  return `<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="utf-8">
  <title>Kasa Ekstresi - ${kasaAdi}</title>
  <style>
    ${getBaseStyles()}
  </style>
</head>
<body>
  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px;">
    ${logoHtml}
    <div style="text-align: right;">
      <h2 style="margin: 0; font-size: 14pt;">Kasa Ekstresi: ${kasaAdi}</h2>
      <p style="margin: 3px 0 0 0; font-size: 9pt; color: #666;">Bakiye: <strong>${formatCurrency(bakiye)}</strong></p>
    </div>
  </div>

  <table class="table">
    <thead>
      <tr>
        <th style="width: 12%;">Tarih</th>
        <th style="width: 15%;">İşlem Türü</th>
        <th style="width: 37%;">Açıklama</th>
        <th class="text-right" style="width: 12%;">Giriş</th>
        <th class="text-right" style="width: 12%;">Çıkış</th>
        <th class="text-right" style="width: 12%;">Bakiye</th>
      </tr>
    </thead>
    <tbody>
      ${rowsHtml || '<tr><td colspan="6" class="text-center" style="padding: 15px;">Hareket bulunamadı.</td></tr>'}
    </tbody>
  </table>
</body>
</html>`;
};

/**
 * Fatura / Teklif / Sipariş HTML Şablonu
 */
export const generateFaturaHtml = (
  payload: any,
  logoBase64: string | null,
  docType: 'fatura' | 'teklif' | 'siparis' = 'fatura'
): string => {
  const fatura = payload.Fatura || payload.fatura || payload.Teklif || payload.teklif || payload.Siparis || payload.siparis || {};
  const detaylar = payload.Detaylar || payload.detaylar || payload.items || [];
  const cari = payload.Cari || payload.cari || {};

  const titles: Record<string, string> = {
    fatura: 'SATIŞ FATURASI',
    teklif: 'FİYAT TEKLİFİ',
    siparis: 'SİPARİŞ FORMU'
  };
  const docTitle = titles[docType] || 'FATURA';
  const docNo = fatura.faturaNo || fatura.FaturaNo || fatura.teklifNo || fatura.TeklifNo || fatura.siparisNo || fatura.SiparisNo || '-';
  const cariUnvan = cari.unvan || cari.Unvan || fatura.cariUnvan || fatura.CariUnvan || '-';

  const logoHtml = logoBase64 ? `
    <div class="logo-wrapper">
      <img class="company-logo" src="data:image/png;base64,${logoBase64}" alt="Logo" />
    </div>
  ` : '';

  const itemRows = detaylar.map((d: any) => {
    const ad = d.stokAdi || d.StokAdi || d.aciklama || d.Aciklama || '-';
    const miktar = d.miktar || d.Miktar || 1;
    const birim = d.birim || d.Birim || 'Adet';
    const birimFiyat = d.birimFiyat || d.BirimFiyat || 0;
    const kdv = d.kdvOrani || d.KdvOrani || d.kdv || 0;
    const tutar = d.toplamTutar || d.ToplamTutar || (miktar * birimFiyat);

    return `
      <tr>
        <td>${ad}</td>
        <td class="text-right">${miktar} ${birim}</td>
        <td class="text-right">${formatCurrency(birimFiyat)}</td>
        <td class="text-center">%${kdv}</td>
        <td class="text-right" style="font-weight: 600;">${formatCurrency(tutar)}</td>
      </tr>
    `;
  }).join('');

  const araToplam = fatura.araToplam || fatura.AraToplam || 0;
  const kdvToplam = fatura.kdvToplam || fatura.KdvToplam || fatura.toplamKDV || 0;
  const genelToplam = fatura.genelToplam || fatura.GenelToplam || 0;

  return `<!DOCTYPE html>
<html lang="tr">
<head>
  <meta charset="utf-8">
  <title>${docTitle} - ${docNo}</title>
  <style>
    ${getBaseStyles()}
  </style>
</head>
<body>
  <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 15px;">
    ${logoHtml}
    <div style="text-align: right;">
      <h1 style="margin: 0; font-size: 15pt; color: #111;">${docTitle}</h1>
      <p style="margin: 3px 0 0 0; font-size: 9pt; color: #555;">Belge No: <strong>${docNo}</strong></p>
      <p style="margin: 2px 0 0 0; font-size: 8.5pt; color: #555;">Tarih: ${formatDate(fatura.tarih || fatura.Tarih)}</p>
    </div>
  </div>

  <div class="card">
    <div class="card-title">Müşteri / Cari Bilgileri</div>
    <div class="card-body">
      <table class="info-table">
        <tr>
          <td class="info-label">Sayın:</td>
          <td class="info-value"><strong>${cariUnvan}</strong></td>
        </tr>
        ${cari.vergiDairesi || cari.VergiDairesi ? `
        <tr>
          <td class="info-label">Vergi Dairesi / No:</td>
          <td class="info-value">${cari.vergiDairesi || cari.VergiDairesi} / ${cari.vergiNo || cari.VergiNo || '-'}</td>
        </tr>` : ''}
        ${cari.telefon || cari.Telefon ? `
        <tr>
          <td class="info-label">Telefon:</td>
          <td class="info-value">${cari.telefon || cari.Telefon}</td>
        </tr>` : ''}
      </table>
    </div>
  </div>

  <table class="table" style="margin-bottom: 15px;">
    <thead>
      <tr>
        <th>Ürün / Hizmet Açıklaması</th>
        <th class="text-right" style="width: 80px;">Miktar</th>
        <th class="text-right" style="width: 90px;">Birim Fiyat</th>
        <th class="text-center" style="width: 50px;">KDV</th>
        <th class="text-right" style="width: 100px;">Toplam Tutar</th>
      </tr>
    </thead>
    <tbody>
      ${itemRows || '<tr><td colspan="5" class="text-center" style="padding: 15px;">Kalem bulunamadı.</td></tr>'}
    </tbody>
  </table>

  <div style="display: flex; justify-content: flex-end; margin-top: 10px;">
    <div style="width: 250px;">
      <table class="info-table" style="border: 0.5pt solid #ddd; background: #fafafa; padding: 6px;">
        <tr>
          <td class="info-label" style="padding: 4px 8px;">Ara Toplam:</td>
          <td class="text-right" style="padding: 4px 8px;">${formatCurrency(araToplam)}</td>
        </tr>
        <tr>
          <td class="info-label" style="padding: 4px 8px;">KDV Toplam:</td>
          <td class="text-right" style="padding: 4px 8px;">${formatCurrency(kdvToplam)}</td>
        </tr>
        <tr style="border-top: 1pt solid #ccc;">
          <td class="info-label" style="padding: 6px 8px; font-size: 10pt;">Genel Toplam:</td>
          <td class="text-right" style="padding: 6px 8px; font-size: 11pt; font-weight: bold; color: #111;">
            ${formatCurrency(genelToplam)}
          </td>
        </tr>
      </table>
    </div>
  </div>
</body>
</html>`;
};
