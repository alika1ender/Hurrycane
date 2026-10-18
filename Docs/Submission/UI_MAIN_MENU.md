# Low-poly giriş menüsü

- Petrol mavisi zemin, kırık beyaz yazılar ve amber ana eylem. Kesik köşeli ana kart ve butonlar evin low-poly görünümüyle uyumlu.
- Vektör olarak çizilen `FacetedPanel` kenarları çözünürlükten bağımsızdır; yeni raster görsel veya harici asset kullanılmaz.
- Kayıt varsa Devam Et, yoksa Yeni Oyun vurgulanır. Gün/sağlık bilgisi ayrı satırdadır; yeni oyunun kayıtları değiştirdiği açıklanır.
- Nasıl Oynanır, Ayarlar, Çıkış ve üç günün küçük bölüm kartları. Açık arka plan evin görünmesini sağlar.
- Yeni metinler Türkçe/İngilizce destekler. Mevcut pause menüsü ve doğal ekran çözünürlüğü davranışı korunur.

## Kontrol

- İngilizce ve Türkçe Play Mode ekran görüntüleri gözle incelendi; başlık, alt bilgi, bölüm kartları ve butonlar okunaklı.
- Beş ana menü butonu oluşturuluyor; Nasıl Oynanır ve Ayarlar geri çağrıları ilgili panelleri açıyor.
- Yeni görünümde shader, texture veya font lisansı eklenmedi. Mevcut Barlow Condensed font lisansı geçerli.
- Devam Et butonu doğrulandı: kayıt yüklendi ve phase=Opening aşamasına geçildi. Derleme hatası yok; yalnızca önceden bilinen frosted-glass sahne uyarısı var.
