# Esc menüsü ve başlangıç çözünürlüğü

- Esc artık imleç durumundan bağımsız bir pause durumu açar/kapatır. Sayaç, hareket ve etkileşim durur.
- Menü: Devam Et, Ayarlar, Kontroller, Ana Menü, Oyundan Çık ve gün başından yeniden başlatma kartları. İki sütun yerleşimi ekran dışına taşmayı önler.
- Alt menülerde Esc önce ayarlar/kontrol kartını kapatır; sonraki Esc oyuna döner.
- Ana menüye dönüş gün başı kayıtlarını korur; yeni oyun başlatılmaz.
- Varsayılan pencere 1920x1080, yeniden boyutlandırılabilir. Windows başlangıcında NativeDisplay doğal ekran çözünürlüğünü kenarlıksız tam ekran olarak seçer; eski 1024x768 kaydının büyütülmesi önlenir.
- Play Mode doğrulaması: pause açıkken 50 saniyelik sayaç değişmedi, timeScale=0; ayarlardan Esc dönünce pause korundu; devam edince timeScale=1; ana menüye dönüşten sonra phase=Menu ve kayıt korundu.
