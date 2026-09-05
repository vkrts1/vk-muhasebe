const fs = require('fs');
const path = require('path');

const filePath = 'e:/avalonia yedek/ermaymuhasebe/ermaymuhasebe-mobil/src/screens/RaporlarScreen.tsx';
let content = fs.readFileSync(filePath, 'utf8');

// 1. Replace the reports array
const newArray = `  const reports: ReportItem[] = [
    { title: 'Genel Özet', category: 'Genel Durum', description: 'İşletmenin genel mali özetini gösterir.' },
    { title: 'Aylık Tahsilat ve Ödeme Analizi', category: 'Finans', description: 'Ay bazlı ödeme türlerine göre tahsilat ve yönlendirme özeti.' },
    { title: 'Kredi Kartı Detay Raporu', category: 'Finans', description: 'Kaydedilen ve yönlendirilen kredi kartı işlem detayları.' },
    { title: 'Nakit İşlem Detay Raporu', category: 'Finans', description: 'Tüm nakit kasa hareketlerinin detaylı dökümü.' },
    { title: 'Çek Detay Raporu', category: 'Finans', description: 'Portföydeki ve işlem görmüş çeklerin detayları.' },
    { title: 'Havale / EFT Detay Raporu', category: 'Finans', description: 'Banka havale ve EFT işlemlerinin detaylı listesi.' },
    { title: 'Cari Bakiye Raporu', category: 'Cari Hesap', description: 'Müşteri ve tedarikçi bakiyeleri.' },
    { title: 'Cari Hareket Dökümü', category: 'Cari Hesap', description: 'Cari hesapların işlem detayları.' },
    { title: 'Yaşlandırma Raporu', category: 'Cari Hesap', description: 'Borç/alacak yaşlandırma analizi.' },
    { title: 'Hareketsiz Cariler', category: 'Cari Hesap', description: 'İşlem görmeyen cari hesaplar.' },
    { title: 'Stok Mevcudu', category: 'Stok', description: 'Güncel stok miktarları ve değerleri.' },
    { title: 'Stok Hareketleri', category: 'Stok', description: 'Giriş-çıkış stok hareket dökümü.' },
    { title: 'Kritik Stok Seviyesi', category: 'Stok', description: 'Minimum seviyenin altındaki ürünler.' },
    { title: 'Ölü Stok Raporu', category: 'Stok', description: 'Çakışan stok kartlarının listesi (Birleştirilmesi Gerekenler).' },
    { title: 'Stok Devir Hızı', category: 'Stok', description: 'Stokların dönüşüm hızı analizi.' },
    { title: 'Satış Faturası Dökümü', category: 'Satış', description: 'Kesilen satış faturalarının listesi.' },
    { title: 'Ürün Karlılık Raporu', category: 'Karlılık', description: 'Ürün bazlı kar/zarar analizi.' },
    { title: 'Müşteri Karlılık Analizi', category: 'Karlılık', description: 'Müşteri bazlı kar/zarar analizi.' },
    { title: 'En Çok Satan Ürünler', category: 'Satış', description: 'Satış adedine göre top listeler.' },
    { title: 'Gelir Tablosu', category: 'Finans', description: 'Dönemsel gelir ve gider özeti.' },
    { title: 'Detaylı Gelir ve Maliyet Analizi', category: 'Karlılık', description: 'Tarih bazlı ürün satışlarının kime yapıldığı, en son kimden ne kadara alındığı ve satır bazlı kâr analizi.' },
    { title: 'Nakit Akış Tablosu', category: 'Finans', description: 'Nakit giriş ve çıkışlarının takibi.' },
    { title: 'Müşteri ABC Analizi', category: 'Stratejik', description: 'Ciroya göre müşteri sınıflandırması. A Sınıfı: En yüksek %80, B Sınıfı: Sonraki %15, C Sınıfı: Kalan %5' },
    { title: 'Kar-Zarar Mukayesesi', category: 'Stratejik', description: 'Yıllık, aylık ve haftalık bazda performans analizi.' },
    { title: 'Vadesi Geçmiş Alacaklar', category: 'Cari Hesap', description: 'Ödeme süresi geçmiş tahsilatlar.' },
    { title: 'Müşteri Kayıp (Churn)', category: 'Stratejik', description: 'Kayıp riski olan pasif müşteriler.' },
    { title: 'Fiyat Dalgalanma Raporu', category: 'Stratejik', description: 'Ürün fiyat değişim trendleri.' },
    { title: 'Müşteri Sadakat (LTV)', category: 'Stratejik', description: 'Müşteri yaşam boyu değer analizi.' },
    { title: 'Finansal Isı Haritası', category: 'Stratejik', description: 'Günlük finansal aktivite yoğunluğu.' },
    { title: 'Tahsilat Süresi (DSO)', category: 'Stratejik', description: 'Ortalama tahsilat hızı (gün).' },
    { title: 'Bütçe / Hedef Takibi', category: 'Stratejik', description: 'Aylık hedeflere ulaşma durumu.' }
  ];`;

