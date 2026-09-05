using System;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Services
{
    public class FinansService : IFinansService
    {
        private readonly DatabaseService _dbService;
        private readonly IUnitOfWork _uow;

        public FinansService(DatabaseService dbService, IUnitOfWork uow)
        {
            _dbService = dbService;
            _uow = uow;
        }

        public async Task<bool> SaveTransactionAsync(FinancialTransactionRequest request)
        {
            if (request.Cari == null || request.Amount <= 0) return false;

            var db = await _dbService.GetConnectionAsync();
            var refId = Guid.NewGuid().ToString("N");
            var evrakNo = GetEvrakNoPrefix(request.Method) + DateTime.Now.ToString("yyMMddHHmmss");

            // Transaction başlatıyoruz
            bool success = false;
            await db.RunInTransactionAsync(tran =>
            {
                // 1. Müşteri/Cari Hareketi Ekleme
                bool isOdeme = request.TransactionType == "Ödeme" || request.TransactionType == "Borç Dekontu";
                var mainCH = new CariHareket
                {
                    CariId = request.Cari.Id,
                    CariUnvan = request.Cari.Unvan ?? "",
                    Tarih = request.Date,
                    EvrakNo = evrakNo,
                    RefId = refId,
                    IslemTuru = $"{request.TransactionType} ({GetMethodAbbreviation(request.Method)})",
                    Aciklama = $"[{request.Method}] {request.Description}".Trim(),
                    Borc = isOdeme ? request.Amount : 0,
                    Alacak = !isOdeme ? request.Amount : 0,
                    YonlendirilenCariId = request.DirectedSupplier?.Id,
                    YonlendirilenCariUnvan = request.DirectedSupplier?.Unvan
                };
                tran.Insert(mainCH);

                // Müşteri cari bakiyesini güncelle
                var customer = tran.Find<CariKart>(request.Cari.Id);
                if (customer != null)
                {
                    customer.Borc += mainCH.Borc;
                    customer.Alacak += mainCH.Alacak;
                    tran.Update(customer);
                }

                // 2. Kasa veya Banka Hareketi Ekleme (Eğer finans hesabı seçildiyse)
                if (request.SelectedKasaOrBanka != null)
                {
                    var financialAccount = tran.Find<BankaKart>(request.SelectedKasaOrBanka.Id);
                    if (financialAccount != null)
                    {
                        bool isKasa = financialAccount.KartTuru == "Kasa";

                        if (isKasa)
                        {
                            // Kasa Giriş/Çıkış hareketi
                            var kasaHareket = new KasaHareket
                            {
                                KasaId = financialAccount.Id,
                                CariId = request.Cari.Id,
                                CariUnvan = request.Cari.Unvan,
                                Tarih = request.Date,
                                EvrakNo = evrakNo,
                                RefId = refId,
                                IslemTuru = mainCH.IslemTuru,
                                Aciklama = $"{request.Cari.Unvan} - {request.TransactionType} ({request.Description})",
                                Giren = !isOdeme ? request.Amount : 0,
                                Cikan = isOdeme ? request.Amount : 0,
                                YonlendirilenCariId = request.DirectedSupplier?.Id,
                                YonlendirilenCariUnvan = request.DirectedSupplier?.Unvan
                            };
                            tran.Insert(kasaHareket);

                            // Kasa bakiyesini güncelle
                            financialAccount.GuncelBakiye += (kasaHareket.Giren - kasaHareket.Cikan);
                        }
                        else
                        {
                            // Banka Giriş/Çıkış hareketi
                            var bankaHareket = new BankaHareket
                            {
                                BankaId = financialAccount.Id,
                                CariId = request.Cari.Id,
                                CariUnvan = request.Cari.Unvan,
                                Tarih = request.Date,
                                EvrakNo = evrakNo,
                                RefId = refId,
                                IslemTuru = mainCH.IslemTuru,
                                Aciklama = $"{request.Cari.Unvan} - {request.TransactionType} ({request.Description})",
                                Giren = !isOdeme ? request.Amount : 0,
                                Cikan = isOdeme ? request.Amount : 0,
                                Tutar = request.Amount,
                                YonlendirilenCariId = request.DirectedSupplier?.Id,
                                YonlendirilenCariUnvan = request.DirectedSupplier?.Unvan
                            };
                            tran.Insert(bankaHareket);

                            // Banka bakiyesini güncelle
                            financialAccount.GuncelBakiye += (bankaHareket.Giren - bankaHareket.Cikan);
                        }

                        tran.Update(financialAccount);
                    }
                }

                // 3. Detay Tablolarına Kayıt (Kredi Kartı / EFT)
                if (request.Method == "Kredi Kartı")
                {
                    var kkIslem = new KrediKartiIslem
                    {
                        MusteriId = request.Cari.Id,
                        MusteriUnvan = request.Cari.Unvan,
                        Tarih = request.Date,
                        Tutar = request.Amount,
                        Banka = request.BankaAdi ?? "",
                        KartNo = request.KartHesapNo ?? "",
                        OnayKodu = refId, // RefId ile doğrudan ilişkilendiriyoruz
                        Durum = request.DirectedSupplier != null ? "Tedarikçiye Verildi" : "Portföyde",
                        Aciklama = request.Description,
                        IslemTuru = request.TransactionType,
                        SlipDosyaYolu = request.SlipDekontPath,
                        YonlendirilenCariId = request.DirectedSupplier?.Id,
                        YonlendirilenCariUnvan = request.DirectedSupplier?.Unvan
                    };
                    tran.Insert(kkIslem);
                }
                else if (request.Method == "Havale / EFT" || request.Method == "Havale/EFT")
                {
                    var eftIslem = new EftIslem
                    {
                        MusteriId = request.Cari.Id,
                        MusteriUnvan = request.Cari.Unvan,
                        Tarih = request.Date,
                        Tutar = request.Amount,
                        Banka = request.BankaAdi ?? "",
                        HesapNo = request.KartHesapNo ?? "",
                        DekontNo = refId, // RefId ile doğrudan ilişkilendiriyoruz
                        Durum = request.DirectedSupplier != null ? "Tedarikçiye Yönlendirildi" : "Tamamlandı",
                        Aciklama = request.Description,
                        IslemTuru = request.TransactionType,
                        DekontPath = request.SlipDekontPath,
                        YonlendirilenCariId = request.DirectedSupplier?.Id,
                        YonlendirilenCariUnvan = request.DirectedSupplier?.Unvan
                    };
                    tran.Insert(eftIslem);
                }

                // 4. Yönlendirilen Tedarikçi (Ciro / Endorsement) Kayıtları
                if (request.DirectedSupplier != null)
                {
                    // Tedarikçi Cari Hareketi (Tedarikçi Borçlandırılır - Ödeme Yapıldı)
                    var supplierCH = new CariHareket
                    {
                        CariId = request.DirectedSupplier.Id,
                        CariUnvan = request.DirectedSupplier.Unvan ?? "",
                        Tarih = request.YonlendirmeTarihi ?? request.Date,
                        EvrakNo = evrakNo + "-SUP",
                        RefId = refId + "-SUP",
                        IslemTuru = $"Ödeme ({GetMethodAbbreviation(request.Method)} Ciro)",
                        Aciklama = $"[Ciro] {request.Cari.Unvan} üzerinden ciro edilen {request.Method} tahsilatı. ({request.Description})".Trim(),
                        Borc = request.Amount, // Tedarikçiye yapılan ödeme (Borç)
                        Alacak = 0
                    };
                    tran.Insert(supplierCH);

                    // Tedarikçi cari bakiyesini güncelle
                    var supplier = tran.Find<CariKart>(request.DirectedSupplier.Id);
                    if (supplier != null)
                    {
                        supplier.Borc += supplierCH.Borc;
                        supplier.Alacak += supplierCH.Alacak;
                        tran.Update(supplier);
                    }

                    // Kasa/Banka ciro çıkış hareketi (Kullanıcı onaylı)
                    if (request.SelectedKasaOrBanka != null)
                    {
                        var financialAccount = tran.Find<BankaKart>(request.SelectedKasaOrBanka.Id);
                        if (financialAccount != null)
                        {
                            bool isKasa = financialAccount.KartTuru == "Kasa";

                            if (isKasa)
                            {
                                var ciroKasaHareket = new KasaHareket
                                {
                                    KasaId = financialAccount.Id,
                                    CariId = request.DirectedSupplier.Id,
                                    CariUnvan = request.DirectedSupplier.Unvan,
                                    Tarih = request.YonlendirmeTarihi ?? request.Date,
                                    EvrakNo = evrakNo + "-SUP",
                                    RefId = refId + "-SUP",
                                    IslemTuru = supplierCH.IslemTuru,
                                    Aciklama = $"Ciro Çıkışı -> {request.DirectedSupplier.Unvan} ({request.Cari.Unvan} üzerinden)",
                                    Giren = 0,
                                    Cikan = request.Amount
                                };
                                tran.Insert(ciroKasaHareket);

                                // Kasa bakiyesini güncelle (Giriş yapılmıştı, şimdi çıkış yapıyoruz, net etki 0)
                                financialAccount.GuncelBakiye += (ciroKasaHareket.Giren - ciroKasaHareket.Cikan);
                            }
                            else
                            {
                                var ciroBankaHareket = new BankaHareket
                                {
                                    BankaId = financialAccount.Id,
                                    CariId = request.DirectedSupplier.Id,
                                    CariUnvan = request.DirectedSupplier.Unvan,
                                    Tarih = request.YonlendirmeTarihi ?? request.Date,
                                    EvrakNo = evrakNo + "-SUP",
                                    RefId = refId + "-SUP",
                                    IslemTuru = supplierCH.IslemTuru,
                                    Aciklama = $"Ciro Çıkışı -> {request.DirectedSupplier.Unvan} ({request.Cari.Unvan} üzerinden)",
                                    Giren = 0,
                                    Cikan = request.Amount,
                                    Tutar = request.Amount
                                };
                                tran.Insert(ciroBankaHareket);

                                // Banka bakiyesini güncelle
                                financialAccount.GuncelBakiye += (ciroBankaHareket.Giren - ciroBankaHareket.Cikan);
                            }

                            tran.Update(financialAccount);
                        }
                    }
                }

                success = true;
            });

            if (success)
            {
                try
                {
                    await _uow.Cariler.RecalculateBalanceAsync(request.Cari.Id);
                    if (request.DirectedSupplier != null)
                    {
                        await _uow.Cariler.RecalculateBalanceAsync(request.DirectedSupplier.Id);
                    }

                    var updatedCari = await _uow.Cariler.GetByIdAsync(request.Cari.Id);
                    if (updatedCari != null) await _dbService.SaveCariAsync(updatedCari);

                    if (request.DirectedSupplier != null)
                    {
                        var updatedSupplier = await _uow.Cariler.GetByIdAsync(request.DirectedSupplier.Id);
                        if (updatedSupplier != null) await _dbService.SaveCariAsync(updatedSupplier);
                    }
                }
                catch { }
            }

            return success;
        }

        public async Task<bool> DeleteTransactionAsync(CariHareket hareket)
        {
            if (hareket == null) return false;

            var db = await _dbService.GetConnectionAsync();
            var refId = hareket.RefId;

            // Eğer RefId boşsa (eski kayıtlarda), fallback olarak eski loose match mekanizmasını kullanıyoruz
            if (string.IsNullOrEmpty(refId))
            {
                // CariRepository'deki cascade delete mantığına devrediyoruz
                var result = await _uow.Cariler.DeleteHareketAsync(hareket);
                return result > 0;
            }

            // Temiz Guid'i elde ediyoruz (Ciro bacağının sonundaki -SUP ekini kaldırıyoruz)
            var baseRefId = refId.EndsWith("-SUP") ? refId.Substring(0, refId.Length - 4) : refId;
            var supRefId = baseRefId + "-SUP";

            bool success = false;
            await db.RunInTransactionAsync(tran =>
            {
                // 1. Cari Hareketleri Bul ve Bakiye Düzeltmesi Yap
                var harekets = tran.Table<CariHareket>()
                    .Where(h => h.RefId == baseRefId || h.RefId == supRefId)
                    .ToList();

                foreach (var h in harekets)
                {
                    var cari = tran.Find<CariKart>(h.CariId);
                    if (cari != null)
                    {
                        // Bakiyeyi geri alıyoruz
                        cari.Borc -= h.Borc;
                        cari.Alacak -= h.Alacak;
                        tran.Update(cari);
                    }
                    tran.Delete(h);
                }

                // 2. Kasa Hareketlerini Geri Al ve Sil
                var kasaHarekets = tran.Table<KasaHareket>()
                    .Where(k => k.RefId == baseRefId || k.RefId == supRefId)
                    .ToList();

                foreach (var kh in kasaHarekets)
                {
                    var kasa = tran.Find<BankaKart>(kh.KasaId);
                    if (kasa != null)
                    {
                        kasa.GuncelBakiye -= (kh.Giren - kh.Cikan);
                        tran.Update(kasa);
                    }
                    tran.Delete(kh);
                }

                // 3. Banka Hareketlerini Geri Al ve Sil
                var bankaHarekets = tran.Table<BankaHareket>()
                    .Where(b => b.RefId == baseRefId || b.RefId == supRefId)
                    .ToList();

                foreach (var bh in bankaHarekets)
                {
                    var banka = tran.Find<BankaKart>(bh.BankaId);
                    if (banka != null)
                    {
                        banka.GuncelBakiye -= (bh.Giren - bh.Cikan);
                        tran.Update(banka);
                    }
                    tran.Delete(bh);
                }

                // 4. Detay Tablolarından Kayıtları Sil
                var kkIslemler = tran.Table<KrediKartiIslem>()
                    .Where(k => k.OnayKodu == baseRefId)
                    .ToList();
                foreach (var kk in kkIslemler) tran.Delete(kk);

                var eftIslemler = tran.Table<EftIslem>()
                    .Where(e => e.DekontNo == baseRefId)
                    .ToList();
                foreach (var eft in eftIslemler) tran.Delete(eft);

                success = true;
            });

            if (success)
            {
                try
                {
                    await _dbService.EnsureInitializedAsync();
                    await _uow.Cariler.RecalculateBalanceAsync(hareket.CariId);
                    if (hareket.YonlendirilenCariId.HasValue)
                    {
                        await _uow.Cariler.RecalculateBalanceAsync(hareket.YonlendirilenCariId.Value);
                    }

                    var updatedCari = await _uow.Cariler.GetByIdAsync(hareket.CariId);
                    if (updatedCari != null) await _dbService.SaveCariAsync(updatedCari);
                }
                catch { }
            }

            return success;
        }

        private static string GetEvrakNoPrefix(string method) => method switch
        {
            "Kredi Kartı" => "KK-",
            "Havale / EFT" => "EFT-",
            "Havale/EFT" => "EFT-",
            "Çek" => "CK-",
            _ => "TS-"
        };

        private static string GetMethodAbbreviation(string method) => method switch
        {
            "Kredi Kartı" => "KK",
            "Havale / EFT" => "EFT",
            "Havale/EFT" => "EFT",
            "EFT" => "EFT",
            "Çek" => "Çek",
            "Nakit" => "Nakit",
            _ => method
        };
    }
}
