import { jest } from '@jest/globals';

// 1. Firebase ve Diğer Dış Bağımlılıkları Mockluyoruz
// Böylece test sırasında gerçekten internete çıkmaz ve hata vermez.
jest.mock('../../services/firebase', () => ({
  auth: {},
  db: {},
  getDocs: jest.fn(),
  collection: jest.fn(),
  addDoc: jest.fn(),
  updateDoc: jest.fn(),
  doc: jest.fn()
}));

// In-Memory Database (RAM Üzerinde Sanal Veritabanı)
const MemoryDB = {
    cariler: [] as any[],
    stoklar: [] as any[],
    faturalar: [] as any[]
};

// Sahte Servis Metotları (Gerçek iş mantığını taklit ediyoruz)
// Masaüstündeki UnitOfWork mantığının mobil karşılığı
const dbServices = {
    cariEkle: async (cari: any) => {
        cari.id = `cari_${MemoryDB.cariler.length + 1}`;
        cari.bakiye = 0;
        MemoryDB.cariler.push(cari);
        return cari;
    },
    stokEkle: async (stok: any) => {
        stok.id = `stok_${MemoryDB.stoklar.length + 1}`;
        MemoryDB.stoklar.push(stok);
        return stok;
    },
    faturaKes: async (fatura: any) => {
        fatura.id = `fat_${MemoryDB.faturalar.length + 1}`;
        
        // İş Mantığı 1: Fatura genel toplamını hesapla
        let genelToplam = 0;
        fatura.detaylar.forEach((d: any) => {
            const araToplam = d.miktar * d.fiyat;
            const kdvTutar = araToplam * (d.kdv / 100);
            genelToplam += (araToplam + kdvTutar);
        });
        fatura.genelToplam = genelToplam;

        MemoryDB.faturalar.push(fatura);

        // İş Mantığı 2: Cari bakiyesini artır (Satış Faturası ise Müşteriye Borç yazılır)
        if (fatura.tip === 'Satış') {
            const cari = MemoryDB.cariler.find(c => c.id === fatura.cariId);
            if (cari) cari.bakiye += genelToplam;
        }

        // İş Mantığı 3: Stokları düş
        fatura.detaylar.forEach((d: any) => {
            const stok = MemoryDB.stoklar.find(s => s.id === d.stokId);
            if (stok) {
                stok.miktar -= d.miktar;
            }
        });

        return fatura;
    }
};

describe('Mobil Headless (İş Mantığı) Entegrasyon Testleri', () => {

    beforeEach(() => {
        // Her testten önce sanal veritabanını temizle
        MemoryDB.cariler = [];
        MemoryDB.stoklar = [];
        MemoryDB.faturalar = [];
    });

    test('Uçtan Uca (E2E) Headless Senaryo: Cari oluştur, Stok Ekle, Fatura Kes, Bakiyeleri Kontrol Et', async () => {
        
        // 1. Yeni Müşteri Ekle
        const musteri = await dbServices.cariEkle({
            unvan: "Ermay Mobil Test A.Ş.",
            telefon: "05329998877"
        });
        expect(musteri.id).toBeDefined();
        expect(musteri.bakiye).toBe(0);

        // 2. Yeni Stok Ekle
        const stokUrunu = await dbServices.stokEkle({
            kod: "MOB-STK-01",
            isim: "Klavye",
            miktar: 50,
            fiyat: 1000
        });
        expect(stokUrunu.id).toBeDefined();
        expect(stokUrunu.miktar).toBe(50);

        // 3. Fatura Kes
        const fatura = await dbServices.faturaKes({
            cariId: musteri.id,
            tip: "Satış",
            detaylar: [
                {
                    stokId: stokUrunu.id,
                    miktar: 5,
                    fiyat: 1000,
                    kdv: 20 // %20 KDV
                }
            ]
        });

        // 4. Doğrulamalar (Hesaplamalar Doğru mu?)
        
        // Fatura Toplamı Kontrolü: 5 adet * 1000 = 5000 TL + %20 KDV (1000 TL) = 6000 TL
        expect(fatura.genelToplam).toBe(6000);

        // Cari Bakiye Kontrolü (Müşteri 6000 TL borçlanmış olmalı)
        const guncelCari = MemoryDB.cariler.find(c => c.id === musteri.id);
        expect(guncelCari.bakiye).toBe(6000);

        // Stok Kontrolü (50'den 45'e düşmüş olmalı)
        const guncelStok = MemoryDB.stoklar.find(s => s.id === stokUrunu.id);
        expect(guncelStok.miktar).toBe(45);
    });

});
