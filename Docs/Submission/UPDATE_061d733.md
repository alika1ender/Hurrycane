# Windows build güncellemesi: 061d733

Önceki teslim ZIP'i 28eca29 sürümündeydi. Son oyun kodu 061d733e68fbfd3a56dad6c326d97f3ef045cebd commit'indedir (Fail idle days and accelerate disaster heartbeat).

Doğrulanan davranış:
- Day 1: hiçbir oyun görevi yapılmadan, intro onayı iş sayılmadan, gün sonunda Failed=True ve başarısız son. Day 2'ye geçilmiyor.
- Day 2: o gün hiçbir yeni iş yapılmadan, başlangıçta zaten sağlanan koşullar iş sayılmadan, gün sonunda Failed=True ve başarısız son. Day 3'e geçilmiyor.
- Kontroller Unity Play Mode'da x60 aşama sayacıyla yapıldı. Yeni Windows build'inin paket/doğrulama bilgisi paket içindeki BUILD_SOURCE.txt ve Submission/DELIVERY_MANIFEST.json içindedir.

Açık editörde Burst geçici çıktı klasörü erişim hatası verdiği için, bu commit'in takip edilen Assets/Packages/ProjectSettings dosyaları temiz bir ASCII yola kopyalanıp Windows x86-64 release build hazırlanmıştır. Oyun kodu değiştirilmemiştir. Ayrı kopyadaki SubmissionReleaseBuild.cs yalnız Editor build aracıdır.

Windows standalone kontrolü: yeni ZIP ayrı klasöre çıkarılarak Day 2 kaydından açıldı. Hiç oyun görevi yapılmadan, normal hızda, gün sonunda başarısız ara sahne görüldü; Day 3 başlamadı. Build: 0 hata, 2 CS0618 uyarısı (AudioDirector.cs eski FindObjectsSortMode API kullanımı).
