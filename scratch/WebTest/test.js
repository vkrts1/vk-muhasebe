const puppeteer = require('puppeteer-core');
const fs = require('fs');
const path = require('path');

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function run() {
  console.log("Calisan Chrome tarayicisina baglaniliyor...");
  let browser;
  try {
    browser = await puppeteer.connect({
      browserURL: 'http://127.0.0.1:9222',
      defaultViewport: null // Orijinal pencere boyutunu koru
    });
    console.log("Tarayiciya basariyla baglandi!");
  } catch (err) {
    console.error("Tarayiciya baglanirken hata olustu (9222 portu acik mi?):", err);
    process.exit(1);
  }

  try {
    const pages = await browser.pages();
    // localhost:5005 sayfasını bul veya yeni sekme aç
    let page = pages.find(p => p.url().includes('localhost:5005'));
    
    if (!page) {
      console.log("Localhost:5005 sayfasi bulunamadi, yeni sayfa aciliyor...");
      page = await browser.newPage();
      await page.goto('http://localhost:5005', { waitUntil: 'load', timeout: 30000 });
    } else {
      console.log("Mevcut Localhost:5005 sekmesi bulundu, odaklaniliyor ve yenileniyor...");
      await page.bringToFront();
      await page.reload({ waitUntil: 'load', timeout: 30000 });
    }

    console.log("Blazor uygulamasinin yuklenmesi bekleniyor...");
    // 8 saniye bekleyip yüklenip yüklenmediğini kontrol edelim (Blazor WASM ilk yükleme süresi için)
    await delay(8000); 

    const artifactDir = "C:\\Users\\mazik\\.gemini\\antigravity-ide\\brain\\24a286b5-3c81-4e87-aa89-c1316d2980c8";
    
    // 1. Dashboard Ekran Görüntüsü
    await page.screenshot({ path: path.join(artifactDir, 'web_1_dashboard.png') });
    console.log("Dashboard ekran goruntusu alindi.");

    // Menüleri gezme simülasyonu
    const menuItems = [
      { name: 'Cari Hesaplar', url: '/cari-hesaplar' },
      { name: 'Stok Kartlari', url: '/stok-kartlari' },
      { name: 'Faturalar', url: '/faturalar' },
      { name: 'Finans', url: '/finans' },
      { name: 'Vade Takip', url: '/vade-takip' },
      { name: 'Raporlar', url: '/raporlar' },
      { name: 'Siparisler', url: '/siparisler' },
      { name: 'Teklifler', url: '/teklifler' },
      { name: 'Gorev Panosu', url: '/gorev-panosu' },
      { name: 'Hesap Makinesi', url: '/hesap-makinesi' },
      { name: 'Ayarlar', url: '/ayarlar' }
    ];

    for (let i = 0; i < menuItems.length; i++) {
      const item = menuItems[i];
      console.log(`Menuye gidiliyor: ${item.name} (${item.url})`);
      try {
        // Blazor sayfasında doğrudan URL'e gitmek en güvenli yöntemdir (tıklama hatasını önler)
        await page.goto(`http://localhost:5005${item.url}`, { waitUntil: 'load', timeout: 15000 });
        await delay(3000); // Sayfa renderı ve veri çekme işlemi için biraz bekleyelim
        
        const screenshotName = `web_${i + 2}_${item.name.toLowerCase().replace(/[^a-z0-9]/g, '_')}.png`;
        await page.screenshot({ path: path.join(artifactDir, screenshotName) });
        console.log(`- ${item.name} ekran goruntusu alindi: ${screenshotName}`);
      } catch (clickErr) {
        console.log(`- HATA: ${item.name} yuklenirken hata olustu:`, clickErr.message);
      }
    }

    console.log("Test simulasyonu basariyla tamamlandi!");
  } catch (e) {
    console.error("Test sirasinda hata olustu:", e);
  } finally {
    await browser.disconnect();
  }
}

run();
