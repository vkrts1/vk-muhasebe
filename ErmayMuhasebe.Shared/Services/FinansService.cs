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
        private readonly CloudSyncService _syncService;

        public FinansService(DatabaseService dbService, IUnitOfWork uow, CloudSyncService syncService)
        {
            _dbService = dbService;
            _uow = uow;
            _syncService = syncService;
        }

        public async Task<bool> SaveTransactionAsync(FinancialTransactionRequest request)
        {
            if (request.Cari == null || request.Amount <= 0) return false;

            var db = await _dbService.GetConnectionAsync();
            var refId = Guid.NewGuid().ToString("N");
            var evrakNo = GetEvrakNoPrefix(request.Method) + DateTime.Now.ToString("yyMMddHHmmss");

            CariHareket? mainCH = null;
            CariHareket? supplierCH = null;
            KasaHareket? kasaHareket = null;
            KasaHareket? ciroKasaHareket = null;
            BankaHareket? bankaHareket = null;
            BankaHareket? ciroBankaHareket = null;
            KrediKartiIslem? kk = null;
            EftIslem? eft = null;
            BankaKart? updatedFinancialAccount = null;

            // Transaction başlatıyoruz
            bool success = false;
            await db.RunInTransactionAsync(tran =>
            {
                // 1. Müşteri/Cari Hareketi Ekleme
                bool isOdeme = request.TransactionType == "Ödeme" || request.TransactionType == "Borç Dekontu";
                mainCH = new CariHareket
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
                            kasaHareket = new KasaHareket
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
                            bankaHareket = new BankaHareket
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
                        updatedFinancialAccount = financialAccount;
                    }
                }

                // 3. Detay Tablolarına Kayıt (Kredi Kartı / EFT)
                if (request.Method == "Kredi Kartı")
                {
                    kk = new KrediKartiIslem
                    {
                        MusteriId = request.Cari.Id,
                        MusteriUnvan = request.Cari.Unvan ?? "",
                        Tarih = request.Date,
                        Tutar = request.Amount,
                        Banka = request.BankaAdi ?? "",
                        KartNo = request.KartHesapNo ?? "",
                        OnayKodu = refId, // RefId ile doğrudan ilişkilendiriyoruz
                        Durum = request.DirectedSupplier != null ? "Tedarikçiye Verildi" : "Portföyde",
                        Aciklama = request.Description ?? "",
                        IslemTuru = request.TransactionType,
                        SlipDosyaYolu = request.SlipDekontPath ?? "",
                        YonlendirilenCariId = request.DirectedSupplier?.Id,
                        YonlendirilenCariUnvan = request.DirectedSupplier?.Unvan
                    };
                    tran.Insert(kk);
                }
                else if (request.Method == "Havale / EFT" || request.Method == "Havale/EFT" || request.Method == "EFT")
                {
                    eft = new EftIslem
                    {
                        MusteriId = request.Cari.Id,
                        MusteriUnvan = request.Cari.Unvan ?? "",
                        Tarih = request.Date,
                        Tutar = request.Amount,
                        Banka = request.BankaAdi ?? "",
                        BankaId = request.SelectedKasaOrBanka?.Id ?? 0,
                        HesapNo = request.KartHesapNo ?? "",
                        DekontNo = refId, // RefId ile doğrudan ilişkilendiriyoruz
                        Durum = request.DirectedSupplier != null ? "Tedarikçiye Yönlendirildi" : "Tamamlandı",
                        Aciklama = request.Description ?? "",
                        IslemTuru = request.TransactionType,
                        DekontPath = request.SlipDekontPath ?? "",
                        YonlendirilenCariId = request.DirectedSupplier?.Id,
                        YonlendirilenCariUnvan = request.DirectedSupplier?.Unvan
                    };
                    tran.Insert(eft);
                }

                // 4. Yönlendirilen Tedarikçi (Ciro / Endorsement) Kayıtları
                if (request.DirectedSupplier != null)
                {
                    // Tedarikçi Cari Hareketi (Tedarikçi Borçlandırılır - Ödeme Yapıldı)
                    supplierCH = new CariHareket
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
                                ciroKasaHareket = new KasaHareket
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
                                ciroBankaHareket = new BankaHareket
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
                            updatedFinancialAccount = financialAccount;
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
                    if (updatedCari != null) 
                    {
                        await _dbService.SaveCariAsync(updatedCari);
                        await _syncService.SyncCariAsync(updatedCari);
                    }

                    if (request.DirectedSupplier != null)
                    {
                        var updatedSupplier = await _uow.Cariler.GetByIdAsync(request.DirectedSupplier.Id);
                        if (updatedSupplier != null) 
                        {
                            await _dbService.SaveCariAsync(updatedSupplier);
                            await _syncService.SyncCariAsync(updatedSupplier);
                        }
                    }

                    // --- CANLI BULUT SENKRONİZASYONU (MOBİL VE BULUT İÇİN ANINDA EŞİTLEME) ---
                    if (mainCH != null) await _syncService.SyncCariHareketAsync(mainCH);
                    if (supplierCH != null) await _syncService.SyncCariHareketAsync(supplierCH);
                    if (kasaHareket != null) await _syncService.SyncKasaHareketAsync(kasaHareket);
                    if (ciroKasaHareket != null) await _syncService.SyncKasaHareketAsync(ciroKasaHareket);
                    if (bankaHareket != null) await _syncService.SyncBankaHareketAsync(bankaHareket);
                    if (ciroBankaHareket != null) await _syncService.SyncBankaHareketAsync(ciroBankaHareket);
                    if (updatedFinancialAccount != null) await _syncService.SyncBankaAsync(updatedFinancialAccount);
                    if (kk != null) await _syncService.SyncKrediKartiIslemAsync(kk);
                    if (eft != null) await _syncService.SyncEftIslemAsync(eft);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FinansService] Bulut esitleme hatasi: {ex.Message}");
                }
            }

            return success;
        }

        public async Task<bool> DeleteTransactionAsync(CariHareket hareket)
        {
            if (hareket == null) return false;

            var db = await _dbService.GetConnectionAsync();
            var refId = hareket.RefId;
            var evrakNo = hareket.EvrakNo;
            var hareketId = hareket.Id;

            // Eğer RefId boşsa ve evrakNo da yoksa doğrudan ID üzerinden silme yapıyoruz
            var baseRefId = !string.IsNullOrEmpty(refId)
                ? (refId.EndsWith("-SUP") ? refId.Substring(0, refId.Length - 4) : refId)
                : "";
            var supRefId = !string.IsNullOrEmpty(baseRefId) ? baseRefId + "-SUP" : "";

            List<CariHareket> chToDelete = new();
            List<KasaHareket> khToDelete = new();
            List<BankaHareket> bhToDelete = new();
            List<KrediKartiIslem> kkToDelete = new();
            List<EftIslem> eftToDelete = new();
            List<int> affectedCariIds = new();
            List<int> affectedKasaBankaIds = new();

            bool success = false;
            await db.RunInTransactionAsync(tran =>
            {
                // 1. Cari Hareketleri Bul ve Bakiye Düzeltmesi Yap
                chToDelete = tran.Table<CariHareket>()
                    .Where(h => (!string.IsNullOrEmpty(baseRefId) && (h.RefId == baseRefId || h.RefId == supRefId)) ||
                                (!string.IsNullOrEmpty(evrakNo) && h.EvrakNo == evrakNo) ||
                                (hareketId > 0 && h.Id == hareketId))
                    .ToList();

                if (hareketId > 0 && !chToDelete.Any(x => x.Id == hareketId))
                {
                    var single = tran.Find<CariHareket>(hareketId);
                    if (single != null) chToDelete.Add(single);
                }

                foreach (var h in chToDelete)
                {
                    affectedCariIds.Add(h.CariId);
                    if (h.YonlendirilenCariId.HasValue) affectedCariIds.Add(h.YonlendirilenCariId.Value);

                    var cari = tran.Find<CariKart>(h.CariId);
                    if (cari != null)
                    {
                        cari.Borc -= h.Borc;
                        cari.Alacak -= h.Alacak;
                        tran.Update(cari);
                    }
                    tran.Delete(h);
                }

                // 2. Kasa Hareketlerini Geri Al ve Sil
                khToDelete = tran.Table<KasaHareket>()
                    .Where(k => (!string.IsNullOrEmpty(baseRefId) && (k.RefId == baseRefId || k.RefId == supRefId)) ||
                                (!string.IsNullOrEmpty(evrakNo) && k.EvrakNo == evrakNo))
                    .ToList();

                foreach (var kh in khToDelete)
                {
                    affectedKasaBankaIds.Add(kh.KasaId);
                    var kasa = tran.Find<BankaKart>(kh.KasaId);
                    if (kasa != null)
                    {
                        kasa.GuncelBakiye -= (kh.Giren - kh.Cikan);
                        tran.Update(kasa);
                    }
                    tran.Delete(kh);
                }

                // 3. Banka Hareketlerini Geri Al ve Sil
                bhToDelete = tran.Table<BankaHareket>()
                    .Where(b => (!string.IsNullOrEmpty(baseRefId) && (b.RefId == baseRefId || b.RefId == supRefId)) ||
                                (!string.IsNullOrEmpty(evrakNo) && b.EvrakNo == evrakNo))
                    .ToList();

                foreach (var bh in bhToDelete)
                {
                    affectedKasaBankaIds.Add(bh.BankaId);
                    var banka = tran.Find<BankaKart>(bh.BankaId);
                    if (banka != null)
                    {
                        banka.GuncelBakiye -= (bh.Giren - bh.Cikan);
                        tran.Update(banka);
                    }
                    tran.Delete(bh);
                }

                // 4. Detay Tablolarından Kayıtları Sil
                kkToDelete = tran.Table<KrediKartiIslem>()
                    .Where(k => (!string.IsNullOrEmpty(baseRefId) && k.OnayKodu == baseRefId) ||
                                (!string.IsNullOrEmpty(evrakNo) && k.OnayKodu == evrakNo))
                    .ToList();
                foreach (var kkItem in kkToDelete) tran.Delete(kkItem);

                eftToDelete = tran.Table<EftIslem>()
                    .Where(e => (!string.IsNullOrEmpty(baseRefId) && e.DekontNo == baseRefId) ||
                                (!string.IsNullOrEmpty(evrakNo) && e.DekontNo == evrakNo))
                    .ToList();
                foreach (var eftItem in eftToDelete) tran.Delete(eftItem);

                success = true;
            });

            if (success)
            {
                try
                {
                    await _dbService.EnsureInitializedAsync();

                    // Etkilenen carilerin bakiyelerini yeniden hesapla ve buluta senkronize et
                    foreach (var cId in affectedCariIds.Distinct())
                    {
                        await _uow.Cariler.RecalculateBalanceAsync(cId);
                        var updatedCari = await _uow.Cariler.GetByIdAsync(cId);
                        if (updatedCari != null)
                        {
                            await _dbService.SaveCariAsync(updatedCari);
                            await _syncService.SyncCariAsync(updatedCari);
                        }
                    }

                    // Etkilenen kasa ve bankaları buluta senkronize et
                    foreach (var kbId in affectedKasaBankaIds.Distinct())
                    {
                        var kb = await _uow.Bankalar.GetByIdAsync(kbId);
                        if (kb != null)
                        {
                            await _syncService.SyncBankaAsync(kb);
                        }
                    }

                    // --- CANLI BULUT SENKRONİZASYONU: SİLİNEN TÜM HAREKETLERİ FIREBASE'DEN DE SİL ---
                    foreach (var ch in chToDelete)
                    {
                        await _syncService.DeleteCariHareketAsync(ch.Id);
                    }
                    if (hareketId > 0 && !chToDelete.Any(x => x.Id == hareketId))
                    {
                        await _syncService.DeleteCariHareketAsync(hareketId);
                    }

                    foreach (var kh in khToDelete)
                    {
                        await _syncService.DeleteKasaHareketAsync(kh.Id);
                    }

                    foreach (var bh in bhToDelete)
                    {
                        await _syncService.DeleteBankaHareketAsync(bh.Id);
                    }

                    foreach (var kkItem in kkToDelete)
                    {
                        await _syncService.DeleteKrediKartiIslemAsync(kkItem.Id);
                    }

                    foreach (var eftItem in eftToDelete)
                    {
                        await _syncService.DeleteEftIslemAsync(eftItem.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FinansService] Cloud delete sync error: {ex.Message}");
                }
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
