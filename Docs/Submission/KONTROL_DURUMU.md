# Teslim kontrol durumu — 18 Ekim 2026

## Hazır
- Public Git repo kimlik doğrulamasız erişilebilir: https://github.com/alika1ender/Hurrycane
- Git LFS bütünlük kontrolü başarılı.
- Windows 64-bit release build başarılı: Mono, Development kapalı, HouseMap sahnesi; 0 hata, 0 uyarı. Build raporu Submission/build-status-2.json.
- Güncel tek trailer: Trailer/Hurrycane_Trailer.mp4. Bu dosyadan 59 saniyelik 1920x1080 30 FPS H.264/AAC teslim kopyası hazırlandı. Tam video/ses çözme kontrolü başarılı.
- Teslim dosyaları Submission/TESLIM_EDILECEK klasöründe toplanıyor. Eski Submission/Video kopyaları güncel teslim değildir.
- CALISTIRMA.txt ve FORM_METINLERI.md hazır. Lisanslar ve ses kaynak kayıtları oyun klasörüne eklendi.

## Doğrulama
- ZIP CRC kontrolü geçti; ZIP ayrı klasöre çıkarıldı ve gerçek Windows uygulaması olarak açıldı. Ana menü, Yeni Oyun, açılış, telefon uyarısı, kontrol kartı ve Day 1 sayacı görüldü. Fareyle ilerleme doğrulandı. Otomatik klavye girdileri güvenilir doğrulanamadı; manuel kontrol testi ekip tarafından yapılmalı.
- Bu build üzerinde üç günlük tam oynanış testi yapılmadı. Başlangıçta runtime exception/çökme görülmedi. Bilinen uyarı: COL_Bath_FrostedGlass için ayrı görsel mesh bulunamadı.
- Build sırasında Unity'nin yeniden yazdığı proje/render ayarları kaynak sürümüyle birlikte kaydedilecek.

## Ekip doğrulaması gerekenler
- Takım adı ve tüm kayıtlı üyeler; başka AI sağlayıcıları veya sonradan eklenen kaynaklar.
- Microsoft Zira sesinin çıktı dağıtım izni doğrulanmadı. Anlaşılmaz olması lisans kanıtı değildir. Dağıtım izni doğrulanmalı veya izinli bir kayıtla değiştirilmeli.
- Blender gömülü font mesh yazılarının kaynağı/lisansı ve MCP for Unity paket lisansı henüz tam doğrulanmadı.
- Hazır kaynak/AI formundaki metinler taslaktır; ekip tamamını gözden geçirmeli.
- Siteye yükleme ve teslim makbuzu henüz yok.

Son yeni teslim kaydı: 18 Ekim 2026 18.00 (Türkiye/Kuzey Kıbrıs). Takım erişim kodu repoya yazılmamalıdır.

## Teslim dosyaları
- Submission/TESLIM_EDILECEK/Hurrycane_Windows_x64.zip
- Submission/TESLIM_EDILECEK/Hurrycane_Trailer.mp4
Dosya SHA-256 değerleri ve boyutları Submission/DELIVERY_MANIFEST.json içinde tutulur. İlk build denemesi Input System assembly-yol hatasıyla durdu; script domain yenilemesinden sonra ikinci deneme başarılı oldu. Paket yalnız başarılı ikinci build'den hazırlandı. Mevcut oyuncu kayıtları testten önce yedeklendi ve sonrasında geri kopyalandı.