content = content.replace(/const reports: ReportItem\[\] = \[\s+([^\]]+)\s+\];/g, newArray);

// 2. Rename the specific titles in calculateReport
content = content.replace(/else if \(reportTitle === 'Müşteri Kayıp Analizi \(Churn\)'\)/g, "else if (reportTitle === 'Müşteri Kayıp (Churn)')");
content = content.replace(/else if \(reportTitle === 'Müşteri LTV \(Yaşam Boyu Değer\)'\)/g, "else if (reportTitle === 'Müşteri Sadakat (LTV)')");
content = content.replace(/else if \(reportTitle === 'Bütçe\/Hedef Takibi'\)/g, "else if (reportTitle === 'Bütçe / Hedef Takibi')");

// 3. Remove the blocks

const fifoBlock = `    else if (reportTitle === 'FIFO Maliyet Analizi') {
      headers = ['Ürün', 'Satılan Adet', 'FIFO Maliyet', 'Satış Geliri', 'FIFO Kâr'];
      rows = activeStoklar.map(s => {
        const moves = stokHareketler.filter(h => String(h.stokId) === String(s.id));
        const r = hesapFifo(moves, s);
        if (r.satilanAdet === 0) return null;
        return [s.stokAdi || 'Ürün', r.satilanAdet.toFixed(0), formatMoney(r.fifoMaliyet), formatMoney(r.satis), formatMoney(r.kar)];
      }).filter(Boolean) as string[][];
    }
`;
content = content.replace(fifoBlock, "");

const kasaBlock = `    else if (reportTitle === 'Kasa Hareketleri') {
      headers = ['Tarih', 'İşlem', 'Giren', 'Çıkan'];
      rows = kasaHareketler.sort((a,b) => new Date(b.tarih).getTime() - new Date(a.tarih).getTime()).map(h => [
        h.tarih,
        h.islemTuru,
        formatMoney(h.giren || 0),
        formatMoney(h.cikan || 0)
      ]);
    }
`;
content = content.replace(kasaBlock, "");

const cekRiskBlock = `    else if (reportTitle === 'Çek Risk Analizi') {
      headers = ['Portföy No', 'Cari', 'Vade', 'Tutar', 'Risk Puanı', 'Risk'];
      rows = cekler.filter(x => !x.isDeleted).map(x => {
        const risk = cekRiskPuani(x);
        return [x.portfoyNo || x.seriNo || x.id?.toString() || '-', x.cariUnvan || x.borclu || '-', x.vadeTarihi || '-', formatMoney(x.tutar || 0), \`\${risk.puan}\`, risk.label];
      }).sort((a, b) => parseFloat(b[4]) - parseFloat(a[4]));
    }
`;
content = content.replace(cekRiskBlock, "");

const senetBlock = `    else if (reportTitle === 'Senet Detay') {
      headers = ['Portföy No', 'Cari', 'Vade', 'Tutar', 'Durum'];
      rows = senetler.filter(x => !x.isDeleted).sort((a, b) => new Date(b.vadeTarihi).getTime() - new Date(a.vadeTarihi).getTime()).map(x => [
        x.portfoyNo || x.seriNo || x.id?.toString() || '-',
        x.cariUnvan || x.borclu || '-',
        x.vadeTarihi || '-',
        formatMoney(x.tutar || 0),
        x.durum || '-'
      ]);
    }
`;
content = content.replace(senetBlock, "");

const fiyatListesiBlock = `    else if (reportTitle === 'Fiyat Listesi (Katalog)') {
      headers = ['Ürün', 'Barkod', 'Birim', 'Satış Fiyatı', 'İndirimli?'];
      rows = activeStoklar.map(s => [
        s.stokAdi || '-',
        s.barkod || '-',
        s.birim || 'Adet',
        formatMoney(s.satisFiyati || 0),
        (s.indirimliFiyat && s.indirimliFiyat < (s.satisFiyati || 0)) ? \`Indirim: \${formatMoney(s.indirimliFiyat)}\` : 'Hayır'
      ]).sort((a, b) => parseFloat(b[3].replace(/[^\\d.-]/g, '')) - parseFloat(a[3].replace(/[^\\d.-]/g, '')));
    }
`;
content = content.replace(fiyatListesiBlock, "");

fs.writeFileSync(filePath, content);
console.log("File updated successfully.");
