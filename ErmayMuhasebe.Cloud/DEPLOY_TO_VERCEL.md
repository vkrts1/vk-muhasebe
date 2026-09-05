# Vercel'e Nasıl Yüklenir?

Bu projeyi dünya genelinde erişilebilir hale getirmek için Vercel kullanabilirsiniz. İşlem çok basittir:

### 1. Hazırlık
Öncelikle Vercel CLI (Komut Satırı Aracı) yüklü olmalıdır. Eğer yüklü değilse:
```bash
npm install -g vercel
```
Veya Vercel'e [web sitesinden](https://vercel.com) Github hesabınızla bağlanabilirsiniz.

### 2. Projeyi Yayınlama (Deploy)

Terminalde şu komutları sırasıyla çalıştırın:

1. **Projeyi Yayınlanabilir Hale Getir (Build):**
   ```bash
   dotnet publish -c Release -o output
   ```

2. **Yayınla:**
   ```bash
   cd output/wwwroot
   vercel --prod
   ```
   *(Vercel size birkaç soru soracaktır, hepsine ENTER diyerek varsayılanları kabul edin)*

### 3. Sonuç
İşlem bittiğinde size `https://ermaymuhasebe-cloud.vercel.app` benzeri bir link verecektir. Bu linki müşterilerinizle paylaşabilirsiniz.

---
**Önemli Not:** 
Firebase ayarlarını (`Ayarlar` sayfasından) yaptıktan sonra, verileriniz tüm cihazlarda (Masaüstü, Web, Cep Telefonu) anlık olarak eşitlenecektir.
