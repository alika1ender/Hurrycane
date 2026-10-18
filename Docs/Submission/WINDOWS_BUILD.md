# Windows teslim build'i

- Platform: Windows x86-64, Mono, release; Unity 6000.5.3f1.
- Sahne: Assets/Scenes/HouseMap.unity.
- Başarılı build: 18 Ekim 2026; 0 hata / 0 uyarı; yaklaşık 418.3 MB.
- Build çıktısı: çalışma alanındaki Submission/Windows/Hurrycane.
- Teslim ZIP: Submission/TESLIM_EDILECEK/Hurrycane_Windows_x64.zip.
- Runtime DLL, *_Data, MonoBleedingEdge ve D3D12 klasörleri korunmuştur. Burst DoNotShip sembolleri ZIP'e eklenmemiştir.
- ZIP ayrı klasöre çıkarılarak açılış kontrolü yapıldı: menü, Yeni Oyun, intro, telefon, kontrol kartı ve Day 1 sayacı çalışıyor.
- Tam oyun turu ve tüm klavye kontrolleri bu standalone sürümde doğrulanmadı.
- Test makinesi: Windows 11, Intel Core Ultra 7 255H, NVIDIA RTX 5060 Laptop GPU. Bu, asgari sistem gereksinimi beyanı değildir.
- Lisans belirsizlikleri KONTROL_DURUMU.md içinde açıkça kayıtlıdır.

Kaynak sürümü paket içindeki BUILD_SOURCE.txt dosyasına yazılır. Bu build mevcut takip edilen oyun kodu ve Unity'nin build sırasında yeniden yazdığı render/proje ayarlarından alınmıştır. Trailer, recovery sahneleri ve yerel önizlemeler kaynak teslim commit'ine eklenmemiştir; derlemenin tek sahnesi HouseMap'tir.

## Son güncelleme
061d733 oyun kodundan temiz kopyada release build alındı; 0 hata ve AudioDirector.cs içindeki eski Unity API çağrıları için 2 CS0618 uyarısı. Yerel ZIP ve önceki doğrudan EXE yolu güncellendi. Önceki build ZIP'i Submission/PreviousReleases altında tutulur. Day 1/2 sıfır iş başarısızlığı testleri UPDATE_061d733.md içinde kayıtlıdır.
