# Hurrycane — teslim formu metinleri

## Oyun adı
Hurrycane

## Kısa açıklama
Cane, yaklaşan kasırgayı evinde atlatmak zorunda. Üç gün boyunca sınırlı hazırlık süresinde malzemeleri topla, pencere ve kapıları güçlendir, elektrik kesintisine hazırlan ve bodrumu su baskınından koru. Onarılmayan hasar bir sonraki güne taşınır; yükselen nabzını ve sağlığını yöneterek hayatta kal.

## Platform
Windows

## Public repo
https://github.com/alika1ender/Hurrycane

## Çalıştırma ve kontroller
CALISTIRMA.txt içeriğini kullanın.

## AI beyanı — hazır kaynaklardan ayrı
Bunlar bu sohbet ve proje kayıtlarıyla doğrulanan kullanımlardır. Ekip üyeleri başka araç/çıktı kullandıysa eklemelidir.

| Kategori | Seçim | Araç, amaç ve kullanım yeri |
|---|---|---|
| Görsel | Kullanıldı | OpenAI Codex/ChatGPT ve Blender MCP yardımıyla prosedürel ev geometrisi, malzeme düzenlemeleri, basit oyun nesneleri, arayüz, telefon uyarısı ve TV yayın görselleri üretildi/düzenlendi. Hazır modeller ayrıca beyan edilmiştir. Görsel üretiminde başka sağlayıcı kullanıldıysa ekip eklemelidir. |
| Ses | Kullanıldı | AI kodlama yardımıyla ses işleme, miksaj ve sentez betikleri; telefon titreşimi ve darbe sentezi, trailer müziği/miksajı hazırlandı. TV mırıltısı Windows Microsoft Zira sesinden işlendi. Freesound kayıtları hazır ses beyanında ayrıca listelenmiştir. Bu kullanım insan kaydı veya bütünüyle özgün ekip kaydı olarak sunulmamaktadır. |
| Kod | Kullanıldı | OpenAI Codex ve Claude: C# oyun sistemleri, etkileşim, UI, görevler, kayıt, afet simülasyonu, yerelleştirme; Blender Python üretim/dışa aktarım, test ve trailer araçları. Ekip yönlendirmesi, düzenlemesi ve testleriyle entegre edildi. |
| Metin | Kullanıldı | OpenAI Codex/ChatGPT ve Claude yardımıyla oyun tasarımı, görevler, Broski mesajları, ara sahne altyazıları, kontrol açıklamaları, çeviri ve belgeler hazırlandı/düzenlendi. |
| Diğer | Kullanıldı | AI yardımıyla tasarım planlama, hata ayıklama, sahne yerleşimi, otomatik test, trailer kurgu/miksaj betikleri ve teslim belgeleri. |

## Hazır kaynak beyanı — AI'dan ayrı

| Kategori | Seçim | Açıklama |
|---|---|---|
| Görsel | Kullanıldı | Aşağıdaki model/doku paketleri; ölçek, yerleşim, materyal, renk, UV ve dosya biçimi düzenlemeleri yapıldı. |
| Ses | Kullanıldı | Docs/Audio/CREDITS.md dosyasındaki tüm Freesound kayıtları; kesme, döngü, filtreleme, perde ve seviye/miks düzenlemeleri yapıldı. Kaynak başlığı, üretici, bağlantı, lisans ve kullanım yeri o tabloda tek tek verilmiştir. Tabloyu forma açıklama olarak aktarın/ekleyin. |
| Kod | Kullanıldı | Unity paketleri (URP, Input System, uGUI vb.) ve geliştirme aracı MCP for Unity. Tam sürümler Packages/manifest.json ve packages-lock.json içindedir. Unity paketleri kendi paket lisanslarına tabidir; MCP for Unity kaynağı https://github.com/CoplayDev/unity-mcp (paket lisansı ayrıca aşağıdaki denetimde kontrol edilmeli). |
| Metin | Kullanıldı | Barlow Condensed yazı tipi; The Barlow Project Authors; https://github.com/jpt/barlow ; SIL OFL 1.1. UI yazıları ve telefon/kitap ekranlarında kullanıldı. Harici alıntı/hazır metin eklenmişse ekip ayrıca bildirmelidir. |
| Diğer | Kullanıldı | Windows Microsoft Zira TTS, TV haber mırıltısı için filtrelenip işlendi. Çıktı dağıtım izni henüz doğrulanmadı; sadece anlaşılmaz olması izin kanıtı değildir. Blender'ın gömülü yazı tipinden mesh yazıları da kaynak/lisans denetiminde doğrulanmalıdır. |

### Model ve doku kaynakları
| Kaynak | Üretici/sağlayıcı | Bağlantı | Lisans | Kullanım/değişiklik |
|---|---|---|---|---|
| Food Kit 2.0 | Kenney | https://kenney.nl/assets/food-kit | CC0 1.0 | Yiyecek props; ölçek/yerleşim/material düzenleme |
| Survival Pack | Quaternius | https://quaternius.com/packs/survival.html | CC0 1.0 | Su, ilk yardım, çanta vb. kaynak nesneleri; ölçek/yerleşim |
| Ultimate House Interior | Quaternius | https://quaternius.com/packs/ultimatehomeinterior.html | CC0 1.0 | Ev mobilyaları/props; yerleşim/material değişiklikleri |
| Furniture Bits | Kay Lousberg / KayKit | https://kaylousberg.itch.io/furniture-bits | CC0 1.0 | Oturma grubu, lambalar, sandalye ve çerçeve; ölçek/renk/yerleşim |
| Modern TV | Animimo Studios | https://animimostudios.itch.io/free-modern-tv-low-poly-4k-textures | CC0 (projede saklanan bildirim) | TV modeli; ölçek/yön ve yayın ekranı ekleme |
| wood_floor | Poly Haven | https://polyhaven.com/a/wood_floor | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| painted_plaster_wall | Poly Haven | https://polyhaven.com/a/painted_plaster_wall | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| concrete_floor | Poly Haven | https://polyhaven.com/a/concrete_floor | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| interior_tiles | Poly Haven | https://polyhaven.com/a/interior_tiles | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| fabric_pattern_05 | Poly Haven | https://polyhaven.com/a/fabric_pattern_05 | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| Plaster001 | ambientCG | https://ambientcg.com/view?id=Plaster001 | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| Carpet004 | ambientCG | https://ambientcg.com/view?id=Carpet004 | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| Marble012 | ambientCG | https://ambientcg.com/view?id=Marble012 | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| oak_veneer_01 | Poly Haven | https://polyhaven.com/a/oak_veneer_01 | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| fabric_pattern_07 | Poly Haven | https://polyhaven.com/a/fabric_pattern_07 | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |
| terry_cloth | Poly Haven | https://polyhaven.com/a/terry_cloth | CC0-1.0 | Ev yüzeyleri; renk varyantı, ölçek/UV ve harita dönüşümleri |

### Dağıtımla birlikte korunacaklar
CREDITS.txt; Docs/Audio/CREDITS.md; Assets/Art/HouseInterior/Licenses/*; BarlowCondensed-OFL.txt. CC BY 4.0 fırtına kayıtlarının üç asıl üreticisi ve yapılan değişiklikler belirtilmelidir.

### Ekip tarafından tamamlanacak
Takım adı/kadrosu, başka AI sağlayıcıları, ekip üyelerinin sonradan eklediği kaynaklar, nihai trailer seçimi, build testi yapılan donanım. Hiçbiri tahminen doldurulmadı.
